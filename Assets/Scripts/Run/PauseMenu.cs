using UnityEngine;
using UnityEngine.SceneManagement;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Меню паузы в забеге: Esc — пауза, настройки и выход в хаб.
    ///
    /// Без него игрок заперт на планете, пока не погибнет или не пройдёт портал.
    /// Оформление то же, что в хабе: виртуальный холст и тёмные панели.
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenu : MonoBehaviour
    {
        private const string SensitivityKey = "iw.sensitivity";
        private const string InvertKey = "iw.invert";

        [SerializeField] private string _hubSceneName = "Hub";
        [SerializeField] private RunLevel _level;
        [SerializeField] private Health _playerHealth;

        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;

        private bool _paused;
        private bool _settings;
        private bool _confirmReset;

        public void Configure(RunLevel level, Health playerHealth)
        {
            _level = level;
            _playerHealth = playerHealth;
        }

        private void Awake()
        {
            if (_level == null)
                _level = FindAnyObjectByType<RunLevel>();

            if (_playerHealth == null)
            {
                var player = GameObject.FindWithTag("Player");

                if (player != null)
                    _playerHealth = player.GetComponent<Health>();
            }
        }

        private void Update()
        {
            // Пока открыт выбор карточки или игрок мёртв, пауза не нужна:
            // своими экранами они уже останавливают игру.
            if (UiState.StartOpen || (_level != null && _level.IsChoosing))
                return;

            if (_playerHealth != null && !_playerHealth.IsAlive)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
#endif
        }

        private void Toggle()
        {
            _paused = !_paused;

            if (!_paused && UiState.CardsOpen)
                return;

            Time.timeScale = _paused ? 0f : 1f;

            UiState.SetPause(_paused);

            Cursor.lockState = _paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _paused;

            if (!_paused)
                _settings = false;
        }

        private void Close()
        {
            _paused = false;
            _settings = false;
            UiState.SetPause(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Выход посреди забега: добыча идёт по правилу смерти.</summary>
        private void LeaveToHub()
        {
            RunSession.CommitRun(Balance.DeathPenaltyShare);
            SaveSystem.Save();
            UiState.SetPause(false);

            Time.timeScale = 1f;
            SceneManager.LoadScene(_hubSceneName);
        }

        private void OnGUI()
        {
            if (!_paused)
                return;

            EnsureStyles();
            UiCanvas.Begin();

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, UiCanvas.Width, UiCanvas.Height),
                new Color(0.02f, 0.03f, 0.06f, 0.8f));

            var panel = new Rect(340f, 110f, 600f, 520f);
            HealthBarDrawer.DrawRect(panel, new Color(0.05f, 0.06f, 0.09f, 0.96f));
            HealthBarDrawer.DrawRect(new Rect(panel.x, panel.y, panel.width, 4f),
                new Color(0.95f, 0.80f, 0.45f, 0.9f));

            GUI.Label(new Rect(panel.x, panel.y + 24f, panel.width, 40f), "ПАУЗА", _titleStyle);

            if (_settings)
                DrawSettings(panel);
            else
                DrawMain(panel);

            UiCanvas.End();
        }

        private void DrawMain(Rect panel)
        {
            var width = panel.width - 80f;
            var x = panel.x + 40f;
            var y = panel.y + 96f;

            GUI.Label(new Rect(x, y, width, 26f),
                $"Убито: {RunSession.Kills}   Добыча: {RunSession.RunGold} золота, " +
                $"{RunSession.RunExperience} опыта", _hintStyle);

            if (GUI.Button(new Rect(x, y + 46f, width, 58f), "ПРОДОЛЖИТЬ", _buttonStyle))
                Close();

            if (GUI.Button(new Rect(x, y + 116f, width, 58f), "НАСТРОЙКИ", _buttonStyle))
                _settings = true;

            if (GUI.Button(new Rect(x, y + 186f, width, 58f), "В ХАБ", _buttonStyle))
                LeaveToHub();

            GUI.Label(new Rect(x, y + 254f, width, 40f),
                $"Выход посреди забега сохранит " +
                $"{Mathf.RoundToInt(Balance.DeathPenaltyShare * 100f)}% добычи — как при смерти.",
                _hintStyle);
        }

        private void DrawSettings(Rect panel)
        {
            var x = panel.x + 40f;
            var width = panel.width - 80f;
            var y = panel.y + 92f;

            var sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 2.2f);

            GUI.Label(new Rect(x, y, width - 220f, 26f),
                $"Чувствительность мыши: {sensitivity:F1}", _textStyle);

            var updated = GUI.HorizontalSlider(new Rect(x + width - 200f, y + 6f, 200f, 20f),
                sensitivity, 0.4f, 6f);

            if (!Mathf.Approximately(updated, sensitivity))
            {
                PlayerPrefs.SetFloat(SensitivityKey, updated);
                PlayerPrefs.Save();
            }

            var volume = InvaderOfWorlds.AudioSettings.Volume;

            GUI.Label(new Rect(x, y + 34f, width - 220f, 26f),
                $"Громкость: {Mathf.RoundToInt(volume * 100f)}%", _textStyle);

            var newVolume = GUI.HorizontalSlider(new Rect(x + width - 200f, y + 40f, 200f, 20f),
                volume, 0f, 1f);

            if (!Mathf.Approximately(newVolume, volume))
                InvaderOfWorlds.AudioSettings.Volume = newVolume;

            var invert = PlayerPrefs.GetInt(InvertKey, 0) == 1;

            if (GUI.Toggle(new Rect(x, y + 76f, width, 28f), invert, " Инверсия мыши",
                    GUI.skin.toggle) != invert)
            {
                PlayerPrefs.SetInt(InvertKey, invert ? 0 : 1);
                PlayerPrefs.Save();
            }

            GUI.Label(new Rect(x, y + 116f, width, 26f), "Качество графики", _textStyle);

            if (GUI.Button(new Rect(x, y + 144f, 44f, 36f), "−", _buttonStyle))
                QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.GetQualityLevel() - 1), true);

            if (GUI.Button(new Rect(x + 52f, y + 144f, 44f, 36f), "+", _buttonStyle))
                QualitySettings.SetQualityLevel(
                    Mathf.Min(QualitySettings.names.Length - 1, QualitySettings.GetQualityLevel() + 1), true);

            GUI.Label(new Rect(x + 108f, y + 150f, 220f, 26f),
                QualitySettings.names[QualitySettings.GetQualityLevel()], _textStyle);

            DrawResetBlock(new Rect(x, y + 200f, width, 120f));

            if (GUI.Button(new Rect(x, y + 330f, width, 50f), "◀ НАЗАД", _buttonStyle))
                _settings = false;
        }

        /// <summary>Сброс сохранения прямо из паузы, с подтверждением.</summary>
        private void DrawResetBlock(Rect area)
        {
            if (!_confirmReset)
            {
                if (GUI.Button(new Rect(area.x, area.y, area.width, 42f), "СБРОСИТЬ ПРОГРЕСС",
                        _buttonStyle))
                    _confirmReset = true;

                return;
            }

            GUI.Label(new Rect(area.x, area.y, area.width, 22f),
                "Удалятся золото, улучшения и купленное оружие.", _hintStyle);

            if (GUI.Button(new Rect(area.x, area.y + 26f, (area.width - 10f) * 0.5f, 42f),
                    "ДА, СБРОСИТЬ", _buttonStyle))
            {
                SaveSystem.ResetProgress();
                _confirmReset = false;
            }

            if (GUI.Button(new Rect(area.x + (area.width + 10f) * 0.5f, area.y + 26f,
                    (area.width - 10f) * 0.5f, 42f), "ОТМЕНА", _buttonStyle))
                _confirmReset = false;
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 34,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.98f, 0.87f, 0.55f) }
                };
            }

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    normal = { textColor = Color.white }
                };
            }

            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.72f, 0.76f, 0.85f) }
                };
            }

            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
        }
    }
}
