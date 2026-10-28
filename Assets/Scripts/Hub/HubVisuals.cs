using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Визуализация богатства и силы мага: башня растёт с купленными улучшениями,
    /// вокруг появляются кристаллы и груды золота.
    ///
    /// Всё строится из примитивов в рантайме — хаб не нуждается в ручной расстановке
    /// объектов и не ломается при пересборке сцены.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubVisuals : MonoBehaviour
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        private Transform _root;
        private int _shownLevels = -1;
        private int _shownGold = -1;
        private int _shownDamage = -1;
        private int _shownHealth = -1;
        private int _shownSpeed = -1;

        private void Start()
        {
            _root = new GameObject("Tower").transform;
            _root.SetParent(transform, false);
            Rebuild();
        }

        private void Update()
        {
            var levels = MetaProgress.DamageLevel + MetaProgress.HealthLevel + MetaProgress.SpeedLevel;

            if (levels == _shownLevels && RunSession.Gold == _shownGold &&
                MetaProgress.DamageLevel == _shownDamage && MetaProgress.HealthLevel == _shownHealth &&
                MetaProgress.SpeedLevel == _shownSpeed)
                return;

            Rebuild();
        }

        private void Rebuild()
        {
            _shownLevels = MetaProgress.DamageLevel + MetaProgress.HealthLevel + MetaProgress.SpeedLevel;
            _shownGold = RunSession.Gold;
            _shownDamage = MetaProgress.DamageLevel;
            _shownHealth = MetaProgress.HealthLevel;
            _shownSpeed = MetaProgress.SpeedLevel;

            Clear();
            BuildTower(_shownLevels);
            BuildCrystals(MetaProgress.DamageLevel);
            BuildCoins(RunSession.Gold);
        }

        private void Clear()
        {
            for (var i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        /// <summary>Башня: чем больше уровней, тем выше колонна и шире основание.</summary>
        private void BuildTower(int levels)
        {
            var baseRadius = 4f + 0.18f * levels;
            var columnRadius = 1.3f + 0.05f * levels;
            var columnHeight = 5f + 0.55f * levels;

            CreateCylinder("Base", new Vector3(0f, 0.25f, 0f), new Vector3(baseRadius * 2f, 0.5f, baseRadius * 2f),
                new Color(0.42f, 0.40f, 0.44f));

            CreateCylinder("Column", new Vector3(0f, 0.5f + columnHeight * 0.5f, 0f),
                new Vector3(columnRadius * 2f, columnHeight * 0.5f, columnRadius * 2f),
                new Color(0.55f, 0.50f, 0.46f));

            var top = 0.5f + columnHeight;
            CreateSphere("Spire", new Vector3(0f, top + 0.4f, 0f), Vector3.one * (0.9f + 0.12f * levels),
                new Color(0.85f, 0.75f, 0.35f));
        }

        /// <summary>Кристаллы силы: по одному за уровень урона, свечение растёт.</summary>
        private void BuildCrystals(int damageLevel)
        {
            if (damageLevel <= 0)
                return;

            for (var i = 0; i < damageLevel; i++)
            {
                var angle = 360f / Mathf.Max(1, damageLevel) * i;
                var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 4.2f);
                var height = 1.6f + 0.35f * i;

                var crystal = CreateSphere($"Crystal {i}", new Vector3(offset.x, height, offset.z),
                    Vector3.one * (0.45f + 0.08f * damageLevel), new Color(0.95f, 0.35f, 0.25f));

                var renderer = crystal.GetComponent<Renderer>();

                if (renderer != null && renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.EnableKeyword("_EMISSION");
                    renderer.sharedMaterial.SetColor("_EmissionColor", new Color(0.9f, 0.25f, 0.15f) * 0.6f);
                }
            }
        }

        /// <summary>Груды золота: появляются по мере накопления, россыпью у подножия.</summary>
        private void BuildCoins(int gold)
        {
            var coins = Mathf.Clamp(gold / 25, 0, 24);

            for (var i = 0; i < coins; i++)
            {
                var angle = i * 137f;
                var radius = 2.2f + (i % 5) * 0.6f;
                var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, radius);

                CreateCylinder($"Coin {i}", new Vector3(offset.x, 0.55f + (i % 3) * 0.08f, offset.z),
                    new Vector3(0.45f, 0.06f, 0.45f), new Color(0.95f, 0.78f, 0.25f));
            }
        }

        private GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var collider = go.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            Apply(go, color);
            return go;
        }

        private GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var collider = go.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            Apply(go, color);
            return go;
        }

        private void Apply(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = GetMaterial(color);
        }

        /// <summary>Один материал на цвет: не плодим экземпляры при каждой перестройке.</summary>
        private static Material GetMaterial(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null)
                return cached;

            var material = new Material(Shader.Find("Standard")) { color = color };
            Materials[color] = material;
            return material;
        }
    }
}
