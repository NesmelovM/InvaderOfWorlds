
using Components.Spawners;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameFactory
{
    private string PlayerPrefabPath = "Assets/Prefabs/Characters/Player/Player.prefab";
    private string WeaponPrefabPath;
    private string EnemySpawnerPrefabPath;

    private List<string> _enemyMeleePrefabsPaths = new List<string>()
        {
            "Assets/Prefabs/Characters/Enemy/EnemyMelee_fighter.prefab",
            "Assets/Prefabs/Characters/Enemy/EnemyMelee_onehanded.prefab",
            "Assets/Prefabs/Characters/Enemy/EnemyMelee_twohanded.prefab"
        };

    private List<string> _enemyRangePrefabsPaths = new List<string>()
        {
            "Assets/Prefabs/Characters/Enemy/EnemyRange_bow.prefab",
            "Assets/Prefabs/Characters/Enemy/EnemyRange_mage.prefab",
            "Assets/Prefabs/Characters/Enemy/EnemyRange_throw.prefab",
        };

    private string EnemyRangePrefabPath;

    private string LevelTransferPrefabPath;
    private string PlayerHUDPath;
    private string DialogueWindowPath;
    private string PlayerDiedWindowPath;
    private string EscapeWindowPath;
    private string FinalLevelWindowPath;

    //private readonly InputService _inputService;
    //private readonly StaticDataService _staticDataService;
    //private readonly GameStateMachine _gameStateMachine;
    //private readonly DialogueService _dialogueService;

    private GameObject _player;
    private GameObject _hud;
    private GameObject _boss;


    //public GameFactory(InputService inputService, StaticDataService staticDataService,
    //    GameStateMachine gameStateMachine)
    //{
    //    _inputService = inputService;
    //    _staticDataService = staticDataService;
    //    _gameStateMachine = gameStateMachine;
    //}

    public GameObject CreatePlayer(Vector3 spawnPosition)
    {
        GameObject prefab = Resources.Load<GameObject>(PlayerPrefabPath);
        GameObject player = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        _player = player;

        //player.GetComponent<PlayerMove>().Init(_inputService);
        //player.GetComponent<PlayerAnimation>().Init(_inputService);
        //player.GetComponent<PlayerShoot>().Init(_inputService, this);
        //player.GetComponent<PlayerGeneratorConnection>().Init(_inputService);
        //player.GetComponent<PlayerDash>().Init(_inputService);

        return player;
    }

    public SpawnerEnemy CreateEnemySpawner(Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>(EnemySpawnerPrefabPath);
        GameObject enemySpawnerGameObject = Object.Instantiate(prefab, position, Quaternion.identity);

        SpawnerEnemy enemySpawner = enemySpawnerGameObject.GetComponent<SpawnerEnemy>();
        enemySpawner.Init(this);

        return enemySpawner;
    }

    public GameObject CreateEnemy(Vector3 spawnPosition)
    {
        GameObject enemyGameObject;

        var randomPath = _enemyMeleePrefabsPaths[Random.Range(0, _enemyMeleePrefabsPaths.Count)];
        var prefabMelee = Resources.Load<GameObject>(randomPath);
        enemyGameObject = Object.Instantiate(prefabMelee, spawnPosition, Quaternion.identity);


       // enemyGameObject.GetComponent<AgentFollowPlayer>().Init(_player.transform);
        //enemyGameObject.GetComponent<EnemyVulnerableElement>().Init(resistDamageType, _staticDataService);

        return enemyGameObject;
    }

    public GameObject CreatePlayerDiedUI()
    {
        GameObject prefab = Resources.Load<GameObject>(PlayerDiedWindowPath);
        GameObject playerDiedWindow = Object.Instantiate(prefab, _hud.transform);

        return playerDiedWindow;
    }

    //public Projectile CreateProjectile(GeneratorType generatorType, Transform nozzleAnchor)
    //{
    //    var projectileData = _staticDataService.GetDataForProjectile(generatorType);

    //    Projectile prefab = projectileData.ProjectilePrefab;
    //    Projectile projectile = Object.Instantiate(prefab, nozzleAnchor.position, nozzleAnchor.rotation);

    //    projectile.GetComponent<Projectile>().Init(projectileData.Speed, projectileData.Damage, generatorType);

    //    return projectile;
    //}
    //public void CreateLevelTransfer(LevelTransferData levelTransferData)
    //{
    //    GameObject prefab = Resources.Load<GameObject>(LevelTransferPrefabPath);
    //    GameObject enemyGameObject = Object.Instantiate(prefab, levelTransferData.Position, Quaternion.identity);

    //    enemyGameObject.GetComponent<LevelTransfer>().Init(_gameStateMachine, levelTransferData.Size,
    //        levelTransferData.TransferSceneName);
    //}

    //public GameObject CreatePlayerHUD()
    //{
    //    GameObject prefab = Resources.Load<GameObject>(PlayerHUDPath);
    //    GameObject hud = Object.Instantiate(prefab);

    //    hud.GetComponent<PlayerHealthBarUI>().Init(_player);
    //    hud.GetComponent<PlayerDashUI>().Init(_player);
    //    _hud = hud;

    //    return hud;
    //}

    //public GameObject CreateDialogueWindow(string dialogueName, bool onEnter, bool onMobsKilled, bool onBossKilled,
    //    string nextScene)
    //{
    //    var dialogueData = _staticDataService.GetDataForDialogue(dialogueName);

    //    GameObject prefab = Resources.Load<GameObject>(DialogueWindowPath);
    //    GameObject DialogueWindow = Object.Instantiate(prefab, _hud.transform);

    //    DialogueWindow.GetComponent<DialogueWindow>().Init(dialogueData, onEnter, onMobsKilled, onBossKilled, nextScene);

    //    return DialogueWindow;
    //}
}
