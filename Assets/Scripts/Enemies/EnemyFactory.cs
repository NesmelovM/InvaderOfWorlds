using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Создаёт врагов из примитивов. Пока нет моделей и префабов, это капсулы,
    /// но компоненты те же, что будут у настоящих врагов.
    /// </summary>
    public static class EnemyFactory
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        private static readonly Color MeleeColor = new Color(0.78f, 0.22f, 0.18f);
        private static readonly Color RangedColor = new Color(0.35f, 0.45f, 0.85f);
        private static readonly Color EliteColor = new Color(0.55f, 0.10f, 0.35f);

        /// <summary>Ближний бой: подходит и бьёт рукой.</summary>
        public static GameObject Spawn(Planet planet, Transform target, Vector3 surfacePosition,
            Color color, float health, Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, color, health, parent, new Vector3(0.7f, 0.9f, 0.7f));
            enemy.name = "Guard";

            var brain = enemy.AddComponent<EnemyBrain>();
            brain.Configure(planet, target);

            return enemy;
        }

        /// <summary>Дальний бой: держит дистанцию и стреляет снарядом.</summary>
        public static GameObject SpawnRanged(Planet planet, Transform target, Vector3 surfacePosition,
            float health, Transform parent)
        {
            var enemy = CreateBody(planet, surfacePosition, RangedColor, health, parent, new Vector3(0.65f, 0.95f, 0.65f));
            enemy.name = "Archer";

            var brain = enemy.AddComponent<RangedEnemyBrain>();
            brain.Configure(planet, target);

            return enemy;
        }

        /// <summary>Элита: крупнее, толще и бьёт больнее, но медленнее.</summary>
        public static GameObject SpawnElite(Planet planet, Transform target, Vector3 surfacePosition,
            Transform parent)
        {
            var scale = new Vector3(1.25f, 1.5f, 1.25f);
            var enemy = CreateBody(planet, surfacePosition, EliteColor, 180f, parent, scale);
            enemy.name = "Elite";

            var brain = enemy.AddComponent<EnemyBrain>();
            brain.Configure(planet, target);
            brain.ConfigureCombat(damage: 14f, attackCooldown: 1.8f, attackRange: 2.6f, speed: 2.6f, scale: 1f);

            return enemy;
        }

        private static GameObject CreateBody(Planet planet, Vector3 surfacePosition, Color color, float health,
            Transform parent, Vector3 scale)
        {
            var up = planet.UpAt(surfacePosition);

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = surfacePosition;
            enemy.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            enemy.transform.localScale = scale;

            var renderer = enemy.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = GetMaterial(color);

            var healthComponent = enemy.AddComponent<Health>();
            healthComponent.Configure(health);

            enemy.AddComponent<HitFlash>();

            var bar = enemy.AddComponent<EnemyHealthBar>();
            bar.Configure(healthComponent, 2f * scale.y + 0.1f);

            return enemy;
        }

        /// <summary>Один материал на цвет: не создаём по экземпляру на каждого врага.</summary>
        public static Material GetMaterial(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null)
                return cached;

            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            Materials[color] = material;
            return material;
        }

        public static Color MeleeDefaultColor => MeleeColor;
    }
}
