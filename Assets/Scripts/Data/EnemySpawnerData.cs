using System;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class EnemySpawnerData
    {
        public Vector3 Position;

        public EnemySpawnerData(Vector3 position)
        {
            Position = position;
        }
    }
}