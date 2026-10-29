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
        private GUIStyle _buttonStyle;

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

            // Запоминаем состояние: часть прогресса может пережить смерть.
            CarryOver.RememberDeath();

            var deathRules = Balance.DeathRules;
            RunSession.CommitRun(_deathShare, deathRules.Gold, deathRules.Experience);

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
            var rules = Balance.DeathRules;
            var snapshot = CarryOver.DeathSnapshot;
            RunState.Reset();
            CarryOver.Restore(snapshot, rules);
            SceneManager.LoadScene(_hubSceneName);
        }

        private void OnGUI()
        {
            if (!_dead || Time.unscaledTime - _deadAt < _menuDelay)
                return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 38,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.35f, 0.30f) }
                };

                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 19,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };

                if (_buttonStyle == null)
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 20,
                        fontStyle = FontStyle.Bold
                    };
            }

            UiCanvas.Begin();

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, UiCanvas.Width, UiCanvas.Height),
                new Color(0.06f, 0f, 0f, 0.82f));

            var panel = new Rect(340f, 140f, 600f, 440f);
            HealthBarDrawer.DrawRect(panel, new Color(0.09f, 0.05f, 0.06f, 0.96f));
            HealthBarDrawer.DrawRect(new Rect(panel.x, panel.y, panel.width, 4f),
                new Color(0.95f, 0.35f, 0.30f, 0.9f));

            GUI.Label(new Rect(panel.x, panel.y + 24f, panel.width, 46f), "ВЫ ПОГИБЛИ", _titleStyle);

            var y = panel.y + 96f;

            GUI.Label(new Rect(panel.x + 40f, y, panel.width - 80f, 26f),
                $"Убито врагов: {RunSession.Kills}", _textStyle);
            GUI.Label(new Rect(panel.x + 40f, y + 32f, panel.width - 80f, 26f),
                $"Добыто: {_runGold} золота, {_runExperience} опыта", _textStyle);
            GUI.Label(new Rect(panel.x + 40f, y + 74f, panel.width - 80f, 26f),
                $"Зачислено {Mathf.RoundToInt(_deathShare * 100f)}%: " +
                $"{_keptGold} золота, {_keptExperience} опыта", _textStyle);
            GUI.Label(new Rect(panel.x + 40f, y + 106f, panel.width - 80f, 26f),
                $"Потеряно {Mathf.RoundToInt((1f - _deathShare) * 100f)}%: " +
                $"{_runGold - _keptGold} золота, {_runExperience - _keptExperience} опыта", _textStyle);

            if (GUI.Button(new Rect(panel.x + 40f, y + 160f, panel.width - 80f, 58f), "В ХАБ  (R)",
                    _buttonStyle))
                Restart();

            GUI.Label(new Rect(panel.x + 40f, y + 232f, panel.width - 80f, 26f),
                "Добыча уже сохранена — в хабе она ждёт вас.", _textStyle);

            UiCanvas.End();
        }
    }
}
