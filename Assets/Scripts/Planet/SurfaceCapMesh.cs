using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Строит «шапку» сферы — участок поверхности планеты заданного углового радиуса.
    /// Нужна для ауры: плоский диск на сфере висит в воздухе и режет глаз,
    /// а этот меш повторяет кривизну и лежит на грунте.
    ///
    /// Меш строится в локальных координатах вокруг центра планеты, ось «вверх» — +Y.
    /// Объект достаточно повернуть по нормали в точке игрока.
    /// </summary>
    public static class SurfaceCapMesh
    {
        public static Mesh Create(float planetRadius, float capRadiusMeters, float height, int segments = 40,
            int rings = 4)
        {
            var angularRadius = Mathf.Max(0.001f, capRadiusMeters / Mathf.Max(0.01f, planetRadius));
            var vertexCount = 1 + segments * rings;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[segments * rings * 6];

            vertices[0] = Vector3.up * (planetRadius + height);

            for (var ring = 1; ring <= rings; ring++)
            {
                var angle = angularRadius * ring / rings;
                var ringRadius = Mathf.Sin(angle);
                var ringHeight = Mathf.Cos(angle);

                for (var segment = 0; segment < segments; segment++)
                {
                    var phi = 2f * Mathf.PI * segment / segments;
                    var direction = new Vector3(Mathf.Sin(phi) * ringRadius, ringHeight,
                        Mathf.Cos(phi) * ringRadius);

                    vertices[1 + (ring - 1) * segments + segment] = direction * (planetRadius + height);
                }
            }

            var index = 0;

            // Первый пояс — веер вокруг центра.
            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                triangles[index++] = 0;
                triangles[index++] = 1 + next;
                triangles[index++] = 1 + segment;
            }

            // Остальные пояса — квады.
            for (var ring = 1; ring < rings; ring++)
            {
                var currentStart = 1 + (ring - 1) * segments;
                var nextStart = 1 + ring * segments;

                for (var segment = 0; segment < segments; segment++)
                {
                    var next = (segment + 1) % segments;

                    triangles[index++] = currentStart + segment;
                    triangles[index++] = currentStart + next;
                    triangles[index++] = nextStart + next;

                    triangles[index++] = currentStart + segment;
                    triangles[index++] = nextStart + next;
                    triangles[index++] = nextStart + segment;
                }
            }

            var mesh = new Mesh { name = "Surface Cap" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
