using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Портал выхода. Закрыт, пока жив босс; после его смерти открывается,
    /// и вход в него завершает забег победой: вся добыча плюс бонус за чистый заход.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExitPortal : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Health _bossHealth;
        [SerializeField] private Transform _player;

        [Header("Настройки")]
        [SerializeField, Min(0.5f)] private float _radius = 2.6f;
        [Tooltip("Множитель добычи при успешном уходе (1.2 = +20%)")]
        [SerializeField, Min(1f)] private float _bonusShare = 1.2f;
        [SerializeField] private string _hubSceneName = "Hub";

        [SerializeField] private Color _closedColor = new Color(0.45f, 0.12f, 0.12f);
        [SerializeField] private Color _openColor = new Color(0.25f, 0.9f, 0.95f);

        private bool _open;
        private bool _used;
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
            Debug.Log("[PORTAL] Босс повержен, портал открыт.");
        }

        private void Update()
        {
            if (!_open || _used || _player == null)
                return;

            var up = (_player.position - transform.position).normalized;

            if (Vector3.Distance(_player.position, transform.position) > _radius)
                return;

            Complete();
        }

        private void Complete()
        {
            _used = true;

            // Уходим победителем: вся добыча и бонус.
            RunSession.CommitRun(_bonusShare);
            SaveSystem.Save();

            Debug.Log($"[PORTAL] Забег завершён победой. Убито: {RunSession.Kills}, " +
                      $"в кошельке: {RunSession.Gold} золота, {RunSession.Experience} опыта");

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
            if (!_open || _used)
                return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.4f, 1f, 1f) }
                };
            }

            var panel = new Rect(Screen.width * 0.5f - 260f, 70f, 520f, 34f);
            HealthBarDrawer.DrawRect(panel, new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(new Rect(panel.x, panel.y + 5f, panel.width, 26f),
                "ПОРТАЛ ОТКРЫТ — ВОЙДИТЕ В НЕГО", _style);
        }
    }
}
