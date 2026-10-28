using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Порядок биомов. Первый — самый бедный, дальше открываются по мере прохождения
    /// порталов: пять планет на биом.
    /// </summary>
    [CreateAssetMenu(fileName = "Biome Catalog", menuName = "Invader of Worlds/Каталог биомов")]
    public class BiomeCatalog : ScriptableObject
    {
        [Tooltip("Порядок важен: индекс биома = число пройденных порталов / 5")]
        public List<BiomeDefinition> Biomes = new List<BiomeDefinition>();

        [Tooltip("Сколько планет нужно пройти, чтобы открылся следующий биом")]
        [Min(1)] public int PlanetsPerBiome = 5;

        /// <summary>Биом по индексу: выбор игрока в хабе.</summary>
        public BiomeDefinition ForIndex(int index)
        {
            if (Biomes.Count == 0)
                return null;

            return Biomes[Mathf.Clamp(index, 0, Biomes.Count - 1)];
        }

        /// <summary>Сколько биомов уже открыто при указанном числе пройденных порталов.</summary>
        public int UnlockedCount(int portalsPassed)
        {
            var count = 0;

            foreach (var biome in Biomes)
            {
                if (biome != null && portalsPassed >= biome.RequiredPortals)
                    count++;
            }

            return Mathf.Max(1, count);
        }

        /// <summary>
        /// Биом для планеты с указанным номером в забеге: берётся последний биом,
        /// порог которого уже пройден. Пустыня с порогом 0 — стартовая.
        /// </summary>
        public BiomeDefinition ForPlanet(int planetIndex)
        {
            if (Biomes.Count == 0)
                return null;

            BiomeDefinition result = null;

            foreach (var biome in Biomes)
            {
                if (biome == null || planetIndex < biome.RequiredPortals)
                    continue;

                result = biome;
            }

            return result ?? Biomes[0];
        }
    }
}
