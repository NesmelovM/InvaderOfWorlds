using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Портал выхода. Открывается после смерти босса и даёт два пути:
    /// E — идти дальше, на следующую планету (новый сид, сложность ×1.5),
    /// F — вернуться в хаб, забрав всю добычу с бонусом.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExitPortal : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Health _bossHealth;
        [SerializeField] private Transform _player;

        [Header("Настройки")]
        [SerializeField, Min(0.5f)] private float _radius = 2.6f;
        [Tooltip("Множитель добычи при возврате в хаб (1.2 = +20%)")]
        [SerializeField, Min(1f)] private float _bonusShare = 1.2f;
        [SerializeField] private string _hubSceneName = "Hub";

        [SerializeField] private Color _closedColor = new Color(0.45f, 0.12f, 0.12f);
        [SerializeField] private Color _openColor = new Color(0.25f, 0.9f, 0.95f);

        private bool _open;
        private bool _near;
        private Renderer _renderer;
        private GUIStyle _style;

        public bool IsOpen => _open;

        public void Configure(Health bossHealth, Transform player)
        {
            _bossHealth = bossHealth;
            _player = player;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            ApplyColor(_closedColor);
        }

        private void OnEnable()
        {
            if (_bossHealth != null)
                _bossHealth.Died += OnBossDied;
        }

        private void OnDisable()
        {
            if (_bossHealth != null)
                _bossHealth.Died -= OnBossDied;
        }

        private void OnBossDied(Health health)
        {
            _open = true;
            ApplyColor(_openColor);
            SoundBank.Portal(transform.position);
        }

        private void Update()
        {
            if (!_open || _player == null)
                return;

            _near = Vector3.Distance(_player.position, transform.position) <= _radius;

            if (!_near)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.E))
                NextPlanet();
            else if (Input.GetKeyDown(KeyCode.F))
                ReturnToHub();
#endif
        }

        /// <summary>Дальше: та же планета с новым сидом и врагами в полтора раза злее.</summary>
        private void NextPlanet()
        {
            RunState.AdvancePlanet();
            Time.timeScale = 1f;

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        /// <summary>В хаб: вся добыча с бонусом, состояние забега сбрасывается.</summary>
        private void ReturnToHub()
        {
            RunSession.CommitRun(_bonusShare);
            SaveSystem.Save();
            RunState.Reset();

            Time.timeScale = 1f;
            SceneManager.LoadScene(_hubSceneName);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
                return;

            var material = _renderer.material;
            material.color = color;

            if (color == _openColor)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.8f);
            }
        }

        private void OnGUI()
        {
            if (!_open || !_near)
                return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 19,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.4f, 1f, 1f) }
                };
            }

            var panel = new Rect(Screen.width * 0.5f - 330f, 70f, 660f, 64f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.55f));

            GUI.Label(new Rect(panel.x, panel.y + 6f, panel.width, 26f), "ПОРТАЛ ОТКРЫТ", _style);

            GUI.Label(new Rect(panel.x, panel.y + 32f, panel.width, 24f),
                $"E — дальше (сложность ×{RunState.Difficulty * 1.5f:F1})     F — в хаб с добычей", _style);
        }
    }
}
