using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>Боевые параметры одного типа врага: правятся по биомам.</summary>
    [Serializable]
    public class EnemyStats
    {
        [Min(1f)] public float Health = 60f;
        [Min(0f)] public float Damage = 10f;
        [Min(0.1f)] public float Speed = 2.5f;
        [Min(0.05f)] public float AttackCooldown = 1.5f;
        [Min(0.5f)] public float AttackRange = 2.2f;
        [Tooltip("Сколько опыта даёт убийство этого врага")]
        [Min(0)] public int Experience = 10;
    }

    /// <summary>Параметры всех типов врагов одного биома.</summary>
    [Serializable]
    public class BiomeEnemyBalance
    {
        public string Biome = "";

        public EnemyStats Melee = new EnemyStats { Health = 60f, Damage = 10f, Speed = 2.5f, AttackCooldown = 1.5f, AttackRange = 2.2f, Experience = 10 };
        public EnemyStats Ranged = new EnemyStats { Health = 48f, Damage = 10f, Speed = 2.2f, AttackCooldown = 1.6f, AttackRange = 16f, Experience = 14 };
        public EnemyStats Elite = new EnemyStats { Health = 180f, Damage = 14f, Speed = 2.6f, AttackCooldown = 1.8f, AttackRange = 2.6f, Experience = 50 };
        public EnemyStats EliteRanged = new EnemyStats { Health = 140f, Damage = 12f, Speed = 2.4f, AttackCooldown = 1.6f, AttackRange = 20f, Experience = 70 };
        public EnemyStats Boss = new EnemyStats { Health = 520f, Damage = 18f, Speed = 2.4f, AttackCooldown = 6f, AttackRange = 5f, Experience = 300 };
    }

    /// <summary>
    /// Что переживает смерть игрока или переход на следующую планету.
    /// Правится в окне баланса.
    /// </summary>
    [Serializable]
    public class CarryOverRules
    {
        [Tooltip("Золото забега")] public bool Gold = true;
        [Tooltip("Опыт забега")] public bool Experience = true;
        [Tooltip("Уровень и порог следующего")] public bool Level;
        [Tooltip("Набор оружия, взятого в забеге")] public bool Loadout;
        [Tooltip("Бонус здоровья из карточек")] public bool HealthBonus;
        [Tooltip("Бонус скорости из карточек")] public bool SpeedBonus;
        [Tooltip("Бонус урона из карточек")] public bool DamageBonus;
        [Tooltip("Взрыв, регенерация, залп, вампиризм и замедление")] public bool CardEffects;
    }

    /// <summary>Значения карточек одной редкости: их правит инструмент баланса.</summary>
    [Serializable]
    public class CardTierBalance
    {
        [Tooltip("Множитель урона")] public float DamageMult = 1.10f;
        [Tooltip("Прибавка здоровья")] public float HealthAdd = 15f;
        [Tooltip("Множитель скорости")] public float SpeedMult = 1.05f;
        [Tooltip("Здоровье за убийство")] public float HealPerKill = 1f;
        [Tooltip("Множитель скорости атаки")] public float AttackSpeedMult = 1.10f;
        [Tooltip("Множитель получаемого опыта")] public float ExperienceMult = 1.10f;
        [Tooltip("Замедление врагов, доля")] public float SlowPercent = 0.08f;
        [Tooltip("Урон взрыва при убийстве")] public float ExplosionDamage;
        [Tooltip("Радиус взрыва, м")] public float ExplosionRadius = 4f;
        [Tooltip("Регенерация здоровья в секунду")] public float RegenPerSecond;
        [Tooltip("Сколько снарядов добавляет карточка")] public int ExtraShots;
    }

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

        [Header("Карточки уровня")]
        public CardTierBalance CommonCards = new CardTierBalance
        {
            DamageMult = 1.10f, HealthAdd = 15f, SpeedMult = 1.05f, HealPerKill = 1f,
            AttackSpeedMult = 1.10f, ExperienceMult = 1.10f, SlowPercent = 0.08f,
            ExplosionDamage = 10f, ExplosionRadius = 3.5f, RegenPerSecond = 0.5f, ExtraShots = 0
        };

        public CardTierBalance RareCards = new CardTierBalance
        {
            DamageMult = 1.20f, HealthAdd = 30f, SpeedMult = 1.10f, HealPerKill = 3f,
            AttackSpeedMult = 1.20f, ExperienceMult = 1.20f, SlowPercent = 0.12f,
            ExplosionDamage = 20f, ExplosionRadius = 4f, RegenPerSecond = 1f, ExtraShots = 1
        };

        public CardTierBalance EpicCards = new CardTierBalance
        {
            DamageMult = 1.35f, HealthAdd = 50f, SpeedMult = 1.18f, HealPerKill = 6f,
            AttackSpeedMult = 1.35f, ExperienceMult = 1.35f, SlowPercent = 0.20f,
            ExplosionDamage = 35f, ExplosionRadius = 5f, RegenPerSecond = 2f, ExtraShots = 1
        };

        [Header("Опыт по планетам")]
        [Tooltip("Во сколько раз больше опыта за врага на каждой следующей планете забега")]
        [Min(1f)] public float ExperiencePerPlanet = 1.25f;

        [Header("Что переносится")]
        [Tooltip("Правила после смерти: остальное сбрасывается")]
        public CarryOverRules OnDeath = new CarryOverRules { Level = false, Loadout = false };

        [Tooltip("Правила при переходе на следующую планету")]
        public CarryOverRules OnNextPlanet = new CarryOverRules
        {
            Level = true, Loadout = true, HealthBonus = true,
            SpeedBonus = true, DamageBonus = true, CardEffects = true
        };

        [Header("Враги по биомам")]
        [Tooltip("Индекс записи = индекс биома в каталоге")]
        public List<BiomeEnemyBalance> BiomeEnemies = new List<BiomeEnemyBalance>();

        [Header("Биомы")]
        [Tooltip("Сколько планет одного биома игрок проходит, прежде чем ему предложат следующий биом")]
        [Min(1)] public int PlanetsPerBiome = 3;

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
        public static float ExperiencePerPlanet => Config != null ? Config.ExperiencePerPlanet : 1.25f;
        public static int PlanetsPerBiome => Config != null ? Config.PlanetsPerBiome : 3;

        /// <summary>Параметры врага для текущего биома; null — использовать значения из кода.</summary>
        public static EnemyStats EnemyStatsFor(int biomeIndex, EnemyType type)
        {
            if (Config == null || biomeIndex < 0 || biomeIndex >= Config.BiomeEnemies.Count)
                return null;

            var block = Config.BiomeEnemies[biomeIndex];

            if (block == null)
                return null;

            return type switch
            {
                EnemyType.Melee => block.Melee,
                EnemyType.Ranged => block.Ranged,
                EnemyType.Elite => block.Elite,
                EnemyType.EliteRanged => block.EliteRanged,
                EnemyType.Boss => block.Boss,
                _ => null
            };
        }

        /// <summary>Параметры врага для биома, в котором проходит забег.</summary>
        public static EnemyStats EnemyStatsForCurrentBiome(EnemyType type)
        {
            return EnemyStatsFor(Progress.SelectedBiome, type);
        }
        public static CarryOverRules DeathRules => Config != null ? Config.OnDeath : new CarryOverRules();
        public static CarryOverRules PlanetRules => Config != null ? Config.OnNextPlanet : new CarryOverRules { Level = true, Loadout = true, HealthBonus = true, SpeedBonus = true, DamageBonus = true, CardEffects = true };

        /// <summary>Значения карточек для редкости: 0 — обычная, 1 — редкая, 2 — эпическая.</summary>
        public static CardTierBalance Cards(int rarity)
        {
            if (Config == null)
                return null;

            return rarity switch
            {
                1 => Config.RareCards,
                2 => Config.EpicCards,
                _ => Config.CommonCards
            };
        }

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
