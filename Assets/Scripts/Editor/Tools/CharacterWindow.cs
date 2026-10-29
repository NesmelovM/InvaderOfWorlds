using UnityEditor;
using UnityEngine;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Окно «Персонажи»: модель, оружие и рост для каждого типа врага,
    /// плюс параметры процедурной анимации.
    ///
    /// Правки пишутся в Resources/Characters/Config.asset, который читает игра.
    /// </summary>
    public class CharacterWindow : EditorWindow
    {
        private const string Folder = "Assets/Resources/Characters";
        private const string AssetPath = Folder + "/Config.asset";

        private CharacterConfig _config;
        private Vector2 _scroll;

        [MenuItem("Инструменты разработчика/Персонажи", false, 20)]
        public static void Open()
        {
            GetWindow<CharacterWindow>("Персонажи");
        }

        private void OnEnable()
        {
            _config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(AssetPath);
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "Конфиг персонажей не создан. Игра использует встроенные модели и оружие.",
                    MessageType.Info);

                if (GUILayout.Button("Создать конфиг персонажей", GUILayout.Height(32f)))
                    CreateConfig();

                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _config.FitHeightToType = EditorGUILayout.Toggle("Подгонять рост под тип", _config.FitHeightToType);
            EditorGUILayout.HelpBox(
                _config.FitHeightToType
                    ? "Модели масштабируются под рост типа: мечник 1.8, элита 2.5 и так далее."
                    : "Модель остаётся своего размера, капсула подгоняется под неё.",
                MessageType.None);

            _config.Colorize = EditorGUILayout.Toggle("Красить модели", _config.Colorize);
            EditorGUILayout.HelpBox(
                _config.Colorize
                    ? "Модели перекрашиваются в один цвет забега."
                    : "Цвет берётся из самого префаба — можно ставить уже покрашенных врагов.",
                MessageType.None);

            EditorGUILayout.Space(6f);

            DrawPlayer();

            EditorGUILayout.Space(10f);
            DrawAnimation();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Сохранить", GUILayout.Height(28f)))
                {
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                    InvaderOfWorlds.Characters.ResetCache();
                }

                if (GUILayout.Button("Заполнить из кода", GUILayout.Height(28f)))
                {
                    FillDefaults(_config);
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                    InvaderOfWorlds.Characters.ResetCache();
                }
            }
        }

        private void DrawPlayer()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("ИГРОК", EditorStyles.boldLabel);

                _config.Player.Model = (GameObject)EditorGUILayout.ObjectField("Модель (риг)",
                    _config.Player.Model, typeof(GameObject), false);
                _config.Player.Weapon = (GameObject)EditorGUILayout.ObjectField("Оружие",
                    _config.Player.Weapon, typeof(GameObject), false);

                _config.Player.Height = EditorGUILayout.Slider("Рост", _config.Player.Height, 0.8f, 3f);
                _config.Player.WeaponScale = EditorGUILayout.Slider("Размер оружия",
                    _config.Player.WeaponScale, 0.3f, 2f);
                using (new EditorGUI.DisabledScope(!_config.Colorize))
                    _config.PlayerTint = EditorGUILayout.ColorField("Цвет мантии", _config.PlayerTint);

                if (_config.Player.Model == null)
                {
                    EditorGUILayout.HelpBox("Пусто — используется модель из сцены " +
                                            "(Characters/man-cult_Rig).", MessageType.None);
                }
            }
        }

        private static void DrawEnemy(string title, EnemyLook look, string defaultModel, string defaultWeapon,
            float defaultHeight)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                look.Model = (GameObject)EditorGUILayout.ObjectField("Модель (риг)", look.Model,
                    typeof(GameObject), false);
                look.Weapon = (GameObject)EditorGUILayout.ObjectField("Оружие", look.Weapon,
                    typeof(GameObject), false);
                look.Height = EditorGUILayout.Slider("Рост", look.Height, 0.8f, 6f);
                look.WeaponScale = EditorGUILayout.Slider("Размер оружия", look.WeaponScale, 0.3f, 2f);

                if (look.Model == null && look.Weapon == null)
                {
                    EditorGUILayout.HelpBox($"Пусто — используется {defaultModel} и {defaultWeapon}, " +
                                            $"рост {defaultHeight:F1} м.", MessageType.None);
                }
            }
        }

        private void DrawAnimation()
        {
            EditorGUILayout.LabelField("ПРОЦЕДУРНАЯ АНИМАЦИЯ", EditorStyles.boldLabel);

            _config.StepSpeed = EditorGUILayout.Slider(
                new GUIContent("Частота шага", "Как быстро качаются ноги при движении"),
                _config.StepSpeed, 2f, 16f);

            _config.LegAmplitude = EditorGUILayout.Slider("Амплитуда ног", _config.LegAmplitude, 5f, 60f);
            _config.ArmAmplitude = EditorGUILayout.Slider("Амплитуда рук", _config.ArmAmplitude, 5f, 60f);
            _config.AttackDuration = EditorGUILayout.Slider("Длительность замаха", _config.AttackDuration,
                0.1f, 0.8f);
        }

        [MenuItem("Инструменты разработчика/Создать конфиг персонажей", false, 21)]
        public static CharacterConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CharacterConfig>(AssetPath);

            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Characters");

            var config = ScriptableObject.CreateInstance<CharacterConfig>();
            FillDefaults(config);

            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CHARACTERS] Создан конфиг {AssetPath}");
            return config;
        }

        private void CreateConfig()
        {
            _config = EnsureConfig();
        }

        private static void FillDefaults(CharacterConfig config)
        {
            Fill(config.Melee, "Characters/man-soldier_Rig", "Weapons/sword", 1.8f);
            Fill(config.Ranged, "Characters/man-cult_Rig", "Weapons/bow", 1.9f);
            Fill(config.Elite, "Characters/man-knight_Rig", "Weapons/hammer", 2.5f);
            Fill(config.EliteRanged, "Characters/man-cult_Rig", "Weapons/bow", 2.4f);
            Fill(config.Boss, "Characters/giant_Rig", "Weapons/club-zulu", 4.2f);

            config.Player = new EnemyLook { Height = 1.95f };
            config.PlayerTint = new Color(0.16f, 0.07f, 0.23f);

            config.StepSpeed = 8.5f;
            config.LegAmplitude = 32f;
            config.ArmAmplitude = 26f;
            config.AttackDuration = 0.28f;
        }

        private static void Fill(EnemyLook look, string modelPath, string weaponPath, float height)
        {
            look.Model ??= AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(modelPath));
            look.Weapon ??= AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(weaponPath));
            look.Height = height;
            look.WeaponScale = 1f;
        }

        /// <summary>Resources-путь превращается в путь ассета для редактора.</summary>
        private static string PathFor(string resourcePath)
        {
            var guids = AssetDatabase.FindAssets($"{System.IO.Path.GetFileName(resourcePath)} t:Model");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.EndsWith($"{System.IO.Path.GetFileName(resourcePath)}.fbx") ||
                    path.EndsWith($"{System.IO.Path.GetFileName(resourcePath)}.prefab"))
                    return path;
            }

            return resourcePath;
        }
    }
}
