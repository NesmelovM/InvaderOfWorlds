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

            // Коллайдер планеты — сфера радиусом с сам шар, иначе персонаж провалится.
            var collider = planetObject.AddComponent<SphereCollider>();
            collider.radius = PlanetRadius;

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

            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 55f;
            controller.stepOffset = 0.4f;
            controller.skinWidth = 0.02f;

            // Визуал: капсула без собственного коллайдера, иначе он конфликтует с контроллером.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(playerObject.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            // Ставим на «северный полюс»: так сразу видно, что верх персонажа — от центра планеты.
            var up = Vector3.up;
            playerObject.transform.position = planet.SurfacePoint(up) + up * 1.2f;
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
