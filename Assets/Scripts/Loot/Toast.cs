using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Всплывающие подписи о том, что подобрал игрок.
    ///
    /// Сообщения показываются стопкой и живут достаточно долго, чтобы их
    /// прочитать, даже если предметы подобраны подряд один за другим.
    /// </summary>
    public static class Toast
    {
        private const float Lifetime = 4.5f;
        private const int MaxLines = 6;

        private struct Line
        {
            public string Text;
            public float BornAt;
            public Color Color;
        }

        private static readonly List<Line> Lines = new List<Line>(MaxLines);
        private static GUIStyle _style;

        public static void Show(string text, Color? color = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (Lines.Count >= MaxLines)
                Lines.RemoveAt(0);

            Lines.Add(new Line
            {
                Text = text,
                BornAt = Time.unscaledTime,
                Color = color ?? new Color(0.95f, 0.88f, 0.60f)
            });

            var drawer = Object.FindAnyObjectByType<ToastDrawer>();

            if (drawer == null)
                new GameObject("Toast Drawer").AddComponent<ToastDrawer>();
        }

        /// <summary>Очищает подписи, например при переходе на другую планету.</summary>
        public static void Clear()
        {
            Lines.Clear();
        }

        /// <summary>Сколько сообщений сейчас видно: они рисуются стопкой.</summary>
        public static int Collect(List<VisibleLine> target)
        {
            target.Clear();

            for (var i = Lines.Count - 1; i >= 0; i--)
            {
                var age = Time.unscaledTime - Lines[i].BornAt;

                if (age > Lifetime)
                {
                    Lines.RemoveAt(i);
                    continue;
                }

                var alpha = 1f - Mathf.Clamp01((age - Lifetime * 0.7f) / (Lifetime * 0.3f));

                // Новые сверху, поэтому собираем в обратном порядке.
                target.Add(new VisibleLine
                {
                    Text = Lines[i].Text,
                    Color = Lines[i].Color,
                    Alpha = alpha
                });
            }

            target.Reverse();
            return target.Count;
        }

        public struct VisibleLine
        {
            public string Text;
            public Color Color;
            public float Alpha;
        }

        public static GUIStyle Style
        {
            get
            {
                _style ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                return _style;
            }
        }
    }

    /// <summary>Рисует стопку сообщений из <see cref="Toast"/>.</summary>
    public class ToastDrawer : MonoBehaviour
    {
        private readonly List<Toast.VisibleLine> _lines = new List<Toast.VisibleLine>(6);

        private void OnGUI()
        {
            var count = Toast.Collect(_lines);

            if (count == 0)
                return;

            UiCanvas.Begin();

            const float lineHeight = 34f;
            var top = UiCanvas.Height * 0.52f;
            var style = Toast.Style;

            for (var i = 0; i < count; i++)
            {
                var line = _lines[i];
                var y = top + i * lineHeight;
                var alpha = line.Alpha;

                style.normal.textColor = new Color(line.Color.r, line.Color.g, line.Color.b, alpha);

                HealthBarDrawer.DrawRect(
                    new Rect(UiCanvas.Width * 0.5f - 220f, y - 3f, 440f, 32f),
                    new Color(0.04f, 0.05f, 0.08f, 0.7f * alpha));

                GUI.Label(new Rect(0f, y, UiCanvas.Width, lineHeight), line.Text, style);
            }

            UiCanvas.End();
        }
    }
}
