using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Полоска здоровья игрока в левом нижнем углу.
    /// Рисуется на общем виртуальном холсте, поэтому её размер одинаков на любом окне
    /// и она не наезжает на экран выбора карточки.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHud : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Color _fill = new Color(0.25f, 0.8f, 0.35f);
        [SerializeField] private Color _lowFill = new Color(0.85f, 0.25f, 0.2f);

        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();
        }

        private void OnGUI()
        {
            if (_health == null)
                return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            UiCanvas.Begin();

            var panel = new Rect(40f, UiCanvas.Height - 118f, 380f, 84f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.55f));

            GUI.Label(new Rect(panel.x + 16f, panel.y + 8f, panel.width - 32f, 28f),
                $"HP {_health.Current:F0} / {_health.Max:F0}", _labelStyle);

            var bar = new Rect(panel.x + 16f, panel.y + 44f, panel.width - 32f, 26f);
            var fill = _health.Normalized < 0.3f ? _lowFill : _fill;
            HealthBarDrawer.Draw(bar, _health.Normalized, fill);

            UiCanvas.End();
        }
    }
}
