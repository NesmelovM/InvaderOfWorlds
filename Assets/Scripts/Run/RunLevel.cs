using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Повышение уровня внутри забега: за опыт с врагов игрок выбирает одну из трёх
    /// карточек — улучшение оружия, характеристик персонажа или окружения.
    ///
    /// Именно здесь собирается билд: на старте есть одно оружие, остальное
    /// приходит карточками, поэтому каждый забег складывается по-новому.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunLevel : MonoBehaviour
    {
        private struct Card
        {
            public string Title;
            public string Description;
            public Action Apply;
        }

        [SerializeField, Min(1)] private int _startLevel = 1;
        [SerializeField, Min(0)] private int _baseExperience = 60;
        [SerializeField, Min(1f)] private float _experienceGrowth = 1.45f;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private WeaponSystem _weapons;
        [SerializeField] private Transform _player;

        private readonly List<Card> _cards = new List<Card>(3);
        private int _level;
        private int _experienceForNext;
        private int _shownExperience;
        private bool _choosing;
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _buttonStyle;

        public int Level => _level;

        public void Configure(Health playerHealth, WeaponSystem weapons, Transform player)
        {
            _playerHealth = playerHealth;
            _weapons = weapons;
            _player = player;
        }

        private void Start()
        {
            _level = _startLevel;
            _experienceForNext = _baseExperience;
            _shownExperience = RunSession.RunExperience;
        }

        private void Update()
        {
            if (_choosing)
                return;

            var experience = RunSession.RunExperience;

            if (experience < _shownExperience)
                _shownExperience = experience;

            if (experience < _experienceForNext)
                return;

            _shownExperience = experience;
            _experienceForNext = Mathf.RoundToInt(_experienceForNext * _experienceGrowth);
            BeginChoice();
        }

        private void BeginChoice()
        {
            _level++;
            BuildCards();

            if (_cards.Count == 0)
                return;

            _choosing = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Собирает три разные карточки: оружие, персонаж, окружение.</summary>
        private void BuildCards()
        {
            _cards.Clear();

            var weaponId = FindWeaponOffer();

            if (weaponId >= 0)
            {
                var weapon = Armory.Catalog[weaponId];
                _cards.Add(new Card
                {
                    Title = $"Оружие: {weapon.Name}",
                    Description = weapon.Description,
                    Apply = () => EquipWeapon(weaponId)
                });
            }

            _cards.Add(new Card
            {
                Title = "Заточка",
                Description = "+15% урона всем оружиям",
                Apply = () =>
                {
                    RunModifiers.MultiplyWeaponDamage(1.15f);
                    _weapons?.MultiplyDamage(1.15f);
                }
            });

            _cards.Add(new Card
            {
                Title = "Стойкость",
                Description = "+20 к здоровью и полное лечение",
                Apply = () =>
                {
                    if (_playerHealth == null)
                        return;

                    _playerHealth.IncreaseMax(20f);
                    _playerHealth.Heal(_playerHealth.Max);
                }
            });

            _cards.Add(new Card
            {
                Title = "Лёгкость",
                Description = "+8% скорости передвижения",
                Apply = () =>
                {
                    var motor = _player != null ? _player.GetComponent<PlayerMotor>() : null;

                    if (motor != null)
                        motor.ApplySpeedMultiplier(1.08f);
                }
            });

            _cards.Add(new Card
            {
                Title = "Тяжёлый воздух",
                Description = "враги двигаются на 10% медленнее",
                Apply = () => RunModifiers.SlowEnemies(0.1f)
            });

            _cards.Add(new Card
            {
                Title = "Вампиризм",
                Description = "+2 здоровья за каждое убийство",
                Apply = () => RunModifiers.AddHealPerKill(2f)
            });

            Shuffle();
        }

        /// <summary>Ищет купленное, но не взятое в набор оружие.</summary>
        private static int FindWeaponOffer()
        {
            for (var i = 0; i < Armory.Catalog.Length; i++)
            {
                if (Armory.IsOwned(i) && !Armory.IsEquipped(i))
                    return i;
            }

            return -1;
        }

        private void EquipWeapon(int id)
        {
            if (_weapons == null || !_weapons.TryEquip(Armory.ToSlot(id)))
                return;

            Armory.TryEquip(id);
            SaveSystem.Save();
        }

        private void Shuffle()
        {
            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }

            if (_cards.Count > 3)
                _cards.RemoveRange(3, _cards.Count - 3);
        }

        private void Choose(int index)
        {
            if (index < 0 || index >= _cards.Count)
                return;

            _cards[index].Apply();

            _choosing = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log($"[LEVEL] Уровень {_level}, выбрано: {_cards[index].Title}");
        }

        private void OnGUI()
        {
            if (!_choosing)
                return;

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.02f, 0.03f, 0.06f, 0.82f));

            EnsureStyles();

            GUI.Label(new Rect(0f, Screen.height * 0.16f, Screen.width, 40f),
                $"УРОВЕНЬ {_level} — выберите улучшение", _titleStyle);

            var cardWidth = Mathf.Min(360f, (Screen.width - 120f) / 3f);
            var totalWidth = cardWidth * _cards.Count + 24f * (_cards.Count - 1);
            var startX = (Screen.width - totalWidth) * 0.5f;
            var top = Screen.height * 0.28f;
            var cardHeight = 200f;

            for (var i = 0; i < _cards.Count; i++)
            {
                var rect = new Rect(startX + i * (cardWidth + 24f), top, cardWidth, cardHeight);
                HealthBarDrawer.DrawRect(rect, new Color(0.12f, 0.14f, 0.2f, 0.95f));

                GUI.Label(new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, 30f), _cards[i].Title, _titleStyle);
                GUI.Label(new Rect(rect.x + 16f, rect.y + 52f, rect.width - 32f, 90f), _cards[i].Description, _textStyle);

                if (GUI.Button(new Rect(rect.x + 16f, rect.y + cardHeight - 56f, rect.width - 32f, 40f),
                        "ВЗЯТЬ", _buttonStyle))
                {
                    Choose(i);
                }
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.88f, 0.95f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        }
    }
}
