using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Меш дуги для визуализации замаха: плоский серп в локальной плоскости XZ,
    /// направленный вдоль +Z. Так видно, куда именно бьёт оружие ближнего боя.
    /// </summary>
    public static class ArcMesh
    {
        public static Mesh Create(float innerRadius, float outerRadius, float angleDegrees, int segments = 16)
        {
            segments = Mathf.Max(3, segments);

            var half = angleDegrees * 0.5f * Mathf.Deg2Rad;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(-half, half, t);
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);

                vertices[i * 2] = new Vector3(sin * innerRadius, 0f, cos * innerRadius);
                vertices[i * 2 + 1] = new Vector3(sin * outerRadius, 0f, cos * outerRadius);
            }

            var index = 0;

            for (var i = 0; i < segments; i++)
            {
                var innerA = i * 2;
                var outerA = i * 2 + 1;
                var innerB = (i + 1) * 2;
                var outerB = (i + 1) * 2 + 1;

                triangles[index++] = innerA;
                triangles[index++] = outerA;
                triangles[index++] = outerB;

                triangles[index++] = innerA;
                triangles[index++] = outerB;
                triangles[index++] = innerB;
            }

            var mesh = new Mesh { name = "Slash Arc" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
