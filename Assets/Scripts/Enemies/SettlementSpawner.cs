using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Заселяет поселения, которые расставил PlanetContentGenerator.
    /// Число стражей берётся из данных поселения, но ограничивается сверху:
    /// для вертикального среза хватает нескольких врагов на деревню.
    /// </summary>
    public class SettlementSpawner : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [SerializeField] private PlanetContentGenerator _generator;
        [SerializeField] private Transform _player;

        [Header("Настройки")]
        [SerializeField, Min(1)] private int _maxPerSettlement = 4;
        [SerializeField, Min(0f)] private float _spread = 9f;
        [SerializeField, Min(1)] private int _placementAttempts = 24;
        [Tooltip("Минимальное расстояние между врагами при появлении")]
        [SerializeField, Min(0f)] private float _characterSpacing = 1.8f;
        [Tooltip("Доля стражей дальнего боя")]
        [SerializeField, Range(0f, 1f)] private float _rangedChance = 0.3f;
        [Tooltip("Ставить в каждое поселение одного элитного врага")]
        [SerializeField] private bool _elitePerSettlement = true;
        [Tooltip("Ставить в каждое поселение элитного лучника")]
        [SerializeField] private bool _eliteArcherPerSettlement = true;
        [SerializeField, Min(1f)] private float _health = 60f;
        [SerializeField] private Color _color = new Color(0.78f, 0.22f, 0.18f);
        [SerializeField] private bool _spawnOnStart = true;

        public void Configure(Planet planet, PlanetContentGenerator generator, Transform player)
        {
            _planet = planet;
            _generator = generator;
            _player = player;
        }

        private void Start()
        {
            if (_spawnOnStart)
                SpawnAll();
        }

        [ContextMenu("Заселить поселения")]
        public void SpawnAll()
        {
            if (_planet == null || _generator == null || _player == null)
            {
                Debug.LogWarning("[SettlementSpawner] Не все ссылки заданы.", this);
                return;
            }

            // Сложность растёт не только цифрами: на поздних планетах врагов больше.
            var density = Mathf.Clamp(RunState.Difficulty, 1f, 1.9f);

            foreach (var settlement in _generator.Settlements)
            {
                var count = Mathf.Min(
                    Mathf.RoundToInt(settlement.Guards * density), _maxPerSettlement);

                for (var i = 0; i < count; i++)
                    SpawnGuard(settlement.Center);

                if (_elitePerSettlement)
                    SpawnElite(settlement.Center);

                // На второй планете и дальше элита встречается чаще.
                if (_eliteArcherPerSettlement || density > 1.15f)
                    SpawnEliteArcher(settlement.Center);
            }

        }

        /// <summary>
        /// Ставит одного стража рядом с центром поселения. Место проверяется:
        /// если точка занята стеной, постройкой или другим врагом — берётся соседняя.
        /// Если места нет совсем, страж не появляется: лучше меньше врагов, чем
        /// трое в одной точке.
        /// </summary>
        public GameObject SpawnGuard(Vector3 around)
        {
            if (SpawnPlacement.TryFindFreeGroundPosition(_planet, around, _spread, _placementAttempts,
                    out var position, out var up, _characterSpacing))
            {
                return CreateGuard(position);
            }

            // Вторая попытка с более широким радиусом — поселение может быть тесным.
            if (SpawnPlacement.TryFindFreeGroundPosition(_planet, around, _spread * 2.5f, _placementAttempts,
                    out position, out up, _characterSpacing))
            {
                return CreateGuard(position);
            }

            Debug.LogWarning($"[SettlementSpawner] Нет свободного места у {around:F1}, страж пропущен.", this);
            return null;
        }

        /// <summary>Элита крупнее обычной стражи, поэтому ей нужно больше свободного места.</summary>
        public GameObject SpawnElite(Vector3 around)
        {            if (SpawnPlacement.TryFindFreeGroundPosition(_planet, around, _spread, _placementAttempts,
                    out var position, out _, _characterSpacing * 1.6f))
            {
                return EnemyFactory.SpawnElite(_planet, _player, position, transform);
            }

            Debug.LogWarning($"[SettlementSpawner] Нет места для элиты у {around:F1}, пропущена.", this);
            return null;
        }

        /// <summary>Элитный лучник — тоже крупный, ему нужен запас места.</summary>
        public GameObject SpawnEliteArcher(Vector3 around)
        {
            if (SpawnPlacement.TryFindFreeGroundPosition(_planet, around, _spread, _placementAttempts,
                    out var position, out _, _characterSpacing * 1.4f))
            {
                return EnemyFactory.SpawnEliteRanged(_planet, _player, position, transform);
            }

            Debug.LogWarning($"[SettlementSpawner] Нет места для элитного лучника у {around:F1}, пропущен.", this);
            return null;
        }

        /// <summary>Часть стражи — дальнего боя, чтобы поселение не было однородной толпой.</summary>
        private GameObject CreateGuard(Vector3 position)
        {
            return Random.value < _rangedChance
                ? EnemyFactory.SpawnRanged(_planet, _player, position, _health * 0.8f, transform)
                : EnemyFactory.Spawn(_planet, _player, position, _color, _health, transform);
        }
    }
}
