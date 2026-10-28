using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Счётчик забега в правом верхнем углу: золото, опыт, убийства.
    /// Виден всегда, чтобы было понятно, растёт ли награда за забег.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunHud : MonoBehaviour
    {
        private GUIStyle _style;

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
            }

            var panel = new Rect(Screen.width - 500f, 10f, 488f, 34f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.45f));

            GUI.Label(new Rect(panel.x, panel.y + 6f, panel.width - 8f, 24f),
                $"Кошелёк {RunSession.Gold}   Опыт {RunSession.Experience}   " +
                $"Забег +{RunSession.RunGold} / +{RunSession.RunExperience}   Убито {RunSession.Kills}", _style);
        }
    }
}
