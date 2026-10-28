using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Хаб: магазин постоянных улучшений и вход в портал.
    /// Живёт в отдельной сцене, поэтому забег и подготовка не мешают друг другу.
    /// </summary>
    [DisallowMultipleComponent]
    public class HubDirector : MonoBehaviour
    {
        [SerializeField] private string _planetSceneName = "TestPlanet";

        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _buttonStyle;

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
            SceneManager.LoadScene(_planetSceneName);
        }

        private void OnGUI()
        {
            EnsureStyles();

            var panel = new Rect(24f, 24f, 400f, 260f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.6f));

            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, 360f, 28f), "БАШНЯ МАГОВ", _titleStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 44f, 360f, 24f),
                $"Золото: {RunSession.Gold}    Опыт: {RunSession.Experience}", _textStyle);

            DrawUpgradeRow(new Rect(panel.x + 16f, panel.y + 78f, 368f, 40f), "Урон",
                MetaProgress.DamageLevel, MetaProgress.DamageCost, MetaProgress.TryBuyDamage);

            DrawUpgradeRow(new Rect(panel.x + 16f, panel.y + 124f, 368f, 40f), "Здоровье",
                MetaProgress.HealthLevel, MetaProgress.HealthCost, MetaProgress.TryBuyHealth);

            DrawUpgradeRow(new Rect(panel.x + 16f, panel.y + 170f, 368f, 40f), "Скорость",
                MetaProgress.SpeedLevel, MetaProgress.SpeedCost, MetaProgress.TryBuySpeed);

            if (GUI.Button(new Rect(panel.x + 16f, panel.y + 216f, 368f, 34f), "ВОЙТИ В ПОРТАЛ (E)", _buttonStyle))
                EnterPortal();
        }

        private void DrawUpgradeRow(Rect rect, string title, int level, int cost, System.Func<bool> buy)
        {
            GUI.Label(new Rect(rect.x, rect.y, 200f, 24f), $"{title}: ур. {level}/{MetaProgress.MaxLevel}", _textStyle);

            var affordable = level < MetaProgress.MaxLevel && RunSession.Gold >= cost;
            var caption = level >= MetaProgress.MaxLevel ? "максимум" : $"купить · {cost}";

            var previous = GUI.enabled;
            GUI.enabled = affordable;

            if (GUI.Button(new Rect(rect.x + 210f, rect.y, 150f, 28f), caption, _buttonStyle))
                buy();

            GUI.enabled = previous;
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
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
        }
    }
}
