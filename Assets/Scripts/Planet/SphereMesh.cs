using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Гладкая сфера для планеты.
    ///
    /// Примитив Unity — это ~500 вершин: на радиусе 90 метров он выглядит гранёным.
    /// Здесь сфера строится с заданной плотностью, поэтому силуэт круглый,
    /// а нормали сглажены.
    /// </summary>
    public static class SphereMesh
    {
        public static Mesh Create(float radius, int segments = 128, int rings = 64)
        {
            segments = Mathf.Max(8, segments);
            rings = Mathf.Max(4, rings);

            var vertices = new Vector3[(segments + 1) * (rings + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];

            for (var ring = 0; ring <= rings; ring++)
            {
                var v = ring / (float)rings;
                var phi = Mathf.PI * v;

                for (var segment = 0; segment <= segments; segment++)
                {
                    var u = segment / (float)segments;
                    var theta = 2f * Mathf.PI * u;

                    var normal = new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(theta),
                        Mathf.Cos(phi),
                        Mathf.Sin(phi) * Mathf.Sin(theta));

                    var index = ring * (segments + 1) + segment;
                    normals[index] = normal;
                    vertices[index] = normal * radius;
                    uv[index] = new Vector2(u, 1f - v);
                }
            }

            var triangles = new int[segments * rings * 6];
            var t = 0;

            for (var ring = 0; ring < rings; ring++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var current = ring * (segments + 1) + segment;
                    var next = current + segments + 1;

                    triangles[t++] = current;
                    triangles[t++] = current + 1;
                    triangles[t++] = next;

                    triangles[t++] = current + 1;
                    triangles[t++] = next + 1;
                    triangles[t++] = next;
                }
            }

            var mesh = new Mesh { name = $"Planet Sphere {radius:F0}" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
