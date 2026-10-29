using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Столкновение персонажей со стенами и постройками.
    ///
    /// Движение аналитическое, физика его не разрешает, поэтому каждый шаг
    /// проверяется сферой. Персонажи друг друга не считают препятствием —
    /// для них есть отдельное расталкивание.
    /// </summary>
    public static class CharacterObstacles
    {
        private static readonly RaycastHit[] Buffer = new RaycastHit[12];

        /// <summary>
        /// Подрезает шаг, если впереди стена. Возвращает скорректированное смещение.
        /// </summary>
        public static Vector3 Slide(Transform self, Vector3 up, float bodyHeight, float radius,
            Vector3 delta, LayerMask mask)
        {
            var distance = delta.magnitude;

            if (distance < 0.0001f)
                return delta;

            var origin = self.position + up * bodyHeight;
            var direction = delta / distance;

            var count = Physics.SphereCastNonAlloc(origin, radius, direction, Buffer, distance + 0.05f, mask,
                QueryTriggerInteraction.Ignore);

            var blocked = false;

            for (var i = 0; i < count; i++)
            {
                var hit = Buffer[i];

                if (hit.collider == null)
                    continue;

                if (hit.collider.transform.IsChildOf(self))
                    continue;

                // Другие персонажи — не стена.
                if (hit.collider.GetComponentInParent<EnemyMarker>() != null)
                    continue;

                blocked = true;
                break;
            }

            if (!blocked)
                return delta;

            // Скользим вдоль плоскости стены, а не упираемся в неё намертво.
            var normal = GetBlockingNormal(origin, direction, distance, mask);
            return Vector3.ProjectOnPlane(delta, normal);
        }

        /// <summary>
        /// Выталкивает персонажа из геометрии, если он оказался внутри: в углу
        /// скольжение по одной нормали может завести внутрь второй стены.
        /// Возвращает суммарное смещение наружу.
        /// </summary>
        public static Vector3 ResolvePenetration(Collider body, Vector3 up, float maxPush,
            LayerMask mask)
        {
            if (body == null || maxPush <= 0f)
                return Vector3.zero;

            var offset = Vector3.zero;
            var center = body.bounds.center;
            var radius = Mathf.Max(0.2f, body.bounds.extents.x);
            var hits = Physics.OverlapSphere(center, radius + 0.05f, mask, QueryTriggerInteraction.Ignore);

            foreach (var other in hits)
            {
                if (other == null || other == body || other.transform.IsChildOf(body.transform))
                    continue;

                if (other.GetComponentInParent<EnemyMarker>() != null)
                    continue;

                if (!Physics.ComputePenetration(body, body.transform.position, body.transform.rotation,
                        other, other.transform.position, other.transform.rotation, out var direction,
                        out var distance))
                    continue;

                // Только горизонтальная составляющая: вверх-вниз персонажа держит грунт.
                var flat = Vector3.ProjectOnPlane(direction, up);

                if (flat.sqrMagnitude < 0.0001f)
                    continue;

                offset += flat.normalized * Mathf.Min(distance, maxPush);
            }

            return offset.sqrMagnitude > maxPush * maxPush ? offset.normalized * maxPush : offset;
        }

        private static Vector3 GetBlockingNormal(Vector3 origin, Vector3 direction, float distance, LayerMask mask)
        {
            var count = Physics.SphereCastNonAlloc(origin, 0.35f, direction, Buffer, distance + 0.05f, mask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var hit = Buffer[i];

                if (hit.collider == null)
                    continue;

                if (hit.collider.GetComponentInParent<EnemyMarker>() != null)
                    continue;

                return hit.normal;
            }

            return direction * -1f;
        }
    }
}
