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

            foreach (var settlement in _generator.Settlements)
            {
                var count = Mathf.Min(settlement.Guards, _maxPerSettlement);

                for (var i = 0; i < count; i++)
                    SpawnGuard(settlement.Center);
            }

            Debug.Log($"[SettlementSpawner] Заселено поселений: {_generator.Settlements.Count}");
        }

        /// <summary>Ставит одного стража рядом с центром поселения, на поверхности.</summary>
        /// <summary>
        /// Ставит одного стража рядом с центром поселения. Место проверяется:
        /// если точка занята стеной или постройкой — берётся соседняя.
        /// </summary>
        public GameObject SpawnGuard(Vector3 around)
        {
            if (!SpawnPlacement.TryFindFreeGroundPosition(_planet, around, _spread, _placementAttempts,
                    out var position, out var up))
            {
                Debug.LogWarning($"[SettlementSpawner] Не нашёл свободного места у {around}, ставлю в центре.", this);
                position = around;
                up = _planet.UpAt(around);
            }

            return EnemyFactory.Spawn(_planet, _player, position, _color, _health, transform);
        }
    }
}
