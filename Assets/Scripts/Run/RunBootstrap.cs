using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Начало планеты: при первом заходе — новый забег, при переходе «дальше» —
    /// восстановление билда, который несёт игрок.
    ///
    /// Порядок важен: сброс состояния должен произойти раньше, чем планета
    /// прочитает сид и биом (-100), иначе первый забег получит состояние
    /// предыдущего, а поселения — пустой биом.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public class RunBootstrap : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _baseHealth = 100f;

        private void Start()
        {
            var firstPlanet = RunState.PlanetIndex == 0;

            if (firstPlanet)
            {
                RunState.Reset();
                RunSession.ResetRun();
            }

            RunModifiers.Reset();

            var player = GameObject.FindWithTag("Player");

            if (player == null)
                return;

            var health = player.GetComponent<Health>();

            if (health != null)
            {
                health.Configure(Balance.PlayerHealth + MetaProgress.HealthBonus + RunState.HealthBonus);
                RunModifiers.PlayerHealth = health;
            }

            var attack = player.GetComponent<WeaponSystem>();

            if (attack != null)
            {
                attack.ApplyDamageMultiplier(MetaProgress.DamageMultiplier);
                attack.ClearSlots();

                if (firstPlanet)
                {
                    var startingWeapon = Armory.Equipped.Count > 0 ? Armory.Equipped[0] : 0;
                    attack.TryEquip(Armory.ToSlot(startingWeapon));

                    RunState.Loadout.Clear();
                    RunState.Loadout.Add(startingWeapon);
                }
                else
                {
                    foreach (var id in RunState.Loadout)
                        attack.TryEquip(Armory.ToSlot(id));
                }

                if (RunState.WeaponDamageMultiplier > 1f)
                    attack.MultiplyDamage(RunState.WeaponDamageMultiplier);
            }

            if (GetComponent<RunCompass>() == null)
                gameObject.AddComponent<RunCompass>();

            if (GetComponent<Ambience>() == null)
                gameObject.AddComponent<Ambience>();

            if (player.GetComponent<RunRegen>() == null)
                player.AddComponent<RunRegen>();

            // Перед первым заходом даём выбрать оружие из купленного.
            if (firstPlanet)
                RunStartWeaponChoice.Show(player);

            var motor = player.GetComponent<PlayerMotor>();

            if (motor != null)
                motor.ApplySpeedMultiplier(MetaProgress.SpeedMultiplier);
        }
    }
}
