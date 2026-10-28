using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Камера из-за плеча для сферического мира.
    ///
    /// Cinemachine FreeLook здесь не подходит: он считает «верх» мировым Y, поэтому
    /// на обратной стороне планеты картинка переворачивается. Здесь верх — нормаль
    /// планеты в точке игрока, а горизонтальный угол накапливается в касательной
    /// плоскости, поэтому обход планеты по кругу не ломает управление.
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
        [SerializeField] private Vector2 _pitchLimits = new Vector2(-65f, 55f);
        [SerializeField, Min(0f)] private float _sensitivity = 2.2f;
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.06f;

        [Header("Столкновения")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField, Min(0.05f)] private float _collisionRadius = 0.25f;

        [Header("Курсор")]
        [SerializeField] private bool _lockCursor = true;

        private float _pitch;
        private Vector3 _forward = Vector3.forward;
        private Vector3 _smoothVelocity;
        private Vector3 _position;
        private bool _initialized;

        /// <summary>Горизонтальное направление камеры в касательной плоскости.</summary>
        public Vector3 PlanarForward => _forward;

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

            // 1. Опорное направление держим в касательной плоскости, чтобы при обходе
            //    планеты камера не «закручивалась» вокруг игрока.
            _forward = Vector3.ProjectOnPlane(_forward, up);
            if (_forward.sqrMagnitude < 0.0001f)
                _forward = Vector3.ProjectOnPlane(_target.forward, up);
            _forward = _forward.normalized;

            // 2. Мышь: горизонталь — поворот вокруг нормали, вертикаль — наклон.
            ReadLook(out var look);
            _forward = Quaternion.AngleAxis(look.x * _sensitivity, up) * _forward;
            _pitch = Mathf.Clamp(_pitch - look.y * _sensitivity, _pitchLimits.x, _pitchLimits.y);

            // 3. Итоговый поворот: смотрим вдоль _forward, верх — нормаль планеты.
            var rotation = Quaternion.LookRotation(_forward, up) * Quaternion.Euler(-_pitch, 0f, 0f);

            // 4. Позиция: плечо + отвод назад, затем проверка на препятствия.
            var pivot = _target.position + up * _pivotHeight;
            var desired = pivot + rotation * new Vector3(_shoulderOffset, 0f, -_distance);
            desired = ResolveCollision(pivot, desired);

            if (!_initialized)
            {
                _position = desired;
                _initialized = true;
            }

            _position = Vector3.SmoothDamp(_position, desired, ref _smoothVelocity, _smoothTime);
            transform.SetPositionAndRotation(_position, rotation);
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
