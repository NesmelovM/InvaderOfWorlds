using System.Collections.Generic;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Оружейная: каталог оружия, покупка за золото и сборка своего набора в слоты.
    ///
    /// Первые два образца есть сразу — иначе новый игрок не сможет драться.
    /// Остальные покупаются и свободно ставятся в любой из шести слотов.
    /// </summary>
    public static class Armory
    {
        public struct WeaponDef
        {
            public string Name;
            public string Description;
            public WeaponKind Kind;
            public float Damage;
            public float Cooldown;
            public float Range;
            public float ProjectileSpeed;
            public int Cost;
        }

        public static readonly WeaponDef[] Catalog =
        {
            new WeaponDef
            {
                Name = "Клинок", Description = "быстрый удар вблизи", Kind = WeaponKind.Melee,
                Damage = 34f, Cooldown = 0.5f, Range = 3.2f, Cost = 0
            },
            new WeaponDef
            {
                Name = "Арбалет", Description = "снаряд по дальней цели", Kind = WeaponKind.Bolt,
                Damage = 18f, Cooldown = 1.1f, Range = 11f, ProjectileSpeed = 26f, Cost = 0
            },
            new WeaponDef
            {
                Name = "Молот", Description = "медленный, но тяжёлый удар", Kind = WeaponKind.Melee,
                Damage = 62f, Cooldown = 1.3f, Range = 3.6f, Cost = 120
            },
            new WeaponDef
            {
                Name = "Веер стрел", Description = "частые слабые снаряды", Kind = WeaponKind.Bolt,
                Damage = 12f, Cooldown = 0.45f, Range = 9f, ProjectileSpeed = 30f, Cost = 150
            },
            new WeaponDef
            {
                Name = "Импульс", Description = "взрыв вокруг героя", Kind = WeaponKind.Nova,
                Damage = 22f, Cooldown = 1.8f, Range = 4.5f, Cost = 200
            },
            new WeaponDef
            {
                Name = "Глефа", Description = "широкий замах", Kind = WeaponKind.Melee,
                Damage = 45f, Cooldown = 0.8f, Range = 4.2f, Cost = 180
            },
            new WeaponDef
            {
                Name = "Искра", Description = "мощный дальний выстрел", Kind = WeaponKind.Bolt,
                Damage = 30f, Cooldown = 1.6f, Range = 14f, ProjectileSpeed = 22f, Cost = 220
            },
            new WeaponDef
            {
                Name = "Копьё", Description = "пробивает до четырёх целей", Kind = WeaponKind.Pierce,
                Damage = 26f, Cooldown = 1.3f, Range = 13f, ProjectileSpeed = 30f, Cost = 240
            },
            new WeaponDef
            {
                Name = "Громовая цепь", Description = "молния по трём целям", Kind = WeaponKind.Chain,
                Damage = 20f, Cooldown = 1.9f, Range = 9f, Cost = 280
            },
            new WeaponDef
            {
                Name = "Орбитальный клинок", Description = "кружит вокруг героя", Kind = WeaponKind.Orbiter,
                Damage = 12f, Cooldown = 0.3f, Range = 2.2f, Cost = 260
            },
            new WeaponDef
            {
                Name = "Аура", Description = "постоянный урон в радиусе", Kind = WeaponKind.Aura,
                Damage = 10f, Cooldown = 0.5f, Range = 3.5f, Cost = 300
            },
            new WeaponDef
            {
                Name = "Бумеранг", Description = "летит и возвращается", Kind = WeaponKind.Boomerang,
                Damage = 22f, Cooldown = 1.6f, Range = 12f, ProjectileSpeed = 18f, Cost = 250
            },
            new WeaponDef
            {
                Name = "Ракета", Description = "сама наводится на цель", Kind = WeaponKind.Homing,
                Damage = 26f, Cooldown = 2.2f, Range = 16f, ProjectileSpeed = 14f, Cost = 320
            }
        };

        private static readonly List<int> EquippedWeapons = new List<int>();

        /// <summary>Битовая маска купленного оружия.</summary>
        public static int OwnedMask { get; private set; } = 0b11;

        public static IReadOnlyList<int> Equipped => EquippedWeapons;

        public static bool IsOwned(int id)
        {
            return id >= 0 && id < Catalog.Length && (OwnedMask & (1 << id)) != 0;
        }

        public static bool IsEquipped(int id)
        {
            return EquippedWeapons.Contains(id);
        }

        public static bool TryBuy(int id)
        {
            if (id < 0 || id >= Catalog.Length || IsOwned(id))
                return false;

            if (!RunSession.TrySpend(Catalog[id].Cost))
                return false;

            OwnedMask |= 1 << id;
            return true;
        }

        /// <summary>Поставить в свободный слот. Больше шести оружий не влезает.</summary>
        public static bool TryEquip(int id)
        {
            if (!IsOwned(id) || IsEquipped(id) || EquippedWeapons.Count >= WeaponSystem.MaxSlots)
                return false;

            EquippedWeapons.Add(id);
            return true;
        }

        public static bool TryUnequip(int id)
        {
            return EquippedWeapons.Remove(id);
        }

        /// <summary>Превращает запись каталога в слот оружия для боя.</summary>
        public static WeaponSlot ToSlot(int id)
        {
            var def = Catalog[id];

            return new WeaponSlot
            {
                Kind = def.Kind,
                Damage = def.Damage,
                Cooldown = def.Cooldown,
                Range = def.Range,
                ProjectileSpeed = def.ProjectileSpeed,
                Enabled = true
            };
        }

        public static void Load(int ownedMask, string equipped)
        {
            OwnedMask = ownedMask == 0 ? 0b11 : ownedMask;
            EquippedWeapons.Clear();

            if (!string.IsNullOrWhiteSpace(equipped))
            {
                foreach (var part in equipped.Split(','))
                {
                    if (int.TryParse(part.Trim(), out var id) && IsOwned(id) && EquippedWeapons.Count < WeaponSystem.MaxSlots)
                        EquippedWeapons.Add(id);
                }
            }

            if (EquippedWeapons.Count > 0)
                return;

            // По умолчанию — стартовый набор.
            EquippedWeapons.Add(0);
            EquippedWeapons.Add(1);
        }

        public static string SerializeEquipped()
        {
            return string.Join(",", EquippedWeapons);
        }

        public static void Reset()
        {
            OwnedMask = 0b11;
            EquippedWeapons.Clear();
            EquippedWeapons.Add(0);
            EquippedWeapons.Add(1);
        }
    }
}
