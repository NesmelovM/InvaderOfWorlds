using UnityEditor;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Unity 6 по умолчанию генерирует решение в формате .slnx, а Visual Studio 2026
    /// (сборка 18.x) такой файл не открывает — вместо проекта появляется пустое окно.
    ///
    /// Visual Studio-пакет Unity ищет у постпроцессоров статический метод
    /// OnSelectingCSProjectStyle и, если он вернул "Legacy", пишет обычный .sln,
    /// который VS понимает. Этот класс и возвращает нужный стиль.
    /// </summary>
    public class ProjectGenerationStyle : AssetPostprocessor
    {
        // ReSharper disable once UnusedMember.Local
        private static string OnSelectingCSProjectStyle()
        {
            return "Legacy";
        }
    }
}
