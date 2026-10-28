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
        public static float EnemySpeedMultiplier = 1f;
        public static float HealPerKill;
        public static float WeaponDamageMultiplier = 1f;

        /// <summary>Идентификаторы оружия из оружейной, которые несёт игрок.</summary>
        public static readonly List<int> Loadout = new List<int>();

        public static void Reset()
        {
            PlanetIndex = 0;
            Difficulty = 1f;
            Level = 1;
            EnemySpeedMultiplier = 1f;
            HealPerKill = 0f;
            WeaponDamageMultiplier = 1f;
            Loadout.Clear();
            Seed = Random.Range(1, int.MaxValue);
        }

        /// <summary>Следующая планета: новый сид, сложность выше в полтора раза.</summary>
        public static void AdvancePlanet()
        {
            PlanetIndex++;
            Difficulty *= 1.5f;
            Seed = Random.Range(1, int.MaxValue);
        }

        public static void SetSeed(int seed)
        {
            Seed = seed;
        }
    }
}
