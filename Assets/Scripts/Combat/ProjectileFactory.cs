using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Создание снарядов: общее для врагов и оружия игрока.
    /// Снаряд — примитив с триггерным коллайдером, чтобы не блокировать других.
    /// </summary>
    public static class ProjectileFactory
    {
        public static void Spawn(Vector3 position, Vector3 direction, float speed, float damage,
            Transform owner, Color color, int pierce = 0, Planet planet = null)
        {
            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Projectile";
            projectileObject.transform.position = position;
            projectileObject.transform.localScale = Vector3.one * 0.3f;

            var collider = projectileObject.GetComponent<SphereCollider>();

            if (collider != null)
                collider.isTrigger = true;

            var renderer = projectileObject.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = EnemyFactory.GetMaterial(color);

            var projectile = projectileObject.AddComponent<Projectile>();
            projectile.Launch(direction, speed, damage, owner, pierce, planet);
        }
    }
}
