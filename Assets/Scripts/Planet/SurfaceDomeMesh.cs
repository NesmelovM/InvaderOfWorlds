using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Купол на поверхности планеты: холм, который действительно лежит на сфере.
    ///
    /// Раньше холм был сплющенной сферой через неравномерный масштаб. Unity такие
    /// коллайдеры не поддерживает: она берёт наибольшую ось и делает обычную сферу
    /// радиусом 12 — визуально холм, а физически шар на десять метров над планетой.
    ///
    /// Здесь меш строится по поверхности: высота плавно спадает к краям,
    /// поэтому получается настоящий холм с пологими склонами.
    /// </summary>
    public static class SurfaceDomeMesh
    {
        public static Mesh Create(float planetRadius, float domeRadius, float height, int segments = 48,
            int rings = 8)
        {
            segments = Mathf.Max(8, segments);
            rings = Mathf.Max(2, rings);

            var angularRadius = Mathf.Max(0.0001f, domeRadius / Mathf.Max(0.01f, planetRadius));
            var vertices = new Vector3[1 + segments * rings];
            var triangles = new int[segments * rings * 6];

            vertices[0] = Vector3.up * (planetRadius + height);

            for (var ring = 1; ring <= rings; ring++)
            {
                var t = ring / (float)rings;
                var angle = angularRadius * t;

                // Профиль (1 - t²)² даёт нулевой наклон у края: холм плавно вливается в грунт.
                var falloff = (1f - t * t) * (1f - t * t);
                var radius = planetRadius + height * falloff;

                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);

                for (var segment = 0; segment < segments; segment++)
                {
                    var phi = 2f * Mathf.PI * segment / segments;
                    var direction = new Vector3(sin * Mathf.Cos(phi), cos, sin * Mathf.Sin(phi));
                    vertices[1 + (ring - 1) * segments + segment] = direction * radius;
                }
            }

            var index = 0;

            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                triangles[index++] = 0;
                triangles[index++] = 1 + next;
                triangles[index++] = 1 + segment;
            }

            for (var ring = 1; ring < rings; ring++)
            {
                var currentStart = 1 + (ring - 1) * segments;
                var nextStart = 1 + ring * segments;

                for (var segment = 0; segment < segments; segment++)
                {
                    var next = (segment + 1) % segments;

                    triangles[index++] = currentStart + segment;
                    triangles[index++] = nextStart + next;
                    triangles[index++] = nextStart + segment;

                    triangles[index++] = currentStart + segment;
                    triangles[index++] = currentStart + next;
                    triangles[index++] = nextStart + next;
                }
            }

            var mesh = new Mesh { name = "Surface Dome" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
