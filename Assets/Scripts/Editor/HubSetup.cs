using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Собирает сцену хаба: небольшая планета, площадка башни, круг телепорта,
    /// игрок и магазин улучшений. Хаб — отдельная сцена, чтобы забег и подготовка
    /// не мешали друг другу.
    /// </summary>
    public static class HubSetup
    {
        private const float PlanetRadius = 35f;
        private const string ScenePath = "Assets/Scenes/Hub.unity";

        [MenuItem("Invader of Worlds/Собрать хаб")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            var planet = CreatePlanet();
            var player = CreatePlayer(planet, out var motor);
            var cameraObject = CreateCamera(out var cameraRig);

            motor.Configure(planet, cameraObject.transform);
            cameraRig.Configure(player.transform, planet);

            CreateTower(planet);
            CreatePortalRing(planet);

            var hubObject = new GameObject("Hub");
            hubObject.AddComponent<HubDirector>();
            hubObject.AddComponent<RunHud>();

            SaveScene(scene);
            Selection.activeGameObject = player;

            Debug.Log("[Invader of Worlds] Хаб собран. Ходите WASD, магазин слева сверху, " +
                      "в портал — кнопка или E.");
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(0.75f, 0.82f, 1f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(35f, 140f, 0f);
        }

        private static Planet CreatePlanet()
        {
            var planetObject = new GameObject("Planet");
            var planet = planetObject.AddComponent<Planet>();
            planet.SetRadius(PlanetRadius);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<SphereCollider>());
            visual.transform.SetParent(planetObject.transform, false);
            visual.transform.localScale = Vector3.one * PlanetRadius * 2f;

            var renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial("HubPlanet", new Color(0.22f, 0.26f, 0.34f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return planet;
        }

        private static GameObject CreatePlayer(Planet planet, out PlayerMotor motor)
        {
            var playerObject = new GameObject("Player");
            playerObject.tag = "Player";

            var body = playerObject.AddComponent<CapsuleCollider>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.9f, 0f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(playerObject.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            var up = Vector3.up;
            playerObject.transform.position = planet.SurfacePoint(up) + up * 0.02f;
            playerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

            motor = playerObject.AddComponent<PlayerMotor>();

            var health = playerObject.AddComponent<Health>();
            health.Configure(100f);
            health.SetDestroyOnDeath(false);

            playerObject.AddComponent<PlayerHud>();

            return playerObject;
        }

        private static GameObject CreateCamera(out PlayerCameraRig rig)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2000f;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.08f);

            cameraObject.AddComponent<AudioListener>();
            rig = cameraObject.AddComponent<PlayerCameraRig>();

            return cameraObject;
        }

        /// <summary>Площадка башни: широкая плита рядом с точкой появления.</summary>
        private static void CreateTower(Planet planet)
        {
            var direction = Quaternion.AngleAxis(14f, Vector3.right) * Vector3.up;
            var position = planet.Center + direction * (planet.Radius + 0.5f);

            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Tower Platform";
            platform.transform.position = position;
            platform.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            platform.transform.localScale = new Vector3(16f, 0.5f, 16f);
            platform.GetComponent<Renderer>().sharedMaterial = GetMaterial("HubStone", new Color(0.55f, 0.52f, 0.48f));

            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Tower Column";
            column.transform.position = planet.Center + direction * (planet.Radius + 4f);
            column.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            column.transform.localScale = new Vector3(6f, 4f, 6f);
            column.GetComponent<Renderer>().sharedMaterial = GetMaterial("HubStone", new Color(0.55f, 0.52f, 0.48f));
        }

        /// <summary>Круг телепорта — просто светящийся блин у ног игрока.</summary>
        private static void CreatePortalRing(Planet planet)
        {
            var direction = Vector3.up;
            var position = planet.Center + direction * (planet.Radius + 0.08f);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Teleport Ring";
            ring.transform.position = position;
            ring.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            ring.transform.localScale = new Vector3(7f, 0.05f, 7f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.GetComponent<Renderer>().sharedMaterial = GetMaterial("HubPortal", new Color(0.25f, 0.85f, 0.95f));
        }

        private static Material GetMaterial(string name, Color color)
        {
            const string folder = "Assets/Materials/Test";

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Materials", "Test");

            var path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            const string folder = "Assets/Scenes";

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            var scenes = EditorBuildSettings.scenes;

            foreach (var entry in scenes)
            {
                if (entry.path == ScenePath)
                    return;
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            updated[0] = new EditorBuildSettingsScene(ScenePath, true);
            System.Array.Copy(scenes, 0, updated, 1, scenes.Length);
            EditorBuildSettings.scenes = updated;
        }
    }
}
