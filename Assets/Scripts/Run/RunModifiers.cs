using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Временные усилители забега. Данные лежат в <see cref="RunState"/>,
    /// чтобы переживать переход на следующую планету: здесь только удобный доступ.
    /// </summary>
    public static class RunModifiers
    {
        public static float EnemySpeedMultiplier => RunState.EnemySpeedMultiplier;
        public static float HealPerKill => RunState.HealPerKill;
        public static float WeaponDamageMultiplier => RunState.WeaponDamageMultiplier;

        public static Health PlayerHealth { get; set; }

        public static void SlowEnemies(float percent)
        {
            RunState.EnemySpeedMultiplier = Mathf.Max(0.25f,
                RunState.EnemySpeedMultiplier * (1f - Mathf.Clamp01(percent)));
        }

        public static void AddHealPerKill(float amount)
        {
            RunState.HealPerKill += Mathf.Max(0f, amount);
        }

        public static void MultiplyWeaponDamage(float multiplier)
        {
            if (multiplier > 0f)
                RunState.WeaponDamageMultiplier *= multiplier;
        }

        public static void Reset()
        {
            PlayerHealth = null;
        }
    }
}
