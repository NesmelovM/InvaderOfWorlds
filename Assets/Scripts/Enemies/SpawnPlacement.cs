using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Поиск свободной точки для появления на поверхности планеты.
    /// Нужен, чтобы стража не появлялась внутри стен, ступеней и прочих объектов:
    /// кандидат проверяется сферой на пересечения, и если место занято — берётся другое.
    /// </summary>
    public static class SpawnPlacement
    {
        /// <summary>
        /// Ищет свободную точку в радиусе <paramref name="spread"/> вокруг заданной.
        /// Возвращает false, если все попытки заняты.
        /// </summary>
        public static bool TryFindFreeGroundPosition(Planet planet, Vector3 around, float spread, int attempts,
            out Vector3 surfacePosition, out Vector3 up)
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

                if (!IsFree(position, direction))
                    continue;

                surfacePosition = position;
                up = direction;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Свободно ли место для тела: сфера на высоте пояса не должна пересекать
        /// ничего, кроме других персонажей (они не считаются препятствием).
        /// </summary>
        public static bool IsFree(Vector3 surfacePosition, Vector3 up, float bodyHeight = 1f,
            float clearance = 0.45f)
        {
            var center = surfacePosition + up * bodyHeight;
            var overlaps = Physics.OverlapSphere(center, clearance, ~0, QueryTriggerInteraction.Ignore);

            foreach (var overlap in overlaps)
            {
                if (overlap == null)
                    continue;

                if (overlap.GetComponentInParent<Health>() != null)
                    continue;

                return false;
            }

            return true;
        }
    }
}
