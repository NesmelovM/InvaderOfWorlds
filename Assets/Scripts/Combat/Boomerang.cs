using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Бумеранг: летит вперёд на заданную дистанцию и возвращается к хозяину.
    /// Задевает цели дважды — туда и обратно, но одну и ту же цель не чаще,
    /// чем раз в полсекунды, иначе на толпе он был бы бесконечным уроном.
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
        private Vector3 _origin;
        private Vector3 _direction;
        private float _travelled;
        private float _life;
        private bool _returning;

        public void Launch(Transform owner, Vector3 direction, float speed, float damage, float range)
        {
            _owner = owner;
            _origin = transform.position;
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _range = range;

            var up = owner != null ? owner.up : Vector3.up;
            _direction = Vector3.ProjectOnPlane(_direction, up).normalized;
            _life = _lifetime;
        }

        private void Update()
        {
            _life -= Time.deltaTime;

            var up = _owner != null ? _owner.up : Vector3.up;

            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (!_returning)
            {
                transform.position += _direction * (_speed * Time.deltaTime);
                _travelled += _speed * Time.deltaTime;

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

                var target = _owner.position + up * 1f;
                var toOwner = target - transform.position;

                if (toOwner.magnitude < 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                transform.position += toOwner.normalized * (_speed * Time.deltaTime);
            }

            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
            DamageAround(up);
        }

        private void DamageAround(Vector3 up)
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
