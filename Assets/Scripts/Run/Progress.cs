using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Мета-прогресс вне забега: сколько порталов пройдено всего и какой биом выбран.
    ///
    /// Биомы открываются по числу пройденных порталов и сохраняются между запусками,
    /// поэтому выбор в хабе действует на следующий забег.
    /// </summary>
    public static class Progress
    {
        public static int TotalPortals { get; private set; }
        public static int SelectedBiome { get; private set; }

        public static void Load(int portals, int selectedBiome)
        {
            TotalPortals = Mathf.Max(0, portals);
            SelectedBiome = Mathf.Max(0, selectedBiome);
            ClampSelection();
        }

        /// <summary>Считает портал пройденным: открывает следующий биом.</summary>
        public static void AddPortal()
        {
            TotalPortals++;
        }

        public static void SelectBiome(int index, int available)
        {
            SelectedBiome = Mathf.Clamp(index, 0, Mathf.Max(0, available - 1));
        }

        public static void Reset()
        {
            TotalPortals = 0;
            SelectedBiome = 0;
        }

        private static void ClampSelection()
        {
            var catalog = Resources.Load<BiomeCatalog>("Biomes/Catalog");

            if (catalog != null && catalog.Biomes.Count > 0)
                SelectedBiome = Mathf.Clamp(SelectedBiome, 0, catalog.Biomes.Count - 1);
        }
    }
}
