using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ставит модель персонажа внутрь игрового объекта.
    ///
    /// Главная тонкость: персонаж стоит на сфере, и его «верх» — это нормаль планеты,
    /// а не мировой Y. Поэтому и рост, и уровень ступней считаются вдоль локальной
    /// оси объекта. Если считать по мировому габаритному боксу, у полюса всё
    /// правильно, а на обратной стороне планеты модель раздувается и висит в воздухе.
    /// </summary>
    public static class CharacterModel
    {
        public static GameObject Attach(GameObject host, string resourcePath, float height, float yawOffset,
            Color? tint = null)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);

            if (prefab == null)
                return null;

            var model = Object.Instantiate(prefab, host.transform, false);
            model.name = "Model";

            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            ComputeVerticalExtents(model, host.transform, out var minY, out var maxY);
            var currentHeight = Mathf.Max(0.01f, maxY - minY);
            model.transform.localScale = Vector3.one * (height / currentHeight);

            // После масштабирования пересчитываем и опускаем ступни на ноль.
            ComputeVerticalExtents(model, host.transform, out minY, out _);
            model.transform.localPosition = new Vector3(0f, -minY, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);

            if (tint.HasValue)
                ApplyTint(model, tint.Value);

            return model;
        }

        /// <summary>
        /// Нижняя и верхняя точки модели вдоль локальной оси «вверх» объекта.
        /// Углы мирового AABB пересчитываются в локальные координаты — только так
        /// результат не зависит от того, в какой точке сферы стоит персонаж.
        /// </summary>
        private static void ComputeVerticalExtents(GameObject model, Transform host, out float minY,
            out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var bounds = renderer.bounds;

                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                    var local = host.InverseTransformPoint(corner);
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                }
            }

            if (minY > maxY)
            {
                minY = 0f;
                maxY = 1f;
            }
        }

        /// <summary>
        /// Перекрашивает модель одним материалом. В паке палитра без текстур,
        /// поэтому цветной силуэт читается лучше исходных серых материалов.
        /// </summary>
        public static void ApplyTint(GameObject model, Color color)
        {
            var material = EnemyFactory.GetMaterial(color);

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }
    }
}
