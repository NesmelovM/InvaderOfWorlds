using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Собирает сцену планеты.
    ///
    /// В сцене лежит только каркас: планета, игрок, камера и системы забега.
    /// Рельеф, поселения, стены, босс, портал и растительность создаются при запуске
    /// компонентом PlanetRuntimeSetup по случайному сиду — поэтому каждая планета
    /// выглядит иначе, а при переходе «дальше» меняется целиком.
    /// </summary>
    public static class TestPlanetSetup
    {
        private const float PlanetRadius = 90f;
        private const string ScenePath = "Assets/Scenes/TestPlanet.unity";

        [MenuItem("Invader of Worlds/Собрать тестовую планету")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightObject = CreateLight();
            var planet = CreatePlanet();
            var player = CreatePlayer(planet, out var motor);
            var cameraObject = CreateCamera(out var cameraRig);
            var generator = CreateContent(planet);

            // Солнце следует за игроком: иначе на обратной стороне планеты всегда ночь.
            var sun = lightObject.AddComponent<SunFollower>();
            sun.Configure(player.transform, planet);

            CreateCombat(planet, player, cameraObject, generator);
            CreateRuntimeSetup(planet, player, generator);

            motor.Configure(planet, cameraObject.transform);
            cameraRig.Configure(player.transform, planet);

            SaveScene(scene);

            Selection.activeGameObject = player;
            Debug.Log("[Invader of Worlds] Планета собрана. Рельеф, поселения, босс и портал " +
                      "создаются при запуске по случайному сиду.");
        }

        private static GameObject CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return lightObject;
        }

        private static Planet CreatePlanet()
        {
            var planetObject = new GameObject("Planet");
            var planet = planetObject.AddComponent<Planet>();
            planet.SetRadius(PlanetRadius);

            var visual = new GameObject("Visual");
            var filter = visual.AddComponent<MeshFilter>();
            var renderer = visual.AddComponent<MeshRenderer>();
            filter.sharedMesh = SphereMesh.Create(PlanetRadius);
            visual.transform.SetParent(planetObject.transform, false);

            renderer.sharedMaterial = GetOrCreateMaterial("TestPlanet", new Color(0.45f, 0.47f, 0.50f));
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

            // Игрок — тот же маг, что стоит у башни в хабе: одна фигура на всю игру.
            // Цвет задаём компонентом: материал, созданный в редакторе, в сцене не сохраняется.
            var model = CharacterModel.Attach(playerObject, "Characters/man-cult", 1.95f, 0f);

            var tint = playerObject.AddComponent<ModelTint>();
            tint.Configure(new Color(0.16f, 0.07f, 0.23f));

            if (model == null)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
                visual.transform.SetParent(playerObject.transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            }

            var up = Vector3.up;
            playerObject.transform.position = planet.SurfacePoint(up) + up * 0.02f;
            playerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

            motor = playerObject.AddComponent<PlayerMotor>();
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

            cameraObject.AddComponent<AudioListener>();
            rig = cameraObject.AddComponent<PlayerCameraRig>();

            return cameraObject;
        }

        private static PlanetContentGenerator CreateContent(Planet planet)
        {
            var contentObject = new GameObject("Planet Content");
            var generator = contentObject.AddComponent<PlanetContentGenerator>();
            generator.Configure(planet);
            return generator;
        }

        /// <summary>Игрок, оружие, здоровье, спавнер поселений и системы забега.</summary>
        private static void CreateCombat(Planet planet, GameObject player, GameObject cameraObject,
            PlanetContentGenerator generator)
        {
            var health = player.AddComponent<Health>();
            health.Configure(100f);
            health.SetDestroyOnDeath(false);

            player.AddComponent<HitFlash>();
            player.AddComponent<PlayerDamageFeedback>();
            player.AddComponent<PlayerHud>();

            var attack = player.AddComponent<WeaponSystem>();
            attack.Configure(cameraObject.transform, planet);

            attack.TryEquip(new WeaponSlot
            {
                Kind = WeaponKind.Melee, Damage = 34f, Cooldown = 0.5f, Range = 3.2f
            });

            attack.TryEquip(new WeaponSlot
            {
                Kind = WeaponKind.Bolt, Damage = 18f, Cooldown = 1.1f, Range = 11f, ProjectileSpeed = 26f
            });

            var spawnerObject = new GameObject("Settlement Spawner");
            var spawner = spawnerObject.AddComponent<SettlementSpawner>();
            spawner.Configure(planet, generator, player.transform);

            var runObject = new GameObject("Run");
            var director = runObject.AddComponent<RunDirector>();
            director.Configure(health);
            runObject.AddComponent<RunBootstrap>();

            var level = runObject.AddComponent<RunLevel>();
            level.Configure(health, attack, player.transform);

            var hud = runObject.AddComponent<RunHud>();
            hud.Configure(level);

            var pause = runObject.AddComponent<PauseMenu>();
            pause.Configure(level, health);
        }

        /// <summary>Сборка планеты при старте: рельеф, постройки, босс и портал.</summary>
        private static void CreateRuntimeSetup(Planet planet, GameObject player, PlanetContentGenerator generator)
        {
            var setupObject = new GameObject("Planet Runtime");
            var setup = setupObject.AddComponent<PlanetRuntimeSetup>();

            var spawner = Object.FindAnyObjectByType<SettlementSpawner>();

            // Каталог биомов подхватываем автоматически, если он уже создан.
            var catalogGuids = AssetDatabase.FindAssets("t:BiomeCatalog");
            BiomeCatalog catalog = null;

            if (catalogGuids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
                catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalog>(path);
            }

            setup.Configure(planet, generator, player.transform, spawner, catalog);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
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
