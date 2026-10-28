using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Собирает тестовую сцену одним пунктом меню: планета-сфера, игрок на поверхности,
    /// камера из-за плеча и генератор поселений, уже связанные между собой.
    ///
    /// Зачем: ручная привязка ссылок в инспекторе — главный источник «почему не работает».
    /// Здесь всё создаётся кодом, поэтому сцену можно пересобрать в любой момент
    /// и получить одинаковый результат.
    /// </summary>
    public static class TestPlanetSetup
    {
        private const float PlanetRadius = 90f;

        [MenuItem("Invader of Worlds/Собрать тестовую планету")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            var planet = CreatePlanet();
            var player = CreatePlayer(planet, out var motor);
            var cameraObject = CreateCamera(planet, out var cameraRig);
            CreateContent(planet);
            CreateTerrainTest(planet);

            // Связываем игрока с планетой и камерой.
            motor.Configure(planet, cameraObject.transform);
            cameraRig.Configure(player.transform, planet);

            SaveScene(scene);

            Selection.activeGameObject = player;
            Debug.Log($"[Invader of Worlds] Тестовая сцена собрана. Радиус планеты {PlanetRadius} м, " +
                      $"игрок стоит на «северном полюсе». Ходите WASD, смотрите мышью, прыжок — Space, бег — Shift.");
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Planet CreatePlanet()
        {
            var planetObject = new GameObject("Planet");
            var planet = planetObject.AddComponent<Planet>();
            planet.SetRadius(PlanetRadius);

            // Коллайдер планете не нужен: движение считается аналитически по сфере.
            // Коллайдер-сфера только мешал бы проверке препятствий и провоцировал
            // выталкивание персонажа физикой.

            // Визуал: примитив-сфера. Позже заменим на планету из LowPolyAlienWorld.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<SphereCollider>());
            visual.transform.SetParent(planetObject.transform, false);
            visual.transform.localScale = Vector3.one * PlanetRadius * 2f;

            var renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return planet;
        }

        private static GameObject CreatePlayer(Planet planet, out PlayerMotor motor)
        {
            var playerObject = new GameObject("Player");

            // Визуал: капсула без коллайдера. Коллайдер персонажу не нужен —
            // препятствия проверяются SphereCast'ом в самом моторе.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(playerObject.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            // Ставим на «северный полюс»: так сразу видно, что верх персонажа — от центра планеты.
            var up = Vector3.up;
            playerObject.transform.position = planet.SurfacePoint(up) + up * 0.02f;
            playerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

            motor = playerObject.AddComponent<PlayerMotor>();
            return playerObject;
        }

        private static GameObject CreateCamera(Planet planet, out PlayerCameraRig rig)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2000f;

            cameraObject.AddComponent<AudioListener>();
            rig = cameraObject.AddComponent<PlayerCameraRig>();

            return cameraObject;
        }

        private static void CreateContent(Planet planet)
        {
            var contentObject = new GameObject("Planet Content");
            var generator = contentObject.AddComponent<PlanetContentGenerator>();
            generator.Configure(planet);
        }

        /// <summary>
        /// Проверочный рельеф: холмы и стена. Нужен, чтобы увидеть, что движение
        /// учитывает высоту грунта, уклон и ступени, а не только гладкую сферу.
        /// </summary>
        private static void CreateTerrainTest(Planet planet)
        {
            var root = new GameObject("Terrain Test");

            // Три холма в разных точках, чтобы не все сразу на пути к экватору.
            var hillDirections = new[]
            {
                new Vector3(0.6f, 0.55f, 0.58f),
                new Vector3(-0.8f, 0.3f, 0.5f),
                new Vector3(0.1f, -0.2f, 0.97f)
            };

            for (var i = 0; i < hillDirections.Length; i++)
            {
                var direction = hillDirections[i].normalized;
                var position = planet.Center + direction * (planet.Radius - 3f);

                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = $"Hill {i + 1}";
                hill.transform.SetParent(root.transform, false);
                hill.transform.position = position;
                hill.transform.localScale = Vector3.one * 14f;
            }

            // Стена: проверяем скольжение вдоль препятствия.
            var wallDirection = new Vector3(0.9f, 0.1f, 0.42f).normalized;
            var wallPosition = planet.Center + wallDirection * planet.Radius;

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(root.transform, false);
            wall.transform.position = wallPosition + wallDirection * 1.6f;
            wall.transform.rotation = Quaternion.LookRotation(wallDirection, Vector3.up) *
                                     Quaternion.Euler(0f, 90f, 0f);
            wall.transform.localScale = new Vector3(6f, 3.2f, 0.6f);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            const string folder = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            const string path = folder + "/TestPlanet.unity";
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
        }
    }
}
