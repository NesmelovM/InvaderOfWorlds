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
        private MaterialPropertyBlock _block;
        private float _timer;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();

            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
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
            Apply(Color.clear);
        }

        private void Apply(Color color)
        {
            if (_renderers == null || _block == null)
                return;

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_block);
                _block.SetColor(ColorId, color);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
