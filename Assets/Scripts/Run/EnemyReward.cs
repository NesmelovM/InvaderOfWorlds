using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Награда за убийство врага. Висит на враге и сообщает сессии о смерти,
    /// чтобы золото и опыт не приходилось считать вручную в каждом поведении.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyReward : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _gold = 5;
        [SerializeField, Min(0)] private int _experience = 10;

        private Health _health;

        public void Configure(int gold, int experience)
        {
            _gold = gold;
            _experience = experience;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void OnDied(Health health)
        {
            RunSession.RegisterKill(
                Mathf.RoundToInt(_gold * Balance.GoldMultiplier),
                Mathf.RoundToInt(_experience * Balance.ExperienceMultiplier));

            // «Вампиризм»: за убийство возвращается немного здоровья.
            if (RunModifiers.HealPerKill <= 0f || RunModifiers.PlayerHealth == null)
                return;

            RunModifiers.PlayerHealth.Heal(RunModifiers.HealPerKill);
        }
    }
}
