using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Движение игрока по сфере без CharacterController.
    ///
    /// Почему так: CharacterController рассчитан на плоский мир. Если держать его
    /// прижатым к выпуклой поверхности, он проникает в сферу, физика выталкивает его
    /// с огромной скоростью (в логах видели ccVel до 100 м/с), контакт теряется,
    /// и горизонтальная скорость гасится. Персонаж «застревает» без всякой стены.
    ///
    /// Здесь позиция считается аналитически: точка всегда лежит на сфере радиуса R,
    /// высота прыжка — отдельная величина над поверхностью. Такое движение нельзя
    /// «продавить» в грунт, потому что грунта как коллайдера нет вообще.
    /// Препятствия (стены, постройки) обрабатываются отдельным SphereCast со скольжением.
    /// </summary>
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

        [Header("Прыжок и гравитация")]
        [SerializeField, Min(0f)] private float _jumpHeight = 1.6f;
        [SerializeField, Min(0f)] private float _gravity = 25f;

        [Header("Геометрия")]
        [Tooltip("Зазор между ступнями и поверхностью")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [Tooltip("Центр капсулы для проверки препятствий")]
        [SerializeField, Min(0f)] private float _bodyCenter = 0.9f;
        [SerializeField, Min(0.05f)] private float _bodyRadius = 0.35f;
        [SerializeField] private LayerMask _obstacleMask = ~0;

        [Header("Отладка")]
        [SerializeField] private bool _logMovement = true;

        private Vector3 _velocity;       // касательная скорость, м/с
        private float _height;           // высота над поверхностью, м
        private float _verticalSpeed;    // вдоль нормали, м/с
        private Vector3 _up = Vector3.up;
        private bool _grounded = true;

        public Vector3 Up => _up;
        public bool IsGrounded => _grounded;
        public float CurrentSpeed => _velocity.magnitude;
        public float Height => _height;

        /// <summary>
        /// Привязка к планете и камере. Нужна и редактору при сборке сцены,
        /// и рантайму, когда игрок создаётся на поверхности после телепорта.
        /// </summary>
        public void Configure(Planet planet, Transform cameraTransform)
        {
            _planet = planet;
            _cameraTransform = cameraTransform;
            _grounded = true;
            _height = 0f;
            _verticalSpeed = 0f;
        }

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (_planet == null)
                return;

            _up = _planet.UpAt(transform.position);

            // Скорость живёт в касательной плоскости текущей точки.
            _velocity = Vector3.ProjectOnPlane(_velocity, _up);

            ReadInput(out var input, out var sprint, out var jump);

            var speed = sprint ? _sprintSpeed : _walkSpeed;
            var desired = BuildMoveDirection(input) * speed;
            _velocity = Vector3.MoveTowards(_velocity, desired, _acceleration * Time.deltaTime);

            UpdateVertical(jump);

            var delta = _velocity * Time.deltaTime;
            delta = ResolveObstacles(delta);

            ApplyDelta(delta);

            _debugLine = BuildDebugLine(input);
            LogState(input);
        }

        /// <summary>Вертикаль: прыжок и падение считаются отдельно от движения по поверхности.</summary>
        private void UpdateVertical(bool jump)
        {
            if (_grounded)
            {
                _height = 0f;
                _verticalSpeed = 0f;

                if (jump)
                {
                    _verticalSpeed = Mathf.Sqrt(2f * _gravity * _jumpHeight);
                    _grounded = false;
                }

                return;
            }

            _verticalSpeed -= _gravity * Time.deltaTime;
            _height += _verticalSpeed * Time.deltaTime;

            if (_height <= 0f)
            {
                _height = 0f;
                _verticalSpeed = 0f;
                _grounded = true;
            }
        }

        /// <summary>Переносит персонажа по касательной и ставит обратно на сферу.</summary>
        private void ApplyDelta(Vector3 delta)
        {
            var nextPosition = transform.position + delta;

            // Новая нормаль — уже в точке, куда идём: движение идёт по дуге большого круга.
            var up = _planet.UpAt(nextPosition);
            _up = up;

            transform.position = _planet.Center + up * (_planet.Radius + _surfaceGap + _height);

            // Разворот: сначала выравниваем «верх» по нормали, затем доворачиваем по движению.
            var aligned = Quaternion.FromToRotation(transform.up, up) * transform.rotation;

            var flatDelta = Vector3.ProjectOnPlane(delta, up);
            if (flatDelta.sqrMagnitude > 0.000001f)
            {
                var target = Quaternion.LookRotation(flatDelta.normalized, up);
                transform.rotation = Quaternion.Slerp(aligned, target, _turnSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = aligned;
            }
        }

        /// <summary>
        /// Препятствия: если путь перекрыт, скользим вдоль стены, а не останавливаемся.
        /// Сама планета коллайдера не имеет, поэтому в проверку не попадает.
        /// </summary>
        private Vector3 ResolveObstacles(Vector3 delta)
        {
            var distance = delta.magnitude;
            if (distance < 0.0001f)
                return delta;

            var origin = transform.position + _up * _bodyCenter;
            var direction = delta / distance;

            if (Physics.SphereCast(origin, _bodyRadius, direction, out var hit, distance + 0.05f,
                    _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return Vector3.ProjectOnPlane(delta, hit.normal);
            }

            return delta;
        }

        /// <summary>
        /// Направление ввода в касательной плоскости, относительно камеры.
        /// Базис строится из forward, а если камера смотрит почти вертикально —
        /// достраивается от правого вектора, чтобы направление не вырождалось.
        /// </summary>
        private Vector3 BuildMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 forward;
            Vector3 right;

            if (_cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(_cameraTransform.forward, _up);

                if (forward.sqrMagnitude > 0.01f)
                {
                    forward.Normalize();
                    right = Vector3.Cross(_up, forward);
                }
                else
                {
                    right = Vector3.ProjectOnPlane(_cameraTransform.right, _up);
                    if (right.sqrMagnitude < 0.01f)
                        return Vector3.zero;

                    right.Normalize();
                    forward = Vector3.Cross(right, _up);
                }
            }
            else
            {
                forward = Vector3.ProjectOnPlane(transform.forward, _up);
                if (forward.sqrMagnitude < 0.0001f)
                    return Vector3.zero;

                forward.Normalize();
                right = Vector3.Cross(_up, forward);
            }

            var direction = forward * input.y + right * input.x;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private float _nextLogTime;
        private string _debugLine = string.Empty;

        private string BuildDebugLine(Vector2 input)
        {
            var distance = (transform.position - _planet.Center).magnitude;
            var upError = Vector3.Angle(transform.up, _up);

            return $"pos {transform.position:F1}  dist {distance:F2}  spd {CurrentSpeed:F2}  " +
                   $"height {_height:F2}  vSpeed {_verticalSpeed:F2}  grounded {_grounded}  " +
                   $"upErr {upError:F1}  input {input:F2}";
        }

        private void OnGUI()
        {
            if (!_logMovement)
                return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
            GUI.Label(new Rect(12f, 12f, 1200f, 24f), _debugLine, style);
        }

        private void LogState(Vector2 input)
        {
            if (!_logMovement || Time.time < _nextLogTime)
                return;

            _nextLogTime = Time.time + 0.5f;
            Debug.Log("[MOVE] " + _debugLine);

            if (input.sqrMagnitude > 0.1f && CurrentSpeed < 0.5f && Time.time > 1f)
                Debug.LogWarning($"[MOVE-STALL] pos={transform.position:F3} up={_up:F3} " +
                                 $"height={_height:F2} grounded={_grounded}");
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private static void ReadInput(out Vector2 move, out bool sprint, out bool jump)
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            sprint = Input.GetKey(KeyCode.LeftShift);
            jump = Input.GetButtonDown("Jump");
        }
#else
        private static void ReadInput(out Vector2 move, out bool sprint, out bool jump)
        {
            move = Vector2.zero;
            sprint = false;
            jump = false;
        }
#endif
    }
}
