using System;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Здоровье сущности. Единственное место, где живёт «сколько осталось»,
    /// чтобы игрок, враги и будущие разрушаемые объекты считали одинаково.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float _max = 100f;
        [SerializeField] private bool _destroyOnDeath = true;
        [SerializeField] private bool _logDamage = true;

        private float _current;

        /// <summary>Текущее и максимальное здоровье — для полосок и цифр урона.</summary>
        public event Action<float, float> Changed;

        public event Action<Health> Died;

        public float Max => _max;
        public float Current => _current;
        public float Normalized => _max <= 0f ? 0f : _current / _max;
        public bool IsAlive => _current > 0f;

        private void Awake()
        {
            _current = _max;
        }

        /// <summary>Настройка из кода: пригодится и спавнеру, и тестовой сцене.</summary>
        public void Configure(float max)
        {
            _max = Mathf.Max(1f, max);
            _current = _max;
            Changed?.Invoke(_current, _max);
        }

        /// <summary>
        /// Остаётся ли объект в сцене после смерти. Игроку нужно остаться:
        /// на нём висят камера, меню и итоги забега.
        /// </summary>
        public void SetDestroyOnDeath(bool value)
        {
            _destroyOnDeath = value;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            _current = Mathf.Max(0f, _current - amount);

            if (_logDamage)
                Debug.Log($"[HP] {name}: -{amount:F0} → {_current:F0}/{_max:F0}");

            Changed?.Invoke(_current, _max);

            if (_current > 0f)
                return;

            Died?.Invoke(this);

            if (_destroyOnDeath)
                Destroy(gameObject);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            _current = Mathf.Min(_max, _current + amount);
            Changed?.Invoke(_current, _max);
        }
    }
}
