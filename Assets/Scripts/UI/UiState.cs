using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Глобальное состояние интерфейса: открыто ли модальное окно — пауза или выбор
    /// улучшения.
    ///
    /// Пока окно открыто, камера не должна слушать мышь: иначе она поворачивается
    /// относительно персонажа, и после закрытия окна взгляд оказывается в другом месте.
    /// </summary>
    public static class UiState
    {
        /// <summary>Открыто окно, которое забирает управление.</summary>
        public static bool Modal { get; private set; }

        /// <summary>Камера и персонаж игнорируют ввод.</summary>
        public static bool BlocksInput => Modal;

        /// <summary>Сбрасываем флаг при старте: статики живут дольше одной сессии Play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Modal = false;
        }

        public static void Open()
        {
            Modal = true;
        }

        public static void Close()
        {
            Modal = false;
        }
    }
}
