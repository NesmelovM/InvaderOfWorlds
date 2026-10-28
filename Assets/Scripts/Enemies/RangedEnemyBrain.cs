using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Дальний враг: держит дистанцию, целится и стреляет снарядом.
    ///
    /// Ходит теми же правилами, что и ближний бой: аналитически по сфере, с учётом
    /// грунта, расталкиванием и объездом соседей. Отличие только в дистанции боя
    /// и в том, что удар летит снарядом, а не рукой.
    /// </summary>
    [DisallowMultipleComponent]
    public class RangedEnemyBrain : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [SerializeField] private Transform _target;

        [Header("Движение")]
        [SerializeField, Min(0f)] private float _speed = 2.8f;
        [SerializeField, Min(0f)] private float _aggroRadius = 30f;
        [SerializeField, Min(0f)] private float _turnSpeed = 8f;

        [Header("Дистанция")]
        [Tooltip("Ближе этого — отступает")]
        [SerializeField, Min(0f)] private float _keepMin = 7f;
        [Tooltip("Дальше этого — подходит")]
        [SerializeField, Min(0f)] private float _keepMax = 12f;
        [Tooltip("Максимальная дальность стрельбы")]
        [SerializeField, Min(1f)] private float _shootRange = 16f;

        [Header("Стрельба")]
        [SerializeField, Min(0f)] private float _damage = 7f;
        [SerializeField, Min(0.1f)] private float _cooldown = 2.2f;
        [Tooltip("Сколько враг целится перед выстрелом")]
        [SerializeField, Min(0.05f)] private float _windupTime = 0.6f;
        [SerializeField, Min(1f)] private float _projectileSpeed = 22f;
        [SerializeField, Range(0f, 0.5f)] private float _windupPulse = 0.15f;

        [Header("Толпа")]
        [SerializeField, Min(0.4f)] private float _personalSpace = 1.2f;
        [SerializeField, Min(0f)] private float _avoidRadius = 2.5f;
        [SerializeField, Min(0f)] private float _avoidStrength = 1.4f;

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [SerializeField, Min(0f)] private float _terrainHeight = 40f;
        [SerializeField, Range(0f, 90f)] private float _slopeLimit = 60f;
        [SerializeField] private LayerMask _groundMask = ~0;
        [Tooltip("Что закрывает обзор лучнику: стены, постройки, рельеф")]
        [SerializeField] private LayerMask _obstacleMask = ~0;
        [Tooltip("Высота глаз лучника над землёй")]
        [SerializeField, Min(0.1f)] private float _eyeHeight = 1.2f;

        private const int NeighbourBufferSize = 16;
        private static readonly Collider[] Neighbours = new Collider[NeighbourBufferSize];
        private readonly RaycastHit[] _sightHits = new RaycastHit[12];

        private Health _health;
        private Vector3 _baseScale;
        private float _nextShotTime;
        private bool _aiming;
        private float _aimEndTime;

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

            if (_aiming)
            {
                UpdateAim(up, toTarget, distance);
                return;
            }

            if (distance > _aggroRadius)
            {
                HoldPosition(up);
                return;
            }

            FaceTowards(toTarget, up);

            // Стреляет только когда действительно видит игрока. Если обзор закрыт
            // стеной или холмом — идёт вперёд, пока видимость не откроется.
            if (!HasLineOfSight(up))
            {
                Strafe(Vector3.ProjectOnPlane(toTarget, up).normalized, up);
                return;
            }

            if (distance < _keepMin)
            {
                Strafe(-Vector3.ProjectOnPlane(toTarget, up).normalized, up);
                return;
            }

            if (distance > _keepMax)
            {
                Strafe(Vector3.ProjectOnPlane(toTarget, up).normalized, up);
                return;
            }

            HoldPosition(up);

            if (distance <= _shootRange && Time.time >= _nextShotTime)
            {
                _aiming = true;
                _aimEndTime = Time.time + _windupTime;
            }
        }

        private void UpdateAim(Vector3 up, Vector3 toTarget, float distance)
        {
            // Игрок спрятался за стену — прекращаем целиться и идём искать обзор.
            if (!HasLineOfSight(up))
            {
                _aiming = false;
                transform.localScale = _baseScale;
                return;
            }

            FaceTowards(toTarget, up);
            HoldPosition(up);

            var progress = 1f - Mathf.Clamp01((_aimEndTime - Time.time) / _windupTime);
            transform.localScale = _baseScale * (1f + _windupPulse * Mathf.Sin(progress * Mathf.PI));

            if (Time.time < _aimEndTime)
                return;

            _aiming = false;
            transform.localScale = _baseScale;
            _nextShotTime = Time.time + _cooldown;

            if (distance > _shootRange * 1.2f)
                return;

            Fire();
        }

        /// <summary>
        /// Прямая видимость игрока: луч от глаз лучника к груди цели.
        /// Персонажи обзор не перекрывают — важно только то, что стоит между:
        /// стены, постройки, рельеф.
        /// </summary>
        private bool HasLineOfSight(Vector3 up)
        {
            var eye = transform.position + up * _eyeHeight;
            var chest = _target.position + up * 1f;
            var toTarget = chest - eye;
            var distance = toTarget.magnitude;

            if (distance < 0.01f)
                return true;

            var direction = toTarget / distance;
            var count = Physics.RaycastNonAlloc(eye, direction, _sightHits, distance, _obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var hit = _sightHits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.collider.GetComponentInParent<Health>() != null)
                    continue;

                return false;
            }

            return true;
        }

        private void Fire()
        {
            var up = _planet.UpAt(transform.position);
            var muzzle = transform.position + up * 1.2f + Vector3.ProjectOnPlane(transform.forward, up).normalized * 0.5f;
            var chest = _target.position + up * 1f;
            var direction = (chest - muzzle).normalized;

            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Projectile";
            projectileObject.transform.position = muzzle;
            projectileObject.transform.localScale = Vector3.one * 0.3f;

            var collider = projectileObject.GetComponent<SphereCollider>();
            if (collider != null)
                collider.isTrigger = true;

            var renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = EnemyFactory.GetMaterial(new Color(1f, 0.75f, 0.2f));

            var projectile = projectileObject.AddComponent<Projectile>();
            projectile.Launch(direction, _projectileSpeed, _damage, transform, 0, _planet);
        }

        private void Strafe(Vector3 direction, Vector3 up)
        {
            var avoidance = ComputeAvoidance(up);
            var moveDirection = (direction + avoidance * _avoidStrength).normalized;
            var step = moveDirection * (_speed * Time.deltaTime);

            var nextPosition = transform.position + step;
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

        private void HoldPosition(Vector3 up)
        {
            SnapToGround(transform.position, up);
            CharacterSeparation.Resolve(transform, up, 0.9f, _personalSpace * 0.5f, 0.9f);
            SnapToGround(transform.position, up);
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
