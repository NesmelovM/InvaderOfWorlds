using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Состояние забега между планетами: всё, что должно пережить перезагрузку сцены.
    ///
    /// При переходе «дальше» игрок, его оружие и уровень создаются заново, поэтому
    /// билд, сложность и сид хранятся здесь. При возврате в хаб состояние сбрасывается.
    /// </summary>
    public static class RunState
    {
        public static int PlanetIndex { get; private set; }
        public static int Seed { get; private set; }
        public static float Difficulty { get; private set; } = 1f;

        public static int Level = 1;
        /// <summary>Сколько планет текущего биома уже пройдено в забеге.</summary>
        public static int PlanetsInBiome;
        public static float EnemySpeedMultiplier = 1f;
        public static float HealPerKill;
        public static float WeaponDamageMultiplier = 1f;
        public static float HealthBonus;
        public static float SpeedMultiplier = 1f;
        public static float AttackSpeedMultiplier = 1f;
        public static float ExperienceMultiplier = 1f;
        public static float ExplosionDamage;
        public static float ExplosionRadius;
        public static float RegenPerSecond;
        public static int ExtraShots;

        /// <summary>Усиления конкретного оружия: они должны переживать смену планеты.</summary>
        public struct WeaponUpgrade
        {
            public int ExtraShots;
            public float DamageMult;
            public float CooldownDiv;
            public float RangeMult;

            public static WeaponUpgrade Default => new WeaponUpgrade
            {
                DamageMult = 1f,
                CooldownDiv = 1f,
                RangeMult = 1f
            };
        }

        public static readonly Dictionary<int, WeaponUpgrade> WeaponUpgrades =
            new Dictionary<int, WeaponUpgrade>();

        /// <summary>Идентификаторы оружия из оружейной, которые несёт игрок.</summary>
        public static readonly List<int> Loadout = new List<int>();

        /// <summary>Биом текущей планеты: внешний вид врагов и окружения.</summary>
        public static BiomeDefinition ActiveBiome;

        public static void Reset()
        {
            PlanetIndex = 0;
            Difficulty = 1f;
            Level = 1;
            PlanetsInBiome = 0;
            EnemySpeedMultiplier = 1f;
            HealPerKill = 0f;
            WeaponDamageMultiplier = 1f;
            HealthBonus = 0f;
            SpeedMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
            ExperienceMultiplier = 1f;
            ExplosionDamage = 0f;
            ExplosionRadius = 0f;
            RegenPerSecond = 0f;
            ExtraShots = 0;
            WeaponUpgrades.Clear();
            Loadout.Clear();
            ActiveBiome = null;
            Seed = Random.Range(1, int.MaxValue);
        }

        /// <summary>Следующая планета: новый сид, сложность выше в полтора раза.</summary>
        public static void AdvancePlanet()
        {
            PlanetIndex++;
            Difficulty *= Balance.DifficultyStep;
            Seed = Random.Range(1, int.MaxValue);
        }

        public static void SetSeed(int seed)
        {
            Seed = seed;
        }
    }
}
