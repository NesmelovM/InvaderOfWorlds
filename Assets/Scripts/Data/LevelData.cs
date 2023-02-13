using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Data/Level")]
    public class LevelData : ScriptableObject
    {
        public string LevelKey;
        public Vector3 StartingPlayerPosition;
        //public List<GeneratorSpawnerData> GeneratorSpawners;
        //public List<GeneratorType> GeneratorTypes;
        public List<EnemySpawnerData> EnemySpawners;
        //public List<LevelTransferData> LevelTransfers;

        [Header("Enemies")]
        //public EnemyPoolData PoolData;
        public int StartingSpawnCooldown;
        public int SecondsBetweenSpawns;
        public int TotalNumberOfEnemies;
        public int MinNumberOfEnemiesOnSpawn;
        public int MaxNumberOfEnemiesOnSpawn;

        [Header("Boss")]
        public Vector3 BossPosition;
        public List<Vector3> BossWaypoints;
    }
}