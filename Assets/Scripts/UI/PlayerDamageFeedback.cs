using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Обратная связь по урону для игрока: красная заливка экрана и тряска камеры.
    /// Без этого непонятно, что тебя вообще ударили и с какой стороны идёт бой.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerDamageFeedback : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private PlayerCameraRig _cameraRig;

        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.28f;
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _shakeStrength = 0.14f;
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.18f;

        private float _alpha;
        private float _previousHealth;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<Health>();

            if (_cameraRig == null)
                _cameraRig = FindFirstObjectByType<PlayerCameraRig>();
        }

        private void Start()
        {
            if (_health != null)
                _previousHealth = _health.Current;
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
            if (current >= _previousHealth)
            {
                _previousHealth = current;
                return;
            }

            _previousHealth = current;
            _alpha = _flashAlpha;
            SoundBank.Hurt(transform.position);

            if (_cameraRig != null)
                _cameraRig.Shake(_shakeStrength, _shakeDuration);
        }

        private void Update()
        {
            if (_alpha <= 0f)
                return;

            _alpha = Mathf.Max(0f, _alpha - _fadeSpeed * Time.deltaTime);
        }

        private void OnGUI()
        {
            if (_alpha <= 0f)
                return;

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.75f, 0.05f, 0.05f, _alpha));
        }
    }
}
