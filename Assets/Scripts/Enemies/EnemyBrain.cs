using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Простое поведение врага: догнать игрока по поверхности сферы и ударить.
    ///
    /// Удар сделан читаемым: враг останавливается, на 0.45 с раздувается (замах),
    /// и только потом наносит урон. Игрок успевает увидеть, что его сейчас ударят,
    /// и отойти — без этого бой превращается в «умер непонятно от чего».
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

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [SerializeField, Min(0f)] private float _terrainHeight = 40f;
        [SerializeField, Range(0f, 90f)] private float _slopeLimit = 60f;
        [SerializeField] private LayerMask _groundMask = ~0;

        private Health _health;
        private float _nextAttackTime;
        private Vector3 _baseScale;
        private bool _windingUp;
        private float _windupEndTime;

        public void Configure(Planet planet, Transform target)
        {
            _planet = planet;
            _target = target;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _baseScale = transform.localScale;

            // Страховка: если ссылки не проставлены в сцене, находим их сами.
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
                SnapToGround(position, up);
                CharacterSeparation.Resolve(transform, up, 0.9f, 0.35f, 0.9f);
                SnapToGround(transform.position, up);
                FaceTowards(toTarget, up);
                TryBeginWindup();
                return;
            }

            if (distance > _aggroRadius)
            {
                SnapToGround(position, up);
                CharacterSeparation.Resolve(transform, up, 0.9f, 0.35f, 0.9f);
                SnapToGround(transform.position, up);
                return;
            }

            var direction = Vector3.ProjectOnPlane(toTarget, up).normalized;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            var step = direction * (_speed * Time.deltaTime);
            var nextPosition = position + step;
            var nextUp = _planet.UpAt(nextPosition);
            var ground = PlanetSurface.SampleGroundRadius(_planet, nextUp, _terrainHeight, _groundMask,
                transform, out var normal);

            // Слишком крутой склон — не лезем, стоим у подножия.
            if (Vector3.Angle(normal, nextUp) > _slopeLimit)
            {
                SnapToGround(position, up);
                return;
            }

            transform.position = _planet.Center + nextUp * (ground + _surfaceGap);
            FaceTowards(step, nextUp);

            // Упираемся в соседа или в игрока, а не проходим сквозь них.
            CharacterSeparation.Resolve(transform, nextUp, 0.9f, 0.35f, 0.9f);
            SnapToGround(transform.position, nextUp);
        }

        private void TryBeginWindup()
        {
            if (Time.time < _nextAttackTime)
                return;

            _windingUp = true;
            _windupEndTime = Time.time + _windupTime;
        }

        /// <summary>Замах: стоим на месте и раздуваемся — это и есть телеграф удара.</summary>
        private void UpdateWindup(Vector3 up, Vector3 toTarget, float distance)
        {
            FaceTowards(toTarget, up);

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

            // Пока враг замахивался, игрок мог отойти — тогда удар уходит в воздух.
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
