using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Снаряд игрока и врагов.
    ///
    /// Летит по прямой и каждый кадр проверяет путь сферой, поэтому не проскакивает
    /// сквозь цели на большой скорости. Своих не задевает.
    /// Умеет пробивать несколько целей: тогда после попадания не исчезает,
    /// а летит дальше, и одну и ту же цель второй раз не бьёт.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 20f;
        [SerializeField, Min(0f)] private float _damage = 7f;
        [SerializeField, Min(0.1f)] private float _lifetime = 4f;
        [SerializeField, Min(0.02f)] private float _radius = 0.18f;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly HashSet<int> _alreadyHit = new HashSet<int>();

        private Vector3 _direction;
        private Transform _owner;
        private float _life;
        private int _pierceLeft;

        public void Launch(Vector3 direction, float speed, float damage, Transform owner, int pierce = 0)
        {
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _owner = owner;
            _life = _lifetime;
            _pierceLeft = Mathf.Max(0, pierce);
        }

        private void Update()
        {
            _life -= Time.deltaTime;

            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var step = _direction * (_speed * Time.deltaTime);
            var distance = step.magnitude;

            if (distance <= 0f)
                return;

            var count = Physics.SphereCastNonAlloc(transform.position, _radius, _direction, _hits, distance, ~0,
                QueryTriggerInteraction.Ignore);

            var blocked = false;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];

                if (hit.collider == null)
                    continue;

                if (_owner != null && hit.collider.transform.IsChildOf(_owner))
                    continue;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                {
                    // Стена или земля: снаряд останавливается.
                    blocked = true;
                    continue;
                }

                var id = damageable.GetHashCode();

                if (!_alreadyHit.Add(id))
                    continue;

                damageable.TakeDamage(_damage);

                if (_pierceLeft > 0)
                {
                    _pierceLeft--;
                    continue;
                }

                Destroy(gameObject);
                return;
            }

            if (blocked)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += step;
        }
    }
}
