using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Регенерация здоровья из карточек: лечит игрока каждый тик.
    ///
    /// Значение накапливается в состоянии забега, поэтому работает и после
    /// перехода на следующую планету.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunRegen : MonoBehaviour
    {
        private Health _health;
        private float _timer;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void Update()
        {
            if (_health == null || !_health.IsAlive || RunState.RegenPerSecond <= 0f)
                return;

            _timer += Time.deltaTime;

            if (_timer < 1f)
                return;

            _timer -= 1f;
            _health.Heal(RunState.RegenPerSecond);
        }
    }
}
