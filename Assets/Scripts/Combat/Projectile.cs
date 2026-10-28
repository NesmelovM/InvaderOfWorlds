using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Снаряд дальнего врага (и в будущем — игрока).
    ///
    /// Летит по прямой и каждый кадр проверяет путь сферой, поэтому не проскакивает
    /// сквозь цели на большой скорости. Своих не задевает.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 20f;
        [SerializeField, Min(0f)] private float _damage = 7f;
        [SerializeField, Min(0.1f)] private float _lifetime = 4f;
        [SerializeField, Min(0.02f)] private float _radius = 0.18f;

        private readonly RaycastHit[] _hits = new RaycastHit[12];

        private Vector3 _direction;
        private Transform _owner;
        private float _life;

        public void Launch(Vector3 direction, float speed, float damage, Transform owner)
        {
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _owner = owner;
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

            var step = _direction * (_speed * Time.deltaTime);
            var distance = step.magnitude;

            if (distance <= 0f)
                return;

            var count = Physics.SphereCastNonAlloc(transform.position, _radius, _direction, _hits, distance, ~0,
                QueryTriggerInteraction.Ignore);

            var nearest = float.MaxValue;
            IDamageable target = null;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];

                if (hit.collider == null)
                    continue;

                if (_owner != null && hit.collider.transform.IsChildOf(_owner))
                    continue;

                // Земля и стены останавливают снаряд, но урона не получают.
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;

                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    target = damageable != null && damageable.IsAlive ? damageable : null;
                }
            }

            if (nearest < float.MaxValue)
            {
                target?.TakeDamage(_damage);
                Destroy(gameObject);
                return;
            }

            transform.position += step;
        }
    }
}
