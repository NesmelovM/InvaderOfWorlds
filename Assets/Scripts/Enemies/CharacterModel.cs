using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ставит модель персонажа внутрь игрового объекта.
    ///
    /// Две тонкости, обе про то, что персонаж живёт на сфере:
    /// 1. Рост и уровень ступней считаются вдоль локальной оси «вверх», а не мировой:
    ///    на обратной стороне планеты мировой Y ничего не значит.
    /// 2. Точки берутся по вершинам мешей, а не по габаритному боксу: у больших
    ///    моделей бокс бывает заметно шире силуэта, и персонаж повисает.
    /// </summary>
    public static class CharacterModel
    {
        public static GameObject Attach(GameObject host, string resourcePath, float height, float yawOffset,
            Color? tint = null, float verticalOffset = 0f)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            return prefab == null ? null : Attach(host, prefab, height, yawOffset, tint, verticalOffset);
        }

        /// <summary>Модель задаётся ассетом напрямую — так биом подставляет своих врагов.</summary>
        public static GameObject Attach(GameObject host, GameObject prefab, float height, float yawOffset,
            Color? tint = null, float verticalOffset = 0f)
        {
            if (prefab == null)
                return null;

            var model = Object.Instantiate(prefab, host.transform, false);
            model.name = "Model";

            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);
            model.transform.localScale = Vector3.one;

            // Рост и посадку на грунт подгоняем по факту, после первого кадра:
            // у скелетных моделей границы сразу после создания ещё не готовы.
            var fitter = model.AddComponent<ModelFitter>();
            fitter.Configure(host.transform, height);

            if (tint.HasValue)
                ApplyTint(model, tint.Value);

            return model;
        }

        /// <summary>
        /// Нижняя и верхняя точки модели вдоль локальной оси «вверх» объекта-хозяина.
        /// Считаем по вершинам мешей — это точнее габаритного бокса.
        /// </summary>
        private static void ComputeVerticalExtents(GameObject model, Transform host, out float minY,
            out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;
            var found = false;

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;

                if (mesh == null)
                    continue;

                var matrix = filter.transform.localToWorldMatrix;

                if (!TrySampleVertices(mesh, matrix, host, ref minY, ref maxY))
                    SampleBounds(filter.GetComponent<Renderer>().bounds, host, ref minY, ref maxY);

                found = true;
            }

            // Скиннинг: у SkinnedMeshRenderer.bounds в момент создания объекта ещё
            // не пересчитаны (как и у обычных рендереров), поэтому берём вершины меша.
            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skinned.sharedMesh;

                if (mesh == null)
                    continue;

                // Для скелетной модели вершины меша лежат в позе привязки и не
                // учитывают трансформ рендерера, поэтому берём его локальные границы:
                // они всегда доступны и уже включают скелет.
                SampleBounds(skinned.localBounds, host, ref minY, ref maxY,
                    skinned.transform.localToWorldMatrix);
                found = true;
            }

            if (found && minY <= maxY)
                return;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                SampleBounds(renderer.bounds, host, ref minY, ref maxY);
                found = true;
            }

            if (!found)
            {
                minY = 0f;
                maxY = 1f;
            }
        }

        /// <summary>Читает вершины меша; при запрете чтения возвращает false.</summary>
        private static bool TrySampleVertices(Mesh mesh, Matrix4x4 matrix, Transform host, ref float minY,
            ref float maxY)
        {
            try
            {
                foreach (var vertex in mesh.vertices)
                {
                    var local = host.InverseTransformPoint(matrix.MultiplyPoint3x4(vertex));
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                }

                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private static void SampleBounds(Bounds bounds, Transform host, ref float minY, ref float maxY,
            Matrix4x4? localToWorld = null)
        {
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                var world = localToWorld.HasValue
                    ? localToWorld.Value.MultiplyPoint3x4(corner)
                    : corner;

                var local = host.InverseTransformPoint(world);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
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
