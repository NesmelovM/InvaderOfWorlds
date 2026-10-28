using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Виртуальный холст интерфейса: всё рисуется в системе 1280×720 и масштабируется
    /// под реальный экран с сохранением пропорций.
    ///
    /// Без этого панели на фиксированных пикселях разъезжались на маленьком окне
    /// и наезжали друг на друга.
    /// </summary>
    public static class UiCanvas
    {
        public const float Width = 1280f;
        public const float Height = 720f;

        public static float Scale => Mathf.Min(Screen.width / Width, Screen.height / Height);

        public static void Begin()
        {
            var scale = Mathf.Max(0.01f, Scale);
            var offsetX = (Screen.width - Width * scale) * 0.5f;
            var offsetY = (Screen.height - Height * scale) * 0.5f;

            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity,
                new Vector3(scale, scale, 1f));
        }

        public static void End()
        {
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
