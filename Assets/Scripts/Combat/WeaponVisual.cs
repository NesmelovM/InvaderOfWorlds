using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Оружие в руке героя: модель под текущий слот и замах при атаке.
    ///
    /// Раньше удар был виден только дугой эффекта — сам клинок в руке отсутствовал.
    /// Здесь модель висит у правого плеча, а при ударе коротко проворачивается,
    /// будто персонаж замахивается.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponVisual : MonoBehaviour
    {
        /// <summary>Модели из пака по индексу оружия в каталоге.</summary>
        private static readonly string[] Models =
        {
            "Weapons/sword",        // Клинок
            "Weapons/bow",          // Арбалет
            "Weapons/hammer",       // Молот
            "Weapons/bow",          // Веер стрел
            "Weapons/club-zulu",    // Импульс
            "Weapons/spear-zulu",   // Глефа
            "Weapons/sword-khopesh",// Искра
            "Weapons/spear-zulu",   // Копьё
            "Weapons/dagger-egypt", // Громовая цепь
            "Weapons/dagger-egypt", // Орбитальный клинок
            "Weapons/club-zulu",    // Аура
            "Weapons/axe",          // Бумеранг
            "Weapons/hammer"        // Ракета
        };

        [SerializeField] private Transform _socket;
        [SerializeField] private float _length = 1.1f;
        [SerializeField] private int _weaponIcon = -1;

        private WeaponSystem _weapons;
        private GameObject _model;
        private int _shownId = -1;
        private GameObject _biomeWeapon;
        private bool _weaponChosen;
        private bool _biomeWeaponRanged;

        /// <summary>Первый активный слот стреляет — значит нужна модель дальнего боя.</summary>
        private bool UsesRangedWeapon
        {
            get
            {
                if (_weapons == null)
                    return false;

                foreach (var slot in _weapons.Slots)
                {
                    if (slot != null && slot.Enabled)
                        return slot.Kind != WeaponKind.Melee;
                }

                return false;
            }
        }

        /// <summary>Случайная модель оружия из списка биома, выбранная на этот забег.</summary>
        private GameObject BiomeWeapon
        {
            get
            {
                var ranged = UsesRangedWeapon;

                // Пересобираем при смене категории: меч и лук нельзя путать.
                if (_weaponChosen && _biomeWeaponRanged != ranged)
                    _weaponChosen = false;

                if (!_weaponChosen)
                {
                    // Биом может быть ещё не назначен в первом кадре, поэтому
                    // повторяем попытку, пока список оружия не станет доступен.
                    _biomeWeapon = Characters.RandomWeapon(ranged);
                    _weaponChosen = _biomeWeapon != null;
                    _biomeWeaponRanged = ranged;
                }

                return _biomeWeapon;
            }
        }
        private float _swing;
        private Quaternion _restRotation;
        private Vector3 _restPosition;

        private void Awake()
        {
            if (_socket == null)
            {
                var socket = new GameObject("Weapon Socket");
                socket.transform.SetParent(transform, false);
                socket.transform.localPosition = new Vector3(0.24f, 1.02f, 0.12f);
                socket.transform.localRotation = Quaternion.Euler(-18f, 8f, -12f);
                _socket = socket.transform;
            }

            // Если модель ригованная, оружие крепим к кисти: тогда его двигает анимация руки.
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(bone.name, "RightHand", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                _socket.SetParent(bone, false);
                _socket.localPosition = Vector3.zero;
                _socket.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            }

            _restPosition = _socket.localPosition;
            _restRotation = _socket.localRotation;

            _weapons = GetComponent<WeaponSystem>();

            if (_weapons != null)
                _weapons.Fired += OnWeaponFired;
        }

        private void OnDestroy()
        {
            if (_weapons != null)
                _weapons.Fired -= OnWeaponFired;
        }

        private void Update()
        {
            RefreshModel();
            AnimateSwing();
        }

        /// <summary>Показывает оружие из первого занятого слота.</summary>
        private void RefreshModel()
        {
            // Сначала смотрим список оружия биома: модель выбирается случайно
            // один раз на забег. Если список пуст — работает каталог оружейной.
            var overrideWeapon = BiomeWeapon != null
                ? BiomeWeapon
                : Characters.PlayerLook != null
                    ? Characters.PlayerLook.Weapon
                    : null;
            var ranged = UsesRangedWeapon;
            var id = overrideWeapon != null ? (ranged ? -3 : -2) : (_weapons != null ? FindWeapon() : -1);

            // Модель могли уничтожить вместе со старой моделью персонажа —
            // тогда пересобираем, даже если оружие не менялось.
            if (id == _shownId && _model != null)
                return;

            _shownId = id;
            EnsureSocket();

            if (_model != null)
                Destroy(_model);

            if (overrideWeapon != null)
            {
                BuildModel(overrideWeapon);
                return;
            }

            if (id < 0 || id >= Models.Length)
                return;

            var prefab = Resources.Load<GameObject>(Models[id]);

            if (prefab == null)
                return;

            BuildModel(prefab);
        }

        /// <summary>
        /// Сокет мог быть уничтожен вместе со старой моделью персонажа (её
        /// пересобирает PlayerAppearance), поэтому создаём его заново при нужде.
        /// </summary>
        private void EnsureSocket()
        {
            if (_socket != null)
                return;

            var holder = transform;

            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(bone.name, "RightHand", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                holder = bone;
                break;
            }

            var socket = new GameObject("Weapon Socket");
            socket.transform.SetParent(holder, false);
            socket.transform.localPosition = holder == transform
                ? new Vector3(0.24f, 1.02f, 0.12f)
                : Vector3.zero;
            socket.transform.localRotation = holder == transform
                ? Quaternion.Euler(-18f, 8f, -12f)
                : Quaternion.Euler(0f, 0f, 90f);

            _socket = socket.transform;
            _restPosition = _socket.localPosition;
            _restRotation = _socket.localRotation;
        }

        private void BuildModel(GameObject prefab)
        {
            if (prefab == null || _socket == null)
                return;

            _model = Instantiate(prefab, _socket);
            _model.name = "Weapon Model";

            foreach (var collider in _model.GetComponentsInChildren<Collider>(true))
                Destroy(collider);

            FitLength(_model, _length);
        }

        private int FindWeapon()
        {
            for (var i = 0; i < _weapons.Slots.Count; i++)
            {
                if (_weapons.Slots[i] != null && _weapons.Slots[i].Enabled)
                {
                    var icon = _weapons.Slots[i].Icon;
                    return icon >= 0 ? icon : i;
                }
            }

            return -1;
        }

        private void OnWeaponFired(WeaponKind kind)
        {
            _swing = 1f;

            var rig = GetComponent<CharacterRig>();

            if (rig != null)
                rig.Attack();
        }

        /// <summary>Замах: модель проворачивается вперёд и возвращается назад.</summary>
        private void AnimateSwing()
        {
            if (_socket == null)
                return;

            if (_swing <= 0f)
            {
                _socket.localRotation = Quaternion.Slerp(_socket.localRotation, _restRotation, Time.deltaTime * 12f);
                _socket.localPosition = Vector3.Lerp(_socket.localPosition, _restPosition, Time.deltaTime * 12f);
                return;
            }

            _swing = Mathf.Max(0f, _swing - Time.deltaTime * 5f);
            var progress = 1f - _swing;
            var angle = Mathf.Sin(progress * Mathf.PI) * 75f;

            _socket.localRotation = _restRotation * Quaternion.Euler(angle, 0f, angle * 0.35f);
            _socket.localPosition = _restPosition + new Vector3(0f, 0f, Mathf.Sin(progress * Mathf.PI) * 0.18f);
        }

        /// <summary>
        /// Подгоняет длину оружия по фактическим вершинам меша.
        /// Renderer.bounds сразу после Instantiate ещё не пересчитан, поэтому
        /// раньше размер получался случайным (лук выходил шире персонажа).
        /// </summary>
        private static void FitLength(GameObject model, float length)
        {
            var root = model.transform;
            var toRoot = root.worldToLocalMatrix;
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;

                if (mesh == null)
                    continue;

                var matrix = toRoot * filter.transform.localToWorldMatrix;

                foreach (var vertex in mesh.vertices)
                {
                    var point = matrix.MultiplyPoint3x4(vertex);
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }

            if (float.IsInfinity(min.x) || float.IsInfinity(max.x))
                return;

            var size = max - min;
            var longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

            if (longest > 0.001f)
                root.localScale *= length / longest;
        }
    }
}
