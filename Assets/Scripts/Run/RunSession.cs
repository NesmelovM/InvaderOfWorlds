using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Состояние забега и накопления.
    ///
    /// Важное различие: <see cref="RunGold"/> и <see cref="RunExperience"/> — добыча
    /// текущего забега, она ещё не зачислена. <see cref="Gold"/> и
    /// <see cref="Experience"/> — то, что уже лежит в кошельке.
    /// Так можно списать долю при смерти и начислить всё при успешном уходе.
    /// </summary>
    public static class RunSession
    {
        /// <summary>Всего золота в кошельке.</summary>
        public static int Gold { get; private set; }

        /// <summary>Всего опыта.</summary>
        public static int Experience { get; private set; }

        /// <summary>Золото, добытое в текущем забеге и ещё не зачисленное.</summary>
        public static int RunGold { get; private set; }

        /// <summary>Опыт текущего забега.</summary>
        public static int RunExperience { get; private set; }

        public static int Kills { get; private set; }

        public static void RegisterKill(int gold, int experience)
        {
            RunGold += gold;
            RunExperience += experience;
            Kills++;
        }

        /// <summary>
        /// Перевести добычу забега в кошелёк. <paramref name="share"/> — какая доля
        /// доходит: 1 при успешном уходе, 0.7 при смерти.
        /// </summary>
        public static void CommitRun(float share)
        {
            // Доля может быть больше единицы: успешный уход даёт бонус.
            share = Mathf.Max(0f, share);

            Gold += Mathf.RoundToInt(RunGold * share);
            Experience += Mathf.RoundToInt(RunExperience * share);

            RunGold = 0;
            RunExperience = 0;
        }

        /// <summary>Обнулить добычу забега перед новым заходом.</summary>
        public static void ResetRun()
        {
            RunGold = 0;
            RunExperience = 0;
            Kills = 0;
        }

        /// <summary>Начислить золото вне боя — например стартовый капитал для прототипа.</summary>
        public static void Grant(int gold)
        {
            if (gold > 0)
                Gold += gold;
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

        /// <summary>Восстановление из сохранения.</summary>
        public static void Load(int gold, int experience)
        {
            Gold = Mathf.Max(0, gold);
            Experience = Mathf.Max(0, experience);
            ResetRun();
        }

        /// <summary>Полный сброс.</summary>
        public static void Reset()
        {
            Gold = 0;
            Experience = 0;
            ResetRun();
        }
    }
}
