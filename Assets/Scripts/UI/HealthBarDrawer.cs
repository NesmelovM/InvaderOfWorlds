using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Рисование полосок без единого ассета: прямоугольники через GUI и
    /// встроенная белая текстура. Достаточно для прототипа, потом заменим на Canvas.
    /// </summary>
    public static class HealthBarDrawer
    {
        private static readonly Color Background = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color Border = new Color(0f, 0f, 0f, 0.85f);

        public static void Draw(Rect rect, float normalized, Color fill)
        {
            normalized = Mathf.Clamp01(normalized);

            DrawRect(rect, Border);

            var inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            DrawRect(inner, Background);

            if (normalized <= 0f)
                return;

            var fillRect = new Rect(inner.x, inner.y, inner.width * normalized, inner.height);
            DrawRect(fillRect, fill);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
