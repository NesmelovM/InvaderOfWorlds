using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Простое поведение врага: догнать игрока по поверхности сферы и ударить.
    /// Ходит тем же способом, что и игрок — аналитически по сфере, с учётом грунта,
    /// поэтому не проваливается и не застревает на склонах.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBrain : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [SerializeField] private Transform _target;

        [Header("Поведение")]
        [SerializeField, Min(0f)] private float _speed = 3.2f;
        [SerializeField, Min(0f)] private float _aggroRadius = 25f;
        [SerializeField, Min(0f)] private float _attackRange = 1.9f;
        [SerializeField, Min(0f)] private float _damage = 8f;
        [SerializeField, Min(0.1f)] private float _attackCooldown = 1.1f;
        [SerializeField, Min(0f)] private float _turnSpeed = 10f;

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [SerializeField, Min(0f)] private float _terrainHeight = 40f;
        [SerializeField, Range(0f, 90f)] private float _slopeLimit = 60f;
        [SerializeField] private LayerMask _groundMask = ~0;

        private Health _health;
        private float _nextAttackTime;

        public void Configure(Planet planet, Transform target)
        {
            _planet = planet;
            _target = target;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
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

            if (distance <= _attackRange)
            {
                SnapToGround(position, up);
                FaceTowards(toTarget, up);
                TryAttack();
                return;
            }

            if (distance > _aggroRadius)
            {
                SnapToGround(position, up);
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

        private void TryAttack()
        {
            if (Time.time < _nextAttackTime)
                return;

            _nextAttackTime = Time.time + _attackCooldown;

            var damageable = _target.GetComponentInParent<IDamageable>();

            if (damageable == null || !damageable.IsAlive)
                return;

            damageable.TakeDamage(_damage);
            Debug.Log($"[ENEMY] {name} ударил на {_damage:F0}");
        }
    }
}
