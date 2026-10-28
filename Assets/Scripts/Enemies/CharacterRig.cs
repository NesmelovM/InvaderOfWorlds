using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Процедурная анимация персонажа по костям.
    ///
    /// В паке есть скелеты, но нет клипов (только превью-анимация на секунду),
    /// поэтому ходьба, замах и падение считаются кодом: находим кости по именам
    /// и добавляем повороты. Работает на любой модели с тем же набором костей —
    /// то есть на всех людях пака, включая врагов.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterRig : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _stepSpeed = 8.5f;
        [SerializeField, Min(0f)] private float _legAmplitude = 32f;
        [SerializeField, Min(0f)] private float _armAmplitude = 26f;
        [SerializeField, Min(0f)] private float _attackDuration = 0.28f;

        [Header("Поза покоя: риги из пака лежат в T-позе")]
        [SerializeField] private Vector3 _leftArmRest = new Vector3(0f, 0f, 68f);
        [SerializeField] private Vector3 _rightArmRest = new Vector3(0f, 0f, -68f);

        private Transform _hips;
        private Transform _spine;
        private Transform _head;
        private Transform _leftUpLeg;
        private Transform _rightUpLeg;
        private Transform _leftArm;
        private Transform _rightArm;

        private Quaternion _hipsBase;
        private Quaternion _spineBase;
        private Quaternion _headBase;
        private Quaternion _leftUpLegBase;
        private Quaternion _rightUpLegBase;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;

        private float _phase;
        private float _attackTimer;
        private bool _dead;
        private float _deadProgress;

        /// <summary>Скорость движения, м/с. Выставляет владелец каждый кадр.</summary>
        public float Speed { get; set; }

        private void Awake()
        {
            // Параметры анимации берём из конфига персонажей, если он есть.
            _stepSpeed = Characters.StepSpeed;
            _legAmplitude = Characters.LegAmplitude;
            _armAmplitude = Characters.ArmAmplitude;
            _attackDuration = Characters.AttackDuration;

            var health = GetComponent<Health>();

            if (health != null)
                health.Died += OnDied;
        }

        private void OnDestroy()
        {
            var health = GetComponent<Health>();

            if (health != null)
                health.Died -= OnDied;
        }

        private void EnsureCached()
        {
            if (_hips != null)
                return;

            CacheBones();

            if (_hips == null)
                return;

            CacheBaseRotations();
        }

        private void CacheBones()
        {
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                var name = bone.name.ToLowerInvariant();

                if (_hips == null && name == "hips") _hips = bone;
                else if (_spine == null && name == "spine1") _spine = bone;
                else if (_head == null && name == "head") _head = bone;
                else if (_leftUpLeg == null && name == "leftupleg") _leftUpLeg = bone;
                else if (_rightUpLeg == null && name == "rightupleg") _rightUpLeg = bone;
                else if (_leftArm == null && name == "leftarm") _leftArm = bone;
                else if (_rightArm == null && name == "rightarm") _rightArm = bone;
            }
        }

        private void CacheBaseRotations()
        {
            // Сначала опускаем руки из T-позы, затем запоминаем позу как базовую.
            ApplyRestPose();

            _hipsBase = RotationOf(_hips);
            _spineBase = RotationOf(_spine);
            _headBase = RotationOf(_head);
            _leftUpLegBase = RotationOf(_leftUpLeg);
            _rightUpLegBase = RotationOf(_rightUpLeg);
            _leftArmBase = RotationOf(_leftArm);
            _rightArmBase = RotationOf(_rightArm);
        }

        /// <summary>
        /// Руки в T-позе смотрят в стороны. Поворачиваем плечо так, чтобы кость
        /// предплечья смотрела вниз — направление считаем от фактической геометрии,
        /// потому что оси костей у пака не совпадают с мировыми.
        /// </summary>
        private void ApplyRestPose()
        {
            PoseLimb(_leftArm, new Vector3(-0.22f, -1f, 0.08f));
            PoseLimb(_rightArm, new Vector3(0.22f, -1f, 0.08f));
        }

        private void PoseLimb(Transform bone, Vector3 desiredLocalDirection)
        {
            if (bone == null || bone.childCount == 0)
                return;

            var child = bone.GetChild(0);
            var current = (child.position - bone.position).normalized;
            var desired = transform.TransformDirection(desiredLocalDirection.normalized);

            if (current.sqrMagnitude < 0.001f || desired.sqrMagnitude < 0.001f)
                return;

            bone.rotation = Quaternion.FromToRotation(current, desired) * bone.rotation;
        }

        private static Quaternion RotationOf(Transform bone)
        {
            return bone != null ? bone.localRotation : Quaternion.identity;
        }

        public void Attack()
        {
            if (_dead)
                return;

            _attackTimer = _attackDuration;
        }

        private void OnDied(Health health)
        {
            _dead = true;
            _deadProgress = 0f;
        }

        private void LateUpdate()
        {
            // Кости ищем лениво: модель могла быть пересоздана другим компонентом
            // уже после Awake, и тогда кеш из Start оказался бы пустым.
            EnsureCached();

            if (_hips == null)
                return;

            if (_dead)
            {
                AnimateDeath();
                return;
            }

            AnimateWalk();
            AnimateAttack();
        }

        private void AnimateWalk()
        {
            var intensity = Mathf.Clamp01(Speed / 6f);

            if (Speed > 0.2f)
                _phase += Speed * _stepSpeed * Time.deltaTime;

            var swing = Mathf.Sin(_phase) * intensity;

            // Ноги и руки в противофазе: обычная походка.
            Apply(_leftUpLeg, _leftUpLegBase, swing * _legAmplitude);
            Apply(_rightUpLeg, _rightUpLegBase, -swing * _legAmplitude);
            Apply(_leftArm, _leftArmBase, -swing * _armAmplitude * 0.7f);
            Apply(_rightArm, _rightArmBase, swing * _armAmplitude * 0.7f);

            // Корпус слегка покачивается, голова держится ровно.
            Apply(_spine, _spineBase, Mathf.Sin(_phase * 2f) * intensity * 3f);
            Apply(_head, _headBase, -Mathf.Sin(_phase * 2f) * intensity * 2f);

            if (intensity < 0.01f)
                Apply(_hips, _hipsBase, Mathf.Sin(Time.time * 1.6f) * 1.2f);
        }

        private void AnimateAttack()
        {
            if (_attackTimer <= 0f)
                return;

            _attackTimer = Mathf.Max(0f, _attackTimer - Time.deltaTime);
            var progress = 1f - _attackTimer / Mathf.Max(0.01f, _attackDuration);
            var angle = Mathf.Sin(progress * Mathf.PI) * 105f;

            Apply(_rightArm, _rightArmBase, -angle);
            Apply(_spine, _spineBase, angle * 0.12f);
        }

        private void AnimateDeath()
        {
            _deadProgress = Mathf.Min(1f, _deadProgress + Time.deltaTime * 1.8f);

            // Падение: корпус уходит вперёд, руки разводятся.
            Apply(_spine, _spineBase, Mathf.Lerp(0f, 78f, _deadProgress));
            Apply(_head, _headBase, Mathf.Lerp(0f, 25f, _deadProgress));
            Apply(_leftArm, _leftArmBase, Mathf.Lerp(0f, 40f, _deadProgress));
            Apply(_rightArm, _rightArmBase, Mathf.Lerp(0f, 40f, _deadProgress));
        }

        private static void Apply(Transform bone, Quaternion baseRotation, float degrees)
        {
            if (bone == null)
                return;

            bone.localRotation = baseRotation * Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
