namespace InvaderOfWorlds
{
    /// <summary>
    /// Состояние текущего забега: золото, опыт, счётчики.
    /// Статика живёт дольше сцены, поэтому накопления переживают перезапуск
    /// уровня — это и есть заготовка мета-прогресса.
    /// </summary>
    public static class RunSession
    {
        public static int Gold { get; private set; }
        public static int Experience { get; private set; }
        public static int Kills { get; private set; }

        public static void RegisterKill(int gold, int experience)
        {
            Gold += gold;
            Experience += experience;
            Kills++;
        }

        /// <summary>Списать золото, если его хватает. Возвращает false, если не хватило.</summary>
        public static bool TrySpend(int gold)
        {
            if (gold <= 0)
                return true;

            if (Gold < gold)
                return false;

            Gold -= gold;
            return true;
        }

        /// <summary>Счётчик убийств обнуляется на каждый забег, накопления — нет.</summary>
        public static void ResetKills()
        {
            Kills = 0;
        }

        /// <summary>Полный сброс — понадобится при выходе в хаб.</summary>
        public static void Reset()
        {
            Gold = 0;
            Experience = 0;
            Kills = 0;
        }
    }
}
