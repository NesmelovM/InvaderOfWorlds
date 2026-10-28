using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Счётчик забега в правом верхнем углу: уровень, опыт, золото и убийства.
    /// Рисуется на общем виртуальном холсте, поэтому не наезжает на другие панели
    /// и не спорит с экраном выбора карточки.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunHud : MonoBehaviour
    {
        [SerializeField] private RunLevel _level;

        private GUIStyle _style;
        private GUIStyle _hintStyle;

        public void Configure(RunLevel level)
        {
            _level = level;
        }

        private void Awake()
        {
            if (_level == null)
                _level = FindAnyObjectByType<RunLevel>();
        }

        private void OnGUI()
        {
            // Во время выбора карточки экран принадлежит ей.
            if (_level != null && _level.IsChoosing)
                return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = Color.white }
                };

                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = new Color(0.82f, 0.86f, 0.95f) }
                };
            }

            UiCanvas.Begin();

            var panel = new Rect(UiCanvas.Width - 470f, 16f, 440f, 66f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.5f));

            GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, panel.width - 28f, 26f),
                $"Кошелёк {RunSession.Gold}   Опыт {RunSession.Experience}   " +
                $"Забег +{RunSession.RunGold} / +{RunSession.RunExperience}   Убито {RunSession.Kills}", _style);

            var levelText = _level != null
                ? $"Уровень {_level.Level}   до следующего: " +
                  $"{Mathf.Max(0, _level.ExperienceForNext - _level.Experience)} опыта"
                : "Уровень —";

            GUI.Label(new Rect(panel.x + 14f, panel.y + 36f, panel.width - 28f, 24f), levelText, _hintStyle);

            UiCanvas.End();
        }
    }
}
