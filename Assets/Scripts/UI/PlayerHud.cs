using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Полоска здоровья игрока в левом нижнем углу.
    /// Как и полоски врагов, рисуется через GUI — без Canvas и ассетов.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHud : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField, Min(1f)] private float _width = 280f;
        [SerializeField, Min(1f)] private float _height = 20f;
        [SerializeField, Min(0f)] private float _margin = 24f;
        [SerializeField] private Color _fill = new Color(0.25f, 0.8f, 0.35f);

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

            var panel = new Rect(_margin, Screen.height - _margin - _height - 26f, _width + 12f, _height + 34f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.45f));

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            GUI.Label(new Rect(panel.x + 8f, panel.y + 3f, 300f, 22f),
                $"HP {_health.Current:F0} / {_health.Max:F0}", _labelStyle);

            var bar = new Rect(panel.x + 6f, panel.y + 26f, _width, _height);
            HealthBarDrawer.Draw(bar, _health.Normalized, _fill);
        }
    }
}
