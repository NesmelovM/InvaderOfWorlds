using UnityEngine;
using UnityEngine.Rendering;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Разрушаемое укрепление: стена, которую можно разбить.
    /// Пока урон накапливается — темнеет и оседает, после смерти разлетается обломками.
    /// </summary>
    [DisallowMultipleComponent]
    public class Destructible : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Color _damageColor = new Color(0.22f, 0.19f, 0.17f);
        [SerializeField] private int _debrisCount = 8;
        [SerializeField] private float _debrisLifetime = 2.5f;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Color _baseColor = Color.gray;
        private Vector3 _baseScale;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();

            _renderer = GetComponentInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            _baseScale = transform.localScale;

            if (_renderer != null && _renderer.sharedMaterial != null)
                _baseColor = _renderer.sharedMaterial.color;
        }

        private void OnEnable()
        {
            if (_health == null)
                return;

            _health.Changed += OnHealthChanged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health == null)
                return;

            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        /// <summary>Чем меньше здоровья, тем темнее и ниже стена.</summary>
        private void OnHealthChanged(float current, float max)
        {
            var damage = 1f - Mathf.Clamp01(max <= 0f ? 1f : current / max);

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_block);
                _block.SetColor(ColorId, Color.Lerp(_baseColor, _damageColor, damage));
                _renderer.SetPropertyBlock(_block);
            }

            transform.localScale = _baseScale * (1f - 0.12f * damage);
        }

        private void OnDied(Health health)
        {
            SpawnDebris();
        }

        private void SpawnDebris()
        {
            for (var i = 0; i < _debrisCount; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = "Debris";
                piece.transform.position = transform.position + Random.insideUnitSphere * 0.6f;
                piece.transform.rotation = Random.rotation;
                piece.transform.localScale = Vector3.one * Random.Range(0.25f, 0.6f);

                var renderer = piece.GetComponent<Renderer>();

                if (renderer != null && _renderer != null)
                    renderer.sharedMaterial = _renderer.sharedMaterial;

                var body = piece.AddComponent<Rigidbody>();
                body.mass = 0.4f;
                body.AddExplosionForce(4f, transform.position, 4f, 0.5f, ForceMode.VelocityChange);

                Destroy(piece, _debrisLifetime);
            }
        }
    }
}
