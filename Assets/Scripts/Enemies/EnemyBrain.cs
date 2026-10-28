using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Поведение врага: занять своё место вокруг игрока и ударить.
    ///
    /// Ключевая деталь — «слот»: у каждого врага свой угол подхода, поэтому они
    /// не сбиваются в клубок у одной точки. Плюс объезд соседей и расталкивание,
    /// чтобы даже в толпе не слипались.
    ///
    /// Удар читаемый: враг на 0.45 с замирает и раздувается, и только потом бьёт.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBrain : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [SerializeField] private Transform _target;

        [Header("Движение")]
        [SerializeField, Min(0f)] private float _speed = 3.2f;
        [SerializeField, Min(0f)] private float _aggroRadius = 25f;
        [SerializeField, Min(0f)] private float _turnSpeed = 10f;

        [Header("Атака")]
        [SerializeField, Min(0f)] private float _attackRange = 1.9f;
        [SerializeField, Min(0f)] private float _damage = 6f;
        [SerializeField, Min(0.1f)] private float _attackCooldown = 1.4f;
        [Tooltip("Сколько враг стоит и раздувается перед ударом")]
        [SerializeField, Min(0.05f)] private float _windupTime = 0.45f;
        [Tooltip("Насколько раздувается модель во время замаха")]
        [SerializeField, Range(0f, 0.5f)] private float _windupPulse = 0.18f;

        [Header("Толпа")]
        [Tooltip("Дистанция между центрами врагов, ближе которой они не сходятся")]
        [SerializeField, Min(0.4f)] private float _personalSpace = 1.2f;
        [Tooltip("Радиус, в котором враг объезжает соседей")]
        [SerializeField, Min(0f)] private float _avoidRadius = 2.5f;
        [Tooltip("Насколько сильно объезд влияет на движение")]
        [SerializeField, Min(0f)] private float _avoidStrength = 1.4f;
        [Tooltip("Разброс углов подхода вокруг игрока, градусы")]
        [SerializeField, Range(0f, 180f)] private float _slotSpread = 75f;

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [SerializeField, Min(0f)] private float _terrainHeight = 40f;
        [SerializeField, Range(0f, 90f)] private float _slopeLimit = 60f;
        [SerializeField] private LayerMask _groundMask = ~0;

        private const int NeighbourBufferSize = 16;
        private static readonly Collider[] Neighbours = new Collider[NeighbourBufferSize];

        private Health _health;
        private float _nextAttackTime;
        private Vector3 _baseScale;
        private bool _windingUp;
        private float _windupEndTime;
        private float _slotAngle;

        public void Configure(Planet planet, Transform target)
        {
            _planet = planet;
            _target = target;
        }

        /// <summary>
        /// Настройка боевых параметров из кода — используется элитой.
        ///
        /// Масштаб здесь не трогаем: он задаётся при создании тела. Раньше метод
        /// умножал масштаб на «базовый», а тот заполняется в Awake — которого
        /// в редакторе при AddComponent не бывает. В итоге модель получала
        /// нулевой размер и была невидима.
        /// </summary>
        public void ConfigureCombat(float damage, float attackCooldown, float attackRange, float speed)
        {
            _damage = damage;
            _attackCooldown = attackCooldown;
            _attackRange = attackRange;
            _speed = speed;

            _baseScale = transform.localScale;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _baseScale = transform.localScale;

            // Свой угол подхода: враги окружают игрока, а не встают в одну точку.
            _slotAngle = Random.Range(-_slotSpread, _slotSpread);

            if (_planet == null)
                _planet = FindFirstObjectByType<Planet>();

            if (_target == null)
            {
                var player = GameObject.FindWithTag("Player");

                if (player != null)
                    _target = player.transform;
            }
        }

        private void Update()
        {
            if (_planet == null || _target == null)
                return;

            if (_health != null && !_health.IsAlive)
                return;

            var position = transform.position;
            var up = _planet.UpAt(position);
            var toTarget = _target.position - position;
            var distance = toTarget.magnitude;

            if (_windingUp)
            {
                UpdateWindup(up, toTarget, distance);
                return;
            }

            if (distance <= _attackRange)
            {
                HoldPosition(up);
                FaceTowards(toTarget, up);
                TryBeginWindup();
                return;
            }

            if (distance > _aggroRadius)
            {
                HoldPosition(up);
                return;
            }

            MoveTowardsSlot(position, up, toTarget);
        }

        /// <summary>Идёт не в центр игрока, а в свою точку на окружности вокруг него.</summary>
        private void MoveTowardsSlot(Vector3 position, Vector3 up, Vector3 toTarget)
        {
            var directionToPlayer = Vector3.ProjectOnPlane(toTarget, up).normalized;

            if (directionToPlayer.sqrMagnitude < 0.0001f)
                return;

            var slotDirection = Quaternion.AngleAxis(_slotAngle, up) * directionToPlayer;
            var slotPoint = _target.position - slotDirection * (_attackRange * 0.85f);

            var toSlot = Vector3.ProjectOnPlane(slotPoint - position, up);

            if (toSlot.sqrMagnitude < 0.0001f)
                return;

            var moveDirection = (toSlot.normalized + ComputeAvoidance(up) * _avoidStrength).normalized;
            var step = moveDirection * (_speed * Time.deltaTime);

            // Стены и постройки обходим так же, как игрок: физика движение не разрешает.
            step = CharacterObstacles.Slide(transform, up, 0.9f, 0.35f, step, ~0);

            var nextPosition = position + step;
            var nextUp = _planet.UpAt(nextPosition);
            var ground = PlanetSurface.SampleGroundRadius(_planet, nextUp, _terrainHeight, _groundMask,
                transform, out var normal);

            if (Vector3.Angle(normal, nextUp) > _slopeLimit)
            {
                HoldPosition(up);
                return;
            }

            transform.position = _planet.Center + nextUp * (ground + _surfaceGap);
            FaceTowards(step, nextUp);

            HoldPosition(nextUp);
        }

        /// <summary>Боковой объезд соседей: суммарное отталкивание от тех, кто рядом.</summary>
        private Vector3 ComputeAvoidance(Vector3 up)
        {
            var center = transform.position + up * 0.9f;
            var count = Physics.OverlapSphereNonAlloc(center, _avoidRadius, Neighbours, ~0,
                QueryTriggerInteraction.Ignore);

            var avoidance = Vector3.zero;

            for (var i = 0; i < count; i++)
            {
                var neighbour = Neighbours[i];

                if (neighbour == null || neighbour.transform.IsChildOf(transform))
                    continue;

                if (neighbour.GetComponentInParent<Health>() == null)
                    continue;

                var away = Vector3.ProjectOnPlane(center - neighbour.bounds.center, up);
                var distance = away.magnitude;

                if (distance < 0.0001f || distance > _avoidRadius)
                    continue;

                avoidance += away.normalized * (1f - distance / _avoidRadius);
            }

            return Vector3.ClampMagnitude(avoidance, 1f);
        }

        /// <summary>Стоять на месте, но не позволять соседям в себя влезть.</summary>
        private void HoldPosition(Vector3 up)
        {
            SnapToGround(transform.position, up);
            CharacterSeparation.Resolve(transform, up, 0.9f, _personalSpace * 0.5f, 0.9f);
            SnapToGround(transform.position, up);
        }

        private void TryBeginWindup()
        {
            if (Time.time < _nextAttackTime)
                return;

            _windingUp = true;
            _windupEndTime = Time.time + _windupTime;
        }

        private void UpdateWindup(Vector3 up, Vector3 toTarget, float distance)
        {
            FaceTowards(toTarget, up);
            HoldPosition(up);

            var progress = 1f - Mathf.Clamp01((_windupEndTime - Time.time) / _windupTime);
            var pulse = 1f + _windupPulse * Mathf.Sin(progress * Mathf.PI);
            transform.localScale = _baseScale * pulse;

            if (Time.time < _windupEndTime)
                return;

            Strike(up, distance);
        }

        private void Strike(Vector3 up, float distance)
        {
            _windingUp = false;
            transform.localScale = _baseScale;
            _nextAttackTime = Time.time + _attackCooldown;

            if (distance > _attackRange * 1.35f)
                return;

            var damageable = _target.GetComponentInParent<IDamageable>();

            if (damageable == null || !damageable.IsAlive)
                return;

            damageable.TakeDamage(_damage);
            Debug.Log($"[ENEMY] {name} ударил на {_damage:F0}");
        }

        private void SnapToGround(Vector3 position, Vector3 up)
        {
            var ground = PlanetSurface.SampleGroundRadius(_planet, up, _terrainHeight, _groundMask,
                transform, out _);

            transform.position = _planet.Center + up * (ground + _surfaceGap);
        }

        private void FaceTowards(Vector3 direction, Vector3 up)
        {
            var flat = Vector3.ProjectOnPlane(direction, up);

            if (flat.sqrMagnitude < 0.0001f)
                return;

            var aligned = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
            var target = Quaternion.LookRotation(flat.normalized, up);
            transform.rotation = Quaternion.Slerp(aligned, target, _turnSpeed * Time.deltaTime);
        }
    }
}
