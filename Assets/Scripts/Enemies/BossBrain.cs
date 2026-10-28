using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Босс у портала выхода. Три умения, как у игрока:
    /// удар по площади, веер снарядов и призыв стражи.
    ///
    /// Каждое умение начинается с замаха: босс замирает и раздувается, давая время
    /// отойти. Без телеграфа бой с боссом превращается в лотерею.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossBrain : MonoBehaviour
    {
        private enum State
        {
            Move,
            Slam,
            Volley,
            Summon
        }

        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [SerializeField] private Transform _target;

        [Header("Движение")]
        [SerializeField, Min(0f)] private float _speed = 2.4f;
        [SerializeField, Min(0f)] private float _aggroRadius = 45f;
        [SerializeField, Min(0f)] private float _turnSpeed = 6f;
        [SerializeField, Min(0f)] private float _keepDistance = 3.5f;

        [Header("Удар по площади")]
        [SerializeField, Min(0f)] private float _slamRange = 5f;
        [SerializeField, Min(1f)] private float _slamRadius = 6f;
        [SerializeField, Min(0f)] private float _slamDamage = 18f;
        [SerializeField, Min(0.1f)] private float _slamCooldown = 6f;
        [SerializeField, Min(0.1f)] private float _slamWindup = 0.8f;

        [Header("Веер снарядов")]
        [SerializeField, Min(1f)] private float _volleyRange = 22f;
        [SerializeField, Min(1)] private int _volleyCount = 5;
        [SerializeField, Min(0f)] private float _volleySpread = 40f;
        [SerializeField, Min(0f)] private float _volleyDamage = 7f;
        [SerializeField, Min(0.1f)] private float _volleyCooldown = 4.5f;
        [SerializeField, Min(0.1f)] private float _volleyWindup = 0.6f;
        [SerializeField, Min(1f)] private float _projectileSpeed = 18f;

        [Header("Призыв стражи")]
        [SerializeField, Min(1)] private int _summonCount = 2;
        [SerializeField, Min(0.1f)] private float _summonCooldown = 16f;
        [SerializeField, Min(0.1f)] private float _summonWindup = 0.6f;
        [SerializeField, Min(0f)] private float _summonRadius = 5f;

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [SerializeField, Min(0f)] private float _terrainHeight = 40f;
        [SerializeField] private LayerMask _groundMask = ~0;

        private readonly Collider[] _overlaps = new Collider[32];
        private Health _health;
        private Vector3 _baseScale;
        private State _state = State.Move;
        private float _stateEndTime;
        private float _nextSlam;
        private float _nextVolley;
        private float _nextSummon;

        public void Configure(Planet planet, Transform target)
        {
            _planet = planet;
            _target = target;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _baseScale = transform.localScale;

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

            if (_state != State.Move)
            {
                UpdateTelegraph(up, toTarget, distance);
                return;
            }

            if (distance > _aggroRadius)
            {
                HoldPosition(up);
                return;
            }

            FaceTowards(toTarget, up);

            if (Time.time >= _nextSlam && distance <= _slamRange)
            {
                Begin(State.Slam, _slamWindup);
                return;
            }

            if (Time.time >= _nextVolley && distance <= _volleyRange)
            {
                Begin(State.Volley, _volleyWindup);
                return;
            }

            if (Time.time >= _nextSummon)
            {
                Begin(State.Summon, _summonWindup);
                return;
            }

            if (distance > _keepDistance)
                MoveTowards(position, up, toTarget);
            else
                HoldPosition(up);
        }

        private void Begin(State state, float windup)
        {
            _state = state;
            _stateEndTime = Time.time + windup;
        }

        private void UpdateTelegraph(Vector3 up, Vector3 toTarget, float distance)
        {
            FaceTowards(toTarget, up);
            HoldPosition(up);

            var windup = Mathf.Max(0.01f, _stateEndTime - Time.time);
            var pulse = 1f + 0.12f * Mathf.Sin(windup * 12f);
            transform.localScale = _baseScale * pulse;

            if (Time.time < _stateEndTime)
                return;

            transform.localScale = _baseScale;
            Execute(distance);
            _state = State.Move;
        }

        private void Execute(float distance)
        {
            switch (_state)
            {
                case State.Slam:
                    Slam();
                    _nextSlam = Time.time + _slamCooldown;
                    break;

                case State.Volley:
                    Volley();
                    _nextVolley = Time.time + _volleyCooldown;
                    break;

                default:
                    Summon();
                    _nextSummon = Time.time + _summonCooldown;
                    break;
            }
        }

        private void Slam()
        {
            var up = _planet.UpAt(transform.position);
            var origin = transform.position + up * 0.9f;
            var count = Physics.OverlapSphereNonAlloc(origin, _slamRadius, _overlaps, ~0,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var candidate = _overlaps[i];

                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                damageable.TakeDamage(_slamDamage);
            }

            Debug.Log($"[BOSS] Удар по площади: {_slamDamage} урона в радиусе {_slamRadius}");
        }

        private void Volley()
        {
            var up = _planet.UpAt(transform.position);
            var origin = transform.position + up * 1.4f;
            var direction = (_target.position + up * 1f - origin).normalized;

            for (var i = 0; i < _volleyCount; i++)
            {
                var t = _volleyCount == 1 ? 0.5f : i / (float)(_volleyCount - 1);
                var angle = Mathf.Lerp(-_volleySpread * 0.5f, _volleySpread * 0.5f, t);
                var spread = Quaternion.AngleAxis(angle, up) * direction;

                ProjectileFactory.Spawn(origin + direction * 0.6f, spread, _projectileSpeed,
                    _volleyDamage, transform, new Color(1f, 0.45f, 0.2f));
            }

            Debug.Log($"[BOSS] Веер: {_volleyCount} снарядов");
        }

        private void Summon()
        {
            var up = _planet.UpAt(transform.position);
            var spawner = GetComponentInParent<SettlementSpawner>();

            for (var i = 0; i < _summonCount; i++)
            {
                var angle = 360f / Mathf.Max(1, _summonCount) * i;
                var offset = Quaternion.AngleAxis(angle, up) * Vector3.forward * _summonRadius;
                var position = transform.position + Vector3.ProjectOnPlane(offset, up);
                var direction = _planet.UpAt(position);
                var ground = PlanetSurface.SampleGroundRadius(_planet, direction, _terrainHeight, _groundMask,
                    transform, out _);

                EnemyFactory.Spawn(_planet, _target, _planet.Center + direction * (ground + 0.02f),
                    new Color(0.7f, 0.35f, 0.25f), 40f, transform.parent);
            }

            Debug.Log($"[BOSS] Призыв стражи: {_summonCount}");
        }

        private void MoveTowards(Vector3 position, Vector3 up, Vector3 toTarget)
        {
            var direction = Vector3.ProjectOnPlane(toTarget, up).normalized;
            var step = direction * (_speed * Time.deltaTime);
            var nextPosition = position + step;
            var nextUp = _planet.UpAt(nextPosition);
            var ground = PlanetSurface.SampleGroundRadius(_planet, nextUp, _terrainHeight, _groundMask,
                transform, out _);

            transform.position = _planet.Center + nextUp * (ground + _surfaceGap);
            FaceTowards(step, nextUp);
            HoldPosition(nextUp);
        }

        private void HoldPosition(Vector3 up)
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
