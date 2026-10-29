using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Окно «Поселения»: ручная правка уже созданного мира.
    ///
    /// Планета генерируется в рантайме, поэтому окно работает в Play Mode:
    /// оно показывает, кто из врагов висит над грунтом, и даёт кнопки, чтобы
    /// починить это на месте — по одному или всех сразу.
    /// </summary>
    public class SettlementToolWindow : EditorWindow
    {
        private Vector2 _scroll;
        private float _floatingThreshold = 0.25f;
        private List<WorldFix.EnemyInfo> _enemies = new List<WorldFix.EnemyInfo>();
        private string _status = "Запустите Play Mode, чтобы увидеть мир.";

        [MenuItem("Инструменты разработчика/Поселения", false, 15)]
        public static void Open()
        {
            GetWindow<SettlementToolWindow>("Поселения");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Правка сгенерированного мира", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Планета создаётся при запуске игры, поэтому окно работает в Play Mode.\n" +
                    "Нажмите Play, вернитесь сюда и обновите список.", MessageType.Info);

                if (GUILayout.Button("Обновить"))
                    Refresh();

                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Обновить", GUILayout.Height(24f)))
                    Refresh();

                if (GUILayout.Button("Проверить заново", GUILayout.Height(24f)))
                    Refresh();
            }

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Выровнять всех по грунту", GUILayout.Height(26f)))
                {
                    var fixedCount = WorldFix.AlignAllToGround();
                    _status = $"Выровнено врагов: {fixedCount}";
                    Refresh();
                }

                if (GUILayout.Button("Убрать зависших", GUILayout.Height(26f)))
                {
                    var removed = WorldFix.ClearFloating(_floatingThreshold);
                    _status = $"Удалено зависших: {removed}";
                    Refresh();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Портал на свободное место", GUILayout.Height(26f)))
                {
                    _status = WorldFix.MovePortalToFreeSpot(out var position)
                        ? $"Портал перенесён: {position.x:F0}, {position.y:F0}, {position.z:F0}"
                        : "Не удалось найти свободное место рядом с порталом.";
                    Refresh();
                }
            }

            _floatingThreshold = EditorGUILayout.Slider(
                new GUIContent("Порог «висит», м", "На сколько ступни выше грунта считать парением"),
                _floatingThreshold, 0.05f, 1f);

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(_status, MessageType.None);

            if (_enemies.Count == 0)
            {
                EditorGUILayout.HelpBox("Врагов в сцене нет — возможно, планета ещё не создана.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Врагов: {_enemies.Count}", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var info in _enemies)
            {
                if (info.Enemy == null)
                    continue;

                var floating = Mathf.Abs(info.FeetOffset) >= _floatingThreshold;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var marker = floating ? "▲" : "•";
                    EditorGUILayout.LabelField(
                        $"{marker} {info.Name}   рост {info.Height:F2} м   " +
                        $"ступни {(info.FeetOffset >= 0f ? "+" : "")}{info.FeetOffset:F2} м",
                        GUILayout.MinWidth(320f));

                    if (GUILayout.Button("К грунту", GUILayout.Width(90f)))
                    {
                        WorldFix.AlignToGround(info.Enemy);
                        Refresh();
                        GUIUtility.ExitGUI();
                        return;
                    }

                    if (GUILayout.Button("Выбрать", GUILayout.Width(80f)))
                        Selection.activeGameObject = info.Enemy.gameObject;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Refresh()
        {
            _enemies = WorldFix.Inspect();

            if (!Application.isPlaying)
            {
                _status = "Запустите Play Mode, чтобы увидеть мир.";
                return;
            }

            var floating = 0;

            foreach (var info in _enemies)
            {
                if (Mathf.Abs(info.FeetOffset) >= _floatingThreshold)
                    floating++;
            }

            _status = _enemies.Count == 0
                ? "Врагов не найдено."
                : $"Врагов {_enemies.Count}, из них висят {floating}.";

            Repaint();
        }
    }
}
