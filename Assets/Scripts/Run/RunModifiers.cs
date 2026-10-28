using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Временные усилители текущего забега. В отличие от мета-прогресса хаба,
    /// они сбрасываются на старте каждого захода — это и есть «билд забега».
    /// </summary>
    public static class RunModifiers
    {
        public static float EnemySpeedMultiplier { get; private set; } = 1f;
        public static float HealPerKill { get; private set; }
        public static float WeaponDamageMultiplier { get; private set; } = 1f;

        public static Health PlayerHealth { get; set; }

        public static void SlowEnemies(float percent)
        {
            EnemySpeedMultiplier = Mathf.Max(0.25f, EnemySpeedMultiplier * (1f - Mathf.Clamp01(percent)));
        }

        public static void AddHealPerKill(float amount)
        {
            HealPerKill += Mathf.Max(0f, amount);
        }

        public static void MultiplyWeaponDamage(float multiplier)
        {
            if (multiplier > 0f)
                WeaponDamageMultiplier *= multiplier;
        }

        public static void Reset()
        {
            EnemySpeedMultiplier = 1f;
            HealPerKill = 0f;
            WeaponDamageMultiplier = 1f;
            PlayerHealth = null;
        }
    }
}
