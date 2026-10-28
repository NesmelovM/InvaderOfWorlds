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
        Nova,

        /// <summary>Клинок, вращающийся вокруг героя и задевающий всё на пути.</summary>
        Orbiter,

        /// <summary>Постоянный урон в радиусе, без выстрелов.</summary>
        Aura,

        /// <summary>Летит вперёд и возвращается к хозяину.</summary>
        Boomerang,

        /// <summary>Сама наводится на ближайшего врага.</summary>
        Homing
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
        [Tooltip("Сколько атак за один тик: залп снарядов или несколько ударов")]
        public int Shots = 1;
        [Tooltip("Индекс оружия в каталоге оружейной: по нему выбирается модель в руке")]
        public int Icon = -1;
        public bool Enabled = true;

        [NonSerialized] public float NextFireTime;
    }

    /// <summary>
    /// Шесть слотов оружия, которые бьют сами, как в Vampire Survivors.
    ///
    /// Всё, что летит, летит по дуге вокруг планеты: прямая на сфере уходит в грунт
    /// или отрывается от поверхности. Аура — не плоский диск, а участок сферы,
    /// повторяющий кривизну. Орбитальный клинок тоже держится на постоянной
    /// высоте над грунтом.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponSystem : MonoBehaviour
    {
        public const int MaxSlots = 6;

        [Header("Ссылки")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Планета нужна, чтобы снаряды шли по дуге, а аура лежала на поверхности")]
        [SerializeField] private Planet _planet;
        [Tooltip("Слои, по которым ищутся враги")]
        [SerializeField] private LayerMask _enemyMask = ~0;

        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float _bodyCenter = 1f;
        [SerializeField, Min(0.05f)] private float _meleeRadius = 0.6f;
        [SerializeField, Min(0f)] private float _turnSpeed = 14f;

        [Header("Слоты")]
        [SerializeField] private List<WeaponSlot> _slots = new List<WeaponSlot>(MaxSlots);

        private readonly Collider[] _targets = new Collider[24];
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly List<Transform> _chainVisited = new List<Transform>(4);
        private readonly List<Transform> _orbiterVisuals = new List<Transform>(2);
        private readonly List<float> _orbiterAngles = new List<float>(2);
        private readonly Dictionary<float, Mesh> _slashMeshes = new Dictionary<float, Mesh>();

        private Transform _auraVisual;
        private Mesh _auraMesh;
        private float _auraMeshRange = -1f;

        public IReadOnlyList<WeaponSlot> Slots => _slots;

        /// <summary>Слот выстрелил: на это подписан визуал оружия в руке.</summary>
        public event System.Action<WeaponKind> Fired;

        public void Configure(Transform cameraTransform, Planet planet = null)
        {
            _cameraTransform = cameraTransform;

            if (planet != null)
                _planet = planet;
        }

        /// <summary>Улучшение из хаба: множитель применяется к урону всех слотов.</summary>
        public void ApplyDamageMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
                return;

            foreach (var slot in _slots)
                slot.Damage *= multiplier;
        }

        /// <summary>Усиление из карточки уровня: множитель ко всем слотам.</summary>
        public void MultiplyDamage(float multiplier)
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

            if (_planet == null)
                _planet = FindAnyObjectByType<Planet>();

            if (_slots.Count == 0)
                _slots.Add(new WeaponSlot());

            // Слоты не должны бить одновременно после загрузки сцены.
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

        /// <summary>Оружие, которое действует постоянно: орбитальный клинок и аура.</summary>
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

            var forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            var direction = Quaternion.AngleAxis(_orbiterAngles[index], up) * forward;
            var position = transform.position + up * 0.9f + direction * slot.Range;

            // Держим клинок на постоянной высоте над грунтом, а не в касательной плоскости.
            if (_planet != null)
                position = _planet.Center + _planet.UpAt(position) * (_planet.Radius + 0.9f);

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
            UpdateAuraMesh(slot.Range);

            if (_auraVisual != null && _planet != null)
            {
                _auraVisual.position = _planet.Center;
                _auraVisual.rotation = Quaternion.FromToRotation(Vector3.up, transform.up);
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

        /// <summary>Меш ауры пересобирается при смене радиуса, а не каждый кадр.</summary>
        private void UpdateAuraMesh(float range)
        {
            if (_auraVisual == null)
                return;

            if (_planet == null)
                return;

            if (Mathf.Approximately(_auraMeshRange, range) && _auraMesh != null)
                return;

            var filter = _auraVisual.GetComponent<MeshFilter>();

            if (filter == null)
                return;

            _auraMesh = SurfaceCapMesh.Create(_planet.Radius, range, 0.06f);
            filter.sharedMesh = _auraMesh;
            _auraMeshRange = range;
        }

        /// <summary>Создаёт и убирает визуальные объекты под текущий набор оружия.</summary>
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
                _auraVisual = CreateAuraVisual(new Color(0.6f, 0.3f, 0.95f));
            else if (!hasAura && _auraVisual != null)
            {
                Destroy(_auraVisual.gameObject);
                _auraVisual = null;
                _auraMesh = null;
                _auraMeshRange = -1f;
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

        /// <summary>Аура — участок поверхности планеты, а не плоский диск.</summary>
        private Transform CreateAuraVisual(Color color)
        {
            var aura = new GameObject("Aura Ring");
            aura.AddComponent<MeshFilter>();
            var renderer = aura.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EnemyFactory.GetMaterial(color);

            return aura.transform;
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

        /// <summary>
        /// Залп: несколько снарядов за один тик с небольшим разбросом по сторонам.
        /// Количество берётся из слота — это правится в инструменте баланса.
        /// </summary>
        private void FireVolley(WeaponSlot slot, Vector3 origin, Vector3 direction, float r, float g, float b,
            int pierce = 0)
        {
            var shots = Mathf.Max(1, slot.Shots);
            var spread = shots > 1 ? 6f : 0f;

            for (var i = 0; i < shots; i++)
            {
                var offset = (i - (shots - 1) * 0.5f) * spread;
                var aimed = Quaternion.AngleAxis(offset, transform.up) * direction;

                ProjectileFactory.Spawn(origin + direction * 0.4f, aimed, slot.ProjectileSpeed, slot.Damage,
                    transform, new Color(r, g, b), pierce, _planet);
            }
        }

        private void Fire(WeaponSlot slot, Transform target)        {
            var up = transform.up;
            var origin = transform.position + up * _bodyCenter;
            var toTarget = target.position + up * 0.9f - origin;

            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            var direction = toTarget.normalized;

            switch (slot.Kind)
            {
                case WeaponKind.Bolt:
                    FireVolley(slot, origin, direction, 0.35f, 0.85f, 1f);
                    break;

                case WeaponKind.Pierce:
                    FireVolley(slot, origin, direction, 0.95f, 0.9f, 0.35f, pierce: 3);
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
                    SoundBank.Swing(origin);
                    break;
            }

            switch (slot.Kind)
            {
                case WeaponKind.Bolt:
                case WeaponKind.Pierce:
                case WeaponKind.Homing:
                case WeaponKind.Boomerang:
                    SoundBank.Shot(origin);
                    break;
                case WeaponKind.Chain:
                case WeaponKind.Nova:
                case WeaponKind.Aura:
                    SoundBank.Magic(origin);
                    break;
            }

            Fired?.Invoke(slot.Kind);
        }

        private void FireMelee(WeaponSlot slot, Vector3 origin, Vector3 direction)
        {
            // Видимый замах: без него удар клинком и глефой выглядел как «ничего не произошло».
            SpawnSlash(origin, direction, slot);

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

        /// <summary>Дуга замаха. Меш кешируется по дальности, чтобы не создавать его на каждый удар.</summary>
        private void SpawnSlash(Vector3 origin, Vector3 direction, WeaponSlot slot)
        {
            var slash = new GameObject("Slash");
            var filter = slash.AddComponent<MeshFilter>();
            filter.sharedMesh = GetSlashMesh(slot.Range);

            var renderer = slash.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EnemyFactory.GetMaterial(new Color(0.85f, 0.95f, 1f));

            slash.transform.position = origin;
            slash.transform.rotation = Quaternion.LookRotation(direction, transform.up);

            slash.AddComponent<SlashEffect>().Launch(transform.up, 1f);
        }

        private Mesh GetSlashMesh(float range)
        {
            if (_slashMeshes.TryGetValue(range, out var mesh) && mesh != null)
                return mesh;

            mesh = ArcMesh.Create(range * 0.35f, range, 110f);
            _slashMeshes[range] = mesh;
            return mesh;
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
            boomerang.Launch(transform, direction, slot.ProjectileSpeed, slot.Damage, slot.Range, _planet);
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
            rocket.Launch(transform, direction, slot.ProjectileSpeed, slot.Damage, _planet);
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
