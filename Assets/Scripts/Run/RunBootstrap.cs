using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Начало забега: сбрасывает временные усилители и выдаёт стартовый набор.
    ///
    /// Из оружейной берётся только одно оружие — остальное игрок получает
    /// карточками при повышении уровня, поэтому билд собирается по ходу забега.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunBootstrap : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _baseHealth = 100f;

        private void Start()
        {
            RunSession.ResetRun();
            RunModifiers.Reset();

            var player = GameObject.FindWithTag("Player");

            if (player == null)
                return;

            var health = player.GetComponent<Health>();
            var maxHealth = _baseHealth + MetaProgress.HealthBonus;

            if (health != null)
            {
                health.Configure(maxHealth);
                RunModifiers.PlayerHealth = health;
            }

            var attack = player.GetComponent<WeaponSystem>();

            if (attack != null)
            {
                attack.ApplyDamageMultiplier(MetaProgress.DamageMultiplier);
                attack.ClearSlots();

                var startingWeapon = Armory.Equipped.Count > 0 ? Armory.Equipped[0] : 0;
                attack.TryEquip(Armory.ToSlot(startingWeapon));
            }

            var motor = player.GetComponent<PlayerMotor>();

            if (motor != null)
                motor.ApplySpeedMultiplier(MetaProgress.SpeedMultiplier);

            Debug.Log($"[RUN] Старт. Здоровье {maxHealth:F0}, урон ×{MetaProgress.DamageMultiplier:F2}, " +
                      $"стартовое оружие: {Armory.Catalog[Armory.Equipped.Count > 0 ? Armory.Equipped[0] : 0].Name}");
        }
    }
}
