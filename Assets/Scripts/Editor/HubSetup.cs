using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Собирает сцену хаба как экран подготовки: статичная камера смотрит на башню,
    /// меню — обычный GUI, персонажем управлять не нужно.
    ///
    /// Башня, кристаллы и золото создаются в рантайме компонентом HubVisuals
    /// и растут по мере покупок.
    /// </summary>
    public static class HubSetup
    {
        private const string ScenePath = "Assets/Scenes/Hub.unity";

        [MenuItem("Invader of Worlds/Собрать хаб")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            // Землю строит HubBackdrop в цвете выбранного биома,
            // поэтому отдельная тёмная плоскость здесь не нужна.
            var cameraObject = CreateCamera();
            CreateMage();
            CreatePortal();

            var hubObject = new GameObject("Hub");
            var director = hubObject.AddComponent<HubDirector>();
            hubObject.AddComponent<HubVisuals>();

            var backdrop = hubObject.AddComponent<HubBackdrop>();
            backdrop.Configure(cameraObject.GetComponent<Camera>());

            director.Configure(FindCatalog(), backdrop);

            SaveScene(scene);
            Debug.Log("[Invader of Worlds] Хаб собран: башня из пака, маг у входа, пейзаж биома за башней.");
        }

        /// <summary>Маг стоит у подножия башни — фигура игрока в хабе, рядом зона отдыха.</summary>
        private static void CreateMage()
        {
            var mage = new GameObject("Mage");
            mage.transform.position = new Vector3(2.6f, 0f, -8.6f);
            mage.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Тёмная мантия: модель из пака одноцветная, поэтому красим её сами.
            // Цвет через компонент — материал из редактора в сцене не сохраняется.
            CharacterModel.Attach(mage, "Characters/man-cult", 2.2f, 0f);
            mage.AddComponent<ModelTint>().Configure(new Color(0.16f, 0.07f, 0.23f));

            CreateRestArea(new Vector3(7.2f, 0f, -6.2f));
        }

        /// <summary>
        /// Портал в хабе: вертикальное зеркало с искрами и светом.
        /// Стоит справа от башни, чтобы не заслонять мага, но сразу бросается в глаза.
        /// </summary>
        private static void CreatePortal()
        {
            var color = new Color(0.30f, 0.85f, 0.95f);
            var root = new GameObject("Hub Portal");
            root.transform.position = new Vector3(-7.5f, 0f, -1.5f);
            root.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

            // Диск повёрнут вертикально: его нормаль смотрит на камеру.
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Portal Disc";
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(7f, 0.12f, 7f);

            Object.DestroyImmediate(disc.GetComponent<Collider>());

            var material = new Material(Shader.Find("Standard"))
            {
                name = "Hub Portal",
                color = color
            };

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.6f);
            disc.GetComponent<Renderer>().sharedMaterial = material;

            var spin = disc.AddComponent<PortalSpin>();
            spin.Configure(18f, color);

            // Искры поднимаются вверх по миру: портал вертикальный.
            PortalVfx.Attach(root.transform, color, Vector3.up);

            // Каменное кольцо-основание, чтобы диск не висел в воздухе.
            var baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "Portal Base";
            baseRing.transform.SetParent(root.transform, false);
            baseRing.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            baseRing.transform.localScale = new Vector3(8.4f, 0.15f, 8.4f);
            Object.DestroyImmediate(baseRing.GetComponent<Collider>());
            baseRing.GetComponent<Renderer>().sharedMaterial =
                EnemyFactory.GetMaterial(new Color(0.30f, 0.32f, 0.36f));
        }

        /// <summary>Шезлонг, полотенце, сумка и столик — башня мага, но отдыхать тоже надо.</summary>
        private static void CreateRestArea(Vector3 origin)
        {
            var root = new GameObject("Rest Area").transform;
            root.position = origin;

            var items = new[]
            {
                ("Hub/lounger", new Vector3(0f, 0f, 0f), 175f),
                ("Hub/towel-beach", new Vector3(1.6f, 0f, 0.9f), 15f),
                ("Hub/beach-bag", new Vector3(-1.7f, 0f, 0.8f), 40f),
                ("Hub/ball-beach", new Vector3(-0.6f, 0.25f, 2.1f), 0f),
                ("Hub/chair-wood", new Vector3(2.6f, 0f, -0.4f), 210f)
            };

            foreach (var (path, offset, yaw) in items)
            {
                var prefab = Resources.Load<GameObject>(path);

                if (prefab == null)
                {
                    Debug.LogWarning($"[Hub] Не найден префаб {path}");
                    continue;
                }

                var instance = Object.Instantiate(prefab, root);
                instance.name = prefab.name;
                instance.transform.localPosition = offset;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                // Мебель не должна быть препятствием в хабе.
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(collider);
            }
        }

        private static BiomeCatalog FindCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:BiomeCatalog");

            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<BiomeCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.shadows = LightShadows.Soft;
            // Свет со стороны камеры: иначе фасады башни и мага остаются в тени.
            lightObject.transform.rotation = Quaternion.Euler(48f, 20f, 0f);

            RenderSettings.ambientLight = new Color(0.22f, 0.24f, 0.30f);
        }

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 12f;

            var renderer = ground.GetComponent<Renderer>();
            renderer.sharedMaterial = GetMaterial("HubGround", new Color(0.13f, 0.14f, 0.18f));
        }

        private static GameObject CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 7.5f, -31f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 5.5f, 3f) - cameraObject.transform.position);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.07f);

            cameraObject.AddComponent<AudioListener>();
            return cameraObject;
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
