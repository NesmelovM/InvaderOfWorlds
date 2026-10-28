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
            var generator = CreateContent(planet);

            // Генерируем расстановку сразу, чтобы босса и портал можно было
            // поставить в сцене, а не только в рантайме.
            generator.Generate();

            CreateTerrainTest(planet);
            CreateCombat(planet, player, cameraObject, generator);
            CreateBossEncounter(planet, player, generator);
            CreateSettlementWalls(planet, generator);

            // Связываем игрока с планетой и камерой.
            motor.Configure(planet, cameraObject.transform);
            cameraRig.Configure(player.transform, planet);

            SaveScene(scene);

            Selection.activeGameObject = player;
            Debug.Log($"[Invader of Worlds] Тестовая сцена собрана. Радиус планеты {PlanetRadius} м, " +
                      $"игрок на «северном полюсе». WASD, мышь, Space — прыжок, Shift — бег, ЛКМ — удар. " +
                      $"Слева ступень, справа стена, рядом два врага, холмы далеко.");
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

            // Визуал: гладкая сфера, а не примитив Unity — на 90 метрах примитив гранёный.
            var visual = new GameObject("Visual");
            var filter = visual.AddComponent<MeshFilter>();
            var meshRenderer = visual.AddComponent<MeshRenderer>();
            filter.sharedMesh = SphereMesh.Create(PlanetRadius);
            visual.transform.SetParent(planetObject.transform, false);

            var renderer = meshRenderer;
            renderer.sharedMaterial = GetOrCreateMaterial("TestPlanet", new Color(0.45f, 0.47f, 0.50f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return planet;
        }

        private static GameObject CreatePlayer(Planet planet, out PlayerMotor motor)
        {
            var playerObject = new GameObject("Player");
            playerObject.tag = "Player";

            // Коллайдер нужен не для физики, а чтобы другие персонажи «видели» игрока
            // при расталкивании. Движение по-прежнему аналитическое.
            var body = playerObject.AddComponent<CapsuleCollider>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.9f, 0f);

            // Визуал: модель рыцаря из пака, а если её нет — капсула.
            var model = CharacterModel.Attach(playerObject, "Characters/man-knight", 1.8f, 0f,
                new Color(0.92f, 0.84f, 0.52f));

            if (model == null)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
                visual.transform.SetParent(playerObject.transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            }

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

        private static PlanetContentGenerator CreateContent(Planet planet)
        {
            var contentObject = new GameObject("Planet Content");
            var generator = contentObject.AddComponent<PlanetContentGenerator>();
            generator.Configure(planet);
            return generator;
        }

        /// <summary>
        /// Боевая часть: здоровье и удар игрока, заселение поселений и пара врагов
        /// рядом с точкой появления, чтобы бой можно было проверить сразу.
        /// </summary>
        private static void CreateCombat(Planet planet, GameObject player, GameObject cameraObject,
            PlanetContentGenerator generator)
        {
            var health = player.AddComponent<Health>();
            health.Configure(100f);

            // Игрок остаётся в сцене после смерти: на нём камера, меню и итоги забега.
            health.SetDestroyOnDeath(false);

            player.AddComponent<HitFlash>();
            player.AddComponent<PlayerDamageFeedback>();
            player.AddComponent<PlayerHud>();

            var attack = player.AddComponent<WeaponSystem>();
            attack.Configure(cameraObject.transform, planet);

            // Два стартовых оружия: удар вблизи и снаряд по дальней цели.
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

            // Управление забегом: смерть, итоги, перезапуск и счётчик наград.
            var runObject = new GameObject("Run");
            var director = runObject.AddComponent<RunDirector>();
            director.Configure(health);
            runObject.AddComponent<RunHud>();
            runObject.AddComponent<RunBootstrap>();

            var root = new GameObject("Test Enemies");
            var enemyColor = new Color(0.85f, 0.25f, 0.20f);

            // По бокам от точки появления. Позиция проверяется на пересечения,
            // чтобы враг не оказался внутри ступени, стены или холма.
            SpawnTestEnemy(planet, player.transform, root.transform, 3.5f, enemyColor, 50f);
            SpawnTestEnemy(planet, player.transform, root.transform, -3.5f, enemyColor, 50f);

            // Дальний враг в 14 метрах: держит дистанцию и стреляет.
            SpawnTestRanged(planet, player.transform, root.transform, 9f);

            // Элита в 19 метрах впереди: там нет стены, её видно от точки появления.
            SpawnTestElite(planet, player.transform, root.transform, 12f);
        }

        private static void SpawnTestElite(Planet planet, Transform player, Transform parent, float angle)
        {
            var around = planet.SurfacePoint(Quaternion.AngleAxis(angle, Vector3.right) * Vector3.up);

            if (!SpawnPlacement.TryFindFreeGroundPosition(planet, around, 4f, 20, out var position, out _, 2.4f))
            {
                Debug.LogWarning("[TestPlanetSetup] Не нашёл свободного места для элиты, ставлю как есть.");
                position = around;
            }

            EnemyFactory.SpawnElite(planet, player, position, parent);
        }

        /// <summary>
        /// Босс и портал выхода в самой дальней точке планеты: до них надо дойти,
        /// а портал открывается только после смерти босса.
        /// </summary>
        private static void CreateBossEncounter(Planet planet, GameObject player, PlanetContentGenerator generator)
        {
            var direction = planet.UpAt(generator.BossPortalPoint);
            var root = new GameObject("Boss Arena");

            var portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "Exit Portal";
            portal.transform.SetParent(root.transform, false);
            portal.transform.position = planet.Center + direction * (planet.Radius + 0.12f);
            portal.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            portal.transform.localScale = new Vector3(8f, 0.08f, 8f);
            Object.DestroyImmediate(portal.GetComponent<Collider>());
            portal.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial("ExitPortal", new Color(0.45f, 0.12f, 0.12f));

            var boss = EnemyFactory.SpawnBoss(planet, player.transform,
                planet.Center + direction * (planet.Radius + 0.02f), root.transform);

            var portalComponent = portal.AddComponent<ExitPortal>();
            portalComponent.Configure(boss.GetComponent<Health>(), player.transform);

            // Стража вокруг портала.
            for (var i = 0; i < 3; i++)
            {
                var angle = 120f * i;
                var offset = Quaternion.AngleAxis(angle, direction) * Vector3.forward * 6f;
                var around = planet.Center + direction * planet.Radius + Vector3.ProjectOnPlane(offset, direction);

                if (SpawnPlacement.TryFindFreeGroundPosition(planet, around, 3f, 12, out var position, out _, 2f))
                    EnemyFactory.Spawn(planet, player.transform, position, new Color(0.75f, 0.30f, 0.20f), 60f,
                        root.transform);
            }
        }

        private static void SpawnTestRanged(Planet planet, Transform player, Transform parent, float angle)
        {
            var around = planet.SurfacePoint(Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up);

            if (!SpawnPlacement.TryFindFreeGroundPosition(planet, around, 4f, 20, out var position, out _))
            {
                Debug.LogWarning("[TestPlanetSetup] Не нашёл свободного места для лучника, ставлю как есть.");
                position = around;
            }

            EnemyFactory.SpawnRanged(planet, player, position, 40f, parent);
        }

        private static void SpawnTestEnemy(Planet planet, Transform player, Transform parent, float angle,
            Color color, float health)
        {
            var around = planet.SurfacePoint(Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up);

            if (!SpawnPlacement.TryFindFreeGroundPosition(planet, around, 4f, 20, out var position, out _))
            {
                Debug.LogWarning("[TestPlanetSetup] Не нашёл свободного места для тестового врага, ставлю как есть.");
                position = around;
            }

            EnemyFactory.Spawn(planet, player, position, color, health, parent);
        }

        /// <summary>
        /// Проверочный рельеф рядом с точкой появления: холм, ступень и стена.
        /// Нужен, чтобы увидеть, что движение учитывает высоту грунта, уклон и ступени,
        /// а не только гладкую сферу.
        /// </summary>
        private static void CreateTerrainTest(Planet planet)
        {
            var root = new GameObject("Terrain Test");
            var spawnUp = Vector3.up;

            // Ступень в 12 метрах слева: 30 см, должна проходиться без прыжка.
            CreateStep(root.transform, planet, Quaternion.AngleAxis(7.6f, Vector3.forward) * spawnUp);

            // Стена в 8 метрах справа: 3 метра высотой, должна не пускать и скользить.
            CreateWall(root.transform, planet, Quaternion.AngleAxis(-5f, Vector3.forward) * spawnUp);

            // Два холма далеко от точки появления: рядом с ней стоят враги,
            // и холм перекрывал бы их коллайдером.
            CreateMound(root.transform, planet, Quaternion.AngleAxis(40f, Vector3.right) * spawnUp);
            CreateMound(root.transform, planet, Quaternion.AngleAxis(-45f, Vector3.forward) * spawnUp);
        }

        /// <summary>
        /// Холм как настоящий купол на поверхности: меш повторяет сферу планеты,
        /// коллайдер — выпуклый меш. Неравномерный масштаб сферы не используется:
        /// Unity превращает такой коллайдер в огромный шар, на котором персонаж
        /// оказывается в десяти метрах над планетой.
        /// </summary>
        private static void CreateMound(Transform parent, Planet planet, Vector3 direction)
        {
            var mound = new GameObject("Hill");
            mound.transform.SetParent(parent, false);
            mound.transform.position = planet.Center;
            mound.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            var filter = mound.AddComponent<MeshFilter>();
            filter.sharedMesh = SurfaceDomeMesh.Create(planet.Radius, 10f, 3f);

            var renderer = mound.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial("TestHill", new Color(0.85f, 0.12f, 0.10f));

            var collider = mound.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = true;
        }

        /// <summary>Невысокая плита-ступень: проверяем подъём на 30 см.</summary>
        private static void CreateStep(Transform parent, Planet planet, Vector3 direction)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = "Step";
            step.transform.SetParent(parent, false);
            step.transform.position = planet.Center + direction * (planet.Radius + 0.15f);
            step.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            step.transform.localScale = new Vector3(6f, 0.3f, 3f);
            Paint(step, GetOrCreateMaterial("TestStep", new Color(0.95f, 0.45f, 0.05f)));
        }

        /// <summary>Стена: проверяем, что персонаж скользит вдоль неё, а не проходит насквозь.</summary>
        private static void CreateWall(Transform parent, Planet planet, Vector3 direction)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = planet.Center + direction * (planet.Radius + 1.5f);
            wall.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            wall.transform.localScale = new Vector3(6f, 3f, 0.5f);
            Paint(wall, GetOrCreateMaterial("TestWall", new Color(0.95f, 0.20f, 0.15f)));

            var health = wall.AddComponent<Health>();
            health.Configure(120f);
            wall.AddComponent<Destructible>();
        }

        /// <summary>
        /// Кольцо стен вокруг каждого поселения: прорыв через укрепления
        /// должен быть реальным действием, а не декорацией.
        /// </summary>
        private static void CreateSettlementWalls(Planet planet, PlanetContentGenerator generator)
        {
            var root = new GameObject("Settlement Walls");
            var material = GetOrCreateMaterial("WallStone", new Color(0.52f, 0.48f, 0.43f));
            const int segments = 6;

            foreach (var settlement in generator.Settlements)
            {
                var up = planet.UpAt(settlement.Center);

                for (var i = 0; i < segments; i++)
                {
                    var angle = 360f / segments * i;
                    var offset = Quaternion.AngleAxis(angle, up) * Vector3.forward * (settlement.Radius * 0.6f);
                    var position = settlement.Center + Vector3.ProjectOnPlane(offset, up);
                    var direction = planet.UpAt(position);

                    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Settlement Wall";
                    wall.transform.SetParent(root.transform, false);
                    wall.transform.position = planet.Center + direction * (planet.Radius + 1.2f);
                    wall.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction) *
                                              Quaternion.Euler(0f, angle, 0f);
                    wall.transform.localScale = new Vector3(5f, 2.4f, 0.5f);
                    wall.GetComponent<Renderer>().sharedMaterial = material;

                    var health = wall.AddComponent<Health>();
                    health.Configure(140f);
                    wall.AddComponent<Destructible>();
                }
            }
        }

        private static void Paint(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Материал-ассет, чтобы цвет пережил сохранение сцены.
        /// Красный нужен, чтобы объекты было видно на белой планете.
        /// </summary>
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
                var shader = Shader.Find("Standard");
                material = new Material(shader) { name = name };
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

            const string path = folder + "/TestPlanet.unity";
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();

            // Сцена должна быть в Build Settings, иначе перезапуск забега по R
            // попытается загрузить индекс -1 и ничего не произойдёт.
            var scenes = EditorBuildSettings.scenes;
            var alreadyRegistered = false;

            foreach (var entry in scenes)
            {
                if (entry.path != path)
                    continue;

                alreadyRegistered = true;
                break;
            }

            if (alreadyRegistered)
                return;

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            updated[0] = new EditorBuildSettingsScene(path, true);
            System.Array.Copy(scenes, 0, updated, 1, scenes.Length);
            EditorBuildSettings.scenes = updated;
        }
    }
}
