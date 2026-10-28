using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Создаёт врага из примитива. Пока нет моделей и префабов, это капсула,
    /// но все компоненты те же, что будут у настоящего врага.
    /// </summary>
    public static class EnemyFactory
    {
        private static Material _material;
        private static Color _materialColor;

        public static GameObject Spawn(Planet planet, Transform target, Vector3 surfacePosition,
            Color color, float health, Transform parent)
        {
            var up = planet.UpAt(surfacePosition);

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Guard";
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = surfacePosition;
            enemy.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            enemy.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            var renderer = enemy.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = GetMaterial(color);

            var healthComponent = enemy.AddComponent<Health>();
            healthComponent.Configure(health);

            var brain = enemy.AddComponent<EnemyBrain>();
            brain.Configure(planet, target);

            return enemy;
        }

        /// <summary>Один материал на цвет: не создаём по экземпляру на каждого врага.</summary>
        private static Material GetMaterial(Color color)
        {
            if (_material != null && _materialColor == color)
                return _material;

            var shader = Shader.Find("Standard");
            _material = new Material(shader) { color = color };
            _materialColor = color;
            return _material;
        }
    }
}
