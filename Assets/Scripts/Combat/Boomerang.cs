using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Бумеранг: летит вперёд на заданную дистанцию и возвращается к хозяину.
    /// Движется по дуге вокруг планеты, чтобы не уходить в грунт и не отрываться
    /// от поверхности. Одну цель бьёт не чаще раза в полсекунды.
    /// </summary>
    public class Boomerang : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 18f;
        [SerializeField, Min(0f)] private float _damage = 22f;
        [SerializeField, Min(1f)] private float _range = 12f;
        [SerializeField, Min(0.05f)] private float _radius = 0.45f;
        [SerializeField, Min(0.1f)] private float _hitCooldown = 0.5f;
        [SerializeField, Min(0.1f)] private float _lifetime = 6f;

        private readonly Collider[] _overlaps = new Collider[16];
        private readonly Dictionary<int, float> _nextHit = new Dictionary<int, float>();

        private Transform _owner;
        private Planet _planet;
        private Vector3 _direction;
        private float _travelled;
        private float _life;
        private bool _returning;

        public void Launch(Transform owner, Vector3 direction, float speed, float damage, float range,
            Planet planet = null)
        {
            _owner = owner;
            _planet = planet;
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _range = range;
            _life = _lifetime;

            var up = owner != null ? owner.up : Vector3.up;
            _direction = Vector3.ProjectOnPlane(_direction, up).normalized;
        }

        private void Update()
        {
            _life -= Time.deltaTime;

            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var step = _speed * Time.deltaTime;
            var position = transform.position;

            if (!_returning)
            {
                PlanetArc.Advance(_planet, ref position, ref _direction, step);
                _travelled += step;

                if (_travelled >= _range)
                    _returning = true;
            }
            else
            {
                if (_owner == null)
                {
                    Destroy(gameObject);
                    return;
                }

                var up = _planet != null ? _planet.UpAt(position) : _owner.up;
                var target = _owner.position + up * 1f;

                if (Vector3.Distance(position, target) < 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                _direction = (target - position).normalized;
                PlanetArc.Advance(_planet, ref position, ref _direction, step);
            }

            transform.position = position;
            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
            DamageAround();
        }

        private void DamageAround()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _overlaps, ~0,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var candidate = _overlaps[i];

                if (candidate == null || (_owner != null && candidate.transform.IsChildOf(_owner)))
                    continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                var id = damageable.GetHashCode();

                if (_nextHit.TryGetValue(id, out var nextTime) && Time.time < nextTime)
                    continue;

                _nextHit[id] = Time.time + _hitCooldown;
                damageable.TakeDamage(_damage);
            }
        }
    }
}
