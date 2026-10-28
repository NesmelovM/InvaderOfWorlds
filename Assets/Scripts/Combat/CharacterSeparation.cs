using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Расталкивание персонажей. Движение у нас аналитическое (трансформом),
    /// поэтому физика сама столкновения не решает — приходится разводить вручную.
    ///
    /// Работает по касательной к сфере: персонажи не могут пройти друг сквозь друга,
    /// но и не взлетают, наезжая на соседа.
    /// </summary>
    public static class CharacterSeparation
    {
        private const int BufferSize = 24;
        private static readonly Collider[] Buffer = new Collider[BufferSize];

        /// <summary>
        /// Раздвигает персонажа, если он пересекается с другим персонажем.
        /// <paramref name="strength"/> — доля исправления (1 = полностью вытолкнуть),
        /// <paramref name="maxPush"/> — предел смещения за кадр, чтобы не дёргалось.
        /// </summary>
        public static void Resolve(Transform self, Vector3 up, float bodyHeight, float radius, float strength,
            float maxPush = 0.25f)
        {
            var center = self.position + up * bodyHeight;
            var minDistance = radius * 2f;

            // Персонажи двигаются трансформом, поэтому физика может не знать их новое
            // положение — без синхронизации проверка увидит устаревшие координаты.
            Physics.SyncTransforms();

            var count = Physics.OverlapSphereNonAlloc(center, minDistance, Buffer, ~0,
                QueryTriggerInteraction.Ignore);

            var push = Vector3.zero;

            for (var i = 0; i < count; i++)
            {
                var other = Buffer[i];

                if (other == null || other.transform.IsChildOf(self))
                    continue;

                // Расталкиваем только персонажей: стены и постройки обрабатываются отдельно.
                if (other.GetComponentInParent<Health>() == null)
                    continue;

                var delta = Vector3.ProjectOnPlane(center - other.bounds.center, up);
                var distance = delta.magnitude;

                if (distance >= minDistance)
                    continue;

                var direction = distance > 0.0001f
                    ? delta / distance
                    : Vector3.ProjectOnPlane(self.forward, up).normalized;

                push += direction * (minDistance - distance);
            }

            if (push.sqrMagnitude < 0.000001f)
                return;

            self.position += Vector3.ClampMagnitude(push * strength, maxPush);
        }
    }
}
