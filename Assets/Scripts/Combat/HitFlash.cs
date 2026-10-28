using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Вспышка при получении урона: кратко подсвечивает модель белым.
    /// Цвет меняется через MaterialPropertyBlock, поэтому материал не клонируется
    /// и одинаковые враги продолжают делить один шар.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitFlash : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Health _health;
        [SerializeField, Min(0.01f)] private float _duration = 0.12f;
        [SerializeField] private Color _color = Color.white;

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _block;
        private float _timer;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();

            _block = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Кеш рендереров и их исходных цветов. Собирается лениво: на момент Awake
        /// модель может быть ещё не создана, и тогда массив оказался бы пустым.
        /// </summary>
        private void EnsureCache()
        {
            if (_renderers != null && _renderers.Length > 0)
                return;

            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];

            for (var i = 0; i < _renderers.Length; i++)
            {
                var material = _renderers[i].sharedMaterial;
                _baseColors[i] = material != null && material.HasProperty(ColorId)
                    ? material.color
                    : Color.white;
            }
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            _timer = _duration;
            Apply(_color);
        }

        private void Update()
        {
            if (_timer <= 0f)
                return;

            _timer -= Time.deltaTime;

            if (_timer > 0f)
                return;

            _timer = 0f;
            Clear();
        }

        private void Apply(Color color)
        {
            if (_block == null)
                return;

            EnsureCache();

            if (_renderers == null || _renderers.Length == 0)
                return;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];

                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_block);
                _block.SetColor(ColorId, color);
                renderer.SetPropertyBlock(_block);
            }
        }

        /// <summary>
        /// Возвращает исходный цвет, взятый прямо из материала. Раньше здесь
        /// сбрасывался Color.clear, и модель залипала чёрной (непрозрачный шейдер
        /// читает обнулённый цвет как чёрный альбедо).
        /// </summary>
        private void Clear()
        {
            if (_renderers == null || _renderers.Length == 0 || _block == null)
                return;

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];

                if (renderer == null)
                    continue;

                var material = renderer.sharedMaterial;

                if (material == null || !material.HasProperty(ColorId))
                    continue;

                renderer.GetPropertyBlock(_block);
                _block.SetColor(ColorId, material.color);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
