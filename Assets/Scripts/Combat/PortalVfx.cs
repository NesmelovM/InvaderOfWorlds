using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Частицы портала: столб искр, поднимающийся от диска, и мягкое свечение.
    ///
    /// Создаётся отдельным объектом, а не дочерним у диска: диск сплющен
    /// (масштаб 8×0.08×8), и дочерние частицы наследовали это сжатие —
    /// размер искры становился сантиметровым, и их не было видно.
    /// </summary>
    public static class PortalVfx
    {
        public static void Attach(Transform portal, Color color)
        {
            var host = new GameObject("Portal VFX");
            host.transform.position = portal.position;
            host.transform.rotation = Quaternion.FromToRotation(Vector3.forward, portal.up);

            var particles = host.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2.2f;
            main.startSpeed = 1.8f;
            main.startSize = 0.35f;
            main.startColor = color;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.06f;
            main.loop = true;
            main.playOnAwake = true;

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 60f;

            // Конус направлен по +Z объекта, а он повёрнут так, что +Z — нормаль планеты.
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 3.0f;
            shape.rotation = Vector3.zero;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.white, 0.4f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = CreateMaterial(color);

            particles.Play();

            // Мягкий свет, чтобы портал подсвечивал землю вокруг.
            var lightObject = new GameObject("Portal Light");
            lightObject.transform.position = portal.position + portal.up * 2.5f;

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 14f;
            light.intensity = 2.6f;
        }

        private static Material CreateMaterial(Color color)
        {
            // Sprites/Default умеет прозрачность и умножает цвет частицы на текстуру.
            // Particles/Standard Unlit без настройки ключей рисует белые квадраты.
            var shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            var material = new Material(shader) { name = "Portal Particles" };
            material.mainTexture = SoftCircle();
            material.SetColor("_Color", Color.white);
            material.SetColor("_TintColor", color);
            material.renderQueue = 3000;
            return material;
        }

        /// <summary>
        /// Мягкая круглая искра. Без текстуры частицы рисуются квадратами —
        /// стандартный белый спрайт выглядит как пиксельная пыль.
        /// </summary>
        private static Texture2D SoftCircle()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Portal Spark",
                wrapMode = TextureWrapMode.Clamp
            };

            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    var alpha = Mathf.Clamp01(1f - distance / center);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
