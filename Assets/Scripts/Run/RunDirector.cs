using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Управляет забегом: следит за смертью игрока, останавливает бой и показывает
    /// итоги. Пока это один забег на сцену, позже сюда добавится возврат в хаб.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunDirector : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;
        [SerializeField, Min(0f)] private float _menuDelay = 0.8f;

        private bool _dead;
        private float _deadAt;
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;

        public void Configure(Health playerHealth)
        {
            _playerHealth = playerHealth;
        }

        private void Awake()
        {
            if (_playerHealth == null)
            {
                var player = GameObject.FindWithTag("Player");

                if (player != null)
                    _playerHealth = player.GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (_playerHealth != null)
                _playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.Died -= OnPlayerDied;
        }

        private void OnPlayerDied(Health health)
        {
            if (_dead)
                return;

            _dead = true;
            _deadAt = Time.unscaledTime;

            // Замораживаем мир, но не ввод: по R нужно перезапустить забег.
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var motor = health.GetComponent<PlayerMotor>();

            if (motor != null)
                motor.enabled = false;

            var attack = health.GetComponent<PlayerAttack>();

            if (attack != null)
                attack.enabled = false;

            Debug.Log($"[RUN] Игрок погиб. Убито: {RunSession.Kills}, золото: {RunSession.Gold}, опыт: {RunSession.Experience}");
        }

        private void Update()
        {
            if (!_dead || Time.unscaledTime - _deadAt < _menuDelay)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R))
                Restart();
#endif
        }

        private void Restart()
        {
            Time.timeScale = 1f;

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        private void OnGUI()
        {
            if (!_dead || Time.unscaledTime - _deadAt < _menuDelay)
                return;

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.06f, 0f, 0f, 0.78f));

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 44,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.35f, 0.30f) }
                };

                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            var top = Screen.height * 0.28f;

            GUI.Label(new Rect(0f, top, Screen.width, 60f), "ВЫ ПОГИБЛИ", _titleStyle);
            GUI.Label(new Rect(0f, top + 74f, Screen.width, 30f), $"Убито врагов: {RunSession.Kills}", _textStyle);
            GUI.Label(new Rect(0f, top + 106f, Screen.width, 30f),
                $"Золото: {RunSession.Gold}    Опыт: {RunSession.Experience}", _textStyle);
            GUI.Label(new Rect(0f, top + 160f, Screen.width, 30f), "R — новый забег", _textStyle);
        }
    }
}
