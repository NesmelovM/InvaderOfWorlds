using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Короткие всплывающие подписи: что подобрал игрок.
    ///
    /// Рисуются поверх игры на виртуальном холсте, чтобы читались при любом
    /// разрешении. Живут пару секунд и гаснут.
    /// </summary>
    public static class Toast
    {
        private const float Lifetime = 2.2f;
        private const int MaxLines = 4;

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

        public static bool TryDraw(out string message, out Color color, out float alpha)
        {
            message = null;
            color = Color.white;
            alpha = 0f;

            for (var i = Lines.Count - 1; i >= 0; i--)
            {
                var age = Time.unscaledTime - Lines[i].BornAt;

                if (age > Lifetime)
                {
                    Lines.RemoveAt(i);
                    continue;
                }

                if (message == null)
                {
                    message = Lines[i].Text;
                    color = Lines[i].Color;
                    alpha = 1f - Mathf.Clamp01((age - Lifetime * 0.6f) / (Lifetime * 0.4f));
                }
            }

            return message != null;
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

    /// <summary>Рисует последнее сообщение из <see cref="Toast"/>.</summary>
    public class ToastDrawer : MonoBehaviour
    {
        private void OnGUI()
        {
            if (!Toast.TryDraw(out var message, out var color, out var alpha))
                return;

            UiCanvas.Begin();

            Toast.Style.normal.textColor = new Color(color.r, color.g, color.b, alpha);

            var rect = new Rect(0f, UiCanvas.Height * 0.62f, UiCanvas.Width, 34f);
            HealthBarDrawer.DrawRect(new Rect(rect.x + UiCanvas.Width * 0.5f - 210f, rect.y - 4f, 420f, 42f),
                new Color(0.04f, 0.05f, 0.08f, 0.72f * alpha));
            GUI.Label(rect, message, Toast.Style);

            UiCanvas.End();
        }
    }
}
