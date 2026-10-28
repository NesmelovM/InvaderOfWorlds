using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Движение игрока по сфере на CharacterController.
    ///
    /// Почему CharacterController, а не Rigidbody: на маленькой планете физический
    /// персонаж дрожит на склонах и залипает на стыках, а контроллер даёт
    /// предсказуемый шаг, скольжение по склону и работу с уступами.
    ///
    /// «Вверх» всегда равен нормали планеты в точке игрока, персонаж разворачивается
    /// так, чтобы его локальная ось Y смотрела от центра планеты.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [Tooltip("Трансформ камеры: относительно него считается направление ввода")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Скорости")]
        [SerializeField, Min(0f)] private float _walkSpeed = 6f;
        [SerializeField, Min(0f)] private float _sprintSpeed = 9f;
        [SerializeField, Min(0f)] private float _acceleration = 45f;
        [SerializeField, Min(0f)] private float _turnSpeed = 12f;

        [Header("Прыжок и прижатие к поверхности")]
        [SerializeField, Min(0f)] private float _jumpHeight = 1.6f;
        [SerializeField, Min(0f)] private float _gravity = 25f;
        [Tooltip("Скорость прижатия к земле, чтобы персонаж не «отрывался» на выпуклостях")]
        [SerializeField, Min(0f)] private float _groundStick = 3f;

        private CharacterController _controller;
        private Vector3 _velocity;         // горизонтальная (касательная) скорость, м/с
        private float _verticalVelocity;   // вдоль «вверх» планеты, м/с
        private Vector3 _up = Vector3.up;

        public Vector3 Up => _up;
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public float CurrentSpeed => _velocity.magnitude;

        /// <summary>
        /// Привязка к планете и камере. Нужна и редактору при сборке сцены,
        /// и рантайму, когда игрок создаётся на поверхности после телепорта.
        /// </summary>
        public void Configure(Planet planet, Transform cameraTransform)
        {
            _planet = planet;
            _cameraTransform = cameraTransform;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            _up = _planet != null ? _planet.UpAt(transform.position) : Vector3.up;

            ReadInput(out var input, out var sprint, out var jump);

            var speed = sprint ? _sprintSpeed : _walkSpeed;
            var desired = BuildMoveDirection(input) * speed;
            _velocity = Vector3.MoveTowards(_velocity, desired, _acceleration * Time.deltaTime);

            ApplyGravityAndJump(jump);
            AlignToSurface();

            var motion = _velocity + _up * _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            KeepOnSurface();
        }

        /// <summary>Направление ввода в касательной плоскости, относительно камеры.</summary>
        private Vector3 BuildMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            var forward = _cameraTransform != null
                ? Vector3.ProjectOnPlane(_cameraTransform.forward, _up).normalized
                : Vector3.ProjectOnPlane(transform.forward, _up).normalized;

            // Если камера смотрит строго по нормали — берём текущий «вперёд» персонажа.
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(transform.forward, _up).normalized;

            var right = Vector3.Cross(_up, forward);
            var direction = forward * input.y + right * input.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void ApplyGravityAndJump(bool jump)
        {
            if (IsGrounded)
            {
                // Небольшое прижатие вместо нуля — иначе на выпуклой сфере персонаж «прыгает» на стыках.
                _verticalVelocity = -_groundStick;

                if (jump)
                    _verticalVelocity = Mathf.Sqrt(2f * _gravity * _jumpHeight);
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }
        }

        /// <summary>Разворачивает капсулу так, чтобы её верх смотрел от центра планеты.</summary>
        private void AlignToSurface()
        {
            var align = Quaternion.FromToRotation(transform.up, _up);
            transform.rotation = align * transform.rotation;

            if (_velocity.sqrMagnitude < 0.01f)
                return;

            var lookDirection = Vector3.ProjectOnPlane(_velocity, _up);
            if (lookDirection.sqrMagnitude < 0.01f)
                return;

            var target = Quaternion.LookRotation(lookDirection, _up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, _turnSpeed * Time.deltaTime);
        }

        /// <summary>Страховка: если физика вытолкнула внутрь сферы — возвращаем на поверхность.</summary>
        private void KeepOnSurface()
        {
            if (_planet == null)
                return;

            var distance = (transform.position - _planet.Center).magnitude;
            if (distance >= _planet.Radius - 1f)
                return;

            var corrected = _planet.SnapToSurface(transform.position, _controller.height * 0.5f + _controller.radius);
            var push = corrected - transform.position;
            _controller.Move(push);
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private static void ReadInput(out Vector2 move, out bool sprint, out bool jump)
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            sprint = Input.GetKey(KeyCode.LeftShift);
            jump = Input.GetButtonDown("Jump");
        }
#else
        // Проект на новом Input System: ввод подставит отдельный адаптер,
        // чтобы движение не зависело от выбранной системы ввода.
        private static void ReadInput(out Vector2 move, out bool sprint, out bool jump)
        {
            move = Vector2.zero;
            sprint = false;
            jump = false;
        }
#endif
    }
}
