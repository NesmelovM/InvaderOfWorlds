using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Оружие в руке персонажа: вешается на кость правой кисти, если она есть,
    /// иначе на сам объект. Так анимация руки автоматически двигает оружие.
    /// </summary>
    public static class WeaponMount
    {
        public static GameObject Attach(GameObject host, string modelPath, float length = 1f)
        {
            return Attach(host, Resources.Load<GameObject>(modelPath), length);
        }

        public static GameObject Attach(GameObject host, GameObject prefab, float length = 1f)
        {
            if (prefab == null)
                return null;

            var hand = FindHand(host);
            var parent = hand != null ? hand : host.transform;

            var weapon = Object.Instantiate(prefab, parent, false);
            weapon.name = "Weapon";

            foreach (var collider in weapon.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            weapon.transform.localPosition = hand != null ? Vector3.zero : new Vector3(0.24f, 1.02f, 0.12f);
            weapon.transform.localRotation = Quaternion.Euler(0f, 0f, hand != null ? 90f : -18f);

            FitLength(weapon, length);
            return weapon;
        }

        /// <summary>Кость правой кисти у людей пака называется RightHand.</summary>
        private static Transform FindHand(GameObject host)
        {
            foreach (var bone in host.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(bone.name, "RightHand", System.StringComparison.OrdinalIgnoreCase))
                    return bone;
            }

            return null;
        }

        /// <summary>
        /// Подгоняет длину оружия по фактическим вершинам меша.
        /// Renderer.bounds сразу после Instantiate ещё не пересчитан, поэтому
        /// раньше размер получался случайным (лук выходил шире персонажа).
        /// </summary>
        private static void FitLength(GameObject model, float length)
        {
            var root = model.transform;
            var toRoot = root.worldToLocalMatrix;
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;

                if (mesh == null)
                    continue;

                var matrix = toRoot * filter.transform.localToWorldMatrix;

                foreach (var vertex in mesh.vertices)
                {
                    var point = matrix.MultiplyPoint3x4(vertex);
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }

            if (float.IsInfinity(min.x) || float.IsInfinity(max.x))
                return;

            var size = max - min;
            var longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

            if (longest > 0.001f)
                root.localScale *= length / longest;
        }
    }
}
