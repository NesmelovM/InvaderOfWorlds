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
        [SerializeField] private string _hubSceneName = "Hub";
        [Tooltip("Доля добычи, которая доходит до кошелька при смерти")]
        [SerializeField, Range(0f, 1f)] private float _deathShare = 0.3f;

        private bool _dead;
        private float _deadAt;
        private int _runGold;
        private int _runExperience;
        private int _keptGold;
        private int _keptExperience;
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

            var attack = health.GetComponent<WeaponSystem>();

            if (attack != null)
                attack.enabled = false;


            // При смерти доходит только часть добычи — остальное теряется.
            // Доля берётся из конфига баланса, значение в компоненте — запасное.
            _deathShare = Balance.DeathPenaltyShare;
            _runGold = RunSession.RunGold;
            _runExperience = RunSession.RunExperience;
            _keptGold = Mathf.RoundToInt(_runGold * _deathShare);
            _keptExperience = Mathf.RoundToInt(_runExperience * _deathShare);

            RunSession.CommitRun(_deathShare);

            // Добыча за забег должна пережить даже закрытие игры.
            SaveSystem.Save();
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
            SceneManager.LoadScene(_hubSceneName);
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
                $"Добыто: {_runGold} золота, {_runExperience} опыта", _textStyle);
            GUI.Label(new Rect(0f, top + 138f, Screen.width, 30f),
                $"Зачислено {Mathf.RoundToInt(_deathShare * 100f)}%: " +
                $"{_keptGold} золота, {_keptExperience} опыта", _textStyle);
            GUI.Label(new Rect(0f, top + 170f, Screen.width, 30f),
                $"Потеряно {Mathf.RoundToInt((1f - _deathShare) * 100f)}%: " +
                $"{_runGold - _keptGold} золота, {_runExperience - _keptExperience} опыта", _textStyle);
            GUI.Label(new Rect(0f, top + 210f, Screen.width, 30f), "R — вернуться в хаб", _textStyle);
        }
    }
}
