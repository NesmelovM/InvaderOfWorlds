using System;
using System.IO;
using UnityEngine;

namespace InvaderOfWorlds
{
    [Serializable]
    public class SaveData
    {
        public int gold;
        public int experience;
        public int damageLevel;
        public int healthLevel;
        public int speedLevel;
        public int skillMask;
        public int ownedWeapons;
        public string equippedWeapons = string.Empty;
    }

    /// <summary>
    /// Сохранение прогресса в JSON рядом с данными приложения.
    /// Пока это только золото, опыт и купленные уровни — ровно то, что должно
    /// переживать закрытие игры.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "progress.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static void Save()
        {
            var data = new SaveData
            {
                gold = RunSession.Gold,
                experience = RunSession.Experience,
                damageLevel = MetaProgress.DamageLevel,
                healthLevel = MetaProgress.HealthLevel,
                speedLevel = MetaProgress.SpeedLevel,
                skillMask = SkillTree.Mask,
                ownedWeapons = Armory.OwnedMask,
                equippedWeapons = Armory.SerializeEquipped()
            };

            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(data, true));
                Debug.Log($"[SAVE] Прогресс сохранён: {Path}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Не удалось сохранить прогресс: {exception.Message}");
            }
        }

        public static bool Load()
        {
            try
            {
                if (!File.Exists(Path))
                    return false;

                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));

                if (data == null)
                    return false;

                RunSession.Load(data.gold, data.experience);
                MetaProgress.Load(data.damageLevel, data.healthLevel, data.speedLevel);
                SkillTree.Load(data.skillMask);
                Armory.Load(data.ownedWeapons, data.equippedWeapons);

                Debug.Log($"[SAVE] Прогресс загружен: золото {data.gold}, " +
                          $"урон {data.damageLevel}, здоровье {data.healthLevel}, скорость {data.speedLevel}");

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Не удалось загрузить прогресс: {exception.Message}");
                return false;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Не удалось удалить сохранение: {exception.Message}");
            }
        }
    }
}
