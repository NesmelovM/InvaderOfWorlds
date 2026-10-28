using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Поиск свободной точки для появления на поверхности планеты.
    ///
    /// Проверяются две разные вещи, и это важно:
    /// 1. Статичная геометрия — стены, ступени, постройки. Врага нельзя ставить внутрь.
    /// 2. Другие персонажи — врага нельзя ставить вплотную к соседу.
    /// Раньше персонажи игнорировались целиком, из-за чего стража появлялась
    /// по три штуки в одной точке.
    /// </summary>
    public static class SpawnPlacement
    {
        private const int BufferSize = 32;
        private static readonly Collider[] Buffer = new Collider[BufferSize];

        public static bool TryFindFreeGroundPosition(Planet planet, Vector3 around, float spread, int attempts,
            out Vector3 surfacePosition, out Vector3 up, float characterSpacing = 1.6f)
        {
            surfacePosition = around;
            up = planet.UpAt(around);

            for (var attempt = 0; attempt < Mathf.Max(1, attempts); attempt++)
            {
                var centerUp = planet.UpAt(around);
                var tangent = Vector3.ProjectOnPlane(Random.onUnitSphere, centerUp);

                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = Vector3.ProjectOnPlane(Vector3.forward, centerUp);

                // Первая попытка — ровно заданная точка, дальше — случайные рядом.
                var radius = attempt == 0 ? 0f : Random.Range(spread * 0.25f, spread);
                var candidate = around + tangent.normalized * radius;
                var direction = planet.UpAt(candidate);
                var ground = PlanetSurface.SampleGroundRadius(planet, direction, 40f, ~0, null, out _);
                var position = planet.Center + direction * (ground + 0.02f);

                if (!IsFree(position, direction, characterSpacing))
                    continue;

                surfacePosition = position;
                up = direction;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Свободно ли место: рядом нет геометрии и рядом нет другого персонажа.
        /// </summary>
        public static bool IsFree(Vector3 surfacePosition, Vector3 up, float characterSpacing,
            float bodyHeight = 1f, float clearance = 0.45f)
        {
            // Враги создаются в одном кадре один за другим: без синхронизации трансформов
            // проверка не увидит коллайдеры, созданные мгновением раньше.
            Physics.SyncTransforms();

            var center = surfacePosition + up * bodyHeight;

            // 1. Геометрия: любое пересечение, кроме персонажей.
            var count = Physics.OverlapSphereNonAlloc(center, clearance, Buffer, ~0, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var overlap = Buffer[i];

                if (overlap == null)
                    continue;

                if (overlap.GetComponentInParent<Health>() != null)
                    continue;

                return false;
            }

            // 2. Соседи: персонажи не должны стоять вплотную.
            if (characterSpacing <= 0f)
                return true;

            count = Physics.OverlapSphereNonAlloc(center, characterSpacing, Buffer, ~0, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var overlap = Buffer[i];

                if (overlap == null)
                    continue;

                if (overlap.GetComponentInParent<Health>() != null)
                    return false;
            }

            return true;
        }
    }
}
