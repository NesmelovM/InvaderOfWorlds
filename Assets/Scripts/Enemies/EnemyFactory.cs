using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Создаёт врагов: модель из пака (если загрузилась) плюс все боевые компоненты.
    ///
    /// Модели из Low Poly Ultimate Pack приходят с произвольным масштабом, поэтому
    /// CharacterModel приводит их к нужному росту. Если модель не найдена,
    /// остаётся цветная капсула — игра не зависит от ассетов.
    /// </summary>
    public static class EnemyFactory
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        private static readonly Color MeleeColor = new Color(0.78f, 0.22f, 0.18f);
        private static readonly Color RangedColor = new Color(0.35f, 0.45f, 0.85f);
        private static readonly Color EliteColor = new Color(0.55f, 0.10f, 0.35f);
        private static readonly Color EliteRangedColor = new Color(0.90f, 0.52f, 0.14f);
        private static readonly Color BossColor = new Color(0.42f, 0.04f, 0.12f);

        /// <summary>Ближний бой: подходит и бьёт рукой.</summary>
        public static GameObject Spawn(Planet planet, Transform target, Vector3 surfacePosition,
            Color color, float health, Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, color, health, parent, 1.8f, 0.35f,
                gold: 5, experience: 10, modelPath: "Characters/man-soldier_Rig", weaponPath: "Weapons/sword", modelOverride: RunState.ActiveBiome?.MeleeModel);
            enemy.name = "Guard";

            var brain = enemy.AddComponent<EnemyBrain>();
            brain.Configure(planet, target);
            brain.ScaleDamage(RunState.Difficulty * Balance.EnemyDamageMultiplier);

            return enemy;
        }

        /// <summary>Дальний бой: держит дистанцию и стреляет снарядом.</summary>
        public static GameObject SpawnRanged(Planet planet, Transform target, Vector3 surfacePosition,
            float health, Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, RangedColor, health, parent, 1.9f, 0.33f,
                gold: 7, experience: 14, modelPath: "Characters/man-cult_Rig", weaponPath: "Weapons/bow", modelOverride: RunState.ActiveBiome?.RangedModel);
            enemy.name = "Archer";

            var brain = enemy.AddComponent<RangedEnemyBrain>();
            brain.Configure(planet, target);
            brain.ScaleDamage(RunState.Difficulty * Balance.EnemyDamageMultiplier);

            return enemy;
        }

        /// <summary>Элита: крупнее, толще и бьёт больнее, но медленнее.</summary>
        public static GameObject SpawnElite(Planet planet, Transform target, Vector3 surfacePosition,
            Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, EliteColor, 180f, parent, 2.5f, 0.5f,
                gold: 25, experience: 50, modelPath: "Characters/man-knight_Rig", weaponPath: "Weapons/hammer", modelOverride: RunState.ActiveBiome?.EliteModel);
            enemy.name = "Elite";

            var brain = enemy.AddComponent<EnemyBrain>();
            brain.Configure(planet, target);
            brain.ConfigureCombat(damage: 14f, attackCooldown: 1.8f, attackRange: 2.6f, speed: 2.6f);
            brain.ScaleDamage(RunState.Difficulty * Balance.EnemyDamageMultiplier);

            return enemy;
        }

        /// <summary>Элитный лучник: крупнее обычного, стреляет чаще и дальше.</summary>
        public static GameObject SpawnEliteRanged(Planet planet, Transform target, Vector3 surfacePosition,
            Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, EliteRangedColor, 140f, parent, 2.4f, 0.5f,
                gold: 35, experience: 70, modelPath: "Characters/man-cult_Rig", weaponPath: "Weapons/bow", modelOverride: RunState.ActiveBiome?.EliteArcherModel);
            enemy.name = "Elite Archer";

            var brain = enemy.AddComponent<RangedEnemyBrain>();
            brain.Configure(planet, target);
            brain.ConfigureCombat(damage: 12f, cooldown: 1.6f, windupTime: 0.5f, shootRange: 20f, speed: 2.4f);
            brain.ScaleDamage(RunState.Difficulty * Balance.EnemyDamageMultiplier);

            return enemy;
        }

        /// <summary>Босс: крупный, живёт у портала выхода, владеет тремя умениями.</summary>
        public static GameObject SpawnBoss(Planet planet, Transform target, Vector3 surfacePosition,
            Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, BossColor, 520f, parent, 4.2f, 0.9f,
                gold: 150, experience: 300, modelPath: "Characters/giant_Rig", weaponPath: "Weapons/club-zulu", modelOverride: RunState.ActiveBiome?.BossModel);
            enemy.name = "Boss";

            var brain = enemy.AddComponent<BossBrain>();
            brain.Configure(planet, target);
            brain.ScaleDamage(RunState.Difficulty * Balance.EnemyDamageMultiplier);

            return enemy;
        }

        private static GameObject CreateBody(Planet planet, Vector3 surfacePosition, Color color, float health,
            Transform parent, float height, float radius, int gold, int experience, string modelPath,
            string weaponPath = null, GameObject modelOverride = null)
        {
            var up = planet.UpAt(surfacePosition);

            var enemy = new GameObject("Enemy");
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = surfacePosition;
            enemy.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

            var body = enemy.AddComponent<CapsuleCollider>();
            body.height = height;
            body.radius = radius;
            body.center = new Vector3(0f, height * 0.5f, 0f);

            // Модель из биома важнее встроенной: биом задаёт облик врагов.
            var model = modelOverride != null
                ? CharacterModel.Attach(enemy, modelOverride, height, 0f, color)
                : CharacterModel.Attach(enemy, modelPath, height, 0f, color);

            if (model == null)
                CreateFallbackVisual(enemy, color, height, radius);
            else
            {
                // Процедурная анимация по костям и оружие в правой руке.
                enemy.AddComponent<CharacterRig>();

                if (!string.IsNullOrEmpty(weaponPath))
                    WeaponMount.Attach(enemy, weaponPath, height * 0.55f);
            }

            var healthComponent = enemy.AddComponent<Health>();
            healthComponent.Configure(health * Mathf.Max(1f, RunState.Difficulty) * Balance.EnemyHealthMultiplier);

            enemy.AddComponent<EnemyMarker>();
            enemy.AddComponent<HitFlash>();

            var bar = enemy.AddComponent<EnemyHealthBar>();
            bar.Configure(healthComponent, height + 0.2f);

            var reward = enemy.AddComponent<EnemyReward>();
            reward.Configure(gold, experience);

            return enemy;
        }

        /// <summary>Запасной вариант, если модели нет: цветная капсула.</summary>
        private static void CreateFallbackVisual(GameObject host, Color color, float height, float radius)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Fallback";
            visual.transform.SetParent(host.transform, false);
            visual.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            visual.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            var collider = visual.GetComponent<Collider>();

            if (collider != null)
                Object.Destroy(collider);

            var renderer = visual.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = GetMaterial(color);
        }

        /// <summary>Один материал на цвет: не создаём по экземпляру на каждого врага.</summary>
        public static Material GetMaterial(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null)
                return cached;

            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };

            // Низкополигональный мир должен быть матовым: с блеском по умолчанию
            // гладкие холмы и песок вспыхивают белым бликом на скользящем угле.
            material.SetFloat("_Glossiness", 0.05f);
            material.SetFloat("_Metallic", 0f);

            Materials[color] = material;
            return material;
        }

        public static Color MeleeDefaultColor => MeleeColor;
    }
}
