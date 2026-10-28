using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Мета-прогресс: постоянные улучшения, которые покупаются в хабе за золото
    /// и действуют во всех следующих забегах. Хранится статикой — как и накопления,
    /// переживает перезагрузку сцены.
    /// </summary>
    public static class MetaProgress
    {
        public const int MaxLevel = 5;

        public static int DamageLevel { get; private set; }
        public static int HealthLevel { get; private set; }
        public static int SpeedLevel { get; private set; }

        /// <summary>Насколько сильно герой бьёт: +15% за уровень.</summary>
        public static float DamageMultiplier => 1f + 0.15f * DamageLevel;

        /// <summary>Прибавка к здоровью: +20 за уровень.</summary>
        public static float HealthBonus => 20f * HealthLevel;

        /// <summary>Скорость передвижения: +5% за уровень.</summary>
        public static float SpeedMultiplier => 1f + 0.05f * SpeedLevel;

        public static int DamageCost => 40 + 40 * DamageLevel;
        public static int HealthCost => 30 + 30 * HealthLevel;
        public static int SpeedCost => 25 + 25 * SpeedLevel;

        public static bool TryBuyDamage()
        {
            if (DamageLevel >= MaxLevel || !RunSession.TrySpend(DamageCost))
                return false;

            DamageLevel++;
            return true;
        }

        public static bool TryBuyHealth()
        {
            if (HealthLevel >= MaxLevel || !RunSession.TrySpend(HealthCost))
                return false;

            HealthLevel++;
            return true;
        }

        public static bool TryBuySpeed()
        {
            if (SpeedLevel >= MaxLevel || !RunSession.TrySpend(SpeedCost))
                return false;

            SpeedLevel++;
            return true;
        }

        public static void Reset()
        {
            DamageLevel = 0;
            HealthLevel = 0;
            SpeedLevel = 0;
        }

        /// <summary>Восстановление уровней из сохранения с ограничением диапазона.</summary>
        public static void Load(int damageLevel, int healthLevel, int speedLevel)
        {
            DamageLevel = Mathf.Clamp(damageLevel, 0, MaxLevel);
            HealthLevel = Mathf.Clamp(healthLevel, 0, MaxLevel);
            SpeedLevel = Mathf.Clamp(speedLevel, 0, MaxLevel);
        }
    }
}
