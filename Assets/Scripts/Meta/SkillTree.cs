using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Дерево прокачки: узлы покупаются за опыт, часть требует предыдущих.
    /// В отличие от магазина хаба (золото, простые линии), здесь есть зависимости —
    /// так игрок выбирает путь, а не просто покупает всё подряд.
    /// </summary>
    public static class SkillTree
    {
        public struct Node
        {
            public string Title;
            public string Effect;
            public int Cost;
            public int[] Requires;
        }

        public static readonly Node[] Nodes =
        {
            new Node { Title = "Сила I", Effect = "+10% урона", Cost = 60, Requires = new int[0] },
            new Node { Title = "Сила II", Effect = "+15% урона", Cost = 140, Requires = new[] { 0 } },
            new Node { Title = "Живучесть I", Effect = "+25 HP", Cost = 50, Requires = new int[0] },
            new Node { Title = "Живучесть II", Effect = "+25 HP", Cost = 120, Requires = new[] { 2 } },
            new Node { Title = "Ловкость", Effect = "+8% скорости", Cost = 70, Requires = new int[0] },
            new Node { Title = "Третий слот", Effect = "взрыв вокруг героя", Cost = 200, Requires = new[] { 0, 2 } }
        };

        public static int Mask { get; private set; }

        public static bool IsUnlocked(int id)
        {
            return id >= 0 && id < Nodes.Length && (Mask & (1 << id)) != 0;
        }

        public static bool CanUnlock(int id)
        {
            if (id < 0 || id >= Nodes.Length || IsUnlocked(id))
                return false;

            foreach (var required in Nodes[id].Requires)
            {
                if (!IsUnlocked(required))
                    return false;
            }

            return true;
        }

        public static bool TryUnlock(int id)
        {
            if (!CanUnlock(id) || !RunSession.TrySpendExperience(Nodes[id].Cost))
                return false;

            Mask |= 1 << id;
            return true;
        }

        public static float DamageBonus => (IsUnlocked(0) ? 0.10f : 0f) + (IsUnlocked(1) ? 0.15f : 0f);

        public static float HealthBonus => (IsUnlocked(2) ? 25f : 0f) + (IsUnlocked(3) ? 25f : 0f);

        public static float SpeedBonus => IsUnlocked(4) ? 0.08f : 0f;

        public static bool HasNova => IsUnlocked(5);

        public static void Load(int mask)
        {
            Mask = mask;
        }

        public static void Reset()
        {
            Mask = 0;
        }
    }
}
