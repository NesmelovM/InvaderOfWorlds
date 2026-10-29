using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>Параметры одного оружия, которые правятся в инструменте баланса.</summary>
    [Serializable]
    public class WeaponBalance
    {
        public float Damage = 20f;
        public float Range = 3f;
        public float Cooldown = 0.8f;
        [Tooltip("Сколько атак за один тик")]
        public int Shots = 1;
        public float ProjectileSpeed = 24f;
        public int Cost;
    }

    /// <summary>
    /// Единый конфиг баланса: оружие, опыт, экономика, враги, игрок, сложность.
    ///
    /// Лежит в Resources, поэтому игра читает его и в редакторе, и в собранном билде.
    /// Если файла нет, значения берутся из кода — игра не ломается.
    /// </summary>
    [CreateAssetMenu(fileName = "Config", menuName = "Invader of Worlds/Баланс")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Оружие")]
        public List<WeaponBalance> Weapons = new List<WeaponBalance>();

        [Header("Опыт и уровни")]
        [Min(0.1f)] public float ExperienceMultiplier = 1f;
        [Min(1)] public int LevelBaseExperience = 45;
        [Min(1f)] public float LevelGrowth = 1.27f;

        [Header("Экономика")]
        [Min(0f)] public float GoldMultiplier = 1f;
        [Range(0f, 1f)] public float DeathPenaltyShare = 0.3f;

        [Header("Враги")]
        [Min(0.1f)] public float EnemyHealthMultiplier = 1f;
        [Min(0.1f)] public float EnemyDamageMultiplier = 1f;
        [Min(0.1f)] public float EnemySpeedMultiplier = 1f;

        [Header("Игрок")]
        [Min(1f)] public float PlayerHealth = 100f;
        [Min(1f)] public float WalkSpeed = 10f;
        [Min(1f)] public float SprintSpeed = 20f;

        [Header("Сложность")]
        [Min(1f)] public float DifficultyStep = 1.32f;
    }

    /// <summary>Доступ к конфигу баланса с безопасными значениями по умолчанию.</summary>
    public static class Balance
    {
        private const string ResourcePath = "Balance/Config";

        private static BalanceConfig _config;
        private static bool _loaded;

        public static BalanceConfig Config
        {
            get
            {
                if (!_loaded)
                {
                    _config = Resources.Load<BalanceConfig>(ResourcePath);
                    _loaded = true;
                }

                return _config;
            }
        }

        public static float ExperienceMultiplier => Config != null ? Config.ExperienceMultiplier : 1f;
        public static int LevelBaseExperience => Config != null ? Config.LevelBaseExperience : 60;
        public static float LevelGrowth => Config != null ? Config.LevelGrowth : 1.45f;
        public static float GoldMultiplier => Config != null ? Config.GoldMultiplier : 1f;
        public static float DeathPenaltyShare => Config != null ? Config.DeathPenaltyShare : 0.3f;
        public static float EnemyHealthMultiplier => Config != null ? Config.EnemyHealthMultiplier : 1f;
        public static float EnemyDamageMultiplier => Config != null ? Config.EnemyDamageMultiplier : 1f;
        public static float EnemySpeedMultiplier => Config != null ? Config.EnemySpeedMultiplier : 1f;
        public static float PlayerHealth => Config != null ? Config.PlayerHealth : 100f;
        public static float WalkSpeed => Config != null ? Config.WalkSpeed : 10f;
        public static float SprintSpeed => Config != null ? Config.SprintSpeed : 20f;
        public static float DifficultyStep => Config != null ? Config.DifficultyStep : 1.5f;

        /// <summary>Сбрасывает кеш: нужно после правок конфига в редакторе.</summary>
        public static void ResetCache()
        {
            _config = null;
            _loaded = false;
        }

        /// <summary>Переопределение оружия из конфига, если оно там есть.</summary>
        public static bool TryWeapon(int id, out WeaponBalance weapon)
        {
            weapon = null;

            if (Config == null || id < 0 || id >= Config.Weapons.Count)
                return false;

            weapon = Config.Weapons[id];
            return weapon != null;
        }

        public static int WeaponCost(int id, int fallback)
        {
            return TryWeapon(id, out var weapon) ? weapon.Cost : fallback;
        }
    }
}
