using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ближний бой игрока: короткий сферический замах перед персонажем.
    /// Пока это единственная атака, но интерфейс тот же, что понадобится
    /// заклинаниям и стрельбе — «нанести урон в конусе/сфере».
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;

        [Header("Удар")]
        [SerializeField, Min(0f)] private float _damage = 34f;
        [SerializeField, Min(0.1f)] private float _range = 2.8f;
        [SerializeField, Min(0.05f)] private float _radius = 0.55f;
        [SerializeField, Min(0.05f)] private float _cooldown = 0.45f;
        [SerializeField, Min(0f)] private float _bodyCenter = 1f;
        [SerializeField] private LayerMask _targetMask = ~0;

        [Header("Отладка")]
        [SerializeField] private bool _logHits = true;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private float _nextAttackTime;

        public void Configure(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>Улучшение из хаба: множитель урона применяется один раз на старте забега.</summary>
        public void ApplyDamageMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
                return;

            _damage *= multiplier;
        }

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!Input.GetButtonDown("Fire1") || Time.time < _nextAttackTime)
                return;

            _nextAttackTime = Time.time + _cooldown;
            Attack();
#endif
        }

        private void Attack()
        {
            var up = transform.up;
            var forward = _cameraTransform != null
                ? Vector3.ProjectOnPlane(_cameraTransform.forward, up)
                : Vector3.ProjectOnPlane(transform.forward, up);

            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(transform.forward, up);

            if (forward.sqrMagnitude < 0.0001f)
                return;

            forward.Normalize();

            var origin = transform.position + up * _bodyCenter;
            var count = Physics.SphereCastNonAlloc(origin, _radius, forward, _hits, _range, _targetMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                damageable.TakeDamage(_damage);

                if (_logHits)
                    Debug.Log($"[ATTACK] {hit.collider.name} получил {_damage:F0}");

                break;
            }
        }
    }
}
