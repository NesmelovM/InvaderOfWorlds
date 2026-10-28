using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ставит модель персонажа внутрь игрового объекта.
    ///
    /// Две тонкости, на которых легко ошибиться:
    /// 1. Модели из пака приходят с произвольным масштабом и пивотом — приводим рост
    ///    и опускаем ступни на ноль.
    /// 2. Высоту нельзя считать по мировым габаритам: персонаж стоит на сфере,
    ///    у него своя ось «вверх», и мировой Y там ничего не значит. Считаем проекцию
    ///    на локальную ось объекта.
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

            var bounds = CalculateBounds(model);
            var scale = height / Mathf.Max(0.01f, bounds.size.y);
            model.transform.localScale = Vector3.one * scale;

            // Высота ступней относительно объекта — проекция на его локальную ось «вверх».
            bounds = CalculateBounds(model);
            var feetOffset = Vector3.Dot(bounds.min - host.transform.position, host.transform.up);
            model.transform.localPosition = new Vector3(0f, -feetOffset, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);

            if (tint.HasValue)
                ApplyTint(model, tint.Value);

            return model;
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

        private static Bounds CalculateBounds(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            var bounds = renderers[0].bounds;

            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }
    }
}
