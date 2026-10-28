using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    public enum WeaponKind
    {
        /// <summary>Ближний бой: короткий замах перед героем.</summary>
        Melee,

        /// <summary>Снаряд в ближайшего врага.</summary>
        Bolt,

        /// <summary>Пробивающий снаряд: задевает несколько целей подряд.</summary>
        Pierce,

        /// <summary>Молния: бьёт цель и перескакивает на соседних.</summary>
        Chain,

        /// <summary>Волна вокруг героя, задевает всех в радиусе.</summary>
        Nova
    }

    /// <summary>Один слот оружия: чем бьёт, как часто и насколько далеко.</summary>
    [Serializable]
    public class WeaponSlot
    {
        public WeaponKind Kind = WeaponKind.Melee;
        public float Damage = 20f;
        public float Cooldown = 0.8f;
        public float Range = 3f;
        public float ProjectileSpeed = 24f;
        public bool Enabled = true;

        [NonSerialized] public float NextFireTime;
    }

    /// <summary>
    /// Шесть слотов оружия, которые бьют сами, как в Vampire Survivors.
    /// Игрок только двигается и выбирает, что взять: оружие циклично стреляет
    /// по ближайшему врагу в своём радиусе, каждое со своей перезарядкой.
    ///
    /// Такой подход убирает необходимость целиться и превращает бой в управление
    /// позицией — что и нужно на маленькой планете.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponSystem : MonoBehaviour
    {
        public const int MaxSlots = 6;

        [Header("Ссылки")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Слои, по которым ищутся враги")]
        [SerializeField] private LayerMask _enemyMask = ~0;

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _bodyCenter = 1f;
        [SerializeField, Min(0.05f)] private float _meleeRadius = 0.6f;
        [SerializeField, Min(0f)] private float _turnSpeed = 14f;

        [Header("Слоты")]
        [SerializeField] private List<WeaponSlot> _slots = new List<WeaponSlot>(MaxSlots);

        [Header("Отладка")]
        [SerializeField] private bool _logShots;

        private readonly Collider[] _targets = new Collider[24];
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly List<Transform> _chainVisited = new List<Transform>(4);

        public IReadOnlyList<WeaponSlot> Slots => _slots;

        public void Configure(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>Улучшение из хаба: множитель применяется к урону всех слотов.</summary>
        public void ApplyDamageMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
                return;

            foreach (var slot in _slots)
                slot.Damage *= multiplier;
        }

        /// <summary>Поставить оружие в свободный слот — пригодится оружейной в хабе.</summary>
        public bool TryEquip(WeaponSlot slot)
        {
            if (slot == null || _slots.Count >= MaxSlots)
                return false;

            slot.NextFireTime = 0f;
            _slots.Add(slot);
            return true;
        }

        /// <summary>Очистить набор — перед сборкой нового в оружейной.</summary>
        public void ClearSlots()
        {
            _slots.Clear();
        }

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_slots.Count == 0)
                _slots.Add(new WeaponSlot());

            // Слоты не должны бить одновременно после загрузки сцены.
            for (var i = 0; i < _slots.Count; i++)
                _slots[i].NextFireTime = Time.time + i * 0.15f;
        }

        private void Update()
        {
            var maxRange = 0f;

            foreach (var slot in _slots)
            {
                if (slot != null && slot.Enabled)
                    maxRange = Mathf.Max(maxRange, slot.Range);
            }

            if (maxRange <= 0f)
                return;

            var target = FindNearestTarget(maxRange, out var distance);

            if (target == null)
                return;

            FaceTowards(target.position);

            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];

                if (slot == null || !slot.Enabled || distance > slot.Range)
                    continue;

                if (Time.time < slot.NextFireTime)
                    continue;

                slot.NextFireTime = Time.time + Mathf.Max(0.05f, slot.Cooldown);
                Fire(slot, target);
            }
        }

        private Transform FindNearestTarget(float range, out float distance)
        {
            distance = float.MaxValue;

            // Сначала живые враги: стены подождут, даже если они ближе.
            var enemy = FindNearest(range, ref distance, true);

            if (enemy != null)
                return enemy;

            return FindNearest(range, ref distance, false);
        }

        private Transform FindNearest(float range, ref float distance, bool onlyEnemies)
        {
            Transform nearest = null;

            var center = transform.position + transform.up * _bodyCenter;
            var count = Physics.OverlapSphereNonAlloc(center, range, _targets, _enemyMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var candidate = _targets[i];

                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;

                var isEnemy = candidate.GetComponentInParent<EnemyMarker>() != null;

                // Второй проход — только по постройкам.
                if (isEnemy != onlyEnemies)
                    continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                var candidateDistance = Vector3.Distance(center, candidate.bounds.center);

                if (candidateDistance < distance)
                {
                    distance = candidateDistance;
                    nearest = candidate.transform;
                }
            }

            return nearest;
        }

        private void Fire(WeaponSlot slot, Transform target)
        {
            var up = transform.up;
            var origin = transform.position + up * _bodyCenter;
            var toTarget = target.position + up * 0.9f - origin;

            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            var direction = toTarget.normalized;

            switch (slot.Kind)
            {
                case WeaponKind.Bolt:
                    ProjectileFactory.Spawn(origin + direction * 0.4f, direction, slot.ProjectileSpeed,
                        slot.Damage, transform, new Color(0.35f, 0.85f, 1f));
                    break;

                case WeaponKind.Pierce:
                    ProjectileFactory.Spawn(origin + direction * 0.4f, direction, slot.ProjectileSpeed,
                        slot.Damage, transform, new Color(0.95f, 0.9f, 0.35f), pierce: 3);
                    break;

                case WeaponKind.Chain:
                    FireChain(slot, origin, target);
                    break;

                case WeaponKind.Nova:
                    FireNova(slot, origin);
                    break;

                default:
                    FireMelee(slot, origin, direction);
                    break;
            }

            if (_logShots)
                Debug.Log($"[WEAPON] {slot.Kind} по {target.name} на {slot.Damage:F0}");
        }

        private void FireMelee(WeaponSlot slot, Vector3 origin, Vector3 direction)
        {
            var count = Physics.SphereCastNonAlloc(origin, _meleeRadius, direction, _hits, slot.Range,
                _enemyMask, QueryTriggerInteraction.Ignore);

            var best = float.MaxValue;
            IDamageable bestTarget = null;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                if (hit.distance >= best)
                    continue;

                best = hit.distance;
                bestTarget = damageable;
            }

            bestTarget?.TakeDamage(slot.Damage);
        }

        /// <summary>
        /// Молния: бьёт первую цель, затем перескакивает на ближайшую непоражённую.
        /// С каждым прыжком урон падает — иначе толпа умирала бы от одного разряда.
        /// </summary>
        private void FireChain(WeaponSlot slot, Vector3 origin, Transform firstTarget)
        {
            _chainVisited.Clear();

            var current = firstTarget;
            var damage = slot.Damage;
            const int jumps = 3;
            const float jumpRadius = 6f;

            for (var i = 0; i < jumps && current != null; i++)
            {
                var damageable = current.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    break;

                damageable.TakeDamage(damage);
                _chainVisited.Add(current);
                damage *= 0.7f;

                current = FindChainTarget(current.position, jumpRadius);
            }
        }

        private Transform FindChainTarget(Vector3 position, float radius)
        {
            var count = Physics.OverlapSphereNonAlloc(position, radius, _targets, _enemyMask,
                QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            var best = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var candidate = _targets[i];

                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;

                if (_chainVisited.Contains(candidate.transform))
                    continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                var distance = Vector3.Distance(position, candidate.bounds.center);

                if (distance >= best)
                    continue;

                best = distance;
                nearest = candidate.transform;
            }

            return nearest;
        }

        private void FireNova(WeaponSlot slot, Vector3 origin)
        {
            var count = Physics.OverlapSphereNonAlloc(origin, slot.Range, _targets, _enemyMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var candidate = _targets[i];

                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                damageable.TakeDamage(slot.Damage);
            }
        }

        private void FaceTowards(Vector3 worldPosition)
        {
            var up = transform.up;
            var flat = Vector3.ProjectOnPlane(worldPosition - transform.position, up);

            if (flat.sqrMagnitude < 0.0001f)
                return;

            var aligned = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
            var target = Quaternion.LookRotation(flat.normalized, up);
            transform.rotation = Quaternion.Slerp(aligned, target, _turnSpeed * Time.deltaTime);
        }
    }
}
