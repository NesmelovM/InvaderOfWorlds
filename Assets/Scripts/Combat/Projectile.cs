using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Снаряд игрока и врагов.
    ///
    /// Летит не по прямой, а по дуге вокруг планеты: прямая на сфере уходит в грунт
    /// или отрывается от поверхности. Каждый кадр путь проверяется сферой,
    /// поэтому цель не проскакивается на большой скорости. Своих не задевает.
    /// Умеет пробивать несколько целей.
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
        private Planet _planet;
        private float _life;
        private int _pierceLeft;

        public void Launch(Vector3 direction, float speed, float damage, Transform owner, int pierce = 0,
            Planet planet = null)
        {
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _owner = owner;
            _planet = planet;
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

            var step = _speed * Time.deltaTime;
            var nextPosition = transform.position;
            var nextDirection = _direction;

            PlanetArc.Advance(_planet, ref nextPosition, ref nextDirection, step);

            var toNext = nextPosition - transform.position;
            var distance = toNext.magnitude;

            if (distance > 0.0001f &&
                !TryResolveHit(toNext / distance, distance))
            {
                _direction = nextDirection;
                transform.position = nextPosition;
            }

            if (_life <= 0f)
                Destroy(gameObject);
        }

        /// <summary>Проверяет путь и наносит урон. Возвращает true, если снаряд уничтожен.</summary>
        private bool TryResolveHit(Vector3 direction, float distance)
        {
            var count = Physics.SphereCastNonAlloc(transform.position, _radius, direction, _hits, distance, ~0,
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
                return true;
            }

            if (blocked)
            {
                Destroy(gameObject);
                return true;
            }

            return false;
        }
    }
}
