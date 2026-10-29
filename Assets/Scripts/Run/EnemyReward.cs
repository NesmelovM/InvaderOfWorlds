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

        /// <summary>Карточка «Осколки»: смерть врага бьёт соседей по площади.</summary>
        private void ExplodeAround()
        {
            if (RunState.ExplosionDamage <= 0f || RunState.ExplosionRadius <= 0f)
                return;

            var center = transform.position;
            var radius = RunState.ExplosionRadius;

            foreach (var other in FindObjectsByType<EnemyMarker>(FindObjectsSortMode.None))
            {
                if (other == null || other.gameObject == gameObject)
                    continue;

                if (Vector3.Distance(other.transform.position, center) > radius)
                    continue;

                var health = other.GetComponent<Health>();

                if (health != null && health.IsAlive)
                    health.TakeDamage(RunState.ExplosionDamage);
            }
        }

        private void OnDied(Health health)
        {
            SoundBank.Coin(transform.position);

            // Монеты летят к игроку: золото уже начислено выше, это чистый эффект.
            var player = RunModifiers.PlayerHealth != null ? RunModifiers.PlayerHealth.transform : null;
            Coin.Burst(transform.position, player, UnityEngine.Random.Range(2, 5));
            RunSession.RegisterKill(
                Mathf.RoundToInt(_gold * Balance.GoldMultiplier),
                Mathf.RoundToInt(_experience * Balance.ExperienceMultiplier *
                                 RunState.ExperienceMultiplier *
                                 Mathf.Pow(Balance.ExperiencePerPlanet, RunState.PlanetIndex)));

            ExplodeAround();

            // «Вампиризм»: за убийство возвращается немного здоровья.
            if (RunModifiers.HealPerKill <= 0f || RunModifiers.PlayerHealth == null)
                return;

            RunModifiers.PlayerHealth.Heal(RunModifiers.HealPerKill);
        }
    }
}
