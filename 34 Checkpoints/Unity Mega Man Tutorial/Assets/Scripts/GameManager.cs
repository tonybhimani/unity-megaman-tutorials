using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance = null;

    // flag to trigger the next scene
    bool startNextScene;
    string nextSceneName;
    string lastSceneName;

    GameObject HUDCanvas;
    GameObject WeaponsMenu;

    GameObject player;

    CameraFollow cameraFollow;

    int enemyPrefabCount;

    bool showMessage;
    bool firstMessage;
    int messageIndex;
    string messageText;
    float messageTimer;
    float messageDelay = 2.5f;

    bool isGameOver;
    bool playerReady;
    bool initReadyScreen;

    // for pausing the game
    bool isGamePaused;
    bool pauseMusic;
    bool canPauseGame;

    bool inWeaponsMenu;
    bool inCameraTransition;

    float timeScale;

    int levelPoints;
    int playerLives;
    int playerScore;
    List<int> bonusScore = new List<int>();

    float gameRestartTime;
    float gamePlayerReadyTime;

    public float gameRestartDelay = 5f;
    public float gamePlayerReadyDelay = 3f;

    public int gamePlayerStartLives = 3;

    public AssetPalette assetPalette;

    // save and restore the player weapons between scene loads
    PlayerController.WeaponTypes playerWeaponType;
    PlayerController.WeaponsStruct[] playerWeapons;

    public enum GameScenes
    {
        TitleScreen,
        IntroScene,
        MainScene,
        StageSelect,
        GameOver,
        CutManStage,
        GutsManStage,
        IceManStage,
        BombManStage,
        FireManStage,
        ElecManStage,
        DrWilyStage1,
        DrWilyStage2,
        DrWilyStage3,
        DrWilyStage4
    };
    public GameScenes gameScene = GameScenes.TitleScreen;

    public enum StagesList
    {
        CutMan,
        GutsMan,
        IceMan,
        BombMan,
        FireMan,
        ElecMan,
        DrWily1,
        DrWily2,
        DrWily3,
        DrWily4
    };

    [System.Serializable]
    public struct StagesStruct
    {
        public GameScenes GameScene;
        public bool Completed;
    }
    public StagesStruct[] GameStages;

    public struct WorldViewCoordinates
    {
        public float Top;
        public float Right;
        public float Bottom;
        public float Left;
    }
    public WorldViewCoordinates worldViewCoords;

    TextMeshProUGUI playerScoreText;
    TextMeshProUGUI screenMessageText;

    // Initialize the singleton instance
    private void Awake()
    {
        // If there is not already an instance of GameManager, set it to this
        if (Instance == null)
        {
            Instance = this;
        }
        // If an instance already exists, destroy whatever this object is to enforce the singleton
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        // Set GameManager to DontDestroyOnLoad so that it won't be destroyed when reloading our scene
        DontDestroyOnLoad(gameObject);

        // set the asset palette up only one time
        if (assetPalette == null)
        {
            assetPalette = GetComponent<AssetPalette>();
            enemyPrefabCount = Enum.GetNames(typeof(AssetPalette.EnemyList)).Length;
        }

        // initialize the player numbers
        ResetPlayerLives();
        ResetPointsCollected();
    }

    // called first
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // called when the game is terminated
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // called second
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // get HUD Canvas and Weapons Menu
        HUDCanvas = GameObject.Find("HUDCanvas");
        WeaponsMenu = GameObject.Find("WeaponsMenu");

        if (WeaponsMenu != null)
        {
            // once the weapons menu is found, add on menu event listeners
            WeaponsMenu.GetComponent<WeaponsMenu>().ShowMenuEvent.AddListener(this.OnWeaponsMenuEnter);
            WeaponsMenu.GetComponent<WeaponsMenu>().ExitMenuEvent.AddListener(this.OnWeaponsMenuExit);

            // it's setup now hide the weapons menu on the screen
            WeaponsMenu.SetActive(false);
        }

        // init functions for each game scene
        switch (gameScene)
        {
            case GameScenes.TitleScreen:
                StartTitleScreen();
                break;
            case GameScenes.IntroScene:
                StartIntroScene();
                break;
            case GameScenes.StageSelect:
                StartStageSelectScreen();
                break;
            case GameScenes.GameOver:
                StartGameOverScreen();
                break;
            case GameScenes.MainScene:
            case GameScenes.CutManStage:
            case GameScenes.GutsManStage:
            case GameScenes.IceManStage:
            case GameScenes.BombManStage:
            case GameScenes.FireManStage:
            case GameScenes.ElecManStage:
            case GameScenes.DrWilyStage1:
            case GameScenes.DrWilyStage2:
            case GameScenes.DrWilyStage3:
            case GameScenes.DrWilyStage4:
                StartPlayerLevelScene();
                break;
        }
    }

    // called third
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        // game loop functions for each game scene
        switch (gameScene)
        {
            case GameScenes.TitleScreen:
                TitleScreenLoop();
                break;
            case GameScenes.IntroScene:
                IntroSceneLoop();
                break;
            case GameScenes.StageSelect:
                StageSelectScreenLoop();
                break;
            case GameScenes.GameOver:
                GameOverScreenLoop();
                break;
            case GameScenes.MainScene:
            case GameScenes.CutManStage:
            case GameScenes.GutsManStage:
            case GameScenes.IceManStage:
            case GameScenes.BombManStage:
            case GameScenes.FireManStage:
            case GameScenes.ElecManStage:
            case GameScenes.DrWilyStage1:
            case GameScenes.DrWilyStage2:
            case GameScenes.DrWilyStage3:
            case GameScenes.DrWilyStage4:
                MainPlayerLevelLoop();
                break;
        }

        // game pause
        if (Input.GetKeyDown(KeyCode.P))
        {
            PauseGame();
        }

        // weapons menu
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ShowWeaponsMenu();
        }
    }

    public string GetScene(GameScenes scene)
    {
        // just convert scene type to a string and return it
        // to make it easier the GameScenes enum string is the scene name
        return scene.ToString();
    }

    public GameScenes GetScene(string scene)
    {
        // convert the scene name string to the type
        return (GameScenes)Enum.Parse(typeof(GameScenes), scene);
    }

    public void StartNextScene()
    {
        // this version just calls the last scene
        startNextScene = true;
        nextSceneName = lastSceneName;
        // reset the checkpoint manager settings
        if (Checkpoint.Instance != null) Checkpoint.Instance.ResetManager();
    }

    public void StartNextScene(GameScenes gameScene)
    {
        // flag to trigger starting the next scene
        // scenes call this function to tell the GameManager they're done
        startNextScene = true;
        nextSceneName = GetScene(gameScene);
        // reset the checkpoint manager settings
        if (Checkpoint.Instance != null) Checkpoint.Instance.ResetManager();
    }

    private void StartTitleScreen()
    {
        // add any init code here for title screen
        AllowGamePause(false);
    }

    private void TitleScreenLoop()
    {
        // scene change triggered by StartNextScene()
        if (startNextScene)
        {
            // can do other things here before loading the next scene
            startNextScene = false;
            gameScene = GetScene(nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void StartIntroScene()
    {
        // add any init code here for intro scene
        AllowGamePause(false);
    }

    private void IntroSceneLoop()
    {
        // scene change triggered by StartNextScene()
        if (startNextScene)
        {
            // can do other things here before loading the next scene
            startNextScene = false;
            gameScene = GetScene(nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void StartStageSelectScreen()
    {
        // add any init code here for title screen
        AllowGamePause(false);
    }

    private void StageSelectScreenLoop()
    {
        // scene change triggered by StartNextScene()
        if (startNextScene)
        {
            // can do other things here before loading the next scene
            startNextScene = false;
            gameScene = GetScene(nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void StartGameOverScreen()
    {
        // add any init code here for intro scene
        AllowGamePause(false);
    }

    private void GameOverScreenLoop()
    {
        // scene change triggered by StartNextScene()
        if (startNextScene)
        {
            // game over screen set the next scene
            startNextScene = false;
            gameScene = GetScene(nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void StartPlayerLevelScene()
    {
        // game loop vars
        isGameOver = false;
        playerReady = true;
        initReadyScreen = true;
        firstMessage = true;
        AllowGamePause(false);
        gamePlayerReadyTime = gamePlayerReadyDelay;
        // get the player and camera follow objects
        player = GameObject.FindGameObjectWithTag("Player");
        cameraFollow = Camera.main.GetComponent<CameraFollow>();
        // if any checkpoints have been reached then update the scene
        if (Checkpoint.Instance != null) Checkpoint.Instance.UpdateScene();
        // he starts falling right way, freeze at scene start
        FreezePlayer(true);
        // restore player weapons
        //RestorePlayerWeapons();
        // get tmp text objects for score and screen message
        playerScoreText = GameObject.Find("PlayerScore").GetComponent<TextMeshProUGUI>();
        screenMessageText = GameObject.Find("ScreenMessage").GetComponent<TextMeshProUGUI>();
    }

    private void MainPlayerLevelLoop()
    {
        // player ready screen - wait the delay time and show READY on screen
        if (playerReady)
        {
            // initialize objects and set READY text
            if (initReadyScreen)
            {
                //FreezePlayer(true);
                FreezeEnemies(true);
                screenMessageText.alignment = TextAlignmentOptions.Center;
                screenMessageText.alignment = TextAlignmentOptions.Top;
                screenMessageText.fontStyle = FontStyles.UpperCase;
                screenMessageText.fontSize = 24;
                screenMessageText.text = "\n\n\n\nREADY";
                initReadyScreen = false;
                // set the player transform to null so the camera will stay still
                // the player will call TeleportLanded() to signal animation finished
                if (cameraFollow != null) cameraFollow.player = null;
            }
            // countdown READY screen pause
            gamePlayerReadyTime -= Time.deltaTime;
            if (gamePlayerReadyTime < 0)
            {
                FreezePlayer(false);
                FreezeEnemies(false);
                TeleportPlayer(true);
                screenMessageText.text = "";
                playerReady = false;
                AllowGamePause(true);
            }
            return;
        }

        // show player score
        if (playerScoreText != null)
        {
            playerScoreText.text = String.Format("<mspace=\"{0}\">{1:0000000}</mspace>", playerScoreText.fontSize, playerScore);
        }

        // if the game isn't over then spawn enemies
        if (!isGameOver)
        {
            // here is where we can do things while the game is running
            GetWorldViewCoordinates();
            //ShowMessage();
            //UpdateScore();
            //SpawnEnemies();
            //RepositionEnemies();
            DestroyStrayBullets();

            // scene change triggered by StartNextScene()
            if (startNextScene)
            {
                // save player weapons
                SavePlayerWeapons();
                // load the next scene -- set in previous scene
                startNextScene = false;
                gameScene = GetScene(nextSceneName);
                SceneManager.LoadScene(nextSceneName);
            }
        }
        else
        {
            // game over, wait delay then reload scene
            gameRestartTime -= Time.deltaTime;
            if (gameRestartTime < 0)
            {
                // check player lives
                if (playerLives > 0)
                {
                    // play same scene
                    lastSceneName = SceneManager.GetActiveScene().name;
                    SceneManager.LoadScene(lastSceneName);
                }
                else
                {
                    // out of lives, game over screen
                    gameScene = GameScenes.GameOver;
                    SceneManager.LoadScene(GetScene(gameScene));
                }
            }
        }
    }

    // the extra life (1UP) being collected should add to the player's life count
    public void AddPlayerLives(int lives)
    {
        playerLives += lives;
    }

    // objects that offer score points should call this method upon their defeat to add to the player's score
    public void AddScorePoints(int points)
    {
        playerScore += points;
    }

    // keep track of how many bonus balls are collected and add up the points
    // at the end of a boss battle the bonus score would be added to the overall player score
    public void AddBonusPoints(int points)
    {
        bonusScore.Add(points);
    }

    // get player score from another scene or object etc
    public int GetScorePoints()
    {
        return playerScore;
    }

    // points completing the level and is added to the overall player score at the end
    public void SetLevelPoints(int points)
    {
        levelPoints = points;
    }

    // mark a level as completed to track our progress
    public void SetLevelCompleted(StagesList stage)
    {
        GameStages[(int)stage].Completed = true;
    }

    public void FreezePlayer(bool freeze)
    {
        // freeze player and input
        if (player != null)
        {
            player.GetComponent<PlayerController>().FreezeInput(freeze);
            player.GetComponent<PlayerController>().FreezePlayer(freeze);
        }
    }

    public bool IsGamePaused()
    {
        // return the game pause state
        return isGamePaused;
    }

    public void AllowGamePause(bool pause)
    {
        // flag to allow if the game can be paused
        canPauseGame = pause;
    }

    public void PauseGame(bool pauseMusic = true)
    {
        // if we can pause the game and it isn't already paused
        if (canPauseGame && !isGamePaused)
        {
            isGamePaused = true;
            timeScale = Time.timeScale;
            Time.timeScale = 0;
            if (pauseMusic) SoundManager.Instance.MusicSource.Pause();
            SoundManager.Instance.Play(assetPalette.pauseMenuClip);
            // get current player weapon type and save it
            if (player != null) playerWeaponType = player.GetComponent<PlayerController>().playerWeapon;
            // this is optional if you want to display an on-screen message
            //  save previous message if any
            messageText = screenMessageText.text;
            screenMessageText.text = "PAUSED";
        }
        else if (isGamePaused && !inWeaponsMenu)
        {
            // if the game is paused then unpause it, but not when in weapons menu
            isGamePaused = false;
            Time.timeScale = timeScale;
            if (pauseMusic) SoundManager.Instance.MusicSource.Play();
            // use player's SwitchWeapon instead of TeleportPlayer
            //TeleportPlayer(true, false);
            if (player != null) player.GetComponent<PlayerController>().SwitchWeapon(playerWeaponType);
            // this is optional if you did display an on-screen message
            //   restore previous message if any
            screenMessageText.text = messageText;
        }
    }

    public bool InWeaponsMenu()
    {
        // return in the weapons menu
        return inWeaponsMenu;
    }

    public void ShowWeaponsMenu()
    {
        // can only show the weapons menu if pausing is allowed
        if (!canPauseGame) return;

        if (!inWeaponsMenu)
        {
            // OnWeaponsMenuEnter() is called by WeaponsMenu ShowMenuEvent
            if (WeaponsMenu != null && player != null)
            {
                WeaponsMenu.GetComponent<WeaponsMenu>().SetMenuData(playerLives,
                    player.GetComponent<PlayerController>().playerWeapon,
                    player.GetComponent<PlayerController>().weaponsData);
                WeaponsMenu.GetComponent<WeaponsMenu>().ShowMenu();
            }
        }
        else
        {
            // OnWeaponsMenuExit() is called by WeaponsMenu ExitMenuEvent
            if (WeaponsMenu != null) WeaponsMenu.GetComponent<WeaponsMenu>().ExitMenu();
        }
    }

    public void OnWeaponsMenuEnter()
    {
        // if the game isn't paused then do it (plays the clip)
        //   otherwise play the sound clip again for the weapons menu
        if (!isGamePaused)
        {
            // pause the game
            pauseMusic = false;
            PauseGame(pauseMusic);
        }
        else
        {
            // music keeps playing
            pauseMusic = true;
            // play the pause menu clip
            SoundManager.Instance.Play(assetPalette.pauseMenuClip);
        }

        // hide the HUD Canvas, Player, and everything else
        if (HUDCanvas != null) HUDCanvas.SetActive(false);
        if (player != null) player.GetComponent<PlayerController>().HidePlayer(true);
        HideEverything(true);

        // in the weapons menu
        inWeaponsMenu = true;
    }

    public void OnWeaponsMenuExit()
    {
        // show the HUD Canvas, Player, and everything else
        if (HUDCanvas != null) HUDCanvas.SetActive(true);
        if (player != null) player.GetComponent<PlayerController>().HidePlayer(false);
        HideEverything(false);

        // get the player's selected weapon (overrides existing and sets in PauseGame)
        playerWeaponType = WeaponsMenu.GetComponent<WeaponsMenu>().GetWeaponSelection();

        // not in the weapons menu anymore
        inWeaponsMenu = false;

        // unpause the game
        PauseGame(pauseMusic);
    }

    public bool InCameraTransition()
    {
        // return camera is doing a transition
        return inCameraTransition;
    }

    public void SetInCameraTransition(bool transition)
    {
        // flag that the camera is in a transition state
        inCameraTransition = transition;
    }

    public void FreezeEnemies(bool freeze)
    {
        // freeze all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyController>().FreezeEnemy(freeze);
        }
    }

    public void FreezeExplosions(bool freeze)
    {
        // freeze all explosions
        GameObject[] explosions = GameObject.FindGameObjectsWithTag("Explosion");
        foreach (GameObject explosion in explosions)
        {
            explosion.GetComponent<ExplosionScript>().FreezeExplosion(freeze);
        }
    }

    public void FreezeItems(bool freeze)
    {
        // find all objects with the item script and freeze them
        ItemScript[] itemScripts = GameObject.FindObjectsOfType<ItemScript>();
        foreach (ItemScript itemScript in itemScripts)
        {
            itemScript.FreezeItem(freeze);
        }
    }

    public void FreezeWeapons(bool freeze)
    {
        // freeze all platform beams
        GameObject[] beams = GameObject.FindGameObjectsWithTag("PlatformBeam");
        foreach (GameObject beam in beams)
        {
            beam.GetComponent<MagnetBeamScript>().FreezeBeam(freeze);
        }
        // freeze all bombs
        GameObject[] bombs = GameObject.FindGameObjectsWithTag("Bomb");
        foreach (GameObject bomb in bombs)
        {
            bomb.GetComponent<BombScript>().FreezeBomb(freeze);
        }
        // freeze all bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            bullet.GetComponent<BulletScript>().FreezeBullet(freeze);
        }
    }

    public void FreezeEverything(bool freeze)
    {
        // one method to freeze everything except the player if needed
        FreezeEnemies(freeze);
        FreezeExplosions(freeze);
        FreezeItems(freeze);
        FreezeWeapons(freeze);
    }

    public void HideEnemies(bool hide)
    {
        // hide all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyController>().HideEnemy(hide);
        }
    }

    public void HideExplosions(bool hide)
    {
        // hide all explosions
        GameObject[] explosions = GameObject.FindGameObjectsWithTag("Explosion");
        foreach (GameObject explosion in explosions)
        {
            explosion.GetComponent<ExplosionScript>().HideExplosion(hide);
        }
    }

    public void HideItems(bool hide)
    {
        // find all objects with the item script and hide them
        ItemScript[] itemScripts = GameObject.FindObjectsOfType<ItemScript>();
        foreach (ItemScript itemScript in itemScripts)
        {
            itemScript.HideItem(hide);
        }
    }

    public void HideWeapons(bool hide)
    {
        // hide all platform beams
        GameObject[] beams = GameObject.FindGameObjectsWithTag("PlatformBeam");
        foreach (GameObject beam in beams)
        {
            beam.GetComponent<MagnetBeamScript>().HideBeam(hide);
        }
        // hide all bombs
        GameObject[] bombs = GameObject.FindGameObjectsWithTag("Bomb");
        foreach (GameObject bomb in bombs)
        {
            bomb.GetComponent<BombScript>().HideBomb(hide);
        }
        // hide all bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            bullet.GetComponent<BulletScript>().HideBullet(hide);
        }
    }

    public void HideEverything(bool hide)
    {
        // one method to hide everything except the player if needed
        HideEnemies(hide);
        HideExplosions(hide);
        HideItems(hide);
        HideWeapons(hide);
    }

    private void TeleportPlayer(bool teleport, bool descend = true)
    {
        // teleport player - happens after READY screen
        if (player != null)
        {
            player.GetComponent<PlayerController>().Teleport(teleport, descend);
        }
    }

    public void TeleportFinished()
    {
        // player should call this once done teleporting into the scene
        // reattach the player transform to the Camera Follow script so
        // it'll start following the player again
        // (prevent the camera from moving until the "intro" is done)
        if (player != null && cameraFollow != null)
        {
            if (cameraFollow.player == null)
            {
                cameraFollow.player = player.transform;
            }
        }
    }

    public void SavePlayerWeapons()
    {
        // save the player weapons structs
        if (player != null)
        {
            playerWeapons = player.GetComponent<PlayerController>().weaponsData;
        }
    }

    public void RestorePlayerWeapons()
    {
        // restore the player weapons structs
        if (player != null && playerWeapons != null)
        {
            player.GetComponent<PlayerController>().weaponsData = playerWeapons;
        }
    }

    public void FillWeaponEnergies()
    {
        // start all energy bars at full
        if (playerWeapons != null)
        {
            for (int i = 0; i < playerWeapons.Length; i++)
            {
                playerWeapons[i].currentEnergy = playerWeapons[i].maxEnergy;
            }
        }
    }

    public void PlayerDefeated()
    {
        // game over :(
        isGameOver = true;
        gameRestartTime = gameRestartDelay;
        AllowGamePause(false);
        // one less player life
        playerLives--;
        // stop all sounds
        SoundManager.Instance.Stop();
        SoundManager.Instance.StopMusic();
        // freeze player and input
        FreezePlayer(true);
        // freeze all enemies
        FreezeEnemies(true);
        // destroy all weapons
        DestroyWeapons();
        // save player weapons
        SavePlayerWeapons();
    }

    private void GetWorldViewCoordinates()
    {
        // get camera world coordinates just outside the left-bottom and top-right views
        Vector3 wv0 = Camera.main.ViewportToWorldPoint(new Vector3(-0.1f, -0.1f, 0));
        Vector3 wv1 = Camera.main.ViewportToWorldPoint(new Vector3(1.1f, 1.1f, 0));
        // and then update the world view coords var so it can used when needed
        worldViewCoords.Left = wv0.x;
        worldViewCoords.Bottom = wv0.y;
        worldViewCoords.Right = wv1.x;
        worldViewCoords.Top = wv1.y;
    }

    private void ShowMessage()
    {
        // words of encouragement :)
        string[] messages = {
            "GO GO GO",
            "YOU'RE TOO GOOD",
            "GREAT JOB PLAYER",
            "RACK UP THAT SCORE",
            "THIS MIGHT BE TOUGH"
        };

        // pick a random message when there are no enemies
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            messageIndex = firstMessage ? 0 : UnityEngine.Random.Range(0, messages.Length);
            messageText = messages[messageIndex];
            messageTimer = messageDelay;
            showMessage = true;
        }

        if (showMessage)
        {
            // show the message on screen
            screenMessageText.alignment = TextAlignmentOptions.Center;
            screenMessageText.alignment = TextAlignmentOptions.Top;
            screenMessageText.fontStyle = FontStyles.UpperCase;
            screenMessageText.fontSize = 24;
            screenMessageText.text = messageText;

            // show the message for the timer duration
            messageTimer -= Time.deltaTime;
            if (messageTimer < 0)
            {
                screenMessageText.text = "";
                firstMessage = false;
                showMessage = false;
            }
        }
    }

    private void UpdateScore()
    {
        // update the player score each wave by adding on any bonus points
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            playerScore += bonusScore.Sum();
            bonusScore.Clear();
        }
    }

    private void SpawnEnemies()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            // 5 enemies at most on screen at one time
            int randomEnemyCount = UnityEngine.Random.Range(1, 6);
            GameObject[] randomEnemies = new GameObject[randomEnemyCount];
            for (int i = 0; i < randomEnemyCount; i++)
            {
                // pick a random enemy and position it
                int enemyIndex = UnityEngine.Random.Range(0, enemyPrefabCount);
                randomEnemies[i] = Instantiate(assetPalette.enemyPrefabs[enemyIndex]);
                randomEnemies[i].name = assetPalette.enemyPrefabs[enemyIndex].name;
                randomEnemies[i].transform.position = new Vector3(worldViewCoords.Right + UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(-1f, 1f), 0);
                // for enemies with colors, randomize them
                switch (randomEnemies[i].name)
                {
                    case "BigEye":
                        // and dropping big eye into the scene inside camera view
                        randomEnemies[i].transform.position = new Vector3(worldViewCoords.Right - UnityEngine.Random.Range(0.75f, 1.5f), 2f, 0);
                        randomEnemies[i].GetComponent<BigEyeController>().SetColor((BigEyeController.BigEyeColors)UnityEngine.Random.Range(0, Enum.GetNames(typeof(BigEyeController.BigEyeColors)).Length));
                        break;
                    case "KillerBomb":
                        randomEnemies[i].GetComponent<KillerBombController>().SetColor((KillerBombController.KillerBombColors)UnityEngine.Random.Range(0, Enum.GetNames(typeof(KillerBombController.KillerBombColors)).Length));
                        break;
                }
            }
        }
    }

    private void RepositionEnemies()
    {
        // check all enemies in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            // outside the bottom view (grounded enemies)
            if (enemy.transform.position.y < worldViewCoords.Bottom)
            {
                switch (enemy.name)
                {
                    case "BigEye":
                        // because he uses velocity while in the air for jumping it's possible 
                        // he can go outside the right boundary, reposition more to the left
                        enemy.transform.position = new Vector3(worldViewCoords.Right - 2.5f, 2f, 0);
                        break;
                }
            }
            // outside the left view (flying enemies)
            if (enemy.transform.position.x < worldViewCoords.Left)
            {
                switch (enemy.name)
                {
                    case "Mambu":
                        enemy.transform.position = new Vector3(worldViewCoords.Right, UnityEngine.Random.Range(-0.9f, 0.7f), 0);
                        break;
                    case "KillerBomb":
                        enemy.transform.position = new Vector3(worldViewCoords.Right, UnityEngine.Random.Range(-1.5f, 1.5f), 0);
                        enemy.GetComponent<KillerBombController>().ResetFollowingPath();
                        break;
                    case "Pepe":
                        enemy.transform.position = new Vector3(worldViewCoords.Right, UnityEngine.Random.Range(-1f, 1f), 0);
                        enemy.GetComponent<PepeController>().ResetFollowingPath();
                        break;
                }
            }
        }
    }

    private void DestroyStrayBullets()
    {
        // destroy all bullets that are outside the camera world view
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            // bullet is out of view so destroy it
            if (bullet.transform.position.x < worldViewCoords.Left ||
                bullet.transform.position.x > worldViewCoords.Right ||
                bullet.transform.position.y > worldViewCoords.Top ||
                bullet.transform.position.y < worldViewCoords.Bottom)
            {
                // buh bye bullet
                Destroy(bullet);
            }
        }
    }

    public void DestroyWeapons()
    {
        // remove all platform beams
        GameObject[] beams = GameObject.FindGameObjectsWithTag("PlatformBeam");
        foreach (GameObject beam in beams) Destroy(beam);
        // remove all bombs
        GameObject[] bombs = GameObject.FindGameObjectsWithTag("Bomb");
        foreach (GameObject bomb in bombs) Destroy(bomb);
        // remove all bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets) Destroy(bullet);
        // remove all explosions
        GameObject[] explosions = GameObject.FindGameObjectsWithTag("Explosion");
        foreach (GameObject explosion in explosions) Destroy(explosion);
    }

    public void TallyPlayerScore()
    {
        // wrapper function to the points tally coroutine
        StartCoroutine(TallyPlayerScoreCo());
    }

    private IEnumerator TallyPlayerScoreCo()
    {
        // we begin with a string of just CLEAR POINTS 
        string scoreString = "CLEAR\nPOINTS\n\n";

        // adjust textmesh pro text properties
        screenMessageText.alignment = TextAlignmentOptions.Center;
        screenMessageText.alignment = TextAlignmentOptions.Top;
        screenMessageText.fontStyle = FontStyles.UpperCase;
        screenMessageText.fontSize = 18;
        screenMessageText.text = String.Format("<mspace=\"{0}\">" + scoreString + "</mspace>", screenMessageText.fontSize);

        // start with CLEAR POINTS and pause
        yield return new WaitForSeconds(1f);

        // add on the level points string
        scoreString += "{1:000000}\n\n";

        // var to add up the level points and how many iterations to loop
        int levelPointsTally = 0;
        int tallyLoops = levelPoints / 1000;

        // start the point tally sound loop
        SoundManager.Instance.Play(assetPalette.pointTallyLoopClip, true);

        /*
         * add the iteration points to the player score
         * add up the level points, show it, with a tiny delay
         * in between each pass and when we hit the end then
         * play the point tally end sound clip
         */
        for (int i = 0; i < tallyLoops; i++)
        {
            playerScore += 1000;
            levelPointsTally += 1000;
            screenMessageText.text = String.Format("<mspace=\"{0}\">" + scoreString + "</mspace>",
                screenMessageText.fontSize, levelPointsTally);
            if (i == tallyLoops - 1)
            {
                SoundManager.Instance.Play(assetPalette.pointTallyEndClip);
            }
            yield return new WaitForSeconds(0.025f);
        }

        // add another delay before the bonus points part
        yield return new WaitForSeconds(1f);

        // add on the bonus ball points string (we have a sprite atlas for the bonus ball)
        scoreString += "<sprite=\"BonusItems\" index=\"16\">1000X{2:00}\nBONUS\n\n{3:000000}";

        // var to add up the bonus points collected
        int bonusPointsTally = 0;

        // start the point tally sound loop
        SoundManager.Instance.Play(assetPalette.pointTallyLoopClip, true);

        /*
         * much like how we did the level points tally, but this time we
         * are doing the bonus points. same bit of code except for a couple
         * more variables passed to the string format for how many balls
         * collected the bonus ball total points
         */
        if (bonusScore.Count > 0)
        {
            // balls collected
            for (int i = 0; i < bonusScore.Count; i++)
            {
                playerScore += bonusScore[i];
                bonusPointsTally += bonusScore[i];
                screenMessageText.text = String.Format("<mspace=\"{0}\">" + scoreString + "</mspace>",
                    screenMessageText.fontSize, levelPointsTally, bonusScore.Count, bonusPointsTally);
                if (i == bonusScore.Count - 1)
                {
                    SoundManager.Instance.Play(assetPalette.pointTallyEndClip);
                }
                yield return new WaitForSeconds(0.025f);
            }
        }
        else
        {
            // nothing collected, just display the bonus and play the end clip
            screenMessageText.text = String.Format("<mspace=\"{0}\">" + scoreString + "</mspace>",
                screenMessageText.fontSize, levelPointsTally, bonusScore.Count, bonusPointsTally);
            SoundManager.Instance.Play(assetPalette.pointTallyEndClip);
        }
    }

    public void ResetPlayerLives()
    {
        // reset the player lives
        playerLives = gamePlayerStartLives;
    }

    public void ResetPointsCollected(bool resetLevelPoints = true, bool resetPlayerScore = true)
    {
        // reset points collected and should be called at the end of each level
        // pass true to clear the player score (should reset when death and no more lives to continue)
        bonusScore.Clear();
        if (resetLevelPoints)
        {
            levelPoints = 0;
        }
        if (resetPlayerScore)
        {
            playerScore = 0;
        }
    }

    // internal to the game manager to pick a random bonus item
    // use the function below GetBonusItem in enemies and objects
    // that need to generate a bonus item drop upon their explosion
    private ItemScript.ItemTypes PickRandomBonusItem()
    {
        /*
		 * Item info from this source
		 * http://tasvideos.org/GameResources/NES/Rockman.html
		 * 
		 * Type					MM1 (random seeds)
		 * extra life			1/128 (63)
		 * big weapon refill	2/128 (5F and 60)
		 * big energy refill	2/128 (61 and 62)
		 * small weapon refill	15/128 (41-4F)
		 * small energy refill	15/128 (50-5E)
		 * bonus pearl			69/128 (0C-40 and 70-7F)
		 * nothing				24/128 (00-0B and 64-6F)
		 * 
		 * Array indexes based on the random seeds
		 * Index 0		nothing
		 * Index 1		bonus pearl
		 * Index 2		small weapon refill
		 * Index 3		small energy refill
		 * Index 4		big weapon refill
		 * Index 5		big energy refill
		 * Index 6		extra life
		 * Index 7		nothing
		 * Index 8		bonus pearl
		 * 
		 * 
		 * This function is based off the Unity documentation
		 * 
		 * Choosing Items with Different Probabilities
		 * https://docs.unity3d.com/2019.3/Documentation/Manual/RandomNumbers.html
		 */
        // probabilities (random seeds) in decimal values
        float[] probabilities = {
            12, 53, 15, 15, 2, 2, 1, 12, 16
        };

        // sum of all probabilities
        float total = 0;

        // item types indexed to match probabilities
        ItemScript.ItemTypes[] items = {
            ItemScript.ItemTypes.Nothing,
            ItemScript.ItemTypes.BonusBall,
            ItemScript.ItemTypes.WeaponEnergySmall,
            ItemScript.ItemTypes.LifeEnergySmall,
            ItemScript.ItemTypes.WeaponEnergyBig,
            ItemScript.ItemTypes.LifeEnergyBig,
            ItemScript.ItemTypes.ExtraLife,
            ItemScript.ItemTypes.Nothing,
            ItemScript.ItemTypes.BonusBall
        };

        // add up all the probability values to get the total
        foreach (float prob in probabilities)
        {
            total += prob;
        }

        // pick a point in the total
        float randomPoint = UnityEngine.Random.value * total;

        // use the chosen index value to get the item type to return
        for (int i = 0; i < probabilities.Length; i++)
        {
            if (randomPoint < probabilities[i])
            {
                //return i;
                return items[i];
            }
            else
            {
                randomPoint -= probabilities[i];
            }
        }
        //return probabilities.Length - 1;
        return items[probabilities.Length - 1];
    }

    // enemies and objects that offer bonus items should call this function
    public GameObject GetBonusItem(ItemScript.ItemTypes itemType)
    {
        GameObject bonusItem = null;

        // pick a random bonus item
        if (itemType == ItemScript.ItemTypes.Random)
        {
            itemType = PickRandomBonusItem();
        }

        // get bonus item prefab from type
        switch (itemType)
        {
            case ItemScript.ItemTypes.Nothing:
                bonusItem = null;
                break;
            case ItemScript.ItemTypes.BonusBall:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.BonusBall];
                break;
            case ItemScript.ItemTypes.ExtraLife:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.ExtraLife];
                break;
            case ItemScript.ItemTypes.LifeEnergyBig:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.LifeEnergyBig];
                break;
            case ItemScript.ItemTypes.LifeEnergySmall:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.LifeEnergySmall];
                break;
            case ItemScript.ItemTypes.WeaponEnergyBig:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.WeaponEnergyBig];
                break;
            case ItemScript.ItemTypes.WeaponEnergySmall:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.WeaponEnergySmall];
                break;
            case ItemScript.ItemTypes.MagnetBeam:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.MagnetBeam];
                break;
            case ItemScript.ItemTypes.WeaponPart:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.WeaponPart];
                break;
            case ItemScript.ItemTypes.Yashichi:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.Yashichi];
                break;
        }

        // return bonus item prefab
        return bonusItem;
    }

    public void SetBonusItemsColorPalette()
    {
        // find all objects with the item script and update the color palettes
        ItemScript[] itemScripts = GameObject.FindObjectsOfType<ItemScript>();
        foreach (ItemScript itemScript in itemScripts)
        {
            itemScript.SetColorPalette();
        }
    }
}