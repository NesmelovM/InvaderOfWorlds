using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace InvaderOfWorlds.EditorTools
{
    /// <summary>
    /// Сборка Windows-билда из командной строки и из меню.
    /// Проверяет то, что не видно в редакторе: загрузку сцен по имени,
    /// модели из Resources и процедурный звук.
    /// </summary>
    public static class BuildScript
    {
        private const string BuildPath = "Builds/Windows/InvaderOfWorlds.exe";

        [MenuItem("Invader of Worlds/Собрать Windows-билд")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Hub.unity", "Assets/Scenes/TestPlanet.unity" },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log($"[BUILD] {summary.result}: {summary.totalSize / (1024 * 1024)} МБ, " +
                      $"ошибок {summary.totalErrors}, предупреждений {summary.totalWarnings}");

            if (summary.result == BuildResult.Succeeded)
                return;

            Debug.LogError("[BUILD] Сборка не удалась, смотрите ошибки выше.");
            EditorApplication.Exit(1);
        }
    }
}
