using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Состояние модальных окон: карточки улучшений и пауза.
    ///
    /// Окна независимы: пауза может быть открыта, карточки — нет, и наоборот.
    /// Раньше оба писали в один флаг, поэтому закрытие одного разблокировало
    /// камеру и снимало паузу у второго.
    /// </summary>
    public static class UiState
    {
        /// <summary>Открыт выбор улучшения.</summary>
        public static bool CardsOpen { get; private set; }

        /// <summary>Открыта пауза.</summary>
        public static bool PauseOpen { get; private set; }

        /// <summary>Открыт выбор оружия перед забегом.</summary>
        public static bool StartOpen { get; private set; }

        /// <summary>Любое из окон забирает управление: камера и персонаж молчат.</summary>
        public static bool BlocksInput => CardsOpen || PauseOpen || StartOpen;

        public static void SetCards(bool open)
        {
            CardsOpen = open;
        }

        public static void SetPause(bool open)
        {
            PauseOpen = open;
        }

        public static void SetStart(bool open)
        {
            StartOpen = open;
        }

        public static void ResetAll()
        {
            CardsOpen = false;
            PauseOpen = false;
            StartOpen = false;
        }

        /// <summary>Сбрасываем флаги при старте: статики живут дольше одной сессии Play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetAll();
        }
    }
}
