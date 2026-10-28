using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Хаб как главное меню: три кнопки, выбор биома и портал.
    ///
    /// Всё рисуется на виртуальном холсте 1280×720, поэтому раскладка одинаково
    /// читается на любом разрешении окна.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubDirector : MonoBehaviour
    {
        private enum Screen
        {
            Main,
            Shop,
            Settings
        }

        private const int ArmoryPageSize = 6;
        private const string SensitivityKey = "iw.sensitivity";
        private const string InvertKey = "iw.invert";

        [SerializeField] private string _planetSceneName = "TestPlanet";
        [SerializeField] private BiomeCatalog _catalog;
        [SerializeField] private HubBackdrop _backdrop;

        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _slotStyle;
        private GUIStyle _bigButtonStyle;
        private GUIStyle _centerStyle;

        private Screen _screen = Screen.Main;
        private int _armoryPage;
        private bool _confirmReset;
        private string _message = string.Empty;
        private float _messageUntil;

        public void Configure(BiomeCatalog catalog, HubBackdrop backdrop)
        {
            _catalog = catalog;
            _backdrop = backdrop;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (!SaveSystem.Load() && RunSession.Gold == 0)
                RunSession.Grant(80);

            if (_catalog == null)
                _catalog = FindCatalog();

            if (_backdrop == null)
                _backdrop = GetComponent<HubBackdrop>();

            RefreshBackdrop();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E))
                EnterPortal();
