using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    public enum WeaponKind
    {
        /// <summary>Р вЂР В»Р С‘Р В¶Р Р…Р С‘Р в„– Р В±Р С•Р в„–: Р С”Р С•РЎР‚Р С•РЎвЂљР С”Р С‘Р в„– Р В·Р В°Р СР В°РЎвЂ¦ Р С—Р ВµРЎР‚Р ВµР Т‘ Р С–Р ВµРЎР‚Р С•Р ВµР С.</summary>
        Melee,

        /// <summary>Р РЋР Р…Р В°РЎР‚РЎРЏР Т‘ Р Р† Р В±Р В»Р С‘Р В¶Р В°Р в„–РЎв‚¬Р ВµР С–Р С• Р Р†РЎР‚Р В°Р С–Р В°.</summary>
        Bolt,

        /// <summary>Р СџРЎР‚Р С•Р В±Р С‘Р Р†Р В°РЎР‹РЎвЂ°Р С‘Р в„– РЎРѓР Р…Р В°РЎР‚РЎРЏР Т‘: Р В·Р В°Р Т‘Р ВµР Р†Р В°Р ВµРЎвЂљ Р Р…Р ВµРЎРѓР С”Р С•Р В»РЎРЉР С”Р С• РЎвЂ Р ВµР В»Р ВµР в„– Р С—Р С•Р Т‘РЎР‚РЎРЏР Т‘.</summary>
        Pierce,

        /// <summary>Р СљР С•Р В»Р Р…Р С‘РЎРЏ: Р В±РЎРЉРЎвЂРЎвЂљ РЎвЂ Р ВµР В»РЎРЉ Р С‘ Р С—Р ВµРЎР‚Р ВµРЎРѓР С”Р В°Р С”Р С‘Р Р†Р В°Р ВµРЎвЂљ Р Р…Р В° РЎРѓР С•РЎРѓР ВµР Т‘Р Р…Р С‘РЎвЂ¦.</summary>
        Chain,

        /// <summary>Р вЂ™Р С•Р В»Р Р…Р В° Р Р†Р С•Р С”РЎР‚РЎС“Р С– Р С–Р ВµРЎР‚Р С•РЎРЏ, Р В·Р В°Р Т‘Р ВµР Р†Р В°Р ВµРЎвЂљ Р Р†РЎРѓР ВµРЎвЂ¦ Р Р† РЎР‚Р В°Р Т‘Р С‘РЎС“РЎРѓР Вµ.</summary>
        Nova,

        /// <summary>Р С™Р В»Р С‘Р Р…Р С•Р С”, Р Р†РЎР‚Р В°РЎвЂ°Р В°РЎР‹РЎвЂ°Р С‘Р в„–РЎРѓРЎРЏ Р Р†Р С•Р С”РЎР‚РЎС“Р С– Р С–Р ВµРЎР‚Р С•РЎРЏ Р С‘ Р В·Р В°Р Т‘Р ВµР Р†Р В°РЎР‹РЎвЂ°Р С‘Р в„– Р Р†РЎРѓРЎвЂ Р Р…Р В° Р С—РЎС“РЎвЂљР С‘.</summary>
        Orbiter,

        /// <summary>Р СџР С•РЎРѓРЎвЂљР С•РЎРЏР Р…Р Р…РЎвЂ№Р в„– РЎС“РЎР‚Р С•Р Р… Р Р† РЎР‚Р В°Р Т‘Р С‘РЎС“РЎРѓР Вµ, Р В±Р ВµР В· Р Р†РЎвЂ№РЎРѓРЎвЂљРЎР‚Р ВµР В»Р С•Р Р†.</summary>
        Aura,

        /// <summary>Р вЂєР ВµРЎвЂљР С‘РЎвЂљ Р Р†Р С—Р ВµРЎР‚РЎвЂР Т‘ Р С‘ Р Р†Р С•Р В·Р Р†РЎР‚Р В°РЎвЂ°Р В°Р ВµРЎвЂљРЎРѓРЎРЏ Р С” РЎвЂ¦Р С•Р В·РЎРЏР С‘Р Р…РЎС“.</summary>
        Boomerang,

        /// <summary>Р РЋР В°Р СР В° Р Р…Р В°Р Р†Р С•Р Т‘Р С‘РЎвЂљРЎРѓРЎРЏ Р Р…Р В° Р В±Р В»Р С‘Р В¶Р В°Р в„–РЎв‚¬Р ВµР С–Р С• Р Р†РЎР‚Р В°Р С–Р В°.</summary>
        Homing
    }

    /// <summary>Р С›Р Т‘Р С‘Р Р… РЎРѓР В»Р С•РЎвЂљ Р С•РЎР‚РЎС“Р В¶Р С‘РЎРЏ: РЎвЂЎР ВµР С Р В±РЎРЉРЎвЂРЎвЂљ, Р С”Р В°Р С” РЎвЂЎР В°РЎРѓРЎвЂљР С• Р С‘ Р Р…Р В°РЎРѓР С”Р С•Р В»РЎРЉР С”Р С• Р Т‘Р В°Р В»Р ВµР С”Р С•.</summary>
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
    /// Р РЃР ВµРЎРѓРЎвЂљРЎРЉ РЎРѓР В»Р С•РЎвЂљР С•Р Р† Р С•РЎР‚РЎС“Р В¶Р С‘РЎРЏ, Р С”Р С•РЎвЂљР С•РЎР‚РЎвЂ№Р Вµ Р В±РЎРЉРЎР‹РЎвЂљ РЎРѓР В°Р СР С‘, Р С”Р В°Р С” Р Р† Vampire Survivors.
    /// Р ВР С–РЎР‚Р С•Р С” РЎвЂљР С•Р В»РЎРЉР С”Р С• Р Т‘Р Р†Р С‘Р С–Р В°Р ВµРЎвЂљРЎРѓРЎРЏ Р С‘ Р Р†РЎвЂ№Р В±Р С‘РЎР‚Р В°Р ВµРЎвЂљ, РЎвЂЎРЎвЂљР С• Р Р†Р В·РЎРЏРЎвЂљРЎРЉ: Р С•РЎР‚РЎС“Р В¶Р С‘Р Вµ РЎвЂ Р С‘Р С”Р В»Р С‘РЎвЂЎР Р…Р С• РЎРѓРЎвЂљРЎР‚Р ВµР В»РЎРЏР ВµРЎвЂљ
    /// Р С—Р С• Р В±Р В»Р С‘Р В¶Р В°Р в„–РЎв‚¬Р ВµР СРЎС“ Р Р†РЎР‚Р В°Р С–РЎС“ Р Р† РЎРѓР Р†Р С•РЎвЂР С РЎР‚Р В°Р Т‘Р С‘РЎС“РЎРѓР Вµ, Р С”Р В°Р В¶Р Т‘Р С•Р Вµ РЎРѓР С• РЎРѓР Р†Р С•Р ВµР в„– Р С—Р ВµРЎР‚Р ВµР В·Р В°РЎР‚РЎРЏР Т‘Р С”Р С•Р в„–.
    ///
    /// Р СћР В°Р С”Р С•Р в„– Р С—Р С•Р Т‘РЎвЂ¦Р С•Р Т‘ РЎС“Р В±Р С‘РЎР‚Р В°Р ВµРЎвЂљ Р Р…Р ВµР С•Р В±РЎвЂ¦Р С•Р Т‘Р С‘Р СР С•РЎРѓРЎвЂљРЎРЉ РЎвЂ Р ВµР В»Р С‘РЎвЂљРЎРЉРЎРѓРЎРЏ Р С‘ Р С—РЎР‚Р ВµР Р†РЎР‚Р В°РЎвЂ°Р В°Р ВµРЎвЂљ Р В±Р С•Р в„– Р Р† РЎС“Р С—РЎР‚Р В°Р Р†Р В»Р ВµР Р…Р С‘Р Вµ
    /// Р С—Р С•Р В·Р С‘РЎвЂ Р С‘Р ВµР в„– РІР‚вЂќ РЎвЂЎРЎвЂљР С• Р С‘ Р Р…РЎС“Р В¶Р Р…Р С• Р Р…Р В° Р СР В°Р В»Р ВµР Р…РЎРЉР С”Р С•Р в„– Р С—Р В»Р В°Р Р…Р ВµРЎвЂљР Вµ.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponSystem : MonoBehaviour
    {
        public const int MaxSlots = 6;

        [Header("Р РЋРЎРѓРЎвЂ№Р В»Р С”Р С‘")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Р РЋР В»Р С•Р С‘, Р С—Р С• Р С”Р С•РЎвЂљР С•РЎР‚РЎвЂ№Р С Р С‘РЎвЂ°РЎС“РЎвЂљРЎРѓРЎРЏ Р Р†РЎР‚Р В°Р С–Р С‘")]
        [SerializeField] private LayerMask _enemyMask = ~0;

        [Header("Р вЂњР ВµР С•Р СР ВµРЎвЂљРЎР‚Р С‘РЎРЏ")]
        [SerializeField, Min(0f)] private float _bodyCenter = 1f;
        [SerializeField, Min(0.05f)] private float _meleeRadius = 0.6f;
        [SerializeField, Min(0f)] private float _turnSpeed = 14f;

        [Header("Р РЋР В»Р С•РЎвЂљРЎвЂ№")]
        [SerializeField] private List<WeaponSlot> _slots = new List<WeaponSlot>(MaxSlots);

        [Header("Р С›РЎвЂљР В»Р В°Р Т‘Р С”Р В°")]
        [SerializeField] private bool _logShots;

        private readonly Collider[] _targets = new Collider[24];
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly List<Transform> _chainVisited = new List<Transform>(4);
        private readonly List<Transform> _orbiterVisuals = new List<Transform>(2);
        private readonly List<float> _orbiterAngles = new List<float>(2);
        private Transform _auraVisual;

        public IReadOnlyList<WeaponSlot> Slots => _slots;

        public void Configure(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>Р Р€Р В»РЎС“РЎвЂЎРЎв‚¬Р ВµР Р…Р С‘Р Вµ Р С‘Р В· РЎвЂ¦Р В°Р В±Р В°: Р СР Р…Р С•Р В¶Р С‘РЎвЂљР ВµР В»РЎРЉ Р С—РЎР‚Р С‘Р СР ВµР Р…РЎРЏР ВµРЎвЂљРЎРѓРЎРЏ Р С” РЎС“РЎР‚Р С•Р Р…РЎС“ Р Р†РЎРѓР ВµРЎвЂ¦ РЎРѓР В»Р С•РЎвЂљР С•Р Р†.</summary>
        public void ApplyDamageMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
                return;

            foreach (var slot in _slots)
                slot.Damage *= multiplier;
        }

        /// <summary>Р СџР С•РЎРѓРЎвЂљР В°Р Р†Р С‘РЎвЂљРЎРЉ Р С•РЎР‚РЎС“Р В¶Р С‘Р Вµ Р Р† РЎРѓР Р†Р С•Р В±Р С•Р Т‘Р Р…РЎвЂ№Р в„– РЎРѓР В»Р С•РЎвЂљ РІР‚вЂќ Р С—РЎР‚Р С‘Р С–Р С•Р Т‘Р С‘РЎвЂљРЎРѓРЎРЏ Р С•РЎР‚РЎС“Р В¶Р ВµР в„–Р Р…Р С•Р в„– Р Р† РЎвЂ¦Р В°Р В±Р Вµ.</summary>
        public bool TryEquip(WeaponSlot slot)
        {
            if (slot == null || _slots.Count >= MaxSlots)
                return false;

            slot.NextFireTime = 0f;
            _slots.Add(slot);
            return true;
        }

        /// <summary>Р С›РЎвЂЎР С‘РЎРѓРЎвЂљР С‘РЎвЂљРЎРЉ Р Р…Р В°Р В±Р С•РЎР‚ РІР‚вЂќ Р С—Р ВµРЎР‚Р ВµР Т‘ РЎРѓР В±Р С•РЎР‚Р С”Р С•Р в„– Р Р…Р С•Р Р†Р С•Р С–Р С• Р Р† Р С•РЎР‚РЎС“Р В¶Р ВµР в„–Р Р…Р С•Р в„–.</summary>
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

            // Р РЋР В»Р С•РЎвЂљРЎвЂ№ Р Р…Р Вµ Р Т‘Р С•Р В»Р В¶Р Р…РЎвЂ№ Р В±Р С‘РЎвЂљРЎРЉ Р С•Р Т‘Р Р…Р С•Р Р†РЎР‚Р ВµР СР ВµР Р…Р Р…Р С• Р С—Р С•РЎРѓР В»Р Вµ Р В·Р В°Р С–РЎР‚РЎС“Р В·Р С”Р С‘ РЎРѓРЎвЂ Р ВµР Р…РЎвЂ№.
            for (var i = 0; i < _slots.Count; i++)
                _slots[i].NextFireTime = Time.time + i * 0.15f;
        }

        private void Update()
        {
            UpdateContinuous(Time.deltaTime);

            var maxRange = 0f;

            foreach (var slot in _slots)
            {
                if (slot == null || !slot.Enabled || IsContinuous(slot.Kind))
                    continue;

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

                if (slot == null || !slot.Enabled || IsContinuous(slot.Kind) || distance > slot.Range)
                    continue;

                if (Time.time < slot.NextFireTime)
                    continue;

                slot.NextFireTime = Time.time + Mathf.Max(0.05f, slot.Cooldown);
                Fire(slot, target);
            }
        }

        private static bool IsContinuous(WeaponKind kind)
        {
            return kind == WeaponKind.Orbiter || kind == WeaponKind.Aura;
        }

        /// <summary>Р С›РЎР‚РЎС“Р В¶Р С‘Р Вµ, Р С”Р С•РЎвЂљР С•РЎР‚Р С•Р Вµ Р Т‘Р ВµР в„–РЎРѓРЎвЂљР Р†РЎС“Р ВµРЎвЂљ Р С—Р С•РЎРѓРЎвЂљР С•РЎРЏР Р…Р Р…Р С•: Р С•РЎР‚Р В±Р С‘РЎвЂљР В°Р В»РЎРЉР Р…РЎвЂ№Р в„– Р С”Р В»Р С‘Р Р…Р С•Р С” Р С‘ Р В°РЎС“РЎР‚Р В°.</summary>
        private void UpdateContinuous(float deltaTime)
        {
            var orbiterSlots = 0;
            var hasAura = false;

            foreach (var slot in _slots)
            {
                if (slot == null || !slot.Enabled)
                    continue;

                if (slot.Kind == WeaponKind.Orbiter)
                    orbiterSlots++;

                if (slot.Kind == WeaponKind.Aura)
                    hasAura = true;
            }

            SyncVisuals(orbiterSlots, hasAura);

            var index = 0;

            foreach (var slot in _slots)
            {
                if (slot == null || !slot.Enabled)
                    continue;

                if (slot.Kind == WeaponKind.Orbiter)
                    UpdateOrbiter(slot, index++, deltaTime);
                else if (slot.Kind == WeaponKind.Aura)
                    UpdateAura(slot);
            }
        }

        private void UpdateOrbiter(WeaponSlot slot, int index, float deltaTime)
        {
            if (index >= _orbiterVisuals.Count)
                return;

            var blade = _orbiterVisuals[index];
            var up = transform.up;

            _orbiterAngles[index] += 240f * deltaTime;

            var direction = Quaternion.AngleAxis(_orbiterAngles[index], up) *
                            Vector3.ProjectOnPlane(transform.forward, up).normalized;
            var position = transform.position + up * 0.9f + direction * slot.Range;

            blade.position = position;
            blade.localScale = Vector3.one * 0.55f;

            if (Time.time < slot.NextFireTime)
                return;

            slot.NextFireTime = Time.time + 0.3f;

            var count = Physics.OverlapSphereNonAlloc(position, 0.8f, _targets, _enemyMask,
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

        private void UpdateAura(WeaponSlot slot)
        {
            if (_auraVisual != null)
            {
                _auraVisual.position = transform.position + transform.up * 0.1f;
                _auraVisual.rotation = Quaternion.FromToRotation(Vector3.up, transform.up);
                _auraVisual.localScale = new Vector3(slot.Range * 2f, 0.02f, slot.Range * 2f);
            }

            if (Time.time < slot.NextFireTime)
                return;

            slot.NextFireTime = Time.time + Mathf.Max(0.4f, slot.Cooldown);

            var count = Physics.OverlapSphereNonAlloc(transform.position + transform.up * 0.5f, slot.Range, _targets,
                _enemyMask, QueryTriggerInteraction.Ignore);

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

        /// <summary>Р РЋР С•Р В·Р Т‘Р В°РЎвЂРЎвЂљ Р С‘ РЎС“Р В±Р С‘РЎР‚Р В°Р ВµРЎвЂљ Р Р†Р С‘Р В·РЎС“Р В°Р В»РЎРЉР Р…РЎвЂ№Р Вµ Р С•Р В±РЎР‰Р ВµР С”РЎвЂљРЎвЂ№ Р С—Р С•Р Т‘ РЎвЂљР ВµР С”РЎС“РЎвЂ°Р С‘Р в„– Р Р…Р В°Р В±Р С•РЎР‚ Р С•РЎР‚РЎС“Р В¶Р С‘РЎРЏ.</summary>
        private void SyncVisuals(int orbiterSlots, bool hasAura)
        {
            if (_orbiterVisuals.Count != orbiterSlots)
            {
                foreach (var visual in _orbiterVisuals)
                {
                    if (visual != null)
                        Destroy(visual.gameObject);
                }

                _orbiterVisuals.Clear();
                _orbiterAngles.Clear();

                for (var i = 0; i < orbiterSlots; i++)
                {
                    _orbiterAngles.Add(i * (360f / Mathf.Max(1, orbiterSlots)));
                    _orbiterVisuals.Add(CreateVisual("Orbital Blade", new Color(0.4f, 0.95f, 1f), PrimitiveType.Sphere));
                }
            }

            if (hasAura && _auraVisual == null)
                _auraVisual = CreateVisual("Aura Ring", new Color(0.6f, 0.3f, 0.95f), PrimitiveType.Cylinder);
            else if (!hasAura && _auraVisual != null)
            {
                Destroy(_auraVisual.gameObject);
                _auraVisual = null;
            }
        }

        private Transform CreateVisual(string name, Color color, PrimitiveType shape)
        {
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = name;

            var collider = visual.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            var renderer = visual.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = EnemyFactory.GetMaterial(color);

            return visual.transform;
        }

        private Transform FindNearestTarget(float range, out float distance)
        {
            distance = float.MaxValue;

            // Р РЋР Р…Р В°РЎвЂЎР В°Р В»Р В° Р В¶Р С‘Р Р†РЎвЂ№Р Вµ Р Р†РЎР‚Р В°Р С–Р С‘: РЎРѓРЎвЂљР ВµР Р…РЎвЂ№ Р С—Р С•Р Т‘Р С•Р В¶Р Т‘РЎС“РЎвЂљ, Р Т‘Р В°Р В¶Р Вµ Р ВµРЎРѓР В»Р С‘ Р С•Р Р…Р С‘ Р В±Р В»Р С‘Р В¶Р Вµ.
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

                // Р вЂ™РЎвЂљР С•РЎР‚Р С•Р в„– Р С—РЎР‚Р С•РЎвЂ¦Р С•Р Т‘ РІР‚вЂќ РЎвЂљР С•Р В»РЎРЉР С”Р С• Р С—Р С• Р С—Р С•РЎРѓРЎвЂљРЎР‚Р С•Р в„–Р С”Р В°Р С.
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

                case WeaponKind.Boomerang:
                    SpawnBoomerang(slot, origin, direction);
                    break;

                case WeaponKind.Homing:
                    SpawnHoming(slot, origin, direction);
                    break;

                case WeaponKind.Nova:
                    FireNova(slot, origin);
                    break;

                default:
                    FireMelee(slot, origin, direction);
                    break;
            }

            if (_logShots)
                Debug.Log($"[WEAPON] {slot.Kind} Р С—Р С• {target.name} Р Р…Р В° {slot.Damage:F0}");
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
        /// Р СљР С•Р В»Р Р…Р С‘РЎРЏ: Р В±РЎРЉРЎвЂРЎвЂљ Р С—Р ВµРЎР‚Р Р†РЎС“РЎР‹ РЎвЂ Р ВµР В»РЎРЉ, Р В·Р В°РЎвЂљР ВµР С Р С—Р ВµРЎР‚Р ВµРЎРѓР С”Р В°Р С”Р С‘Р Р†Р В°Р ВµРЎвЂљ Р Р…Р В° Р В±Р В»Р С‘Р В¶Р В°Р в„–РЎв‚¬РЎС“РЎР‹ Р Р…Р ВµР С—Р С•РЎР‚Р В°Р В¶РЎвЂР Р…Р Р…РЎС“РЎР‹.
        /// Р РЋ Р С”Р В°Р В¶Р Т‘РЎвЂ№Р С Р С—РЎР‚РЎвЂ№Р В¶Р С”Р С•Р С РЎС“РЎР‚Р С•Р Р… Р С—Р В°Р Т‘Р В°Р ВµРЎвЂљ РІР‚вЂќ Р С‘Р Р…Р В°РЎвЂЎР Вµ РЎвЂљР С•Р В»Р С—Р В° РЎС“Р СР С‘РЎР‚Р В°Р В»Р В° Р В±РЎвЂ№ Р С•РЎвЂљ Р С•Р Т‘Р Р…Р С•Р С–Р С• РЎР‚Р В°Р В·РЎР‚РЎРЏР Т‘Р В°.
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

        private void SpawnBoomerang(WeaponSlot slot, Vector3 origin, Vector3 direction)
        {
            var boomerangObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boomerangObject.name = "Boomerang";
            boomerangObject.transform.position = origin + direction * 0.5f;
            boomerangObject.transform.localScale = new Vector3(0.5f, 0.12f, 0.5f);

            var collider = boomerangObject.GetComponent<Collider>();

            if (collider != null)
                collider.isTrigger = true;

            var renderer = boomerangObject.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = EnemyFactory.GetMaterial(new Color(0.95f, 0.75f, 0.35f));

            var boomerang = boomerangObject.AddComponent<Boomerang>();
            boomerang.Launch(transform, direction, slot.ProjectileSpeed, slot.Damage, slot.Range);
        }

        private void SpawnHoming(WeaponSlot slot, Vector3 origin, Vector3 direction)
        {
            var rocketObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rocketObject.name = "Homing Missile";
            rocketObject.transform.position = origin + direction * 0.5f;
            rocketObject.transform.localScale = Vector3.one * 0.3f;

            var collider = rocketObject.GetComponent<Collider>();

            if (collider != null)
                collider.isTrigger = true;

            var renderer = rocketObject.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = EnemyFactory.GetMaterial(new Color(1f, 0.45f, 0.85f));

            var rocket = rocketObject.AddComponent<HomingProjectile>();
            rocket.Launch(transform, direction, slot.ProjectileSpeed, slot.Damage);
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
