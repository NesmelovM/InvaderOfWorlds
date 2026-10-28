using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Медленно вращает диск портала вокруг своей оси и слегка «дышит» яркостью.
    /// Без движения портал читается как плоский блин, а не как живой проход.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortalSpin : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 22f;
        [SerializeField] private float _pulseSpeed = 1.4f;
        [SerializeField] private float _pulseAmount = 0.12f;

        private Material _material;
        private Color _baseColor;

        public void Configure(float speed, Color color)
        {
            _degreesPerSecond = speed;
            _baseColor = color;
        }

        private void Start()
        {
            var renderer = GetComponent<Renderer>();

            if (renderer == null)
                return;

            _material = renderer.material;
            _baseColor = _material.color;
        }

        private void Update()
        {
            // Вращение вокруг локальной нормали: диск остаётся на месте, крутится рисунок.
            transform.Rotate(Vector3.forward, _degreesPerSecond * Time.deltaTime, Space.Self);

            if (_material == null)
                return;

            var pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
            _material.color = _baseColor * pulse;
            _material.EnableKeyword("_EMISSION");
            _material.SetColor("_EmissionColor", _baseColor * (0.6f * pulse));
        }
    }
}
