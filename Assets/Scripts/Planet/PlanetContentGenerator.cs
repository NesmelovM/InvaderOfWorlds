using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Расставляет содержимое планеты: точку входа, поселения со стражей и портал выхода,
    /// которого охраняет босс.
    ///
    /// MVP-подход: один шар + точки на поверхности. Без мешей-кусков, без процедурной
    /// геометрии — расстановка считается детерминированно по seed, чтобы забег можно
    /// было воспроизвести и отладить.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetContentGenerator : MonoBehaviour
    {
        [Serializable]
        public class Settlement
        {
            public Vector3 Center;
            public float Radius;
            public float DefenseRadius;
            public int Guards;
        }

        [Header("Ссылки")]
        [SerializeField] private Planet _planet;

        [Header("Поселения")]
        [SerializeField, Min(1)] private int _settlementsCount = 4;
        [SerializeField, Range(3, 20)] private int _minGuards = 3;
        [SerializeField, Range(3, 20)] private int _maxGuards = 12;
        [SerializeField, Min(1f)] private float _settlementRadius = 14f;
        [Tooltip("Радиус оборонительных сооружений вокруг поселения")]
        [SerializeField, Min(1f)] private float _defenseRadius = 26f;

        [Header("Ограничения расстановки")]
        [Tooltip("Минимальное угловое расстояние между поселениями, в градусах")]
        [SerializeField, Min(0f)] private float _minAngleBetweenSettlements = 25f;

        [Header("Прочее")]
        [SerializeField] private int _seed = 20261028;
        [SerializeField] private bool _generateOnStart = true;

        public Vector3 PlayerSpawnPoint { get; private set; }
        public Vector3 BossPortalPoint { get; private set; }
        public IReadOnlyList<Settlement> Settlements => _settlements;

        /// <summary>Привязка генератора к планете.</summary>
        public void Configure(Planet planet)
        {
            _planet = planet;
        }

        /// <summary>Сколько всего стражи на планете — для планирования волн и наград.</summary>
        public int TotalGuards
        {
            get
            {
                var total = 0;
                foreach (var settlement in _settlements)
                    total += settlement.Guards;
                return total;
            }
        }

        private readonly List<Settlement> _settlements = new List<Settlement>();

        private void Start()
        {
            if (_generateOnStart)
                Generate();
        }

        [ContextMenu("Сгенерировать планету")]
        public void Generate()
        {
            if (_planet == null)
            {
                Debug.LogWarning("[PlanetContentGenerator] Не задана планета.", this);
                return;
            }

            var random = new System.Random(_seed);
            _settlements.Clear();

            PlayerSpawnPoint = _planet.RandomSurfacePoint(random);

            // Поселения: отбрасываем кандидатов, которые слишком близко к входу
            // или друг к другу, чтобы игрок не проходил всё в одной точке.
            var attempts = 0;
            while (_settlements.Count < _settlementsCount && attempts++ < _settlementsCount * 50)
            {
                var candidate = _planet.RandomSurfacePoint(random);

                if (_planet.AngleBetween(candidate, PlayerSpawnPoint) < _minAngleBetweenSettlements)
                    continue;

                var tooClose = false;
                foreach (var settlement in _settlements)
                {
                    if (_planet.AngleBetween(candidate, settlement.Center) < _minAngleBetweenSettlements)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                _settlements.Add(new Settlement
                {
                    Center = candidate,
                    Radius = _settlementRadius,
                    DefenseRadius = _defenseRadius,
                    Guards = random.Next(_minGuards, _maxGuards + 1)
                });
            }

            // Портал выхода — максимально далеко от точки входа: до него надо дойти.
            BossPortalPoint = FindFarthestPoint(random, PlayerSpawnPoint);
        }

        private Vector3 FindFarthestPoint(System.Random random, Vector3 from, int candidatesCount = 96)
        {
            var best = _planet.RandomSurfacePoint(random);
            var bestAngle = -1f;

            for (var i = 0; i < candidatesCount; i++)
            {
                var candidate = _planet.RandomSurfacePoint(random);
                var angle = _planet.AngleBetween(candidate, from);
                if (angle > bestAngle)
                {
                    bestAngle = angle;
                    best = candidate;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            if (_planet == null)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(PlayerSpawnPoint, 1.5f);
            Gizmos.DrawLine(_planet.Center, PlayerSpawnPoint);

            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(BossPortalPoint, 3f);
            Gizmos.DrawLine(_planet.Center, BossPortalPoint);

            foreach (var settlement in _settlements)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                Gizmos.DrawWireSphere(settlement.Center, settlement.Radius);

                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.25f);
                Gizmos.DrawWireSphere(settlement.Center, settlement.DefenseRadius);
            }
        }
    }
}
