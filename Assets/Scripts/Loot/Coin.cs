using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Монета, которая вылетает из врага и летит к игроку.
    ///
    /// Золото начисляет <see cref="EnemyReward"/>, поэтому монеты — только эффект:
    /// они разлетаются, а потом притягиваются к персонажу и исчезают.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        private const float HomingDelay = 0.35f;
        private const float MagnetRadius = 7f;

        private static Material _gold;

        private Transform _target;
        private Vector3 _velocity;
        private float _age;
        private float _spin;

        /// <summary>Раскидывает монеты вокруг точки смерти врага.</summary>
        public static void Burst(Vector3 origin, Transform target, int count)
        {
            if (target == null)
                return;

            for (var i = 0; i < count; i++)
            {
                var coin = Create(origin);

                if (coin == null)
                    continue;

                coin._target = target;
                coin._velocity = new Vector3(
                    Random.Range(-1.6f, 1.6f),
                    Random.Range(2.2f, 3.6f),
                    Random.Range(-1.6f, 1.6f));
            }
        }

        private static Coin Create(Vector3 position)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Coin";
            disc.transform.position = position + Vector3.up * 0.4f;
            disc.transform.localScale = new Vector3(0.16f, 0.02f, 0.16f);

            var collider = disc.GetComponent<Collider>();
            var coin = disc.AddComponent<Coin>();
            coin._spin = Random.Range(180f, 420f);

            if (collider != null)
                Destroy(collider);

            var renderer = disc.GetComponent<Renderer>();

            if (renderer != null)
            {
                _gold ??= EnemyFactory.GetMaterial(new Color(1f, 0.82f, 0.24f));
                renderer.sharedMaterial = _gold;
            }

            return coin;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.Rotate(Vector3.up, _spin * Time.deltaTime, Space.World);

            if (_age < HomingDelay)
            {
                _velocity += Vector3.down * 9.8f * Time.deltaTime;
                transform.position += _velocity * Time.deltaTime;
                return;
            }

            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            var goal = _target.position + Vector3.up * 1.1f;
            var toTarget = goal - transform.position;
            var distance = toTarget.magnitude;

            if (distance < 0.4f)
            {
                Destroy(gameObject);
                return;
            }

            var speed = Mathf.Lerp(6f, 26f, 1f - Mathf.Clamp01(distance / MagnetRadius));
            transform.position += toTarget / Mathf.Max(0.001f, distance) * speed * Time.deltaTime;
        }
    }
}
