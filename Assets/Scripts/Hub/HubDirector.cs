using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Хаб — экран подготовки, а не прогулка. Слева постоянные улучшения за золото,
    /// в центре оружейная, справа собранный набор и вход в портал.
    ///
    /// Всё рисуется на виртуальном холсте 1280×720, поэтому раскладка не разъезжается
    /// ни на маленьком окне, ни на большом мониторе.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubDirector : MonoBehaviour
    {
        private const int ArmoryPageSize = 6;

        [SerializeField] private string _planetSceneName = "TestPlanet";

        private GUIStyle _titleStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _textStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _slotStyle;
        private int _armoryPage;
        private string _message = string.Empty;
        private float _messageUntil;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (SaveSystem.Load())
                return;

            if (RunSession.Gold == 0)
                RunSession.Grant(80);
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

            DrawBackdrop();
            DrawHeader();

            DrawShop(new Rect(40f, 132f, 380f, 480f));
            DrawArmory(new Rect(440f, 132f, 420f, 480f));
            DrawLoadout(new Rect(880f, 132f, 360f, 480f));
            DrawFooter();

            UiCanvas.End();
        }

        private void DrawBackdrop()
        {
            HealthBarDrawer.DrawRect(new Rect(0f, 0f, UiCanvas.Width, UiCanvas.Height),
                new Color(0.02f, 0.03f, 0.05f, 0.55f));
        }

        private void DrawHeader()
        {
            var panel = new Rect(40f, 28f, 1200f, 84f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            GUI.Label(new Rect(panel.x + 22f, panel.y + 12f, 600f, 34f), "БАШНЯ МАГОВ", _titleStyle);

            GUI.Label(new Rect(panel.x + 22f, panel.y + 48f, 600f, 24f),
                "Выберите снаряжение и шагните в портал", _hintStyle);

            GUI.Label(new Rect(panel.x + 700f, panel.y + 14f, 480f, 30f),
                $"Золото: {RunSession.Gold}", _titleStyle);

            GUI.Label(new Rect(panel.x + 700f, panel.y + 48f, 480f, 24f),
                $"Опыт: {RunSession.Experience}    Улучшений куплено: {TotalLevels()}", _hintStyle);
        }

        private void DrawShop(Rect panel)
        {
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, panel.width - 40f, 28f), "УЛУЧШЕНИЯ", _titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 42f, panel.width - 40f, 20f),
                "Действуют на каждый забег", _hintStyle);

            var rowY = panel.y + 76f;

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

            // Уровни показываем квадратиками: в стандартном шрифте нет ● и ○,
            // они рисовались пустыми прямоугольниками.
            var pipSize = 16f;
            var pipY = rect.y + 62f;

            for (var i = 0; i < MetaProgress.MaxLevel; i++)
            {
                var pipRect = new Rect(rect.x + 14f + i * (pipSize + 6f), pipY, pipSize, pipSize);
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
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

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
            {
                DrawWeaponRow(new Rect(panel.x + 20f, rowY + i * 50f, panel.width - 40f, 44f), start + i);
            }

            // Стрелки страниц — иначе тринадцать стволов не помещаются.
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

            GUI.Label(new Rect(rect.x + 12f, rect.y + 5f, rect.width - 150f, 22f), weapon.Name, _textStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 25f, rect.width - 150f, 18f),
                owned ? weapon.Description : $"{weapon.Description} · {weapon.Cost} золота", _hintStyle);

            var caption = equipped ? "снять" : owned ? "взять" : $"{weapon.Cost}";
            var hasRoom = Armory.Equipped.Count < WeaponSystem.MaxSlots;

            var previous = GUI.enabled;
            GUI.enabled = equipped || (owned ? hasRoom : RunSession.Gold >= weapon.Cost);

            if (GUI.Button(new Rect(rect.x + rect.width - 122f, rect.y + 6f, 110f, 34f), caption,
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
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

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

        private void DrawFooter()
        {
            var panel = new Rect(40f, 628f, 1200f, 64f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            if (Time.time < _messageUntil && _message.Length > 0)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 28f), _message,
                    _textStyle);
                return;
            }

            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 28f),
                "Смерть на планете оставит вам 30% добычи. Портал после босса: E — дальше, F — в хаб.",
                _hintStyle);
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
            // Каждый стиль проверяем отдельно: после правок код может выполняться
            // на объекте, где часть стилей ещё не создана, и общий выход по первой
            // готовой ссылке оставлял остальные null.
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.98f, 0.87f, 0.55f) }
                };
            }

            if (_panelStyle == null)
                _panelStyle = new GUIStyle(GUI.skin.label);

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

            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17 };
        }
    }
}
