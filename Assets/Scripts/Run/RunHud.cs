using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ненавязчивый счётчик забега: только собранные за забег монеты, значком.
    ///
    /// Опыт переехал в полоску под здоровьем — там его видно, не отвлекаясь
    /// от боя, а крупная панель в углу больше не нужна.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunHud : MonoBehaviour
    {
        [SerializeField] private RunLevel _level;

        private GUIStyle _countStyle;
        private GUIStyle _killsStyle;
        private Texture2D _coinTexture;
        private Texture2D _skullTexture;

        public void Configure(RunLevel level)
        {
            _level = level;
        }

        private void Awake()
        {
            if (_level == null)
                _level = FindAnyObjectByType<RunLevel>();
        }

        private void OnGUI()
        {
            // Во время выбора карточки экран принадлежит ей.
            if (_level != null && _level.IsChoosing)
                return;

            _countStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.98f, 0.90f, 0.62f) }
            };

            _killsStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.90f, 0.92f, 0.96f) }
            };

            _coinTexture ??= CreateCoinTexture();
            _skullTexture ??= CreateSkullTexture();

            UiCanvas.Begin();

            // Небольшая плашка: монеты за забег и убийства.
            var chip = new Rect(UiCanvas.Width - 214f, 18f, 194f, 38f);
            HealthBarDrawer.DrawRect(chip, new Color(0f, 0f, 0f, 0.32f));

            GUI.DrawTexture(new Rect(chip.x + 12f, chip.y + 9f, 20f, 20f), _coinTexture);
            GUI.Label(new Rect(chip.x + 40f, chip.y + 5f, 56f, 28f),
                RunSession.RunGold.ToString(), _countStyle);

            // Черепок и число убийств.
            GUI.DrawTexture(new Rect(chip.x + 112f, chip.y + 9f, 20f, 20f), _skullTexture);
            GUI.Label(new Rect(chip.x + 140f, chip.y + 5f, 46f, 28f),
                RunSession.Kills.ToString(), _killsStyle);

            UiCanvas.End();
        }

        /// <summary>
        /// Черепок для счётчика убийств: голова, глазницы, нос и зубы —
        /// рисуется попиксельно, чтобы не тащить иконку в проект.
        /// </summary>
        private static Texture2D CreateSkullTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var bone = new Color(0.93f, 0.94f, 0.90f);
            var dark = new Color(0.08f, 0.09f, 0.12f, 1f);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)(size - 1);
                    var v = y / (float)(size - 1);

                    // Череп: круг сверху и челюсть снизу.
                    var head = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.58f)) < 0.34f;
                    var jaw = u > 0.34f && u < 0.66f && v > 0.16f && v < 0.44f;

                    if (!head && !jaw)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    var color = bone;

                    // Глазницы.
                    var leftEye = Vector2.Distance(new Vector2(u, v), new Vector2(0.39f, 0.62f)) < 0.10f;
                    var rightEye = Vector2.Distance(new Vector2(u, v), new Vector2(0.61f, 0.62f)) < 0.10f;
                    var nose = u > 0.46f && u < 0.54f && v > 0.44f && v < 0.54f;

                    // Щели между зубами.
                    var teeth = v < 0.34f && v > 0.18f && Mathf.Repeat(u * 5f, 1f) < 0.22f;

                    if (leftEye || rightEye || nose || teeth)
                        color = dark;

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Мягкий жёлтый кружок вместо слова «монета».</summary>
        private static Texture2D CreateCoinTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.42f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = Mathf.Clamp01((radius - distance) / 2.5f);
                    var edge = Mathf.Clamp01((distance - radius * 0.55f) / (radius * 0.45f));
                    var color = Color.Lerp(new Color(1f, 0.82f, 0.30f), new Color(0.92f, 0.66f, 0.16f), edge);
                    color.a = alpha;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
