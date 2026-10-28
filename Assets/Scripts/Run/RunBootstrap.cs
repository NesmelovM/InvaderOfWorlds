using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Начало планеты: при первом заходе — новый забег, при переходе «дальше» —
    /// восстановление билда, который несёт игрок.
    ///
    /// Из оружейной берётся только одно оружие: остальное приходит карточками
    /// при повышении уровня.
    /// </summary>
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
                health.Configure(_baseHealth + MetaProgress.HealthBonus);
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

            var motor = player.GetComponent<PlayerMotor>();

            if (motor != null)
                motor.ApplySpeedMultiplier(MetaProgress.SpeedMultiplier);
        }
    }
}
