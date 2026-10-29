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
            Color? tint = null, float verticalOffset = 0f, CapsuleCollider body = null)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            return prefab == null ? null : Attach(host, prefab, height, yawOffset, tint, verticalOffset, body);
        }

        /// <summary>Модель задаётся ассетом напрямую — так биом подставляет своих врагов.</summary>
        public static GameObject Attach(GameObject host, GameObject prefab, float height, float yawOffset,
            Color? tint = null, float verticalOffset = 0f, CapsuleCollider body = null)
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

            // Размер берём по локальным границам рендереров: они совпадают
            // с реальным силуэтом, в отличие от мирового Renderer.bounds.
            ApplySize(model, host.transform, body, height);

            // Перекраска только по запросу: по умолчанию остаются родные материалы модели.
            if (tint.HasValue)
                ApplyTint(model, tint.Value);

            return model;
        }

        /// <summary>
        /// Ставит модель ступнями на ноль хозяина и подгоняет капсулу под силуэт.
        /// Считается по mesh.bounds — стабильно и сразу, без ожидания кадров.
        /// </summary>
        private static void ApplySize(GameObject model, Transform host, CapsuleCollider body, float targetHeight)
        {
            if (!TryGetMeshExtents(model, host, out var bottom, out var top, out var halfWidth))
                return;

            var height = Mathf.Max(0.1f, top - bottom);

            // Подгонка роста — только если включена в конфиге персонажей.
            if (Characters.FitHeight && targetHeight > 0.1f)
            {
                var k = targetHeight / height;
                model.transform.localScale *= k;
                bottom *= k;
                top *= k;
                halfWidth *= k;
                height = targetHeight;
            }

            model.transform.localPosition += host.up * (-bottom - 0.02f);

            if (body == null)
                return;

            body.height = Mathf.Max(0.6f, height);
            body.center = new Vector3(0f, body.height * 0.5f, 0f);
            body.radius = Mathf.Clamp(halfWidth * 0.75f, 0.2f, 0.45f);
        }

        /// <summary>Габариты по границам сеток в системе координат хозяина.</summary>
        private static bool TryGetMeshExtents(GameObject model, Transform host, out float bottom,
            out float top, out float halfWidth)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            halfWidth = 0.3f;
            var found = false;
            var maxSide = 0f;
            var toHost = host.worldToLocalMatrix;
            var up = host.up;

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;

                found |= Accumulate(filter.sharedMesh.bounds, filter.transform, toHost, up,
                    ref bottom, ref top, ref maxSide);
            }

            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh == null)
                    continue;

                found |= Accumulate(skinned.localBounds, skinned.transform, toHost, up,
                    ref bottom, ref top, ref maxSide);
            }

            halfWidth = maxSide * 0.5f;
            return found && bottom <= top;
        }

        private static bool Accumulate(Bounds bounds, Transform owner, Matrix4x4 toHost, Vector3 up,
            ref float bottom, ref float top, ref float maxSide)
        {
            var matrix = toHost * owner.localToWorldMatrix;

            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                var local = matrix.MultiplyPoint3x4(corner);
                var along = Vector3.Dot(local, up);
                bottom = Mathf.Min(bottom, along);
                top = Mathf.Max(top, along);
                maxSide = Mathf.Max(maxSide, Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)) * 2f);
            }

            return true;
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
