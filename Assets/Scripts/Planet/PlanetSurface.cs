using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Общие вычисления поверхности планеты для всех, кто по ней ходит.
    /// Сейчас используется врагами; игрок считает то же самое внутри себя.
    /// </summary>
    public static class PlanetSurface
    {
        private static readonly RaycastHit[] Buffer = new RaycastHit[24];

        /// <summary>
        /// Высота грунта в направлении от центра. Персонажи (всё, у чего есть Health)
        /// грунтом не считаются, иначе можно встать на голову врагу.
        /// Ниже базового радиуса планеты грунта нет.
        /// </summary>
        public static float SampleGroundRadius(Planet planet, Vector3 direction, float terrainHeight,
            LayerMask groundMask, Transform ignoreRoot, out Vector3 normal)
        {
            normal = direction;

            var top = planet.Radius + terrainHeight;
            var bottom = Mathf.Max(1f, planet.Radius - terrainHeight);
            var origin = planet.Center + direction * top;

            var count = Physics.RaycastNonAlloc(origin, -direction, Buffer, top - bottom, groundMask,
                QueryTriggerInteraction.Ignore);

            var best = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var hit = Buffer[i];

                if (hit.collider == null)
                    continue;

                if (ignoreRoot != null && hit.collider.transform.IsChildOf(ignoreRoot))
                    continue;

                if (hit.collider.GetComponentInParent<Health>() != null)
                    continue;

                if (hit.distance >= best)
                    continue;

                best = hit.distance;
                normal = hit.normal;
            }

            if (best >= float.MaxValue)
                return planet.Radius;

            return Mathf.Max(top - best, planet.Radius);
        }
    }
}
