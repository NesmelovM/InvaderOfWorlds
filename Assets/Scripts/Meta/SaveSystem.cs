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
        public int portals;
        public int selectedBiome;
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

        /// <summary>
        /// Переносит сохранение из старых папок игры: имя продукта менялось,
        /// а прогресс у игрока терять нельзя. Работает один раз.
        /// </summary>
        public static void MigrateLegacy()
        {
            try
            {
                if (File.Exists(Path))
                    return;

                // Старые пути: другая компания и/или другое имя продукта.
                var root = Directory.GetParent(Application.persistentDataPath)?.Parent?.FullName;

                if (string.IsNullOrEmpty(root))
                    return;

                var candidates = new[]
                {
                    System.IO.Path.Combine(root, "DefaultCompany", "InvaderOfWorlds6", FileName),
                    System.IO.Path.Combine(root, "DefaultCompany", "InvaderOfWorlds", FileName),
                    System.IO.Path.Combine(root, "Dazed Games", "InvaderOfWorlds6", FileName)
                };

                foreach (var candidate in candidates)
                {
                    if (!File.Exists(candidate))
                        continue;

                    Directory.CreateDirectory(Application.persistentDataPath);
                    File.Copy(candidate, Path, true);
                    Debug.Log($"[SAVE] Прогресс перенесён из {candidate}");
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Перенос прогресса не удался: {exception.Message}");
            }
        }

        public static void Save()
        {
            var data = new SaveData
            {
                gold = RunSession.Gold,
                experience = RunSession.Experience,
                damageLevel = MetaProgress.DamageLevel,
                healthLevel = MetaProgress.HealthLevel,
                speedLevel = MetaProgress.SpeedLevel,
                ownedWeapons = Armory.OwnedMask,
                equippedWeapons = Armory.SerializeEquipped(),
                portals = Progress.TotalPortals,
                selectedBiome = Progress.SelectedBiome
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

        /// <summary>
        /// Полный сброс: удаляет файл и возвращает всё к состоянию новой игры.
        /// Нужен, когда прогресс сломан или хочется начать заново.
        /// </summary>
        public static void ResetProgress()
        {
            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Не удалось удалить файл: {exception.Message}");
            }

            RunSession.Load(0, 0);
            RunSession.ResetRun();
            MetaProgress.Load(0, 0, 0);
            Armory.Load(0b11, string.Empty);
            Progress.Load(0, 0);
            RunState.Reset();
            Characters.ResetCache();

            // Как в новой игре: немного золота на первый магазин.
            RunSession.Grant(80);
            Save();

            Debug.Log("[SAVE] Прогресс сброшен");
        }

        public static bool Load()
        {
            try
            {
                MigrateLegacy();

                if (!File.Exists(Path))
                    return false;

                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));

                if (data == null)
                    return false;

                RunSession.Load(data.gold, data.experience);
                MetaProgress.Load(data.damageLevel, data.healthLevel, data.speedLevel);
                Armory.Load(data.ownedWeapons, data.equippedWeapons);
                Progress.Load(data.portals, data.selectedBiome);

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
