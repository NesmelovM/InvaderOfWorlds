using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Выбор оружия перед забегом: из купленного в оружейной показываем три варианта.
    ///
    /// Появляется только на первой планете и только если есть из чего выбирать.
    /// Игра на это время стоит, камера не слушает мышь.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunStartWeaponChoice : MonoBehaviour
    {
        private const int OptionCount = 3;

        private readonly List<int> _options = new List<int>(OptionCount);
        private GameObject _player;
        private bool _open;
        private GUIStyle _titleStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _buttonStyle;

        /// <summary>Показывает выбор, если есть хотя бы два купленных оружия.</summary>
        public static void Show(GameObject player)
        {
            if (player == null)
                return;

            var choice = new GameObject("Start Weapon Choice").AddComponent<RunStartWeaponChoice>();
            choice._player = player;

            if (!choice.Build())
                Destroy(choice.gameObject);
        }

        private bool Build()
        {
            var pool = new List<int>();

            for (var id = 0; id < Armory.Catalog.Length; id++)
            {
                // Предлагаем всё купленное, а не только выбранное в хабе:
                // так игрок может попробовать оружие, которое ещё не носил.
                if (Armory.IsOwned(id))
                    pool.Add(id);
            }

            if (pool.Count < 2)
                return false;

            // Три разных варианта из купленного.
            while (_options.Count < OptionCount && pool.Count > 0)
            {
                var index = Random.Range(0, pool.Count);
                _options.Add(pool[index]);
                pool.RemoveAt(index);
            }

            _open = true;
            Time.timeScale = 0f;
            UiState.SetStart(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SoundBank.LevelUp();
            return true;
        }

        private void Choose(int id)
        {
            var attack = _player.GetComponent<WeaponSystem>();

            if (attack != null)
            {
                attack.ClearSlots();
                attack.TryEquip(Armory.ToSlot(id));

                if (RunState.WeaponDamageMultiplier > 1f)
                    attack.MultiplyDamage(RunState.WeaponDamageMultiplier);
            }

            RunState.Loadout.Clear();
            RunState.Loadout.Add(id);

            _open = false;
            UiState.SetStart(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (!_open)
                return;

            EnsureStyles();
            UiCanvas.Begin();

            HealthBarDrawer.DrawRect(new Rect(0f, 0f, UiCanvas.Width, UiCanvas.Height),
                new Color(0.02f, 0.03f, 0.06f, 0.88f));

            GUI.Label(new Rect(0f, 90f, UiCanvas.Width, 46f), "С ЧЕМ ИДЁМ?", _titleStyle);
            GUI.Label(new Rect(0f, 136f, UiCanvas.Width, 28f),
                "Возьмите одно оружие — остальное найдёте в забеге", _textStyle);

            const float cardWidth = 340f;
            const float cardHeight = 300f;
            const float gap = 30f;
            const float top = 210f;

            var totalWidth = cardWidth * _options.Count + gap * (_options.Count - 1);
            var startX = (UiCanvas.Width - totalWidth) * 0.5f;

            for (var i = 0; i < _options.Count; i++)
            {
                var weapon = Armory.Catalog[_options[i]];
                var rect = new Rect(startX + i * (cardWidth + gap), top, cardWidth, cardHeight);

                HealthBarDrawer.DrawRect(rect, new Color(0.11f, 0.13f, 0.19f, 0.98f));
                HealthBarDrawer.DrawRect(new Rect(rect.x, rect.y, rect.width, 5f),
                    new Color(0.95f, 0.80f, 0.45f, 0.9f));

                GUI.Label(new Rect(rect.x + 18f, rect.y + 26f, rect.width - 36f, 70f), weapon.Name,
                    _cardTitleStyle);
                GUI.Label(new Rect(rect.x + 18f, rect.y + 108f, rect.width - 36f, 120f),
                    weapon.Description, _textStyle);

                if (GUI.Button(new Rect(rect.x + 18f, rect.y + cardHeight - 62f, rect.width - 36f, 44f),
                        "ВЗЯТЬ", _buttonStyle))
                    Choose(_options[i]);
            }

            UiCanvas.End();
        }

        private void EnsureStyles()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            _cardTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            _textStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.88f, 0.95f) }
            };

            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 18 };
        }
    }
}
