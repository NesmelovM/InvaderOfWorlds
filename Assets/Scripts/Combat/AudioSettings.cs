using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Громкость игры. Одна на всё: удары, выстрелы, шаги и портал идут через
    /// AudioListener, поэтому достаточно менять его уровень.
    ///
    /// Значение хранится в PlayerPrefs и применяется до загрузки первой сцены,
    /// иначе первый звук успевает сыграть на прежней громкости.
    /// </summary>
    public static class AudioSettings
    {
        private const string VolumeKey = "iw.volume";
        private const float DefaultVolume = 0.8f;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
            set
            {
                var clamped = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, clamped);
                PlayerPrefs.Save();
                AudioListener.volume = clamped;
            }
        }

        /// <summary>Применяет сохранённую громкость при старте игры.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStart()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
        }
    }
}
