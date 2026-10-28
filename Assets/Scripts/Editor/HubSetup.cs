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
            CreateGround();
            CreateCamera();

            var hubObject = new GameObject("Hub");
            hubObject.AddComponent<HubDirector>();
            hubObject.AddComponent<HubVisuals>();
            hubObject.AddComponent<RunHud>();

            SaveScene(scene);
            Debug.Log("[Invader of Worlds] Хаб собран: статичная камера, магазин и портал. " +
                      "Персонажем управлять не нужно — кликайте по кнопкам.");
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.85f, 0.88f, 1f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(42f, 145f, 0f);

            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.38f);
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

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 9f, -24f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 7f, 0f) - cameraObject.transform.position);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.07f);

            cameraObject.AddComponent<AudioListener>();
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
