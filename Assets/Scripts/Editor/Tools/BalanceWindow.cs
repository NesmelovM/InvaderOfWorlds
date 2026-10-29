using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Инструмент баланса: оружие, опыт, экономика, враги, игрок и сложность
    /// в одном окне, рядом с инструментом биомов.
    ///
    /// Правки пишутся в ассет Resources/Balance/Config, который читает игра.
    /// </summary>
    public class BalanceWindow : EditorWindow
    {
        private const string Folder = "Assets/Resources/Balance";
        private const string AssetPath = Folder + "/Config.asset";

        private BalanceConfig _config;
        private Vector2 _scroll;

        [MenuItem("Инструменты разработчика/Баланс", false, 30)]
        public static void Open()
        {
            GetWindow<BalanceWindow>("Баланс");
        }

        private void OnEnable()
        {
            _config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(AssetPath);
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "Конфиг баланса ещё не создан. Игра сейчас использует значения из кода.",
                    MessageType.Info);

                if (GUILayout.Button("Создать конфиг баланса", GUILayout.Height(32f)))
                    CreateConfig();

                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawWeapons();
            EditorGUILayout.Space(8f);
            DrawProgress();
            EditorGUILayout.Space(8f);
            DrawCards();
            EditorGUILayout.Space(8f);
            DrawCarryOver();
            EditorGUILayout.Space(8f);
            DrawEnemies();
            EditorGUILayout.Space(8f);
            DrawPlayer();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Сохранить", GUILayout.Height(28f)))
                {
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Сбросить к коду", GUILayout.Height(28f)))
                {
                    if (EditorUtility.DisplayDialog("Сброс баланса",
                            "Вернуть все значения к встроенным?", "Да", "Отмена"))
                    {
                        FillDefaults(_config);
                        EditorUtility.SetDirty(_config);
                        AssetDatabase.SaveAssets();
                    }
                }

                if (GUILayout.Button("Заполнить оружие из каталога", GUILayout.Height(28f)))
                {
                    SyncWeapons();
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawWeapons()
        {
            EditorGUILayout.LabelField("ОРУЖИЕ", EditorStyles.boldLabel);

            if (_config.Weapons.Count == 0)
            {
                EditorGUILayout.HelpBox("Список пуст — нажмите «Заполнить оружие из каталога».",
                    MessageType.Warning);
                return;
            }

            for (var i = 0; i < _config.Weapons.Count; i++)
            {
                var weapon = _config.Weapons[i];
                var name = i < Armory.Catalog.Length ? Armory.Catalog[i].Name : $"Оружие {i}";

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                    weapon.Damage = EditorGUILayout.FloatField("Урон", weapon.Damage);
                    weapon.Range = EditorGUILayout.FloatField("Дальность / радиус", weapon.Range);
                    weapon.Cooldown = EditorGUILayout.FloatField("Перезарядка, с", weapon.Cooldown);
                    weapon.Shots = EditorGUILayout.IntSlider("Атак за тик", weapon.Shots, 1, 12);
                    weapon.ProjectileSpeed = EditorGUILayout.FloatField("Скорость снаряда", weapon.ProjectileSpeed);
                    weapon.Cost = EditorGUILayout.IntField("Цена в оружейной", weapon.Cost);
                }
            }
        }

        private void DrawProgress()
        {
            EditorGUILayout.LabelField("ОПЫТ И ЭКОНОМИКА", EditorStyles.boldLabel);

            _config.ExperienceMultiplier = EditorGUILayout.FloatField(
                new GUIContent("Множитель опыта", "Во сколько раз больше опыта с врагов"),
                _config.ExperienceMultiplier);

            _config.GoldMultiplier = EditorGUILayout.FloatField("Множитель золота", _config.GoldMultiplier);
            _config.LevelBaseExperience = EditorGUILayout.IntField("Опыт до 2 уровня", _config.LevelBaseExperience);
            _config.LevelGrowth = EditorGUILayout.FloatField("Рост порога за уровень", _config.LevelGrowth);
            _config.DeathPenaltyShare = EditorGUILayout.Slider("Доля добычи после смерти",
                _config.DeathPenaltyShare, 0f, 1f);
            _config.ExperiencePerPlanet = EditorGUILayout.FloatField(
                new GUIContent("Опыт за планету", "Во сколько раз больше опыта за врага на каждой следующей планете"),
                _config.ExperiencePerPlanet);

            _config.DifficultyStep = EditorGUILayout.FloatField(
                new GUIContent("Рост сложности за планету", "Во сколько раз злее враги на следующей планете"),
                _config.DifficultyStep);
        }

        private void DrawCards()
        {
            EditorGUILayout.LabelField("КАРТОЧКИ УРОВНЯ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Значения карточек по редкости. Пустое или нулевое поле убирает карточку из набора.",
                MessageType.None);

            DrawTier("Обычные", _config.CommonCards);
            DrawTier("Редкие", _config.RareCards);
            DrawTier("Эпические", _config.EpicCards);
        }

        private static void DrawTier(string title, CardTierBalance tier)
        {
            if (tier == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                tier.DamageMult = EditorGUILayout.FloatField("Урон, множитель", tier.DamageMult);
                tier.HealthAdd = EditorGUILayout.FloatField("Здоровье, прибавка", tier.HealthAdd);
                tier.SpeedMult = EditorGUILayout.FloatField("Скорость, множитель", tier.SpeedMult);
                tier.AttackSpeedMult = EditorGUILayout.FloatField("Скорость атаки, множитель",
                    tier.AttackSpeedMult);
                tier.ExperienceMult = EditorGUILayout.FloatField("Опыт, множитель", tier.ExperienceMult);
                tier.HealPerKill = EditorGUILayout.FloatField("Здоровье за убийство", tier.HealPerKill);
                tier.SlowPercent = EditorGUILayout.Slider("Замедление врагов", tier.SlowPercent, 0f, 0.6f);

                EditorGUILayout.Space(4f);
                tier.ExplosionDamage = EditorGUILayout.FloatField("Урон взрыва при смерти",
                    tier.ExplosionDamage);
                tier.ExplosionRadius = EditorGUILayout.FloatField("Радиус взрыва, м", tier.ExplosionRadius);
                tier.RegenPerSecond = EditorGUILayout.FloatField("Регенерация в секунду",
                    tier.RegenPerSecond);
                tier.ExtraShots = EditorGUILayout.IntSlider("Доп. снарядов", tier.ExtraShots, 0, 3);
            }
        }

        private void DrawCarryOver()
        {
            EditorGUILayout.LabelField("ЧТО ПЕРЕНОСИТСЯ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Галочки задают, что остаётся после смерти и что переживает переход на следующую планету.",
                MessageType.None);

            DrawRules("После смерти", _config.OnDeath);

            _config.PlanetsPerBiome = EditorGUILayout.IntSlider(
                new GUIContent("Планет на биом", "Сколько планет одного биома пройти до предложения следующего"),
                _config.PlanetsPerBiome, 1, 10);

            DrawRules("На следующую планету", _config.OnNextPlanet);
        }

        private static void DrawRules(string title, CarryOverRules rules)
        {
            if (rules == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                rules.Gold = EditorGUILayout.Toggle("Золото", rules.Gold);
                rules.Experience = EditorGUILayout.Toggle("Опыт", rules.Experience);
                rules.Level = EditorGUILayout.Toggle("Уровень", rules.Level);
                rules.Loadout = EditorGUILayout.Toggle("Набор оружия", rules.Loadout);
                rules.HealthBonus = EditorGUILayout.Toggle("Бонус здоровья", rules.HealthBonus);
                rules.SpeedBonus = EditorGUILayout.Toggle("Бонус скорости", rules.SpeedBonus);
                rules.DamageBonus = EditorGUILayout.Toggle("Бонусы урона и атаки", rules.DamageBonus);
                rules.CardEffects = EditorGUILayout.Toggle("Эффекты карточек", rules.CardEffects);
            }
        }

        private void DrawEnemies()
        {
            EditorGUILayout.LabelField("ВРАГИ", EditorStyles.boldLabel);

            _config.EnemyHealthMultiplier = EditorGUILayout.FloatField("Здоровье, множитель",
                _config.EnemyHealthMultiplier);
            _config.EnemyDamageMultiplier = EditorGUILayout.FloatField("Урон, множитель",
                _config.EnemyDamageMultiplier);
            _config.EnemySpeedMultiplier = EditorGUILayout.FloatField("Скорость, множитель",
                _config.EnemySpeedMultiplier);
        }

        private void DrawPlayer()
        {
            EditorGUILayout.LabelField("ИГРОК", EditorStyles.boldLabel);

            _config.PlayerHealth = EditorGUILayout.FloatField("Здоровье", _config.PlayerHealth);
            _config.WalkSpeed = EditorGUILayout.FloatField("Скорость шага", _config.WalkSpeed);
            _config.SprintSpeed = EditorGUILayout.FloatField("Скорость бега", _config.SprintSpeed);
        }

        /// <summary>Создаёт конфиг, если его ещё нет. Вызывается и из окна, и из консоли.</summary>
        [MenuItem("Инструменты разработчика/Создать конфиг баланса", false, 31)]
        public static BalanceConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BalanceConfig>(AssetPath);

            if (existing != null)
            {
                Debug.Log($"[BALANCE] Конфиг уже есть: {AssetPath}");
                return existing;
            }

            EnsureFolder();

            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            FillDefaults(config);

            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BALANCE] Создан конфиг {AssetPath} с оружием: {config.Weapons.Count}");
            return config;
        }

        private void CreateConfig()
        {
            _config = EnsureConfig();
        }

        private void SyncWeapons()
        {
            _config.Weapons.Clear();

            foreach (var weapon in Armory.Catalog)
            {
                _config.Weapons.Add(new WeaponBalance
                {
                    Damage = weapon.Damage,
                    Range = weapon.Range,
                    Cooldown = weapon.Cooldown,
                    Shots = 1,
                    ProjectileSpeed = weapon.ProjectileSpeed,
                    Cost = weapon.Cost
                });
            }
        }

        private static void FillDefaults(BalanceConfig config)
        {
            if (config.Weapons.Count == 0)
            {
                foreach (var weapon in Armory.Catalog)
                {
                    config.Weapons.Add(new WeaponBalance
                    {
                        Damage = weapon.Damage,
                        Range = weapon.Range,
                        Cooldown = weapon.Cooldown,
                        Shots = 1,
                        ProjectileSpeed = weapon.ProjectileSpeed,
                        Cost = weapon.Cost
                    });
                }
            }

            config.ExperienceMultiplier = 1f;
            config.ExperiencePerPlanet = 1.25f;
            config.PlanetsPerBiome = 3;
            config.GoldMultiplier = 1f;
            config.LevelBaseExperience = 45;
            config.LevelGrowth = 1.27f;

            config.CommonCards = new CardTierBalance
            {
                DamageMult = 1.10f, HealthAdd = 15f, SpeedMult = 1.05f, HealPerKill = 1f,
                AttackSpeedMult = 1.10f, ExperienceMult = 1.10f, SlowPercent = 0.08f,
                ExplosionDamage = 10f, ExplosionRadius = 3.5f, RegenPerSecond = 0.5f, ExtraShots = 0
            };
            config.RareCards = new CardTierBalance
            {
                DamageMult = 1.20f, HealthAdd = 30f, SpeedMult = 1.10f, HealPerKill = 3f,
                AttackSpeedMult = 1.20f, ExperienceMult = 1.20f, SlowPercent = 0.12f,
                ExplosionDamage = 20f, ExplosionRadius = 4f, RegenPerSecond = 1f, ExtraShots = 1
            };
            config.EpicCards = new CardTierBalance
            {
                DamageMult = 1.35f, HealthAdd = 50f, SpeedMult = 1.18f, HealPerKill = 6f,
                AttackSpeedMult = 1.35f, ExperienceMult = 1.35f, SlowPercent = 0.20f,
                ExplosionDamage = 35f, ExplosionRadius = 5f, RegenPerSecond = 2f, ExtraShots = 1
            };

            config.OnDeath = new CarryOverRules { Level = false, Loadout = false };
            config.OnNextPlanet = new CarryOverRules
            {
                Level = true, Loadout = true, HealthBonus = true,
                SpeedBonus = true, DamageBonus = true, CardEffects = true
            };

            config.DeathPenaltyShare = 0.3f;
            config.DifficultyStep = 1.5f;
            config.EnemyHealthMultiplier = 1f;
            config.EnemyDamageMultiplier = 1f;
            config.EnemySpeedMultiplier = 1f;
            config.PlayerHealth = 100f;
            config.WalkSpeed = 10f;
            config.SprintSpeed = 20f;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Balance");
        }
    }
}
