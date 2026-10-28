using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Хаб — это экран подготовки, а не прогулка: персонажем управлять не нужно.
    /// Здесь магазин постоянных улучшений и вход в портал.
    ///
    /// Курсор разблокирован намеренно: иначе GUI-кнопки не нажимаются.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubDirector : MonoBehaviour
    {
        [SerializeField] private string _planetSceneName = "TestPlanet";

        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Загружаем прошлый прогресс; если сохранения нет — даём стартовый капитал,
            // чтобы магазин можно было проверить сразу.
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

            DrawHeader();
            DrawShop();
            DrawTree();
        }

        /// <summary>Дерево прокачки: узлы за опыт, часть требует предыдущих.</summary>
        private void DrawTree()
        {
            var panel = new Rect(Screen.width - 444f, 24f, 420f, 348f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.62f));

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 380f, 26f), "ДЕРЕВО ПРОКАЧКИ", _titleStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 34f, 380f, 20f),
                $"Опыт: {RunSession.Experience}", _hintStyle);

            for (var i = 0; i < SkillTree.Nodes.Length; i++)
            {
                var node = SkillTree.Nodes[i];
                var rowY = panel.y + 60f + i * 46f;

                var unlocked = SkillTree.IsUnlocked(i);
                var canUnlock = SkillTree.CanUnlock(i);
                var affordable = canUnlock && RunSession.Experience >= node.Cost;

                GUI.Label(new Rect(panel.x + 18f, rowY, 270f, 20f),
                    $"{node.Title} · {node.Effect}", _textStyle);
                GUI.Label(new Rect(panel.x + 18f, rowY + 18f, 270f, 18f),
                    unlocked ? "изучено" : $"опыт: {node.Cost}", _hintStyle);

                var caption = unlocked ? "готово" : canUnlock ? "изучить" : "закрыто";

                var previous = GUI.enabled;
                GUI.enabled = affordable;

                if (GUI.Button(new Rect(panel.x + 296f, rowY + 2f, 106f, 32f), caption, _buttonStyle))
                {
                    if (SkillTree.TryUnlock(i))
                        SaveSystem.Save();
                }

                GUI.enabled = previous;
            }
        }

        private void DrawHeader()
        {
            var panel = new Rect(24f, 24f, 420f, 96f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.62f));

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 380f, 30f), "БАШНЯ МАГОВ", _titleStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 46f, 380f, 22f),
                $"Золото: {RunSession.Gold}", _textStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 68f, 380f, 22f),
                $"Опыт: {RunSession.Experience}    Покупок: {TotalLevels()}", _hintStyle);
        }

        private void DrawShop()
        {
            var panel = new Rect(24f, 132f, 420f, 300f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.62f));

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 380f, 26f), "УЛУЧШЕНИЯ", _titleStyle);

            DrawUpgradeRow(new Rect(panel.x + 18f, panel.y + 46f, 384f, 44f), "Урон", "+15%",
                MetaProgress.DamageLevel, MetaProgress.DamageCost, MetaProgress.TryBuyDamage);

            DrawUpgradeRow(new Rect(panel.x + 18f, panel.y + 96f, 384f, 44f), "Здоровье", "+20 HP",
                MetaProgress.HealthLevel, MetaProgress.HealthCost, MetaProgress.TryBuyHealth);

            DrawUpgradeRow(new Rect(panel.x + 18f, panel.y + 146f, 384f, 44f), "Скорость", "+5%",
                MetaProgress.SpeedLevel, MetaProgress.SpeedCost, MetaProgress.TryBuySpeed);

            if (GUI.Button(new Rect(panel.x + 18f, panel.y + 212f, 384f, 44f),
                    "ВОЙТИ В ПОРТАЛ  (E)", _buttonStyle))
            {
                EnterPortal();
            }

            GUI.Label(new Rect(panel.x + 18f, panel.y + 262f, 384f, 22f),
                "Смерть на планете вернёт вас сюда с добычей", _hintStyle);
        }

        private void DrawUpgradeRow(Rect rect, string title, string effect, int level, int cost,
            System.Func<bool> buy)
        {
            GUI.Label(new Rect(rect.x, rect.y + 2f, 240f, 22f), $"{title}  {effect}", _textStyle);
            GUI.Label(new Rect(rect.x, rect.y + 22f, 240f, 20f), $"уровень {level}/{MetaProgress.MaxLevel}",
                _hintStyle);

            var maxed = level >= MetaProgress.MaxLevel;
            var affordable = !maxed && RunSession.Gold >= cost;

            var caption = maxed ? "максимум" : $"купить · {cost}";

            var previous = GUI.enabled;
            GUI.enabled = affordable;

            if (GUI.Button(new Rect(rect.x + 250f, rect.y + 4f, 134f, 34f), caption, _buttonStyle))
            {
                if (buy())
                    SaveSystem.Save();
            }

            GUI.enabled = previous;
        }

        private static int TotalLevels()
        {
            return MetaProgress.DamageLevel + MetaProgress.HealthLevel + MetaProgress.SpeedLevel;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                normal = { textColor = Color.white }
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.75f, 0.78f, 0.85f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        }
    }
}
