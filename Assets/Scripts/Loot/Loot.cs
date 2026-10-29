using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>Что выпало из сундука или лежит на планете.</summary>
    public enum LootKind
    {
        Gold,
        Heal,
        Card
    }

    /// <summary>
    /// Предмет добычи: вылетает из сундука по кругу, падает на грунт и лежит,
    /// пока игрок его не подберёт. Магнита нет — подбирать надо ногами.
    /// </summary>
    public class Loot : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float _pickupRadius = 1.25f;
        [SerializeField, Min(0f)] private float _bobHeight = 0.12f;

        private LootKind _kind;
        private int _value;
        private int _rarity;
        private Transform _player;
        private Vector3 _velocity;
        private Vector3 _up;
        private bool _landed;
        private float _age;
        private float _bobPhase;

        /// <summary>
        /// Создаёт предмет. <paramref name="direction"/> задаёт сторону разлёта,
        /// чтобы из сундука они ложились ровным кругом.
        /// </summary>
        public static Loot Spawn(Vector3 origin, Vector3 up, Transform player, LootKind kind, int value,
            Vector3? direction = null, int rarity = 0)
        {
            var item = GameObject.CreatePrimitive(kind == LootKind.Gold
                ? PrimitiveType.Cylinder
                : PrimitiveType.Sphere);

            item.name = kind switch
            {
                LootKind.Heal => "Loot Heal",
                LootKind.Card => "Loot Card",
                _ => "Loot Gold"
            };

            item.transform.position = origin;
            item.transform.localScale = kind == LootKind.Gold
                ? new Vector3(0.3f, 0.05f, 0.3f)
                : Vector3.one * (kind == LootKind.Card ? 0.55f : 0.42f);

            var collider = item.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            var renderer = item.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = MaterialFor(kind, rarity);

            var loot = item.AddComponent<Loot>();
            loot._kind = kind;
            loot._value = value;
            loot._rarity = rarity;
            loot._player = player;
            loot._up = up;
            loot._bobPhase = Random.Range(0f, Mathf.PI * 2f);

            // Разлёт: в заданную сторону, иначе куда попало. Немного вверх, чтобы
            // предмет было видно в момент выпадения.
            var side = direction.HasValue ? direction.Value : Random.insideUnitSphere;

            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.forward;

            side = Vector3.ProjectOnPlane(side, up).normalized;
            loot._velocity = side * Random.Range(3.4f, 4.4f) + up * Random.Range(3.4f, 4.2f);

            return loot;
        }

        /// <summary>Цвет предмета: золото жёлтое, лечение зелёное, сфера — по редкости.</summary>
        private static Material MaterialFor(LootKind kind, int rarity)
        {
            if (kind == LootKind.Gold)
                return EnemyFactory.GetMaterial(new Color(1f, 0.82f, 0.24f));

            if (kind == LootKind.Heal)
                return EnemyFactory.GetMaterial(new Color(0.35f, 0.95f, 0.45f));

            var color = rarity switch
            {
                2 => new Color(0.75f, 0.42f, 1f),
                1 => new Color(0.35f, 0.7f, 1f),
                _ => new Color(0.42f, 0.95f, 0.55f)
            };

            return EnemyFactory.GetMaterial(color);
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

            BobAndSpin();

            if ((_player.position - transform.position).sqrMagnitude <= _pickupRadius * _pickupRadius)
                Apply();
        }

        private void FlyOut()
        {
            _velocity += -_up * 14f * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            transform.Rotate(_up, 220f * Time.deltaTime, Space.World);

            if (_age < 0.2f || Vector3.Dot(_velocity, _up) > 0f)
                return;

            var planet = Object.FindAnyObjectByType<Planet>();

            if (planet == null)
                return;

            var up = (transform.position - planet.Center).normalized;
            var ground = planet.Center + up * (planet.Radius + 0.35f);

            if (Vector3.Dot(transform.position - ground, up) > 0f)
                return;

            _landed = true;
            _up = up;
            transform.position = ground;
        }

        /// <summary>Лёгкое покачивание: предмет заметно на песке и в траве.</summary>
        private void BobAndSpin()
        {
            transform.Rotate(_up, 70f * Time.deltaTime, Space.World);

            var planet = Object.FindAnyObjectByType<Planet>();

            if (planet == null)
                return;

            var bob = Mathf.Sin(Time.time * 2.2f + _bobPhase) * _bobHeight;
            var up = (transform.position - planet.Center).normalized;
            var ground = planet.Center + up * (planet.Radius + 0.35f);
            transform.position = ground + up * (0.3f + bob);
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

                case LootKind.Card:
                    var level = Object.FindAnyObjectByType<RunLevel>();

                    if (level != null && level.OfferCards())
                    {
                        SoundBank.LevelUp();
                        break;
                    }

                    // Если выбор карточек недоступен — даём улучшение сразу.
                    var title = level != null ? level.GrantRandomUpgrade() : null;
                    Toast.Show(title != null ? $"Улучшение: {title}" : "Улучшение",
                        new Color(0.75f, 0.5f, 1f));
                    break;
            }

            Destroy(gameObject);
        }
    }
}