#endif
        }

        public void EnterPortal()
        {
            Time.timeScale = 1f;
            SaveSystem.Save();
            SceneManager.LoadScene(_planetSceneName);
        }

        private void OnGUI()
        {
            EnsureStyles();
            UiCanvas.Begin();

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, UiCanvas.Width, UiCanvas.Height),
                new Color(0.02f, 0.03f, 0.05f, 0.45f));

            DrawHeader();

            switch (_screen)
            {
                case Screen.Shop:
                    DrawShopScreen();
                    break;
                case Screen.Settings:
                    DrawSettingsScreen();
                    break;
                default:
                    DrawMainScreen();
                    break;
            }

            DrawFooter();
            UiCanvas.End();
        }

        private void DrawMainScreen()
        {
            // Кнопки меню слева, чтобы башня и биом за ней оставались видны.
            var panel = new Rect(40f, 132f, 420f, 480f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.88f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, panel.width - 40f, 28f), "ГЛАВНОЕ",
                _titleStyle);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 56f, panel.width - 40f, 62f), "ПОКУПКИ",
                    _bigButtonStyle))
                _screen = Screen.Shop;

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 130f, panel.width - 40f, 62f), "НАСТРОЙКИ",
                    _bigButtonStyle))
                _screen = Screen.Settings;

            DrawBiomeSelector(new Rect(panel.x + 20f, panel.y + 216f, panel.width - 40f, 170f), true);

            var previous = GUI.enabled;
            GUI.enabled = Armory.Equipped.Count > 0 || Armory.Catalog.Length > 0;

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + panel.height - 70f, panel.width - 40f, 50f),
                    "В ПОРТАЛ  (E)", _bigButtonStyle))
                EnterPortal();

            GUI.enabled = previous;
        }

        private void DrawBiomeSelector(Rect rect, bool compact)
        {
            HealthBarDrawer.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));

            var unlocked = _catalog != null ? _catalog.UnlockedCount(Progress.TotalPortals) : 1;
            var index = _catalog != null ? Mathf.Clamp(Progress.SelectedBiome, 0, unlocked - 1) : 0;
            var biome = _catalog != null ? _catalog.ForIndex(index) : null;

            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 22f), "БИОМ", _hintStyle);

            var arrowWidth = 48f;
            var nameRect = new Rect(rect.x + arrowWidth + 12f, rect.y + 34f, rect.width - (arrowWidth + 12f) * 2f, 34f);

            var previous = GUI.enabled;
            GUI.enabled = index > 0;

            if (GUI.Button(new Rect(rect.x + 10f, rect.y + 34f, arrowWidth, 34f), "◀", _buttonStyle))
                SelectBiome(index - 1, unlocked);

            GUI.enabled = index < unlocked - 1;

            if (GUI.Button(new Rect(rect.x + rect.width - arrowWidth - 10f, rect.y + 34f, arrowWidth, 34f), "▶",
                    _buttonStyle))
                SelectBiome(index + 1, unlocked);

            GUI.enabled = previous;

            GUI.Label(nameRect, biome != null ? biome.Title : "нет биома", _centerStyle);

            var nextLocked = _catalog != null && index + 1 < _catalog.Biomes.Count &&
                             _catalog.Biomes[index + 1] != null
                ? _catalog.Biomes[index + 1]
                : null;

            var hint = nextLocked != null
                ? $"Следующий: {nextLocked.Title} — после {nextLocked.RequiredPortals} порталов " +
                  $"(пройдено {Progress.TotalPortals})"
                : "Все биомы открыты";

            GUI.Label(new Rect(rect.x + 14f, rect.y + 76f, rect.width - 28f, 40f), hint, _hintStyle);
        }

        private void SelectBiome(int index, int unlocked)
        {
            Progress.SelectBiome(index, unlocked);
            SaveSystem.Save();
            RefreshBackdrop();
        }

        private void RefreshBackdrop()
        {
            if (_backdrop == null || _catalog == null)
                return;

            _backdrop.Show(_catalog.ForIndex(Progress.SelectedBiome));
        }

        private void DrawShopScreen()
        {
            DrawUpgrades(new Rect(40f, 132f, 380f, 480f));
            DrawArmory(new Rect(440f, 132f, 420f, 480f));
            DrawLoadout(new Rect(880f, 132f, 360f, 480f));

            if (GUI.Button(new Rect(40f, 628f, 220f, 56f), "◀ НАЗАД", _bigButtonStyle))
                _screen = Screen.Main;
        }

        private void DrawSettingsScreen()
        {
            var panel = new Rect(40f, 132f, 620f, 480f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.9f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 40f, 28f), "НАСТРОЙКИ",
                _titleStyle);

            var sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 2.2f);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 62f, 260f, 24f),
                $"Чувствительность мыши: {sensitivity:F1}", _textStyle);

            var newSensitivity = GUI.HorizontalSlider(
                new Rect(panel.x + 300f, panel.y + 68f, panel.width - 340f, 20f), sensitivity, 0.4f, 6f);

            if (!Mathf.Approximately(newSensitivity, sensitivity))
            {
                PlayerPrefs.SetFloat(SensitivityKey, newSensitivity);
                PlayerPrefs.Save();
            }

            var volume = InvaderOfWorlds.AudioSettings.Volume;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 104f, 260f, 24f),
                $"Громкость: {Mathf.RoundToInt(volume * 100f)}%", _textStyle);

            var newVolume = GUI.HorizontalSlider(new Rect(panel.x + 300f, panel.y + 110f,
                panel.width - 340f, 20f), volume, 0f, 1f);

            if (!Mathf.Approximately(newVolume, volume))
                InvaderOfWorlds.AudioSettings.Volume = newVolume;

            var invert = PlayerPrefs.GetInt(InvertKey, 0) == 1;

            if (GUI.Toggle(new Rect(panel.x + 20f, panel.y + 146f, 280f, 28f), invert, " Инверсия мыши",
                    GUI.skin.toggle) != invert)
            {
                PlayerPrefs.SetInt(InvertKey, invert ? 0 : 1);
                PlayerPrefs.Save();
            }

            var quality = QualitySettings.GetQualityLevel();
            GUI.Label(new Rect(panel.x + 20f, panel.y + 190f, 260f, 24f), "Качество графики", _textStyle);

            if (GUI.Button(new Rect(panel.x + 300f, panel.y + 184f, 40f, 34f), "−", _buttonStyle))
                QualitySettings.SetQualityLevel(Mathf.Max(0, quality - 1), true);

            if (GUI.Button(new Rect(panel.x + 348f, panel.y + 184f, 40f, 34f), "+", _buttonStyle))
                QualitySettings.SetQualityLevel(Mathf.Min(QualitySettings.names.Length - 1, quality + 1), true);

            GUI.Label(new Rect(panel.x + 404f, panel.y + 190f, 200f, 24f),
                QualitySettings.names[QualitySettings.GetQualityLevel()], _textStyle);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 230f, panel.width - 40f, 26f),
                "Настройки сохраняются сразу и действуют на все забеги.", _hintStyle);

            DrawResetBlock(new Rect(panel.x + 20f, panel.y + 270f, panel.width - 40f, 130f));

            if (GUI.Button(new Rect(40f, 628f, 220f, 56f), "◀ НАЗАД", _bigButtonStyle))
                _screen = Screen.Main;
        }

        private void DrawHeader()
        {
            var panel = new Rect(40f, 28f, 1200f, 84f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.9f));

            GUI.Label(new Rect(panel.x + 22f, panel.y + 12f, 600f, 34f), "БАШНЯ МАГОВ", _titleStyle);
            GUI.Label(new Rect(panel.x + 22f, panel.y + 48f, 600f, 24f),
                $"Порталов пройдено: {Progress.TotalPortals}", _hintStyle);

            GUI.Label(new Rect(panel.x + 700f, panel.y + 14f, 480f, 30f),
                $"Золото: {RunSession.Gold}", _titleStyle);
            GUI.Label(new Rect(panel.x + 700f, panel.y + 48f, 480f, 24f),
                $"Улучшений: {TotalLevels()}   Оружия в наборе: {Armory.Equipped.Count}",
                _hintStyle);
        }

        private void DrawUpgrades(Rect panel)
        {
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.9f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, panel.width - 40f, 28f), "УЛУЧШЕНИЯ",
                _titleStyle);

            var rowY = panel.y + 52f;

            DrawUpgradeRow(new Rect(panel.x + 20f, rowY, panel.width - 40f, 96f), "Урон", "+15% к урону",
                MetaProgress.DamageLevel, MetaProgress.DamageCost, MetaProgress.TryBuyDamage);

            DrawUpgradeRow(new Rect(panel.x + 20f, rowY + 110f, panel.width - 40f, 96f), "Здоровье",
                "+20 к максимуму", MetaProgress.HealthLevel, MetaProgress.HealthCost, MetaProgress.TryBuyHealth);

            DrawUpgradeRow(new Rect(panel.x + 20f, rowY + 220f, panel.width - 40f, 96f), "Скорость",
                "+5% к бегу", MetaProgress.SpeedLevel, MetaProgress.SpeedCost, MetaProgress.TryBuySpeed);
        }

        private void DrawUpgradeRow(Rect rect, string title, string effect, int level, int cost,
            System.Func<bool> buy)
        {
            HealthBarDrawer.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));

            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 24f), title, _textStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 36f, rect.width - 160f, 20f), effect, _hintStyle);

            var pipSize = 16f;

            for (var i = 0; i < MetaProgress.MaxLevel; i++)
            {
                var pipRect = new Rect(rect.x + 14f + i * (pipSize + 6f), rect.y + 62f, pipSize, pipSize);
                HealthBarDrawer.DrawRect(pipRect, i < level
                    ? new Color(0.98f, 0.85f, 0.45f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.12f));
            }

            var maxed = level >= MetaProgress.MaxLevel;
            var caption = maxed ? "максимум" : $"купить · {cost}";
            var previous = GUI.enabled;
            GUI.enabled = !maxed && RunSession.Gold >= cost;

            if (GUI.Button(new Rect(rect.x + rect.width - 140f, rect.y + 30f, 124f, 40f), caption,
                    _buttonStyle))
            {
                if (buy())
                {
                    SaveSystem.Save();
                    Show("Улучшение куплено");
                }
            }

            GUI.enabled = previous;
        }

        private void DrawArmory(Rect panel)
        {
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.9f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, panel.width - 40f, 28f), "ОРУЖЕЙНАЯ",
                _titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 42f, panel.width - 40f, 20f),
                $"В наборе {Armory.Equipped.Count} из {WeaponSystem.MaxSlots}", _hintStyle);

            var total = Armory.Catalog.Length;
            var pages = Mathf.Max(1, Mathf.CeilToInt(total / (float)ArmoryPageSize));
            _armoryPage = Mathf.Clamp(_armoryPage, 0, pages - 1);

            var start = _armoryPage * ArmoryPageSize;
            var rowY = panel.y + 72f;

            for (var i = 0; i < ArmoryPageSize && start + i < total; i++)
                DrawWeaponRow(new Rect(panel.x + 20f, rowY + i * 50f, panel.width - 40f, 44f), start + i);

            var buttonY = panel.y + panel.height - 46f;
            var previous = GUI.enabled;
            GUI.enabled = _armoryPage > 0;

            if (GUI.Button(new Rect(panel.x + 20f, buttonY, 44f, 34f), "◀", _buttonStyle))
                _armoryPage--;

            GUI.enabled = _armoryPage < pages - 1;

            if (GUI.Button(new Rect(panel.x + panel.width - 64f, buttonY, 44f, 34f), "▶", _buttonStyle))
                _armoryPage++;

            GUI.enabled = previous;

            GUI.Label(new Rect(panel.x + 76f, buttonY + 6f, panel.width - 160f, 24f),
                $"страница {_armoryPage + 1} из {pages}", _hintStyle);
        }

        private void DrawWeaponRow(Rect rect, int id)
        {
            var weapon = Armory.Catalog[id];
            var owned = Armory.IsOwned(id);
            var equipped = Armory.IsEquipped(id);

            HealthBarDrawer.DrawRect(rect, equipped
                ? new Color(0.20f, 0.32f, 0.22f, 0.85f)
                : new Color(1f, 1f, 1f, 0.05f));

            GUI.Label(new Rect(rect.x + 12f, rect.y + 4f, rect.width - 150f, 22f), weapon.Name, _textStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 24f, rect.width - 150f, 18f),
                owned ? weapon.Description : $"{weapon.Description} · {weapon.Cost} золота", _hintStyle);

            var caption = equipped ? "снять" : owned ? "взять" : $"{weapon.Cost}";
            var hasRoom = Armory.Equipped.Count < WeaponSystem.MaxSlots;

            var previous = GUI.enabled;
            GUI.enabled = equipped || (owned ? hasRoom : RunSession.Gold >= weapon.Cost);

            if (GUI.Button(new Rect(rect.x + rect.width - 122f, rect.y + 5f, 110f, 34f), caption,
                    _buttonStyle))
            {
                if (equipped)
                    Armory.TryUnequip(id);
                else if (owned)
                    Armory.TryEquip(id);
                else if (Armory.TryBuy(id))
                    Armory.TryEquip(id);

                SaveSystem.Save();
            }

            GUI.enabled = previous;
        }

        private void DrawLoadout(Rect panel)
        {
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.9f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, panel.width - 40f, 28f), "НАБОР", _titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 42f, panel.width - 40f, 20f),
                $"Слотов свободно: {WeaponSystem.MaxSlots - Armory.Equipped.Count}", _hintStyle);

            for (var slot = 0; slot < WeaponSystem.MaxSlots; slot++)
            {
                var rect = new Rect(panel.x + 20f, panel.y + 76f + slot * 44f, panel.width - 40f, 38f);
                HealthBarDrawer.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));

                var text = slot < Armory.Equipped.Count
                    ? $"{slot + 1}. {Armory.Catalog[Armory.Equipped[slot]].Name}"
                    : $"{slot + 1}. пусто";

                GUI.Label(new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 26f), text, _slotStyle);
            }

            var previous = GUI.enabled;
            GUI.enabled = Armory.Equipped.Count > 0;

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + panel.height - 60f, panel.width - 40f, 44f),
                    "В ПОРТАЛ  (E)", _buttonStyle))
                EnterPortal();

            GUI.enabled = previous;
        }

        /// <summary>Сброс сохранения: два шага, чтобы не нажать случайно.</summary>
        private void DrawResetBlock(Rect area)
        {
            if (!_confirmReset)
            {
                if (GUI.Button(new Rect(area.x, area.y, area.width, 46f), "СБРОСИТЬ ПРОГРЕСС",
                        _buttonStyle))
                    _confirmReset = true;

                return;
            }

            GUI.Label(new Rect(area.x, area.y, area.width, 24f),
                "Удалятся золото, улучшения и купленное оружие.", _hintStyle);

            if (GUI.Button(new Rect(area.x, area.y + 30f, (area.width - 10f) * 0.5f, 44f), "ДА, СБРОСИТЬ",
                    _buttonStyle))
            {
                SaveSystem.ResetProgress();
                _confirmReset = false;
                Show("Прогресс сброшен");
            }

            if (GUI.Button(new Rect(area.x + (area.width + 10f) * 0.5f, area.y + 30f,
                    (area.width - 10f) * 0.5f, 44f), "ОТМЕНА", _buttonStyle))
                _confirmReset = false;
        }

        private void DrawFooter()
        {
            if (_screen != Screen.Main)
                return;

            var panel = new Rect(480f, 628f, 760f, 56f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            var text = Time.time < _messageUntil && _message.Length > 0
                ? _message
                : "Смерть на планете оставит 30% добычи. У портала после босса: E — дальше, F — в хаб.";

            GUI.Label(new Rect(panel.x + 16f, panel.y + 16f, panel.width - 32f, 26f), text, _hintStyle);
        }

        private static BiomeCatalog FindCatalog()
        {
            var catalog = Resources.Load<BiomeCatalog>("Biomes/Catalog");

            if (catalog != null)
                return catalog;

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:BiomeCatalog");

            if (guids.Length > 0)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<BiomeCatalog>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
#endif
            return null;
        }

        private void Show(string message)
        {
            _message = message;
            _messageUntil = Time.time + 2f;
        }

        private static int TotalLevels()
        {
            return MetaProgress.DamageLevel + MetaProgress.HealthLevel + MetaProgress.SpeedLevel;
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.98f, 0.87f, 0.55f) }
                };
            }

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 19,
                    normal = { textColor = Color.white },
                    clipping = TextClipping.Clip
                };
            }

            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    normal = { textColor = new Color(0.72f, 0.76f, 0.85f) },
                    wordWrap = true,
                    clipping = TextClipping.Clip
                };
            }

            if (_slotStyle == null)
            {
                _slotStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    normal = { textColor = new Color(0.90f, 0.92f, 0.98f) },
                    clipping = TextClipping.Clip
                };
            }

            if (_centerStyle == null)
            {
                _centerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.98f, 0.90f, 0.65f) }
                };
            }

            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17 };

            if (_bigButtonStyle == null)
                _bigButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
        }
    }
}
