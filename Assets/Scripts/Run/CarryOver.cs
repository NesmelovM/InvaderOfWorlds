using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Правила переноса прогресса между планетами и через смерть.
    ///
    /// Что именно остаётся, задаётся в окне баланса: золото, опыт, уровень,
    /// набор оружия, бонусы здоровья, скорости и урона, эффекты карточек.
    /// </summary>
    public static class CarryOver
    {
        /// <summary>Снимок состояния забега: его сохраняем перед сбросом.</summary>
        public struct Snapshot
        {
            public int Level;
            public float HealthBonus;
            public float SpeedMultiplier;
            public float AttackSpeedMultiplier;
            public float ExperienceMultiplier;
            public float WeaponDamageMultiplier;
            public float EnemySpeedMultiplier;
            public float HealPerKill;
            public float ExplosionDamage;
            public float ExplosionRadius;
            public float RegenPerSecond;
            public int ExtraShots;
            public int PlanetsInBiome;
            public List<int> Loadout;
        }

        /// <summary>Снимок, сделанный в момент смерти.</summary>
        public static Snapshot DeathSnapshot { get; private set; }

        public static Snapshot Capture()
        {
            return new Snapshot
            {
                Level = RunState.Level,
                HealthBonus = RunState.HealthBonus,
                SpeedMultiplier = RunState.SpeedMultiplier,
                AttackSpeedMultiplier = RunState.AttackSpeedMultiplier,
                ExperienceMultiplier = RunState.ExperienceMultiplier,
                WeaponDamageMultiplier = RunState.WeaponDamageMultiplier,
                EnemySpeedMultiplier = RunState.EnemySpeedMultiplier,
                HealPerKill = RunState.HealPerKill,
                ExplosionDamage = RunState.ExplosionDamage,
                ExplosionRadius = RunState.ExplosionRadius,
                RegenPerSecond = RunState.RegenPerSecond,
                ExtraShots = RunState.ExtraShots,
                PlanetsInBiome = RunState.PlanetsInBiome,
                Loadout = new List<int>(RunState.Loadout)
            };
        }

        /// <summary>Запоминает состояние на случай смерти.</summary>
        public static void RememberDeath()
        {
            DeathSnapshot = Capture();
        }

        /// <summary>Возвращает разрешённые правилами поля из снимка.</summary>
        public static void Restore(Snapshot snapshot, CarryOverRules rules)
        {
            if (rules == null)
                return;

            if (rules.Level)
                RunState.Level = snapshot.Level;

            if (rules.HealthBonus)
                RunState.HealthBonus = snapshot.HealthBonus;

            if (rules.SpeedBonus)
                RunState.SpeedMultiplier = snapshot.SpeedMultiplier;

            if (rules.DamageBonus)
            {
                RunState.WeaponDamageMultiplier = snapshot.WeaponDamageMultiplier;
                RunState.AttackSpeedMultiplier = snapshot.AttackSpeedMultiplier;
                RunState.ExperienceMultiplier = snapshot.ExperienceMultiplier;
            }

            if (rules.CardEffects)
            {
                RunState.EnemySpeedMultiplier = snapshot.EnemySpeedMultiplier;
                RunState.HealPerKill = snapshot.HealPerKill;
                RunState.ExplosionDamage = snapshot.ExplosionDamage;
                RunState.ExplosionRadius = snapshot.ExplosionRadius;
                RunState.RegenPerSecond = snapshot.RegenPerSecond;
                RunState.ExtraShots = snapshot.ExtraShots;
            }

            if (rules.Loadout)
            {
                RunState.Loadout.Clear();
                RunState.Loadout.AddRange(snapshot.Loadout);
            }

            RunState.PlanetsInBiome = snapshot.PlanetsInBiome;
        }

        /// <summary>
        /// Переход на следующую планету: обнуляет то, что по правилам не переносится.
        /// </summary>
        public static void ApplyPlanetRules()
        {
            var rules = Balance.PlanetRules;

            if (rules == null)
                return;

            if (!rules.DamageBonus)
            {
                RunState.WeaponDamageMultiplier = 1f;
                RunState.AttackSpeedMultiplier = 1f;
                RunState.ExperienceMultiplier = 1f;
            }

            if (!rules.HealthBonus)
                RunState.HealthBonus = 0f;

            if (!rules.SpeedBonus)
                RunState.SpeedMultiplier = 1f;

            if (!rules.CardEffects)
            {
                RunState.EnemySpeedMultiplier = 1f;
                RunState.HealPerKill = 0f;
                RunState.ExplosionDamage = 0f;
                RunState.ExplosionRadius = 0f;
                RunState.RegenPerSecond = 0f;
                RunState.ExtraShots = 0;
            }

            if (!rules.Loadout)
                RunState.Loadout.Clear();
        }
    }
}
