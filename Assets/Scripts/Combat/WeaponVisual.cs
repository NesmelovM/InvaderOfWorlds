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
            if (_weapons == null)
                return;

            var id = FindWeapon();

            if (id == _shownId)
                return;

            _shownId = id;

            if (_model != null)
                Destroy(_model);

            if (id < 0 || id >= Models.Length)
                return;

            var prefab = Resources.Load<GameObject>(Models[id]);

            if (prefab == null)
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

        private static void FitLength(GameObject model, float length)
        {
            var min = float.MaxValue;
            var max = float.MinValue;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.bounds;
                var size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                min = Mathf.Min(min, size);
                max = Mathf.Max(max, size);
            }

            if (max <= 0.01f)
                return;

            model.transform.localScale *= length / max;
        }
    }
}
