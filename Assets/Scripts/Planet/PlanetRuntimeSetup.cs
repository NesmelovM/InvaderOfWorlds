using System;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Собирает планету при старте сцены: случайный сид, рельеф, постройки,
    /// растительность, стены поселений и арена босса.
    ///
    /// Раньше всё это лежало в сцене и было одинаковым в каждом забеге.
    /// Теперь каждая планета собирается заново — при переходе «дальше» меняется сид
    /// и сложность, а раскладка получается другой.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PlanetRuntimeSetup : MonoBehaviour
    {
        [SerializeField] private Planet _planet;
        [SerializeField] private PlanetContentGenerator _generator;
        [SerializeField] private Transform _player;
        [SerializeField] private SettlementSpawner _settlementSpawner;
        [Tooltip("Порядок биомов: какой из них ставится, зависит от числа пройденных порталов")]
        [SerializeField] private BiomeCatalog _catalog;

        private BiomeDefinition _biome;

        [Header("Рельеф")]
        [SerializeField, Min(0)] private int _hills = 4;
        [SerializeField, Min(1f)] private float _hillRadius = 10f;
        [SerializeField, Min(0.5f)] private float _hillHeight = 3f;

        [Header("Растительность и камни")]
        [Tooltip("Крупные объекты: башни, шатёр, большие камни. Они преграждают путь")]
        [SerializeField, Min(0)] private int _bigPropCount = 90;
        [Tooltip("Мелочь: грибы и мелкие камни. Сквозь них проходим")]
        [SerializeField, Min(0)] private int _smallPropCount = 600;

        private static readonly string[] BigPropPaths =
        {
            "Props/rock-pillar", "Props/rock-sharp", "Props/stone-oval",
            "Props/tower-castle_round", "Props/tower-medieval-wood",
            "Props/barrel", "Props/tent-circus-big"
        };

        private static readonly string[] SmallPropPaths =
        {
            "Props/rocks-small", "Props/stone-pointy",
            "Props/mushroom-boletus", "Props/mushroom-toadstool"
        };

        public void Configure(Planet planet, PlanetContentGenerator generator, Transform player,
            SettlementSpawner settlementSpawner, BiomeCatalog catalog = null)
        {
            _planet = planet;
            _generator = generator;
            _player = player;
            _settlementSpawner = settlementSpawner;

            if (catalog != null)
                _catalog = catalog;
        }

        private void Start()
        {
            if (_planet == null || _generator == null)
                return;

            var seed = RunState.Seed == 0 ? 20261028 : RunState.Seed;
            var random = new System.Random(seed);

            _biome = _catalog != null ? _catalog.ForPlanet(RunState.PlanetIndex) : null;

            _generator.SetSeed(seed);
            _generator.Generate();

            ApplyBiomeColors();
            PlacePlayer();
            SpawnHills(random);
            SpawnBigProps(random);
            SpawnSmallProps(random);
            SpawnSettlementWalls(random);
            SpawnBossArena();
        }

        /// <summary>Цвета поверхности, холмов и неба берутся из биома.</summary>
        private void ApplyBiomeColors()
        {
            if (_biome == null)
                return;

            var visual = _planet.GetComponentInChildren<Renderer>();

            if (visual != null)
                visual.material.color = _biome.GroundColor;

            var camera = Camera.main;

            if (camera != null)
                camera.backgroundColor = _biome.SkyColor;
        }

        private void PlacePlayer()
        {
            if (_player == null)
                return;

            var point = _generator.PlayerSpawnPoint;
            var up = _planet.UpAt(point);

            _player.position = _planet.Center + up * (_planet.Radius + 0.02f);
            _player.rotation = Quaternion.FromToRotation(Vector3.up, up);
        }

        /// <summary>Холмы-куполы: меш повторяет кривизну планеты.</summary>
        private void SpawnHills(System.Random random)
        {
            for (var i = 0; i < _hills; i++)
            {
                var direction = RandomDirection(random);

                var hill = new GameObject($"Hill {i + 1}");
                var filter = hill.AddComponent<MeshFilter>();
                filter.sharedMesh = SurfaceDomeMesh.Create(_planet.Radius, _hillRadius, _hillHeight);

                hill.AddComponent<MeshRenderer>().sharedMaterial =
                    EnemyFactory.GetMaterial(_biome != null ? _biome.HillColor : new Color(0.42f, 0.38f, 0.34f));

                var collider = hill.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;

                hill.transform.position = _planet.Center;
                hill.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            }
        }

        /// <summary>
        /// Крупные объекты: башни, шатёр, большие камни. Коллайдеры оставляем —
        /// через них нельзя пройти, поэтому мир ощущается плотным.
        /// </summary>
        private void SpawnBigProps(System.Random random)
        {
            if (_biome != null && _biome.Big.Count > 0)
            {
                SpawnProps(random, _biome.Big, _biome.BigProps, keepColliders: true);
                return;
            }

            SpawnPropsFromResources(random, BigPropPaths, _bigPropCount, keepColliders: true);
        }

        /// <summary>Мелочь: грибы и мелкие камни. Их много, сквозь них проходим.</summary>
        private void SpawnSmallProps(System.Random random)
        {
            if (_biome != null && _biome.Small.Count > 0)
            {
                SpawnProps(random, _biome.Small, _biome.SmallProps, keepColliders: false);
                return;
            }

            SpawnPropsFromResources(random, SmallPropPaths, _smallPropCount, keepColliders: false);
        }

        private void SpawnProps(System.Random random, System.Collections.Generic.List<GameObject> prefabs,
            int count, bool keepColliders)
        {
            var root = new GameObject(keepColliders ? "Props" : "Small Props").transform;

            for (var i = 0; i < count; i++)
            {
                var prefab = prefabs[random.Next(prefabs.Count)];

                if (prefab == null)
                    continue;

                PlaceProp(prefab, random, root, keepColliders);
            }
        }

        private void SpawnPropsFromResources(System.Random random, string[] paths, int count, bool keepColliders)
        {
            var root = new GameObject(keepColliders ? "Props" : "Small Props").transform;

            for (var i = 0; i < count; i++)
            {
                var prefab = Resources.Load<GameObject>(paths[random.Next(paths.Length)]);

                if (prefab == null)
                    continue;

                PlaceProp(prefab, random, root, keepColliders);
            }
        }

        private void PlaceProp(GameObject prefab, System.Random random, Transform root, bool keepColliders)
        {
            var direction = RandomDirection(random);
            var up = direction.normalized;
            var position = _planet.Center + up * _planet.Radius;

            var prop = Instantiate(prefab, position, Quaternion.identity, root);

            if (!keepColliders)
            {
                foreach (var collider in prop.GetComponentsInChildren<Collider>(true))
                    Destroy(collider);
            }

            var scale = keepColliders ? 1f : 0.5f + (float)random.NextDouble() * 0.7f;
            prop.transform.localScale *= scale;

            if (keepColliders)
                FitBigPropHeight(prop, random);

            prop.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) *
                                     Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

            // Крупные объекты утапливаем на метр: стоят устойчиво и не висят краем на склоне.
            var sink = keepColliders ? 1f : 0f;
            var lowest = LowestPointAlong(prop, up);
            prop.transform.position += up * (-lowest - sink);
        }

        /// <summary>
        /// Крупные объекты не должны быть выше 15 метров: если префаб больше,
        /// масштаб подбирается так, чтобы высота попала в 10–15 м.
        /// Мелкие префабы не растягиваем — бочка не должна стать десятиметровой.
        /// </summary>
        private static void FitBigPropHeight(GameObject prop, System.Random random)
        {
            var height = MeasureHeight(prop);

            if (height <= 15f || height < 0.01f)
                return;

            var target = 10f + (float)random.NextDouble() * 5f;
            prop.transform.localScale *= target / height;
        }

        private static float MeasureHeight(GameObject prop)
        {
            var renderers = prop.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return 0f;

            var bounds = renderers[0].bounds;

            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.size.y;
        }

        /// <summary>Самая низкая точка объекта относительно плоскости, перпендикулярной up.</summary>
        private static float LowestPointAlong(GameObject prop, Vector3 up)
        {
            var renderers = prop.GetComponentsInChildren<Renderer>(true);
            var lowest = float.MaxValue;

            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;

                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                    var projection = Vector3.Dot(corner - prop.transform.position, up);
                    lowest = Mathf.Min(lowest, projection);
                }
            }

            return lowest < float.MaxValue ? lowest : 0f;
        }

        /// <summary>Кольцо стен вокруг каждого поселения — прорыв остаётся задачей.</summary>
        private void SpawnSettlementWalls(System.Random random)
        {
            var root = new GameObject("Settlement Walls").transform;
            var material = EnemyFactory.GetMaterial(new Color(0.52f, 0.48f, 0.43f));
            const int segments = 6;

            foreach (var settlement in _generator.Settlements)
            {
                var up = _planet.UpAt(settlement.Center);

                for (var i = 0; i < segments; i++)
                {
                    var angle = 360f / segments * i;
                    var offset = Quaternion.AngleAxis(angle, up) * Vector3.forward * (settlement.Radius * 0.6f);
                    var position = settlement.Center + Vector3.ProjectOnPlane(offset, up);
                    var direction = _planet.UpAt(position);

                    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Settlement Wall";
                    wall.transform.SetParent(root, false);
                    wall.transform.position = _planet.Center + direction * (_planet.Radius + 1.2f);
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

        /// <summary>Босс и портал в самой дальней точке от входа.</summary>
        private void SpawnBossArena()
        {
            var direction = _planet.UpAt(_generator.BossPortalPoint);
            var root = new GameObject("Boss Arena").transform;

            var portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "Exit Portal";
            portal.transform.SetParent(root, false);
            portal.transform.position = _planet.Center + direction * (_planet.Radius + 0.12f);
            portal.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            portal.transform.localScale = new Vector3(8f, 0.08f, 8f);
            Destroy(portal.GetComponent<Collider>());
            portal.GetComponent<Renderer>().sharedMaterial =
                EnemyFactory.GetMaterial(new Color(0.45f, 0.12f, 0.12f));

            var boss = EnemyFactory.SpawnBoss(_planet, _player,
                _planet.Center + direction * (_planet.Radius + 0.02f), root);

            var portalComponent = portal.AddComponent<ExitPortal>();
            portalComponent.Configure(boss.GetComponent<Health>(), _player);

            for (var i = 0; i < 3; i++)
            {
                var angle = 120f * i;
                var offset = Quaternion.AngleAxis(angle, direction) * Vector3.forward * 6f;
                var around = _planet.Center + direction * _planet.Radius + Vector3.ProjectOnPlane(offset, direction);

                if (SpawnPlacement.TryFindFreeGroundPosition(_planet, around, 3f, 12, out var position, out _, 2f))
                {
                    EnemyFactory.Spawn(_planet, _player, position, new Color(0.75f, 0.30f, 0.20f), 60f, root);
                }
            }
        }

        private static Vector3 RandomDirection(System.Random random)
        {
            var z = random.NextDouble() * 2.0 - 1.0;
            var theta = random.NextDouble() * Mathf.PI * 2.0;
            var r = Math.Sqrt(1.0 - z * z);

            return new Vector3(
                (float)(r * Mathf.Cos((float)theta)),
                (float)z,
                (float)(r * Mathf.Sin((float)theta)));
        }
    }
}
