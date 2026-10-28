using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Полоска здоровья над врагом. Рисуется в экранных координатах,
    /// поэтому не требует Canvas и работает с любой камерой.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField, Min(0.1f)] private float _headOffset = 1.9f;
        [SerializeField, Min(1f)] private float _width = 64f;
        [SerializeField, Min(1f)] private float _height = 7f;
        [SerializeField, Min(0f)] private float _maxDistance = 70f;
        [SerializeField] private Color _fill = new Color(0.85f, 0.22f, 0.16f);

        private Camera _camera;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();

            _camera = Camera.main;
        }

        private void OnGUI()
        {
            if (_health == null || _camera == null || !_health.IsAlive)
                return;

            var worldPosition = transform.position + transform.up * _headOffset;

            if (Vector3.Distance(_camera.transform.position, worldPosition) > _maxDistance)
                return;

            var screen = _camera.WorldToScreenPoint(worldPosition);

            if (screen.z <= 0f)
                return;

            var rect = new Rect(screen.x - _width * 0.5f, Screen.height - screen.y - _height, _width, _height);
            HealthBarDrawer.Draw(rect, _health.Normalized, _fill);
        }
    }
}
