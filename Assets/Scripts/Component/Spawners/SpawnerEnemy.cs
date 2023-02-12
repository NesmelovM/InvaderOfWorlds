using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Components.Spawners
{
    public class SpawnerEnemy : MonoBehaviour
    {
        private GameFactory _gameFactory;



        public void Init(GameFactory gameFactory)
        {
            _gameFactory = gameFactory;

        }

        public GameObject[] Spawn(int numbersOfEnemiesToSpawn)
        {
            var enemies = new List<GameObject>();

            for (var i = 0; i < numbersOfEnemiesToSpawn; i++)
            {
                //var randomEnemyType = enemiesPool[Random.Range(0, enemiesPool.Length)];
                var enemy = _gameFactory.CreateEnemy(transform.position);

                enemies.Add(enemy);
            }

            return enemies.ToArray();
        }
    }
}