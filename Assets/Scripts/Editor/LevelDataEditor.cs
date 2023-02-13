using Components.Markers;
using Data;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LevelData levelData = (LevelData)target;

            if (GUILayout.Button("Collect"))
            {
                levelData.LevelKey = SceneManager.GetActiveScene().name;

                levelData.StartingPlayerPosition = FindObjectOfType<PlayerSpawnMarker>().transform.position;

                //var levelDataGeneratorSpawners = FindObjectsOfType<GeneratorSpawnMarker>()
                //    .Select(x => new GeneratorSpawnerData(x.GeneratorType, x.transform.position, x.UnderTrolley))
                //    .ToList();
                //levelData.GeneratorSpawners = levelDataGeneratorSpawners;

                //levelData.GeneratorTypes = levelDataGeneratorSpawners.Select(x => x.GeneratorType).Distinct().ToList();

                levelData.EnemySpawners = FindObjectsOfType<EnemySpawnMarker>()
                    .Select(x => new EnemySpawnerData(x.transform.position))
                    .ToList();

                //levelData.LevelTransfers = FindObjectsOfType<LevelTransferMarker>()
                //    .Select(x => new LevelTransferData(x.TransferSceneName, x.Size, x.transform.position))
                //    .ToList();

                //levelData.BossPosition = GameObject.FindWithTag("BossSpawnPoint").transform.position;
                //levelData.BossWaypoints = GameObject.FindGameObjectsWithTag("BossWaypoint")
                //    .Select(x => x.transform.position)
                //    .ToList();
            }

            EditorUtility.SetDirty(target);
        }
    }
}