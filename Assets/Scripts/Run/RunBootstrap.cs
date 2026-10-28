using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Применяет купленные в хабе улучшения к игроку в начале забега.
    /// Висит в сцене планеты и срабатывает один раз на старте.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunBootstrap : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _baseHealth = 100f;

        private void Start()
        {            // Добыча прошлого забега уже зачислена, начинаем новый счёт.
            RunSession.ResetRun();

            var player = GameObject.FindWithTag("Player");

            if (player == null)
                return;

            var health = player.GetComponent<Health>();
            var maxHealth = _baseHealth + MetaProgress.HealthBonus + SkillTree.HealthBonus;

            if (health != null)
                health.Configure(maxHealth);

            var attack = player.GetComponent<WeaponSystem>();

            if (attack != null)
            {
                attack.ApplyDamageMultiplier(MetaProgress.DamageMultiplier + SkillTree.DamageBonus);

                // Набор собирается в оружейной хаба, а не задаётся сценой.
                if (Armory.Equipped.Count > 0)
                {
                    attack.ClearSlots();

                    foreach (var id in Armory.Equipped)
                        attack.TryEquip(Armory.ToSlot(id));
                }

                // Узел дерева открывает взрыв, если такого оружия ещё нет в наборе.
                if (SkillTree.HasNova && !HasNovaEquipped())
                {
                    attack.TryEquip(new WeaponSlot
                    {
                        Kind = WeaponKind.Nova, Damage = 22f, Cooldown = 1.8f, Range = 4.5f
                    });
                }
            }

            var motor = player.GetComponent<PlayerMotor>();

            if (motor != null)
                motor.ApplySpeedMultiplier(MetaProgress.SpeedMultiplier + SkillTree.SpeedBonus);

            Debug.Log($"[RUN] Старт. Урон ×{MetaProgress.DamageMultiplier + SkillTree.DamageBonus:F2}, " +
                      $"здоровье {maxHealth:F0}, " +
                      $"скорость ×{MetaProgress.SpeedMultiplier + SkillTree.SpeedBonus:F2}, " +
                      $"узлов дерева: {CountUnlocked()}");
        }

        private static int CountUnlocked()
        {
            var count = 0;

            for (var i = 0; i < SkillTree.Nodes.Length; i++)
            {
                if (SkillTree.IsUnlocked(i))
                    count++;
            }

            return count;
        }

        /// <summary>Есть ли взрыв в собранном наборе — чтобы не дублировать его узлом дерева.</summary>
        private static bool HasNovaEquipped()
        {
            foreach (var id in Armory.Equipped)
            {
                if (Armory.Catalog[id].Kind == WeaponKind.Nova)
                    return true;
            }

            return false;
        }
    }
}
