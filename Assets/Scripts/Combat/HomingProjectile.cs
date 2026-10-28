using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Самонаводящаяся ракета: сама поворачивает к ближайшему врагу,
    /// поэтому попадает даже по бегущей цели. Поворот ограничен — увернуться можно.
    /// </summary>
    public class HomingProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 14f;
        [SerializeField, Min(0f)] private float _damage = 26f;
        [SerializeField, Min(0.1f)] private float _lifetime = 4f;
        [SerializeField, Min(0.02f)] private float _radius = 0.3f;
        [SerializeField, Min(0f)] private float _turnRate = 260f;
        [SerializeField, Min(1f)] private float _seekRadius = 22f;

        private readonly Collider[] _overlaps = new Collider[16];

        private Transform _owner;
        private Vector3 _direction;
        private float _life;

        public void Launch(Transform owner, Vector3 direction, float speed, float damage)
        {
            _owner = owner;
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _life = _lifetime;
        }

        private void Update()
        {
            _life -= Time.deltaTime;

            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var target = FindTarget();

            if (target != null)
            {
                var desired = (target.position + Vector3.up * 0.5f - transform.position).normalized;
                _direction = Vector3.RotateTowards(_direction, desired, _turnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }

            var step = _direction * (_speed * Time.deltaTime);
            var distance = step.magnitude;

            if (distance > 0f && Physics.SphereCast(transform.position, _radius, _direction, out var hit, distance,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                if (_owner != null && hit.collider.transform.IsChildOf(_owner))
                {
                    transform.position += step;
                    return;
                }

                var damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null && damageable.IsAlive)
                    damageable.TakeDamage(_damage);

                Destroy(gameObject);
                return;
            }

            transform.position += step;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private Transform FindTarget()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, _seekRadius, _overlaps, ~0,
                QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            var best = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var candidate = _overlaps[i];

                if (candidate == null || (_owner != null && candidate.transform.IsChildOf(_owner)))
                    continue;

                if (candidate.GetComponentInParent<EnemyMarker>() == null)
                    continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                var distance = Vector3.Distance(transform.position, candidate.bounds.center);

                if (distance >= best)
                    continue;

                best = distance;
                nearest = candidate.transform;
            }

            return nearest;
        }
    }
}
