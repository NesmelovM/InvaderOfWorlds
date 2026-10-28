using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Инструмент для сборки биомов: выбираете папку с префабами — получаете биом.
    ///
    /// Окно доступно через меню «Invader of Worlds → Биомы». Новый биом создаётся
    /// ассетом, его можно править в инспекторе и ставить в каталог в нужном порядке.
    /// </summary>
    public class BiomeToolWindow : EditorWindow
    {
        private const string BiomesFolder = "Assets/Biomes";
        private const string CatalogPath = BiomesFolder + "/BiomeCatalog.asset";

        private BiomeCatalog _catalog;
        private string _name = "Новый биом";
        private int _requiredPortals;
        private DefaultAsset _bigFolder;
        private DefaultAsset _smallFolder;
        private DefaultAsset _wallsFolder;
        private string _wallFilter = "wall|stone|block|pillar|ruin|fence";
        private int _wallSegments = 6;
        private float _wallHeight = 3f;
        private int _bigCount = 90;
        private int _smallCount = 600;
        private GameObject _meleeModel;
        private GameObject _rangedModel;
        private GameObject _eliteModel;
        private GameObject _eliteArcherModel;
        private GameObject _bossModel;
        private Color _ground = new Color(0.45f, 0.47f, 0.50f);
        private Color _hill = new Color(0.42f, 0.38f, 0.34f);
        private Color _sky = new Color(0.02f, 0.03f, 0.08f);

        [MenuItem("Инструменты разработчика/Биомы", false, 10)]
        public static void Open()
        {
            GetWindow<BiomeToolWindow>("Биомы");
        }

        private void OnEnable()
        {
            if (_catalog == null)
                _catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalog>(CatalogPath);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Сборка биома из папок пака", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _name = EditorGUILayout.TextField("Название", _name);
            _requiredPortals = EditorGUILayout.IntField("Нужно порталов", _requiredPortals);

            EditorGUILayout.Space();
            _bigFolder = (DefaultAsset)EditorGUILayout.ObjectField("Папка: крупные", _bigFolder, typeof(DefaultAsset), false);
            _bigCount = EditorGUILayout.IntField("Количество крупных", _bigCount);

            _smallFolder = (DefaultAsset)EditorGUILayout.ObjectField("Папка: мелочь", _smallFolder, typeof(DefaultAsset), false);
            _smallCount = EditorGUILayout.IntField("Количество мелочи", _smallCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Убежища монстров (стены поселений)", EditorStyles.boldLabel);

            _wallsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Папка: стены", _wallsFolder, typeof(DefaultAsset), false);
            _wallFilter = EditorGUILayout.TextField("Фильтр по имени", _wallFilter);
            _wallSegments = EditorGUILayout.IntField("Сегментов в кольце", _wallSegments);
            _wallHeight = EditorGUILayout.FloatField("Высота сегмента, м", _wallHeight);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Внешний вид врагов", EditorStyles.boldLabel);

            _meleeModel = (GameObject)EditorGUILayout.ObjectField("Боец ближнего боя", _meleeModel, typeof(GameObject), false);
            _rangedModel = (GameObject)EditorGUILayout.ObjectField("Лучник", _rangedModel, typeof(GameObject), false);
            _eliteModel = (GameObject)EditorGUILayout.ObjectField("Элитный боец", _eliteModel, typeof(GameObject), false);
            _eliteArcherModel = (GameObject)EditorGUILayout.ObjectField("Элитный лучник", _eliteArcherModel, typeof(GameObject), false);
            _bossModel = (GameObject)EditorGUILayout.ObjectField("Босс", _bossModel, typeof(GameObject), false);

            EditorGUILayout.HelpBox(
                "Пустое поле означает прежнюю модель. Модели можно брать из папки Characters " +
                "или из префабов пака — рост подгоняется автоматически.", MessageType.Info);

            EditorGUILayout.Space();
            _ground = EditorGUILayout.ColorField("Цвет поверхности", _ground);
            _hill = EditorGUILayout.ColorField("Цвет холмов", _hill);
            _sky = EditorGUILayout.ColorField("Цвет неба", _sky);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Каталог (порядок биомов)");
            _catalog = (BiomeCatalog)EditorGUILayout.ObjectField(_catalog, typeof(BiomeCatalog), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Создать биом из папок", GUILayout.Height(28f)))
                CreateBiome();

            GUI.enabled = _catalog != null;

            if (GUILayout.Button("Добавить последний биом в каталог"))
            {
                var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>($"{BiomesFolder}/{_name}.asset");

                if (biome != null)
                    AddToCatalog(biome);
            }

            GUI.enabled = true;
        }

        private void CreateBiome()
        {
            EnsureFolder();

            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            biome.Title = _name;
            biome.RequiredPortals = _requiredPortals;
            biome.GroundColor = _ground;
            biome.HillColor = _hill;
            biome.SkyColor = _sky;
            biome.BigProps = _bigCount;
            biome.SmallProps = _smallCount;
            biome.Big = LoadPrefabs(_bigFolder);
            biome.Small = LoadPrefabs(_smallFolder);
            biome.Walls = LoadPrefabs(_wallsFolder, _wallFilter);
            biome.WallSegments = Mathf.Max(3, _wallSegments);
            biome.WallHeight = Mathf.Max(1f, _wallHeight);
            biome.MeleeModel = _meleeModel;
            biome.RangedModel = _rangedModel;
            biome.EliteModel = _eliteModel;
            biome.EliteArcherModel = _eliteArcherModel;
            biome.BossModel = _bossModel;

            var path = $"{BiomesFolder}/{_name}.asset";
            AssetDatabase.CreateAsset(biome, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BIOMES] Создан биом «{_name}»: {biome.Big.Count} крупных, {biome.Small.Count} мелких");
        }

        private void AddToCatalog(BiomeDefinition biome)
        {
            if (_catalog.Biomes.Contains(biome))
                return;

            _catalog.Biomes.Add(biome);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"[BIOMES] «{biome.Title}» добавлен в каталог, позиция {_catalog.Biomes.Count}");
        }

        private static List<GameObject> LoadPrefabs(DefaultAsset folder, string nameFilter = null)
        {
            var result = new List<GameObject>();

            if (folder == null)
                return result;

            var path = AssetDatabase.GetAssetPath(folder);
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            var filtered = new List<GameObject>();

            foreach (var guid in guids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));

                if (prefab == null)
                    continue;

                result.Add(prefab);

                if (string.IsNullOrWhiteSpace(nameFilter) || System.Text.RegularExpressions.Regex.IsMatch(
                        prefab.name, nameFilter, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    filtered.Add(prefab);
                }
            }

            // Фильтр ничего не нашёл — лучше взять всё, чем оставить биом без стен.
            if (!string.IsNullOrWhiteSpace(nameFilter) && filtered.Count > 0)
                return filtered;

            return result;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(BiomesFolder))
                AssetDatabase.CreateFolder("Assets", "Biomes");
        }

        /// <summary>
        /// Стартовый набор: бедная пустыня из Египта и Индия, открывающаяся после пяти порталов.
        /// Вызывается и из меню, и из командной строки при сборке проекта.
        /// </summary>
        [MenuItem("Инструменты разработчика/Создать стартовые биомы", false, 11)]
        public static void CreateStarterBiomes()
        {
            const string packFolder = "Assets/Additions Assets/polyperfect/Low Poly Ultimate Pack/M/- Prefabs_M";

            EnsureFolder();

            var egypt = $"{packFolder}/Egypt_M";
            var india = $"{packFolder}/India_M";
            var nature = $"{packFolder}/Nature_M";

            var desert = Create("Пустыня", 0, new Color(0.72f, 0.62f, 0.42f), new Color(0.60f, 0.50f, 0.34f),
                new Color(0.55f, 0.45f, 0.30f), egypt, nature, 70, 500, egypt, "wall|stone|block|pillar|ruin|column");

            var indiaBiome = Create("Индия", 5, new Color(0.42f, 0.52f, 0.36f), new Color(0.34f, 0.42f, 0.30f),
                new Color(0.20f, 0.30f, 0.28f), india, nature, 100, 700, india, "wall|stone|block|pillar|ruin|column");

            var catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalog>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BiomeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Biomes.Clear();
            catalog.Biomes.Add(desert);
            catalog.Biomes.Add(indiaBiome);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BIOMES] Стартовые биомы готовы: {desert.Title}, {indiaBiome.Title}");
        }

        private static BiomeDefinition Create(string title, int requiredPortals, Color ground, Color hill,
            Color sky, string bigFolder, string smallFolder, int bigCount, int smallCount,
            string wallsFolder = null, string wallFilter = null)
        {
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>($"{BiomesFolder}/{title}.asset");

            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, $"{BiomesFolder}/{title}.asset");
            }

            biome.Title = title;
            biome.RequiredPortals = requiredPortals;
            biome.GroundColor = ground;
            biome.HillColor = hill;
            biome.SkyColor = sky;
            biome.BigProps = bigCount;
            biome.SmallProps = smallCount;
            biome.Big = LoadPrefabsByPath(bigFolder);
            biome.Small = LoadPrefabsByPath(smallFolder);
            biome.Walls = string.IsNullOrEmpty(wallsFolder) ? biome.Walls : LoadPrefabsByPath(wallsFolder, wallFilter);
            biome.WallSegments = 6;
            biome.WallHeight = 3f;

            EditorUtility.SetDirty(biome);
            return biome;
        }

        private static List<GameObject> LoadPrefabsByPath(string folderPath, string nameFilter = null)
        {
            var result = new List<GameObject>();
            var filtered = new List<GameObject>();

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"[BIOMES] Нет папки {folderPath}");
                return result;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folderPath }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));

                if (prefab == null)
                    continue;

                result.Add(prefab);

                if (!string.IsNullOrWhiteSpace(nameFilter) && System.Text.RegularExpressions.Regex.IsMatch(
                        prefab.name, nameFilter, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    filtered.Add(prefab);
                }
            }

            if (!string.IsNullOrWhiteSpace(nameFilter) && filtered.Count > 0)
                return filtered;

            return result;
        }
    }
}
