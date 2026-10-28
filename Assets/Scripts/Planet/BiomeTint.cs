using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Подмешивает цвет биома в префабы окружения.
    ///
    /// Пак раскрашен сам по себе, но у разных биомов палитры не совпадают:
    /// египетские пирамиды рядом с индийской листвой выглядят чужеродно.
    /// Цвет меняется через MaterialPropertyBlock, поэтому исходные материалы
    /// пака не портятся и объекты не плодят копии материалов.
    /// </summary>
    public static class BiomeTint
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void Apply(GameObject prop, Color color, float strength, System.Random random)
        {
            if (prop == null || strength <= 0f)
                return;

            foreach (var renderer in prop.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;

                if (material == null || !material.HasProperty(ColorId))
                    continue;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                var baseColor = material.color;
                var amount = strength * (0.6f + (float)random.NextDouble() * 0.6f);
                block.SetColor(ColorId, Color.Lerp(baseColor, color, Mathf.Clamp01(amount)));
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
