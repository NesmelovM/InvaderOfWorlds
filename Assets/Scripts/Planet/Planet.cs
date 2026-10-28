using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Описывает планету как сферу: центр, радиус, нормаль поверхности.
    /// Все системы (движение, камера, расстановка контента) берут «верх» отсюда,
    /// чтобы не размазывать математику сферы по проекту.
    /// </summary>
    [DisallowMultipleComponent]
    public class Planet : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _radius = 90f;

        public float Radius => _radius;
        public Vector3 Center => transform.position;

        /// <summary>Задаёт радиус из кода (сборка сцены, рантайм-генерация).</summary>
        public void SetRadius(float radius)
        {
            _radius = Mathf.Max(1f, radius);
        }

        /// <summary>Нормаль поверхности в произвольной точке мира.</summary>
        public Vector3 UpAt(Vector3 worldPosition)
        {
            var delta = worldPosition - Center;
            return delta.sqrMagnitude < 0.0001f ? transform.up : delta.normalized;
        }

        /// <summary>Точка на поверхности в направлении от центра.</summary>
        public Vector3 SurfacePoint(Vector3 direction)
        {
            return Center + direction.normalized * _radius;
        }

        /// <summary>Случайная точка поверхности (равномерно по сфере).</summary>
        public Vector3 RandomSurfacePoint(System.Random random)
        {
            // Равномерное распределение по сфере: z равномерно, угол — равномерно.
            var z = random.NextDouble() * 2.0 - 1.0;
            var theta = random.NextDouble() * Mathf.PI * 2f;
            var r = Mathf.Sqrt(1f - (float)(z * z));
            var direction = new Vector3(r * Mathf.Cos((float)theta), (float)z, r * Mathf.Sin((float)theta));
            return SurfacePoint(direction);
        }

        /// <summary>Проецирует точку на поверхность с отступом по нормали.</summary>
        public Vector3 SnapToSurface(Vector3 worldPosition, float offset = 0f)
        {
            return Center + UpAt(worldPosition) * (_radius + offset);
        }

        /// <summary>Угловое расстояние между двумя точками поверхности, в градусах.</summary>
        public float AngleBetween(Vector3 a, Vector3 b)
        {
            return Vector3.Angle(UpAt(a), UpAt(b));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
            Gizmos.DrawWireSphere(Center, _radius);
        }
    }
}
