using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ставит модель персонажа внутрь игрового объекта.
    ///
    /// Модели из пака приходят с произвольным масштабом и пивотом, поэтому здесь
    /// они приводятся к нужному росту и ставятся ступнями на ноль. Если модель
    /// не загрузилась, остаётся примитив — игра не ломается без ассетов.
    /// </summary>
    public static class CharacterModel
    {
        public static GameObject Attach(GameObject host, string resourcePath, float height, float yawOffset)
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

            // После масштабирования пересчитываем границы и опускаем ступни на ноль.
            bounds = CalculateBounds(model);
            model.transform.localPosition = new Vector3(0f, -bounds.min.y, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);

            return model;
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
