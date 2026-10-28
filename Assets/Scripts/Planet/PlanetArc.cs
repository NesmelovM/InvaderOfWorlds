using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Движение по дуге вокруг планеты.
    ///
    /// Обычная прямая на сфере не работает: снаряд либо уходит в грунт, либо
    /// отрывается от поверхности. Здесь шаг делается поворотом вокруг центра
    /// планеты, поэтому траектория повторяет кривизну, а высота над грунтом
    /// остаётся постоянной.
    /// </summary>
    public static class PlanetArc
    {
        /// <summary>
        /// Сдвигает точку на <paramref name="distance"/> вдоль поверхности
        /// и поворачивает направление вместе с ней.
        /// </summary>
        public static void Advance(Planet planet, ref Vector3 position, ref Vector3 direction, float distance)
        {
            if (planet == null || distance <= 0f)
            {
                position += direction * distance;
                return;
            }

            var up = planet.UpAt(position);
            var axis = Vector3.Cross(up, direction);

            if (axis.sqrMagnitude < 0.000001f)
            {
                position += direction * distance;
                return;
            }

            var radius = Mathf.Max(0.01f, (position - planet.Center).magnitude);
            var angle = distance / radius * Mathf.Rad2Deg;
            var rotation = Quaternion.AngleAxis(angle, axis.normalized);

            position = planet.Center + rotation * (position - planet.Center);
            direction = rotation * direction;
        }

        /// <summary>Поворот направления к цели с сохранением касательности.</summary>
        public static Vector3 SteerTowards(Planet planet, Vector3 position, Vector3 direction, Vector3 target,
            float maxRadians)
        {
            var up = planet != null ? planet.UpAt(position) : Vector3.up;

            var desired = Vector3.ProjectOnPlane(target - position, up);

            if (desired.sqrMagnitude < 0.000001f)
                return direction;

            desired.Normalize();

            var current = Vector3.ProjectOnPlane(direction, up);

            if (current.sqrMagnitude < 0.000001f)
                return desired;

            return Vector3.RotateTowards(current.normalized, desired, maxRadians, 0f);
        }
    }
}
