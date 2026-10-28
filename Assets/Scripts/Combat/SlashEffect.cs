using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Короткий видимый замах: дуга появляется перед героем и быстро исчезает.
    /// Без него удар клинком и глефой выглядел как «ничего не произошло»,
    /// хотя урон наносился.
    /// </summary>
    public class SlashEffect : MonoBehaviour
    {
        [SerializeField, Min(0.03f)] private float _lifetime = 0.16f;
        [SerializeField, Min(0f)] private float _sweepSpeed = 320f;

        private float _life;
        private Vector3 _up;
        private float _startScale;

        public void Launch(Vector3 up, float size)
        {
            _up = up;
            _life = _lifetime;
            _startScale = size;
            transform.localScale = Vector3.one * size;
        }

        private void Update()
        {
            _life -= Time.deltaTime;

            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var progress = 1f - Mathf.Clamp01(_life / _lifetime);

            // Дуга чуть провожает удар и подрастает, чтобы движение читалось.
            transform.Rotate(_up, _sweepSpeed * Time.deltaTime, Space.World);
            transform.localScale = Vector3.one * (_startScale * (1f + 0.25f * progress));
        }
    }
}
