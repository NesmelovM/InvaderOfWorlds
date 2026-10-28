using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Движение игрока по сфере без CharacterController, с учётом рельефа и построек.
    ///
    /// Почему без CharacterController: он рассчитан на плоский мир. Если держать его
    /// прижатым к выпуклой поверхности, он проникает в сферу, физика выталкивает его
    /// с огромной скоростью (в логах видели ccVel до 100 м/с), контакт теряется,
    /// и горизонтальная скорость гасится. Персонаж «застревает» без всякой стены.
    ///
    /// Как устроено:
    /// 1. Высота грунта в направлении персонажа берётся лучом сверху вниз к центру
    ///    планеты. Нет коллайдеров — берётся базовая сфера радиуса R.
    /// 2. Позиция ставится на найденный грунт плюс высота прыжка. Провалиться некуда.
    /// 3. Подъём выше лимита ступени и склон круче лимита считаются препятствием:
    ///    персонаж скользит вдоль стены, а не проходит сквозь неё.
    /// 4. Отдельные объекты (стены, ящики) дополнительно проверяются SphereCast'ом.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Planet _planet;
        [Tooltip("Трансформ камеры: относительно него считается направление ввода")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Скорости")]
        [SerializeField, Min(0f)] private float _walkSpeed = 10f;
        [SerializeField, Min(0f)] private float _sprintSpeed = 20f;
        [SerializeField, Min(0f)] private float _acceleration = 45f;
        [SerializeField, Min(0f)] private float _turnSpeed = 12f;

        [Header("Прыжок и гравитация")]
        [SerializeField, Min(0f)] private float _jumpHeight = 1.6f;
        [SerializeField, Min(0f)] private float _gravity = 25f;

        [Header("Геометрия тела")]
        [Tooltip("Зазор между ступнями и поверхностью")]
        [SerializeField, Min(0f)] private float _surfaceGap = 0.02f;
        [Tooltip("Центр капсулы для проверки препятствий")]
        [SerializeField, Min(0f)] private float _bodyCenter = 0.9f;
        [SerializeField, Min(0.05f)] private float _bodyRadius = 0.35f;
        [SerializeField] private LayerMask _obstacleMask = ~0;

        [Header("Рельеф")]
        [Tooltip("Слои, по которым определяется высота грунта: планета, скалы, постройки")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [Tooltip("Насколько рельеф может подниматься над базовой сферой, м")]
        [SerializeField, Min(0f)] private float _terrainHeight = 40f;
        [Tooltip("Максимальная высота ступени, на которую персонаж заходит без прыжка")]
        [SerializeField, Min(0f)] private float _maxStepHeight = 1.2f;
        [Tooltip("Предельный уклон, по которому можно идти, градусы")]
        [SerializeField, Range(0f, 90f)] private float _slopeLimit = 55f;

        private Vector3 _velocity;       // касательная скорость, м/с
        private float _height;           // высота над грунтом, м
        private float _verticalSpeed;    // вдоль нормали, м/с
        private Vector3 _up = Vector3.up;
        private float _stepDistance;
        [SerializeField, Min(0.5f)] private float _stepLength = 2.4f;
        private Vector3 _groundNormal = Vector3.up;
        private float _groundRadius;
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

            if (_planet != null)
                _groundRadius = SampleGroundRadius(_planet.UpAt(transform.position), out _groundNormal);
        }

        /// <summary>Улучшение из хаба: множитель скорости применяется на старте забега.</summary>
        public void ApplySpeedMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
                return;

            _walkSpeed *= multiplier;
            _sprintSpeed *= multiplier;
        }

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_planet == null)
                _planet = FindAnyObjectByType<Planet>();

            // Скорости берём из конфига баланса: их правят в инструменте.
            _walkSpeed = Balance.WalkSpeed;
            _sprintSpeed = Balance.SprintSpeed;
        }

        private void Update()
        {
            if (_planet == null)
                return;

            _up = _planet.UpAt(transform.position);
            _groundRadius = SampleGroundRadius(_up, out _groundNormal);

            // Скорость живёт в касательной плоскости текущей точки.
            _velocity = Vector3.ProjectOnPlane(_velocity, _up);

            ReadInput(out var input, out var sprint, out var jump);

            var speed = sprint ? _sprintSpeed : _walkSpeed;
            var desired = BuildMoveDirection(input) * speed;
            _velocity = Vector3.MoveTowards(_velocity, desired, _acceleration * Time.deltaTime);

            UpdateVertical(jump);

            var delta = _velocity * Time.deltaTime;
            delta = ResolveObstacles(delta);
            delta = ResolveTerrain(delta);

            ApplyDelta(delta);

            // Не даём пройти сквозь врагов: расталкиваем по касательной и снова сажаем на грунт.
            CharacterSeparation.Resolve(transform, _up, _bodyCenter, _bodyRadius, 0.6f);
            SnapToSurface();

            UpdateFootsteps(delta.magnitude);
        }

        /// <summary>
        /// Шаги: звук через каждые пару метров пути. Привязан к пройденной дистанции,
        /// а не ко времени, поэтому шаги не частит при быстром беге.
        /// </summary>
        private void UpdateFootsteps(float moved)
        {
            if (!_grounded || moved < 0.0001f)
                return;

            _stepDistance += moved;

            if (_stepDistance < _stepLength)
                return;

            _stepDistance = 0f;
            SoundBank.Step(transform.position);
        }

        /// <summary>Возвращает персонажа на текущий грунт после расталкивания.</summary>
        private void SnapToSurface()
        {
            _up = _planet.UpAt(transform.position);
            _groundRadius = SampleGroundRadius(_up, out _groundNormal);
            transform.position = _planet.Center + _up * (_groundRadius + _surfaceGap + _height);
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

        /// <summary>
        /// Насколько крутой склон или высокая ступень встретились на пути.
        /// Крутой склон — скользим вдоль него, высокая ступень — просто не пускаем.
        /// </summary>
        private Vector3 ResolveTerrain(Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.0000001f)
                return delta;

            var nextPosition = transform.position + delta;
            var nextUp = _planet.UpAt(nextPosition);
            var nextGround = SampleGroundRadius(nextUp, out var nextNormal);

            // Склон круче предела — это стена. Скользим вдоль неё.
            if (Vector3.Angle(nextNormal, nextUp) > _slopeLimit)
                return Vector3.ProjectOnPlane(delta, nextNormal);

            // Подъём выше лимита ступени — не пускаем.
            if (_grounded && nextGround - _groundRadius > _maxStepHeight)
                return Vector3.zero;

            return delta;
        }

        /// <summary>Переносит персонажа и сажает его на грунт в новой точке.</summary>
        private void ApplyDelta(Vector3 delta)
        {
            var nextPosition = transform.position + delta;
            var up = _planet.UpAt(nextPosition);

            var newGround = SampleGroundRadius(up, out var normal);

            // Перепад грунта меняет высоту над землёй: подъём «съедает» её, спуск добавляет.
            if (_grounded)
            {
                _height += _groundRadius - newGround;

                if (_height < 0f)
                    _height = 0f;                        // поднимаемся на склон

                if (_height > _maxStepHeight)
                {
                    _grounded = false;                   // обрыв: отрываемся и падаем
                    _verticalSpeed = 0f;
                }
                else
                {
                    _height = 0f;
                    _verticalSpeed = 0f;
                }
            }

            _up = up;
            _groundNormal = normal;
            _groundRadius = newGround;

            transform.position = _planet.Center + up * (newGround + _surfaceGap + _height);

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
        /// Высота грунта в заданном направлении: луч сверху вниз к центру планеты.
        /// Попадания в самого персонажа пропускаются — иначе он становится своей землёй
        /// и уезжает вверх. Если поверхности нет — базовая сфера радиуса R.
        /// </summary>
        private float SampleGroundRadius(Vector3 direction, out Vector3 normal)
        {
            normal = direction;

            var top = _planet.Radius + _terrainHeight;
            var bottom = Mathf.Max(1f, _planet.Radius - _terrainHeight);
            var origin = _planet.Center + direction * top;
            var distance = top - bottom;

            var count = Physics.RaycastNonAlloc(origin, -direction, _groundHits, distance, _groundMask,
                QueryTriggerInteraction.Ignore);

            if (!TryPickNearest(count, _groundHits, out var hit))
                return _planet.Radius;

            normal = hit.normal;

            // Планета — твёрдый шар, и она не имеет коллайдера: грунт задан радиусом.
            // Холм или другой объект может «нырять» под этот уровень (у сплющенной сферы
            // края уходят ниже поверхности). Без этого ограничения персонаж проваливался
            // сквозь планету у подножия холмов.
            var radius = (hit.point - _planet.Center).magnitude;
            return Mathf.Max(radius, _planet.Radius);
        }

        /// <summary>
        /// Отдельные препятствия (стены, ящики): если путь перекрыт — скользим вдоль стены.
        /// </summary>
        private Vector3 ResolveObstacles(Vector3 delta)
        {
            var distance = delta.magnitude;
            if (distance < 0.0001f)
                return delta;

            var origin = transform.position + _up * _bodyCenter;
            var direction = delta / distance;

            if (TrySphereCastIgnoringSelf(origin, _bodyRadius, direction, distance + 0.05f, out var hit))
                return Vector3.ProjectOnPlane(delta, hit.normal);

            return delta;
        }

        private readonly RaycastHit[] _groundHits = new RaycastHit[16];
        private readonly RaycastHit[] _obstacleHits = new RaycastHit[16];

        /// <summary>
        /// Ищет ближайшее попадание, пропуская коллайдеры самого персонажа.
        /// Без этого луч «в грунт» попадает в собственную капсулу: персонаж становится
        /// своей же землёй и уезжает вверх на высоту капсулы каждый кадр.
        /// </summary>
        private bool TrySphereCastIgnoringSelf(Vector3 origin, float radius, Vector3 direction,
            float maxDistance, out RaycastHit nearest)
        {
            nearest = default;

            var count = Physics.SphereCastNonAlloc(origin, radius, direction, _obstacleHits, maxDistance,
                _obstacleMask, QueryTriggerInteraction.Ignore);

            return TryPickNearest(count, _obstacleHits, out nearest);
        }

        private bool TryPickNearest(int count, RaycastHit[] hits, out RaycastHit nearest)
        {
            nearest = default;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];

                // Всё, что принадлежит персонажу, землёй и препятствием не считается.
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                // Враги — не грунт и не стена: сквозь них проходим.
                // Стены и постройки (у них тоже есть Health, но нет метки врага) остаются преградой.
                if (hit.collider.GetComponentInParent<EnemyMarker>() != null)
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                nearest = hit;
            }

            return bestDistance < float.MaxValue;
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
