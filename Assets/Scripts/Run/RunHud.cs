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
        private Texture2D _coinTexture;

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

            _coinTexture ??= CreateCoinTexture();

            UiCanvas.Begin();

            // Небольшая плашка: значок монеты и сколько собрано в этом забеге.
            var chip = new Rect(UiCanvas.Width - 132f, 18f, 112f, 38f);
            HealthBarDrawer.DrawRect(chip, new Color(0f, 0f, 0f, 0.32f));

            GUI.DrawTexture(new Rect(chip.x + 12f, chip.y + 9f, 20f, 20f), _coinTexture);
            GUI.Label(new Rect(chip.x + 40f, chip.y + 5f, 64f, 28f),
                RunSession.RunGold.ToString(), _countStyle);

            UiCanvas.End();
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
