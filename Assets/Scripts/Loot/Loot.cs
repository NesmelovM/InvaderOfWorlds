using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>Что выпало из сундука.</summary>
    public enum LootKind
    {
        Gold,
        Heal,
        Upgrade
    }

    /// <summary>
    /// Предмет из сундука: вылетает по дуге, падает на землю, потом притягивается
    /// к игроку и срабатывает — золото в кошелёк, лечение, улучшение характера.
    /// </summary>
    public class Loot : MonoBehaviour
    {
        private const float PickupRadius = 1.7f;
        private const float MagnetRadius = 3.5f;

        private static Material _gold;
        private static Material _heal;
        private static Material _upgrade;

        private LootKind _kind;
        private int _value;
        private Transform _player;
        private Vector3 _velocity;
        private Vector3 _up;
        private bool _landed;
        private float _age;

        public static void Spawn(Vector3 origin, Vector3 up, Transform player, LootKind kind, int value)
        {
            var item = GameObject.CreatePrimitive(kind == LootKind.Gold
                ? PrimitiveType.Cylinder
                : PrimitiveType.Sphere);

            item.name = kind switch
            {
                LootKind.Heal => "Loot Heal",
                LootKind.Upgrade => "Loot Upgrade",
                _ => "Loot Gold"
            };

            item.transform.position = origin + up * 0.5f;
            item.transform.localScale = kind == LootKind.Gold
                ? new Vector3(0.32f, 0.05f, 0.32f)
                : Vector3.one * 0.5f;

            var collider = item.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            var renderer = item.GetComponent<Renderer>();

            if (renderer != null)
            {
                _gold ??= EnemyFactory.GetMaterial(new Color(1f, 0.82f, 0.24f));
                _heal ??= EnemyFactory.GetMaterial(new Color(0.35f, 0.95f, 0.45f));
                _upgrade ??= EnemyFactory.GetMaterial(new Color(0.72f, 0.45f, 0.95f));
                renderer.sharedMaterial = kind switch
                {
                    LootKind.Heal => _heal,
                    LootKind.Upgrade => _upgrade,
                    _ => _gold
                };
            }

            var loot = item.AddComponent<Loot>();
            loot._kind = kind;
            loot._value = value;
            loot._player = player;
            loot._up = up;
            loot._velocity = up * Random.Range(3.2f, 4.6f) + Random.insideUnitSphere * 1.6f;
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_player == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!_landed)
            {
                FlyOut();
                return;
            }

            MagnetToPlayer();
        }

        private void FlyOut()
        {
            _velocity += -_up * 14f * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            transform.Rotate(_up, 220f * Time.deltaTime, Space.World);

            if (_age < 0.25f || _velocity.y > 0f)
                return;

            // Планета круглая, поэтому «пол» — поверхность по нормали в точке падения.
            var planet = Object.FindAnyObjectByType<Planet>();

            if (planet == null)
                return;

            var up = (transform.position - planet.Center).normalized;
            var ground = planet.Center + up * (planet.Radius + 0.3f);

            if (Vector3.Dot(transform.position - ground, up) > 0f)
                return;

            _landed = true;
            _up = up;
            transform.position = ground;
        }

        private void MagnetToPlayer()
        {
            var goal = _player.position + Vector3.up * 0.8f;
            var toTarget = goal - transform.position;
            var distance = toTarget.magnitude;

            if (distance < PickupRadius)
            {
                Apply();
                return;
            }

            if (distance > MagnetRadius)
            {
                transform.Rotate(_up, 120f * Time.deltaTime, Space.World);
                return;
            }

            var speed = Mathf.Lerp(4f, 18f, 1f - Mathf.Clamp01(distance / MagnetRadius));
            transform.position += toTarget / Mathf.Max(0.001f, distance) * speed * Time.deltaTime;
        }

        private void Apply()
        {
            switch (_kind)
            {
                case LootKind.Gold:
                    RunSession.Grant(_value);
                    SoundBank.Coin(transform.position);
                    Toast.Show($"+{_value} золота", new Color(1f, 0.84f, 0.35f));
                    break;

                case LootKind.Heal:
                    RunModifiers.PlayerHealth?.Heal(_value);
                    SoundBank.LevelUp();
                    Toast.Show($"+{_value} здоровья", new Color(0.45f, 0.95f, 0.5f));
                    break;

                case LootKind.Upgrade:
                    var level = Object.FindAnyObjectByType<RunLevel>();
                    var title = level != null ? level.GrantRandomUpgrade() : null;

                    SoundBank.LevelUp();
                    Toast.Show(title != null ? $"Улучшение: {title}" : "Улучшение", new Color(0.75f, 0.5f, 1f));
                    break;
            }

            Destroy(gameObject);
        }
    }
}
