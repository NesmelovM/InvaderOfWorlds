using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Сундук на планете: открывается по E, когда игрок рядом, и выбрасывает
    /// золото, лечение и иногда улучшение характера.
    ///
    /// Модель собирается из примитивов — отдельный префаб не нужен.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        private const float OpenRadius = 2.6f;

        private static Material _wood;
        private static Material _metal;

        private Transform _lid;
        private Transform _player;
        private bool _opened;
        private float _lidAngle;
        private GUIStyle _hintStyle;

        public static void Spawn(Planet planet, Vector3 direction, Transform player, Transform parent)
        {
            var root = new GameObject("Chest");
            root.transform.SetParent(parent, false);
            root.transform.position = planet.Center + direction.normalized * (planet.Radius - 0.03f);

            var up = direction.normalized;
            var forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;

            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;

            root.transform.rotation = Quaternion.LookRotation(forward, up);

            var chest = root.AddComponent<Chest>();
            chest._player = player;
            chest.Build();
        }

        private void Build()
        {
            _wood ??= EnemyFactory.GetMaterial(new Color(0.34f, 0.20f, 0.10f));
            _metal ??= EnemyFactory.GetMaterial(new Color(0.90f, 0.75f, 0.30f));

            // Плинтус: чуть шире корпуса, чтобы сундук читался стоящим на земле.
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Plinth";
            plinth.transform.SetParent(transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            plinth.transform.localScale = new Vector3(1.2f, 0.12f, 0.82f);
            Paint(plinth, _wood);

            // Корпус.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            body.transform.localScale = new Vector3(1.1f, 0.6f, 0.7f);
            Paint(body, _wood);

            // Крышка: петля по задней кромке, поэтому центр крышки смещён вперёд.
            var pivot = new GameObject("Lid Pivot").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, 0.72f, -0.35f);

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Lid";
            lid.transform.SetParent(pivot, false);
            lid.transform.localPosition = new Vector3(0f, 0.11f, 0.35f);
            lid.transform.localScale = new Vector3(1.14f, 0.22f, 0.74f);
            Paint(lid, _wood);
            _lid = pivot;

            // Металлические полосы по бокам.
            for (var i = -1; i <= 1; i += 2)
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = "Band";
                band.transform.SetParent(transform, false);
                band.transform.localPosition = new Vector3(i * 0.40f, 0.42f, 0f);
                band.transform.localScale = new Vector3(0.08f, 0.62f, 0.72f);
                Paint(band, _metal);
            }

            // Замок спереди.
            var lockPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lockPlate.name = "Lock";
            lockPlate.transform.SetParent(transform, false);
            lockPlate.transform.localPosition = new Vector3(0f, 0.52f, 0.37f);
            lockPlate.transform.localScale = new Vector3(0.2f, 0.2f, 0.06f);
            Paint(lockPlate, _metal);
        }

        private static void Paint(GameObject part, Material material)
        {
            var collider = part.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            var renderer = part.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private void Update()
        {
            if (_opened)
            {
                // Плавно откидываем крышку.
                _lidAngle = Mathf.Lerp(_lidAngle, -105f, Time.deltaTime * 6f);

                if (_lid != null)
                    _lid.localRotation = Quaternion.Euler(_lidAngle, 0f, 0f);

                return;
            }

            if (_player == null)
                return;

            if ((_player.position - transform.position).magnitude > OpenRadius)
                return;

            if (Input.GetKeyDown(KeyCode.E))
                Open();
        }

        private void Open()
        {
            _opened = true;
            SoundBank.Coin(transform.position);

            var lootRoot = new GameObject("Chest Loot").transform;
            lootRoot.position = transform.position;
            var up = transform.up;

            var coins = Random.Range(4, 7);

            for (var i = 0; i < coins; i++)
            {
                Loot.Spawn(transform.position + up * 0.6f + Random.insideUnitSphere * 0.25f,
                    up, _player, LootKind.Gold, Random.Range(4, 9));
            }

            Loot.Spawn(transform.position + up * 0.7f, up, _player, LootKind.Heal, 30);

            // Примерно каждый третий сундук даёт улучшение.
            if (Random.value < 0.35f)
                Loot.Spawn(transform.position + up * 0.8f, up, _player, LootKind.Upgrade, 1);
        }

        private void OnGUI()
        {
            if (_opened || _player == null)
                return;

            if ((_player.position - transform.position).magnitude > OpenRadius)
                return;

            UiCanvas.Begin();

            _hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            var plate = new Rect(UiCanvas.Width * 0.5f - 150f, UiCanvas.Height - 156f, 300f, 40f);
            HealthBarDrawer.DrawRect(plate, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            GUI.Label(new Rect(plate.x, plate.y + 8f, plate.width, 26f),
                "E — открыть сундук", _hintStyle);

            UiCanvas.End();
        }
    }
}
