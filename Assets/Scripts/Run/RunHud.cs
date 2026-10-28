using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Счётчик забега в правом верхнем углу: уровень, опыт, золото и убийства.
    /// Уровень с опытом видны постоянно — иначе непонятно, когда ждать карточку.
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
                    fontSize = 14,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = new Color(0.8f, 0.85f, 0.95f) }
                };
            }

            var panel = new Rect(Screen.width - 500f, 10f, 488f, 58f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.45f));

            GUI.Label(new Rect(panel.x, panel.y + 4f, panel.width - 8f, 24f),
                $"Кошелёк {RunSession.Gold}   Опыт {RunSession.Experience}   " +
                $"Забег +{RunSession.RunGold} / +{RunSession.RunExperience}   Убито {RunSession.Kills}", _style);

            var levelText = _level != null
                ? $"Уровень {_level.Level}   до следующего: {Mathf.Max(0, _level.ExperienceForNext - _level.Experience)} опыта"
                : "Уровень —";

            GUI.Label(new Rect(panel.x, panel.y + 30f, panel.width - 8f, 22f), levelText, _hintStyle);
        }
    }
}
