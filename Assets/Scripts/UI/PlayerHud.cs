using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Полоска здоровья игрока и под ней — полоска опыта текущего уровня.
    ///
    /// Опыт рисуется здесь, а не текстом в углу: так видно, сколько осталось
    /// до следующей карточки, не отвлекаясь от боя.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHud : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Color _fill = new Color(0.25f, 0.8f, 0.35f);
        [SerializeField] private Color _lowFill = new Color(0.85f, 0.25f, 0.2f);
        [SerializeField] private Color _experienceFill = new Color(0.42f, 0.66f, 1f);

        private GUIStyle _labelStyle;
        private GUIStyle _levelStyle;
        private RunLevel _level;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();

            _level = FindAnyObjectByType<RunLevel>();
        }

        private void OnGUI()
        {
            if (_health == null)
                return;

            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _levelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.72f, 0.80f, 0.95f) }
            };

            UiCanvas.Begin();

            var panel = new Rect(40f, UiCanvas.Height - 140f, 380f, 108f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.5f));

            GUI.Label(new Rect(panel.x + 16f, panel.y + 6f, panel.width - 32f, 26f),
                $"HP {_health.Current:F0} / {_health.Max:F0}", _labelStyle);

            var bar = new Rect(panel.x + 16f, panel.y + 36f, panel.width - 32f, 24f);
            HealthBarDrawer.Draw(bar, _health.Normalized, _health.Normalized < 0.3f ? _lowFill : _fill);

            // Опыт: тонкая полоска под здоровьем и номер уровня рядом.
            var experienceBar = new Rect(panel.x + 16f, panel.y + 68f, panel.width - 32f, 8f);
            HealthBarDrawer.DrawRect(experienceBar, new Color(1f, 1f, 1f, 0.10f));

            if (_level != null)
            {
                HealthBarDrawer.Draw(new Rect(experienceBar.x, experienceBar.y,
                        experienceBar.width, experienceBar.height), _level.LevelProgress, _experienceFill);

                GUI.Label(new Rect(panel.x + 16f, panel.y + 78f, panel.width - 32f, 20f),
                    $"Уровень {_level.Level} · {_level.ExperienceIntoLevel} / {_level.ExperienceForLevel}",
                    _levelStyle);
            }

            UiCanvas.End();
        }
    }
}
