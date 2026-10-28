using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Пейзаж за башней мага: выбранный биом виден прямо из хаба.
    ///
    /// Строится из тех же префабов, что и планета, поэтому смена биома в меню
    /// сразу меняет вид за окном.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubBackdrop : MonoBehaviour
    {
        private const int BigProps = 14;
        private const int SmallProps = 36;

        [SerializeField] private Camera _camera;

        private Transform _root;
        private int _shownBiome = -1;

        public void Configure(Camera camera)
        {
            _camera = camera;
        }

        public void Show(BiomeDefinition biome)
        {
            if (_root != null)
                Destroy(_root.gameObject);

            _root = new GameObject("Biome Backdrop").transform;
            _root.SetParent(transform, false);

            if (biome == null)
                return;

            ApplySky(biome);
            BuildGround(biome);
            BuildHills(biome);
            BuildProps(biome);
        }

        private void ApplySky(BiomeDefinition biome)
        {
            var camera = _camera != null ? _camera : Camera.main;

            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = biome.SkyColor;
        }

        private void BuildGround(BiomeDefinition biome)
        {
            // Пологий «пол» за башней: слой земли в цвете биома.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Backdrop Ground";
            ground.transform.SetParent(_root, false);
            // Земля покрывает и передний план: тогда цвет биома виден по всей сцене,
            // а не только за башней.
            ground.transform.position = new Vector3(0f, -0.6f, 14f);
            ground.transform.localScale = new Vector3(130f, 1f, 95f);

            var collider = ground.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            ground.GetComponent<Renderer>().sharedMaterial =
                EnemyFactory.GetMaterial(biome.GroundColor * 0.75f);
        }

        private void BuildHills(BiomeDefinition biome)
        {
            var hills = new[]
            {
                new Vector3(-22f, 0f, 46f),
                new Vector3(6f, 0f, 58f),
                new Vector3(28f, 0f, 42f)
            };

            for (var i = 0; i < hills.Length; i++)
            {
                var hill = new GameObject($"Backdrop Hill {i}");
                hill.transform.SetParent(_root, false);
                hill.transform.position = hills[i];

                var filter = hill.AddComponent<MeshFilter>();
                filter.sharedMesh = SurfaceDomeMesh.Create(60f, 26f, 7f + i * 1.5f);

                hill.AddComponent<MeshRenderer>().sharedMaterial =
                    EnemyFactory.GetMaterial(biome.HillColor);
            }
        }

        private void BuildProps(BiomeDefinition biome)
        {
            var random = new System.Random(Progress.TotalPortals * 7919 + biome.Title.Length);

            PlaceBatch(biome.Big, BigProps, random, big: true);
            PlaceBatch(biome.Small, SmallProps, random, big: false);
        }

        private void PlaceBatch(List<GameObject> prefabs, int count, System.Random random, bool big)
        {
            if (prefabs == null || prefabs.Count == 0)
                return;

            for (var i = 0; i < count; i++)
            {
                var prefab = prefabs[random.Next(prefabs.Count)];

                if (prefab == null)
                    continue;

                // Держим свободный коридор по центру: башня и маг должны читаться.
                var side = random.Next(2) == 0 ? -1f : 1f;
                var x = side * (12f + (float)random.NextDouble() * 28f);
                var z = 14f + (float)random.NextDouble() * 41f;
                var prop = Instantiate(prefab, _root);
                prop.name = prefab.name;

                foreach (var collider in prop.GetComponentsInChildren<Collider>(true))
                    Destroy(collider);

                prop.transform.position = new Vector3(x, 0f, z);
                prop.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

                var target = big ? 7f + (float)random.NextDouble() * 4f : 0.6f + (float)random.NextDouble();
                FitHeight(prop, target);
            }
        }

        private static void FitHeight(GameObject prop, float target)
        {
            var min = float.MaxValue;
            var max = float.MinValue;

            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.bounds;
                min = Mathf.Min(min, bounds.min.y);
                max = Mathf.Max(max, bounds.max.y);
            }

            var height = max - min;

            if (height > 0.01f)
                prop.transform.localScale *= target / height;
        }
    }
}
