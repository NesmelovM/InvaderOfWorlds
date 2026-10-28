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

        /// <summary>Полный сброс — понадобится при выходе в хаб.</summary>
        public static void Reset()
        {
            Gold = 0;
            Experience = 0;
            Kills = 0;
        }
    }
}
