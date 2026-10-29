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
        /// <summary>Редкость карточки: влияет на силу улучшения и цвет рамки.</summary>
        private enum CardRarity
        {
            Common,
            Rare,
            Epic
        }

        private struct Card
        {
            public string Title;
            public string Description;
            public CardRarity Rarity;
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
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _rarityStyle;

        /// <summary>Идёт выбор карточки: HUD забега в этот момент не рисуется.</summary>
        public bool IsChoosing => _choosing;

        public int Level => _level;

        /// <summary>Сколько опыта нужно до следующего уровня — для HUD.</summary>
        public int ExperienceForNext => _experienceForNext;

        /// <summary>Сколько опыта уже набрано в забеге.</summary>
        public int Experience => RunSession.RunExperience;

        public void Configure(Health playerHealth, WeaponSystem weapons, Transform player)
        {
            _playerHealth = playerHealth;
            _weapons = weapons;
            _player = player;
        }

        private void Start()
        {
            // При переходе на следующую планету уровень сохраняется.
            _level = Mathf.Max(_startLevel, RunState.Level);

            _baseExperience = Balance.LevelBaseExperience;
            _experienceGrowth = Balance.LevelGrowth;
            _experienceForNext = _baseExperience;

            for (var i = _startLevel; i < _level; i++)
                _experienceForNext = Mathf.RoundToInt(_experienceForNext * _experienceGrowth);

            _shownExperience = RunSession.RunExperience;
        }

        private void Update()
        {
            if (_choosing || UiState.PauseOpen)
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
            RunState.Level = _level;
            BuildCards();

            if (_cards.Count == 0)
                return;

            _choosing = true;
            UiState.SetCards(true);
            Time.timeScale = 0f;
            SoundBank.LevelUp();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Собирает три карточки разной редкости. Обычные попадаются часто,
        /// редкие — примерно в трети случаев, эпические — редко и заметно сильнее.
        /// </summary>
        private void BuildCards()
        {
            _cards.Clear();

            var common = new List<Card>();
            var rare = new List<Card>();
            var epic = new List<Card>();

            BuildPools(common, rare, epic);

            for (var i = 0; i < 3; i++)
            {
                var card = PickCard(common, rare, epic);

                if (card.HasValue)
                    _cards.Add(card.Value);
            }
        }

        /// <summary>
        /// Даёт случайное улучшение без экрана выбора — так работает награда из сундука.
        /// </summary>
        public void GrantRandomUpgrade()
        {
            var common = new List<Card>();
            var rare = new List<Card>();
            var epic = new List<Card>();

            BuildPools(common, rare, epic);

            var card = PickCard(common, rare, epic);

            if (card.HasValue)
                card.Value.Apply?.Invoke();
        }

        /// <summary>Наполняет наборы карточек для текущего уровня.</summary>
        private void BuildPools(List<Card> common, List<Card> rare, List<Card> epic)
        {
            void Add(List<Card> list, CardRarity rarity, string title, string description, Action apply)
            {
                list.Add(new Card
                {
                    Title = title,
                    Description = description,
                    Rarity = rarity,
                    Apply = apply
                });
            }

            // --- обычные ---
            Add(common, CardRarity.Common, "Заточка", "+15% урона всем оружиям",
                () => BoostDamage(1.15f));
            Add(common, CardRarity.Common, "Стойкость", "+20 к здоровью и полное лечение",
                () => BoostHealth(20f));
            Add(common, CardRarity.Common, "Лёгкость", "+8% скорости передвижения",
                () => BoostSpeed(1.08f));
            Add(common, CardRarity.Common, "Тяжёлый воздух", "враги двигаются на 10% медленнее",
                () => RunModifiers.SlowEnemies(0.1f));
            Add(common, CardRarity.Common, "Вампиризм", "+2 здоровья за каждое убийство",
                () => RunModifiers.AddHealPerKill(2f));

            // --- редкие ---
            Add(rare, CardRarity.Rare, "Острая сталь", "+30% урона всем оружиям",
                () => BoostDamage(1.30f));
            Add(rare, CardRarity.Rare, "Крепкая шкура", "+40 к здоровью и полное лечение",
                () => BoostHealth(40f));
            Add(rare, CardRarity.Rare, "Проворство", "+15% скорости передвижения",
                () => BoostSpeed(1.15f));
            Add(rare, CardRarity.Rare, "Голод", "+5 здоровья за каждое убийство",
                () => RunModifiers.AddHealPerKill(5f));

            var weaponId = FindWeaponOffer();

            if (weaponId >= 0)
            {
                var weapon = Armory.Catalog[weaponId];
                Add(rare, CardRarity.Rare, $"Оружие: {weapon.Name}", weapon.Description,
                    () => EquipWeapon(weaponId));
            }

            // --- эпические ---
            Add(epic, CardRarity.Epic, "Кровавый клинок", "+55% урона всем оружиям",
                () => BoostDamage(1.55f));
            Add(epic, CardRarity.Epic, "Каменная плоть", "+70 к здоровью и полное лечение",
                () => BoostHealth(70f));
            Add(epic, CardRarity.Epic, "Порыв ветра", "+25% скорости передвижения",
                () => BoostSpeed(1.25f));
            Add(epic, CardRarity.Epic, "Пиршество", "+10 здоровья за каждое убийство",
                () => RunModifiers.AddHealPerKill(10f));
            Add(epic, CardRarity.Epic, "Мёртвый воздух", "враги двигаются на 25% медленнее",
                () => RunModifiers.SlowEnemies(0.25f));

        }

        /// <summary>Бросает редкость и забирает случайную карточку из её набора.</summary>
        private static Card? PickCard(List<Card> common, List<Card> rare, List<Card> epic)
        {
            var roll = UnityEngine.Random.value;
            var list = roll < 0.60f ? common : roll < 0.90f ? rare : epic;

            if (list.Count == 0)
                list = common.Count > 0 ? common : rare.Count > 0 ? rare : epic;

            if (list.Count == 0)
                return null;

            var index = UnityEngine.Random.Range(0, list.Count);
            var card = list[index];
            list.RemoveAt(index);
            return card;
        }

        private void BoostDamage(float multiplier)
        {
            RunModifiers.MultiplyWeaponDamage(multiplier);
            _weapons?.MultiplyDamage(multiplier);
        }

        private void BoostHealth(float amount)
        {
            if (_playerHealth == null)
                return;

            _playerHealth.IncreaseMax(amount);
            RunState.HealthBonus += amount;
            _playerHealth.Heal(_playerHealth.Max);
        }

        private void BoostSpeed(float multiplier)
        {
            var motor = _player != null ? _player.GetComponent<PlayerMotor>() : null;

            if (motor != null)
                motor.ApplySpeedMultiplier(multiplier);

            RunState.SpeedMultiplier *= multiplier;
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

            if (!RunState.Loadout.Contains(id))
                RunState.Loadout.Add(id);

            SaveSystem.Save();
        }

        private void ShuffleUnused()
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
            UiState.SetCards(false);

            // Пауза могла быть открыта раньше — тогда время не возвращаем.
            if (!UiState.PauseOpen)
                Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        }

        private void OnGUI()
        {
            if (!_choosing)
                return;

            // Самовосстановление: если по какой-то причине выбор активен, а карточек
            // нет, экран залип бы с пустой паузой. Пересобираем или отпускаем игру.
            if (_cards.Count == 0)
            {
                BuildCards();

                if (_cards.Count == 0)
                {
                    _choosing = false;
                    UiState.SetCards(false);
                    Time.timeScale = 1f;
                    return;
                }
            }

            EnsureStyles();
            UiCanvas.Begin();

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, UiCanvas.Width, UiCanvas.Height),
                new Color(0.02f, 0.03f, 0.06f, 0.86f));

            GUI.Label(new Rect(0f, 76f, UiCanvas.Width, 46f),
                $"УРОВЕНЬ {_level} — выберите улучшение", _titleStyle);

            GUI.Label(new Rect(0f, 122f, UiCanvas.Width, 28f),
                "Забег продолжается после выбора", _hintStyle);

            // Карточки в виртуальном холсте: 3 × 360 плюс отступы ровно по центру.
            const float cardWidth = 360f;
            const float cardHeight = 330f;
            const float gap = 30f;
            const float top = 190f;

            var totalWidth = cardWidth * _cards.Count + gap * (_cards.Count - 1);
            var startX = (UiCanvas.Width - totalWidth) * 0.5f;

            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                var accent = RarityColor(card.Rarity);
                var rect = new Rect(startX + i * (cardWidth + gap), top, cardWidth, cardHeight);

                HealthBarDrawer.DrawRect(rect, new Color(0.11f, 0.13f, 0.19f, 0.98f));
                HealthBarDrawer.DrawRect(new Rect(rect.x, rect.y, rect.width, 5f), accent);

                // Рамка цвета редкости — видно, что карточка особенная, ещё до чтения.
                if (card.Rarity != CardRarity.Common)
                {
                    HealthBarDrawer.DrawRect(new Rect(rect.x, rect.y + cardHeight - 5f, rect.width, 5f), accent);
                    HealthBarDrawer.DrawRect(new Rect(rect.x, rect.y, 4f, cardHeight), accent);
                    HealthBarDrawer.DrawRect(new Rect(rect.x + rect.width - 4f, rect.y, 4f, cardHeight), accent);
                }

                _rarityStyle.normal.textColor = accent;
                GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, rect.width - 40f, 22f), RarityName(card.Rarity),
                    _rarityStyle);
                GUI.Label(new Rect(rect.x + 20f, rect.y + 38f, rect.width - 40f, 62f), card.Title, _titleStyle);
                GUI.Label(new Rect(rect.x + 20f, rect.y + 106f, rect.width - 40f, 150f),
                    card.Description, _textStyle);

                if (GUI.Button(new Rect(rect.x + 20f, rect.y + cardHeight - 66f, rect.width - 40f, 46f),
                        "ВЗЯТЬ", _buttonStyle))
                {
                    Choose(i);
                }
            }

            UiCanvas.End();
        }

        /// <summary>Цвет редкости: серый, синий, фиолетовый.</summary>
        private static Color RarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Rare => new Color(0.35f, 0.65f, 0.95f, 0.95f),
                CardRarity.Epic => new Color(0.72f, 0.45f, 0.95f, 0.95f),
                _ => new Color(0.62f, 0.68f, 0.78f, 0.9f)
            };
        }

        private static string RarityName(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Rare => "РЕДКОЕ",
                CardRarity.Epic => "ЭПИЧЕСКОЕ",
                _ => "ОБЫЧНОЕ"
            };
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
                };
            }

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.85f, 0.88f, 0.95f) }
                };
            }

            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.65f, 0.70f, 0.80f) }
                };
            }

            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };

            if (_rarityStyle == null)
            {
                _rarityStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.70f, 0.75f, 0.85f) }
                };
            }
        }
    }
}
