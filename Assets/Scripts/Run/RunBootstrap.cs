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
        {
            // Добыча прошлого забега уже зачислена, начинаем новый счёт.
            RunSession.ResetRun();

            var player = GameObject.FindWithTag("Player");

            if (player == null)
                return;

            var health = player.GetComponent<Health>();

            if (health != null)
                health.Configure(_baseHealth + MetaProgress.HealthBonus);

            var attack = player.GetComponent<WeaponSystem>();

            if (attack != null)
                attack.ApplyDamageMultiplier(MetaProgress.DamageMultiplier);

            var motor = player.GetComponent<PlayerMotor>();

            if (motor != null)
                motor.ApplySpeedMultiplier(MetaProgress.SpeedMultiplier);

            Debug.Log($"[RUN] Старт. Урон ×{MetaProgress.DamageMultiplier:F2}, " +
                      $"здоровье {_baseHealth + MetaProgress.HealthBonus:F0}, " +
                      $"скорость ×{MetaProgress.SpeedMultiplier:F2}");
        }
    }
}
