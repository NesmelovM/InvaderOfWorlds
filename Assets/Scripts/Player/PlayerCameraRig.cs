using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Камера из-за плеча для сферического мира.
    ///
    /// Ключевая идея: ориентация хранится кватернионом и переносится вместе с кривизной
    /// планеты («параллельный перенос»), а не как вектор в касательной плоскости.
    /// Векторный вариант ломается на экваторе: там «вперёд» и «вверх» могут совпасть,
    /// проекция обнуляется, и управление умирает. Кватернион такой особой точки не имеет.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCameraRig : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Transform _target;
        [SerializeField] private Planet _planet;

        [Header("Положение")]
        [SerializeField, Min(0.5f)] private float _distance = 4.5f;
        [SerializeField] private float _shoulderOffset = 0.7f;
        [SerializeField] private float _pivotHeight = 1.55f;

        [Tooltip("Наклон в градусах: x — вниз, y — вверх")]
        [SerializeField] private Vector2 _pitchLimits = new Vector2(-65f, 55f);

        [SerializeField, Min(0f)] private float _sensitivity = 2.2f;

        [Tooltip("Инверсия вертикали: включите, если мышь вверх опускает камеру")]
        [SerializeField] private bool _invertVertical;

        [SerializeField, Min(0.01f)] private float _smoothTime = 0.06f;

        [Header("Столкновения")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField, Min(0.05f)] private float _collisionRadius = 0.25f;

        [Header("Курсор")]
        [SerializeField] private bool _lockCursor = true;

        private Quaternion _rotation;
        private Vector3 _lastUp = Vector3.up;
        private Vector3 _smoothVelocity;
        private Vector3 _position;
        private bool _initialized;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeStrength;

        /// <summary>
        /// Режим съёмки: мышь не влияет на камеру. Нужен автотестам — курсор в редакторе
        /// живёт своей жизнью, и любое движение мыши уводит кадр.
        /// </summary>
        public bool IgnoreInput { get; set; }

        /// <summary>Горизонтальное направление камеры — для прицеливания и оружия.</summary>
        public Vector3 PlanarForward
        {
            get
            {
                var up = _planet != null ? _planet.UpAt(transform.position) : Vector3.up;
                return Vector3.ProjectOnPlane(_rotation * Vector3.forward, up).normalized;
            }
        }

        /// <summary>Привязка камеры к цели и планете.</summary>
        public void Configure(Transform target, Planet planet)
        {
            _target = target;
            _planet = planet;
            _initialized = false;
        }

        private void Start()
        {
            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            var up = _planet != null ? _planet.UpAt(_target.position) : Vector3.up;
            InitializeIfNeeded(up);

            // 1. Переносим ориентацию вместе с поворотом «верха»: камера едет по сфере,
            //    не закручиваясь вокруг игрока.
            var curvature = Quaternion.FromToRotation(_lastUp, up);
            _rotation = curvature * _rotation;
            _lastUp = up;

            // 2. Мышь: горизонталь — вокруг нормали, вертикаль — вокруг локальной оси X.
            if (!IgnoreInput && !UiState.BlocksInput)
            {
                ReadLook(out var look);
                _rotation = Quaternion.AngleAxis(look.x * _sensitivity, up) * _rotation;
                ApplyPitch(look.y * _sensitivity * (_invertVertical ? -1f : 1f), up);
            }

            // 3. Позиция: плечо, отвод назад, проверка на препятствия.
            var pivot = _target.position + up * _pivotHeight;
            var desired = pivot + _rotation * new Vector3(_shoulderOffset, 0f, -_distance);
            desired = ResolveCollision(pivot, desired);

            if (!_initialized)
            {
                _position = desired;
                _initialized = true;
            }

            _position = Vector3.SmoothDamp(_position, desired, ref _smoothVelocity, _smoothTime);

            var shakeOffset = Vector3.zero;
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                var fade = _shakeDuration <= 0f ? 1f : Mathf.Clamp01(_shakeTimer / _shakeDuration);
                shakeOffset = Random.insideUnitSphere * (_shakeStrength * fade);
            }

            transform.SetPositionAndRotation(_position + shakeOffset, _rotation);
        }

        /// <summary>Короткая тряска камеры — отклик на полученный урон.</summary>
        public void Shake(float strength, float duration)
        {
            _shakeStrength = strength;
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTimer = _shakeDuration;
        }

        private void InitializeIfNeeded(Vector3 up)
        {
            if (_initialized)
                return;

            var forward = Vector3.ProjectOnPlane(_target.forward, up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(Vector3.forward, up);

            _rotation = Quaternion.LookRotation(forward, up);
            _lastUp = up;
        }

        /// <summary>Поворот по вертикали с ограничением, чтобы камера не кувыркалась.</summary>
        private void ApplyPitch(float pitchDelta, Vector3 up)
        {
            if (Mathf.Abs(pitchDelta) < 0.0001f)
                return;

            var forward = _rotation * Vector3.forward;
            var currentPitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(forward, up), -1f, 1f)) * Mathf.Rad2Deg;
            var targetPitch = Mathf.Clamp(currentPitch + pitchDelta, _pitchLimits.x, _pitchLimits.y);
            var applied = targetPitch - currentPitch;

            if (Mathf.Abs(applied) > 0.0001f)
                _rotation = _rotation * Quaternion.AngleAxis(-applied, Vector3.right);
        }

        /// <summary>Не даёт камере уйти за стену: сфера-луч от головы к желаемой точке.</summary>
        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            var offset = desired - pivot;
            var length = offset.magnitude;
            if (length < 0.001f)
                return desired;

            var direction = offset / length;
            if (Physics.SphereCast(pivot, _collisionRadius, direction, out var hit, length,
                    _collisionMask, QueryTriggerInteraction.Ignore))
            {
                var safeDistance = Mathf.Max(0.4f, hit.distance - 0.05f);
                return pivot + direction * safeDistance;
            }

            return desired;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private static void ReadLook(out Vector2 look)
        {
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
#else
        private static void ReadLook(out Vector2 look)
        {
            look = Vector2.zero;
        }
#endif
    }
}
