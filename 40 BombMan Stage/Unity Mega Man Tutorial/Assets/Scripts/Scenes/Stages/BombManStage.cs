using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class BombManStage : MonoBehaviour
{
    // create an animated storyline
    float startTime;
    float runTime;

    // flag that we called for the next scene
    bool calledNextScene;

    // canvas stuff
    Text runTimeText;
    TextMeshProUGUI screenMessageText;

    // size information
    float tileSizeX;
    float tileSizeY;
    float halfTileX;
    float halfTileY;
    float tileScreenSizeWidth;
    float tileScreenSizeHeight;
    float halfScreenWidth;
    float halfScreenHeight;

    // track doors and tiles animation
    bool isSwappingTiles;
    bool isDoorwayMoving;
    bool isDoorway1Open;
    bool isDoorway2Open;

    // player info
    GameObject player;
    Vector3 playerPosition;

    // need access to the boss
    GameObject bombMan;

    // these guys operate a little differently
    bool canSpawnKillerBomb;
    bool hasSpawnedKillerBomb;
    bool midStartKillerBomb;
    float delayKillerBomb;
    bool canSpawnMambu;
    bool hasSpawnedMambu;
    float delayMambu;
    bool pickRandomSuzyMovement;
    float randomSuzyDelay;

    // world view coords
    GameManager.WorldViewCoordinates worldView;

    // objects status
    IDictionary<string, bool> objectActive =
        new Dictionary<string, bool>();

    // objects position
    IDictionary<string, Vector3> objectPosition =
        new Dictionary<string, Vector3>();

    [Header("Scene Settings")]
    // screen size in tiles
    public Vector2Int tileScreenXY =
        new Vector2Int(16, 15);

    // show runtime text
    [SerializeField] bool showRunTime;

    public enum LevelStates { LevelPlay, BossFightIntro, BossFight, PlayerVictory, NextScene };
    public LevelStates levelState = LevelStates.LevelPlay;

    [Header("Audio Clips")]
    public AudioClip doorClip;
    public AudioClip musicClip;
    public AudioClip bossFightClip;
    public AudioClip victoryThemeClip;

    [Header("TileMap Objects")]
    public Grid tmGrid;
    public Tilemap tmBackground;
    public Tilemap tmForeground;
    public Tilemap tmDoorways;

    public TileBase doorTileH;
    public TileBase doorTileV;

    public TileBase[] bgTile1;
    public TileBase[] bgTile2;

    [Header("Bonus Item Objects")]
    public GameObject prefabExtraLife;
    public GameObject prefabLifeEnergyBig;
    public GameObject prefabLifeEnergySmall;
    public GameObject prefabWeaponEnergyBig;
    public GameObject prefabWeaponPart;

    [Header("Enemy Objects")]
    public GameObject prefabKamadoma;
    public GameObject prefabBombombLauncher;
    public GameObject prefabScrewDriver;
    public GameObject prefabBlaster;
    public GameObject prefabSniperJoe;
    public GameObject prefabKillerBomb;
    public GameObject prefabGabyoall;
    public GameObject prefabMambu;
    public GameObject prefabAdheringSuzy;
    public GameObject prefabBombMan;

    [Header("Camera Transitions")]
    public GameObject[] camTransitions;

    void Awake()
    {
        // canvas objects
        runTimeText = GameObject.Find("RunTime").GetComponent<Text>();
        screenMessageText = GameObject.Find("ScreenMessage").GetComponent<TextMeshProUGUI>();

        // track the doors and tiles animation
        isSwappingTiles = false;
        isDoorwayMoving = false;
        isDoorway1Open = false;
        isDoorway2Open = true;

        // grid and tile sizes
        tileSizeX = tmGrid.cellSize.x;
        tileSizeY = tmGrid.cellSize.y;

        halfTileX = tileSizeX / 2f;
        halfTileY = tileSizeY / 2f;

        tileScreenSizeWidth = (float)tileScreenXY.x * tileSizeX;
        tileScreenSizeHeight = (float)tileScreenXY.y * tileSizeY;

        halfScreenWidth = tileScreenSizeWidth / 2f;
        halfScreenHeight = tileScreenSizeHeight / 2f;

        // initialize all objects being active to false
        // kamadomas
        objectActive["Kamadoma1"] = false;
        objectActive["Kamadoma2"] = false;
        objectActive["Kamadoma3"] = false;
        objectActive["Kamadoma4"] = false;
        objectActive["Kamadoma5"] = false;

        // bombombs
        objectActive["BombombLauncher1"] = false;
        objectActive["BombombLauncher2"] = false;
        objectActive["BombombLauncher3"] = false;
        objectActive["BombombLauncher4"] = false;

        // screwdrivers
        objectActive["ScrewDriver1"] = false;
        objectActive["ScrewDriver2"] = false;
        objectActive["ScrewDriver3"] = false;

        // blasters
        objectActive["Blaster1"] = false;
        objectActive["Blaster2"] = false;
        objectActive["Blaster3"] = false;
        objectActive["Blaster4"] = false;
        objectActive["Blaster5"] = false;
        objectActive["Blaster6"] = false;
        objectActive["Blaster7"] = false;

        // sniper joes
        objectActive["SniperJoe1"] = false;
        objectActive["SniperJoe2"] = false;
        objectActive["SniperJoe3"] = false;

        // killerbomb
        objectActive["KillerBomb"] = false;

        // gabyoalls
        objectActive["Gabyoall1"] = false;
        objectActive["Gabyoall2"] = false;
        objectActive["Gabyoall3"] = false;

        // mambu
        objectActive["Mambu"] = false;

        // adhering suzys
        objectActive["AdheringSuzy1"] = false;
        objectActive["AdheringSuzy2"] = false;
        objectActive["AdheringSuzy3"] = false;
        objectActive["AdheringSuzy4"] = false;
        objectActive["AdheringSuzy5"] = false;
        objectActive["AdheringSuzy6"] = false;
        objectActive["AdheringSuzy7"] = false;
        objectActive["AdheringSuzy8"] = false;

        // bonus items
        objectActive["ExtraLife1"] = false;
        objectActive["LifeEnergyBig1"] = false;
        objectActive["LifeEnergySmall1"] = false;
        objectActive["LifeEnergySmall2"] = false;
        objectActive["WeaponEnergyBig1"] = false;

        // initialize all object positions
        // kamadomas
        objectPosition["Kamadoma1"] = new Vector3(TileWorldPos(9, 0), -0.3250194f);
        objectPosition["Kamadoma2"] = new Vector3(TileWorldPos(12, 0), -0.005020976f);
        objectPosition["Kamadoma3"] = new Vector3(TileWorldPos(16, 0), 0.3149771f);
        objectPosition["Kamadoma4"] = new Vector3(TileWorldPos(19, 0), -0.00498461f);
        objectPosition["Kamadoma5"] = new Vector3(TileWorldPos(21, 0), -0.3249879f);

        // bombombs
        objectPosition["BombombLauncher1"] = new Vector3(TileWorldPos(43, 0), -1.2f);
        objectPosition["BombombLauncher2"] = new Vector3(TileWorldPos(51, 0), -1.2f);
        objectPosition["BombombLauncher3"] = new Vector3(TileWorldPos(59, 0), -1.2f);
        objectPosition["BombombLauncher4"] = new Vector3(TileWorldPos(67, 0), -1.2f);

        // screwdrivers
        objectPosition["ScrewDriver1"] = new Vector3(TileWorldPos(80, 0), -0.6449734f);
        objectPosition["ScrewDriver2"] = new Vector3(TileWorldPos(86), -0.6449734f);
        objectPosition["ScrewDriver3"] = new Vector3(TileWorldPos(92), -0.6449734f);

        // blasters
        objectPosition["Blaster1"] = new Vector3(TileWorldPos(92, 0), 2.786f);
        objectPosition["Blaster2"] = new Vector3(TileWorldPos(92, 0), 2.476f);
        objectPosition["Blaster3"] = new Vector3(TileWorldPos(92, 0), 2.156f);
        objectPosition["Blaster4"] = new Vector3(TileWorldPos(92, 0), 1.836f);
        objectPosition["Blaster5"] = new Vector3(TileWorldPos(128, 0), 4.56f);
        objectPosition["Blaster6"] = new Vector3(TileWorldPos(136, 0), 4.56f);
        objectPosition["Blaster7"] = new Vector3(TileWorldPos(144, 0), 4.56f);

        // sniper joes
        objectPosition["SniperJoe1"] = new Vector3(TileWorldPos(101, 0), 4.5f);
        objectPosition["SniperJoe2"] = new Vector3(TileWorldPos(211, 0), 9.94f);
        objectPosition["SniperJoe3"] = new Vector3(TileWorldPos(243, 0), 9.52f);

        // killerbomb (position calculated during play)
        objectPosition["KillerBomb"] = Vector3.zero;

        // gabyoalls
        objectPosition["Gabyoall1"] = new Vector3(TileWorldPos(163), 4.794984f);
        objectPosition["Gabyoall2"] = new Vector3(TileWorldPos(168), 5.274985f);
        objectPosition["Gabyoall3"] = new Vector3(TileWorldPos(176), 4.155014f);

        // mambu (x position calculated during play)
        objectPosition["Mambu"] = new Vector3(0, TileWorldPos(60, 0));

        // adhering suzys
        objectPosition["AdheringSuzy1_1"] = new Vector3(40.39f, TileWorldPos(48));
        objectPosition["AdheringSuzy1_2"] = new Vector3(40.89f, TileWorldPos(48));
        objectPosition["AdheringSuzy2_1"] = new Vector3(40.39f, TileWorldPos(44));
        objectPosition["AdheringSuzy2_2"] = new Vector3(40.89f, TileWorldPos(44));
        objectPosition["AdheringSuzy3_1"] = new Vector3(40.39f, TileWorldPos(41));
        objectPosition["AdheringSuzy3_2"] = new Vector3(40.89f, TileWorldPos(41));
        objectPosition["AdheringSuzy4_1"] = new Vector3(40.39f, TileWorldPos(39));
        objectPosition["AdheringSuzy4_2"] = new Vector3(40.89f, TileWorldPos(39));
        objectPosition["AdheringSuzy5_1"] = new Vector3(40.39f, TileWorldPos(33));
        objectPosition["AdheringSuzy5_2"] = new Vector3(40.89f, TileWorldPos(33));
        objectPosition["AdheringSuzy6_1"] = new Vector3(40.39f, TileWorldPos(29));
        objectPosition["AdheringSuzy6_2"] = new Vector3(40.89f, TileWorldPos(29));
        objectPosition["AdheringSuzy7_1"] = new Vector3(40.39f, TileWorldPos(26));
        objectPosition["AdheringSuzy7_2"] = new Vector3(40.89f, TileWorldPos(26));
        objectPosition["AdheringSuzy8_1"] = new Vector3(40.39f, TileWorldPos(24));
        objectPosition["AdheringSuzy8_2"] = new Vector3(40.89f, TileWorldPos(24));

        // bonus items
        objectPosition["ExtraLife1"] = new Vector3(TileWorldPos(213), 9.670017f);
        objectPosition["LifeEnergyBig1"] = new Vector3(TileWorldPos(98), 1.834994f);
        objectPosition["LifeEnergySmall1"] = new Vector3(TileWorldPos(88), 0.5349771f);
        objectPosition["LifeEnergySmall2"] = new Vector3(TileWorldPos(89), 0.5349771f);
        objectPosition["WeaponEnergyBig1"] = new Vector3(TileWorldPos(98), -0.4222365f);

        // bombman and his weapon part
        objectPosition["BombMan"] = new Vector3(TileWorldPos(258, 0), 1.755008f);
        objectPosition["WeaponPart"] = new Vector3(TileWorldPos(256), TileWorldPos(20));
    }

    // Start is called before the first frame update
    void Start()
    {
        // 4:3 resolution bonus items
        GameManager.Instance.SetResolutionScale(GameManager.ResolutionScales.Scale4x3);

        // set palette for weapons menu
        GameManager.Instance.SetWeaponsMenuPalette(WeaponsMenu.MenuPalettes.BombMan);

        // get the player object
        player = GameObject.FindGameObjectWithTag("Player");

        // set up the scene music - full volume, loop, and play
        SoundManager.Instance.MusicSource.volume = 1.0f;
        SoundManager.Instance.PlayMusic(musicClip);
    }

    // Update is called once per frame
    void Update()
    {
        // this is for seeing the run time of the scene and is helpful for making 
        // the storytelling triggers - use checkbox in inspector to turn it off
        runTimeText.text = showRunTime ? String.Format("RunTime: {0:0.00}", runTime) : "";

        switch (levelState)
        {
            case LevelStates.LevelPlay:
                // save the player's position
                if (player != null) playerPosition = player.transform.position;

                // remove any objects outside of camera view
                RemoveGameObjects();

                // spawn enemies and bonus items as neeeded
                SpawnKamadomas();
                SpawnBombombLaunchers();
                SpawnScrewDrivers();
                SpawnSniperJoes();
                SpawnBlasters();
                SpawnKillerBombs();
                SpawnGabyoalls();
                SpawnMambus();
                SpawnBonusItems();
                RandomizeAdheringSuzys();

                // loop our music track when the playback position is reached
                if (SoundManager.Instance.MusicSource.time >= 37.361f)
                {
                    SoundManager.Instance.MusicSource.time = 7.545f;
                }
                break;
            case LevelStates.BossFightIntro:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // hit the last trigger
                if (UtilityFunctions.InTime(runTime, 0.001f))
                {
                    // don't allow the game to be paused
                    GameManager.Instance.AllowGamePause(false);
                    // start the boss fight music
                    SoundManager.Instance.StopMusic();
                    SoundManager.Instance.MusicSource.volume = 1f;
                    SoundManager.Instance.PlayMusic(bossFightClip);
                    // play swap tiles animation (bg flash)
                    SwapTilesAnimation();
                    // show bombman's health bar
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.EnemyHealth, 0);
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.EnemyHealth, UIEnergyBars.EnergyBarTypes.BombMan);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.EnemyHealth, true);
                    // let go of the ladder and freeze input
                    player.GetComponent<PlayerController>().ResetClimbing();
                    player.GetComponent<PlayerController>().FreezeInput(true);
                }

                // close the doorway to the battle area
                if (UtilityFunctions.InTime(runTime, 3.0f))
                {
                    ToggleDoorway2();
                }

                // show bombman
                if (UtilityFunctions.InTime(runTime, 4.0f))
                {
                    InstantiateBombMan("BombMan", objectPosition["BombMan"]);
                }

                // do bombman's pose and fill health bar
                if (UtilityFunctions.InTime(runTime, 4.5f))
                {
                    bombMan.GetComponent<BombManController>().Pose();
                    StartCoroutine(FillEnemyHealthBar());
                }

                // battle starts, enable boss ai and give player control
                if (UtilityFunctions.InTime(runTime, 5.75f))
                {
                    bombMan.GetComponent<BombManController>().EnableAI(true);
                    player.GetComponent<PlayerController>().FreezeInput(false);
                    // allow the game to be paused
                    GameManager.Instance.AllowGamePause(true);
                    // move on to BossFight state
                    levelState = LevelStates.BossFight;
                }
                break;
            case LevelStates.BossFight:
                // loop the boss fight music clip
                // look for time end and set new position when found
                if (SoundManager.Instance.MusicSource.time >= 15.974f)
                {
                    SoundManager.Instance.MusicSource.time = 3.192f;
                }
                break;
            case LevelStates.PlayerVictory:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // have game manager do the score tally
                if (UtilityFunctions.InTime(runTime, 7.0f))
                {
                    GameManager.Instance.TallyPlayerScore();
                }

                // level completed and move on to the next scene
                if (UtilityFunctions.InTime(runTime, 15.0f))
                {
                    // reset the points collected
                    GameManager.Instance.ResetPointsCollected(true, false);
                    // set this level as completed
                    GameManager.Instance.SetLevelCompleted(GameManager.StagesList.BombMan);
                    // switch to the next scene state
                    levelState = LevelStates.NextScene;
                }
                break;
            case LevelStates.NextScene:
                // tell GameManager to trigger the next scene
                if (!calledNextScene)
                {
                    GameManager.Instance.StartNextScene(GameManager.GameScenes.StageSelect);
                    calledNextScene = true;
                }
                break;
        }
    }

    public void ToggleDoorway1(bool playAudio = true)
    {
        if (!isDoorwayMoving)
        {
            // coroutine does the door animation
            StartCoroutine(ToggleDoorway1Co());
            if (playAudio)
            {
                // play door opening/closing sound
                SoundManager.Instance.Play(doorClip);
            }
        }
    }

    private IEnumerator ToggleDoorway1Co()
    {
        isDoorwayMoving = true;

        if (isDoorway1Open)
        {
            for (int i = 0; i < 4; i++)
            {
                tmDoorways.SetTile(new Vector3Int(244, 61 - i, 0), doorTileV);
                tmDoorways.SetTile(new Vector3Int(245, 61 - i, 0), doorTileV);
                yield return new WaitForSeconds(0.15f);
            }
        }
        else
        {
            for (int i = 3; i >= 0; i--)
            {
                tmDoorways.SetTile(new Vector3Int(244, 61 - i, 0), null);
                tmDoorways.SetTile(new Vector3Int(245, 61 - i, 0), null);
                yield return new WaitForSeconds(0.15f);
            }
        }

        isDoorway1Open = !isDoorway1Open;

        isDoorwayMoving = false;
    }

    public void ToggleDoorway2(bool playAudio = true)
    {
        if (!isDoorwayMoving)
        {
            // coroutine does the door animation
            StartCoroutine(ToggleDoorway2Co());
            if (playAudio)
            {
                // play door opening/closing sound
                SoundManager.Instance.Play(doorClip);
            }
        }
    }

    private IEnumerator ToggleDoorway2Co()
    {
        isDoorwayMoving = true;

        if (isDoorway2Open)
        {
            for (int i = 0; i < 4; i++)
            {
                tmDoorways.SetTile(new Vector3Int(252 + i, 22, 0), doorTileH);
                tmDoorways.SetTile(new Vector3Int(252 + i, 21, 0), doorTileH);
                yield return new WaitForSeconds(0.15f);
            }
        }
        else
        {
            for (int i = 3; i >= 0; i--)
            {
                tmDoorways.SetTile(new Vector3Int(252 + i, 22, 0), null);
                tmDoorways.SetTile(new Vector3Int(252 + i, 21, 0), null);
                yield return new WaitForSeconds(0.15f);
            }
        }

        isDoorway2Open = !isDoorway2Open;

        isDoorwayMoving = false;
    }

    public void SwapTilesAnimation()
    {
        if (!isSwappingTiles)
        {
            StartCoroutine(SwapTilesAnimationCo());
        }
    }

    private IEnumerator SwapTilesAnimationCo()
    {
        isSwappingTiles = true;

        for (int i = 0; i < 5; i++)
        {
            tmBackground.SwapTile(bgTile1[0], bgTile2[0]);
            tmBackground.SwapTile(bgTile1[1], bgTile2[1]);
            yield return new WaitForSeconds(0.15f);

            tmBackground.SwapTile(bgTile2[0], bgTile1[0]);
            tmBackground.SwapTile(bgTile2[1], bgTile1[1]);
            yield return new WaitForSeconds(0.15f);
        }

        isSwappingTiles = false;
    }

    private IEnumerator FillEnemyHealthBar()
    {
        // get enemy max health for bar calculation
        int maxHealth = bombMan.GetComponent<EnemyController>().maxHealth;
        // loop the sound and play the repeat clip we generated
        SoundManager.Instance.Play(bombMan.GetComponent<EnemyController>().energyFillClip, true);
        // increment the enemy health bar with a slight delay between each bar
        for (int i = 1; i <= maxHealth; i++)
        {
            float bars = (float)i / (float)maxHealth;
            UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.EnemyHealth, bars);
            yield return new WaitForSeconds(0.025f);
        }
        // stop playing the repeat sound
        SoundManager.Instance.Stop();
    }

    public void BossDefeated()
    {
        // stop the music
        SoundManager.Instance.StopMusic();
        // hide the enemy health bar
        UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.EnemyHealth, false);
        // destroy all weapons
        GameManager.Instance.DestroyWeapons();
        // set the player to invincible
        player.GetComponent<PlayerController>().Invincible(true);
        // instantiate weapon part
        InstantiateWeaponPart("WeaponPart", objectPosition["WeaponPart"], ItemScript.WeaponPartColors.Orange);
    }

    private void WeaponPartCollected()
    {
        // get start time
        startTime = Time.time;
        // play victory theme clip
        SoundManager.Instance.MusicSource.volume = 1f;
        SoundManager.Instance.PlayMusic(victoryThemeClip, false);
        // freeze the player and input
        GameManager.Instance.FreezePlayer(true);
        // don't allow the game to be paused
        GameManager.Instance.AllowGamePause(false);
        // switch state the player victory
        levelState = LevelStates.PlayerVictory;
    }

    bool OutOfCameraView(Vector3 point)
    {
        // test if point is out of camera view
        return (point.x < worldView.Left || point.x > worldView.Right ||
            point.y > worldView.Top || point.y < worldView.Bottom);
    }

    public void RemoveGameObjects(bool removeAll = false)
    {
        // get the world view coords from the game manager
        worldView = GameManager.Instance.worldViewCoords;

        // Remove Enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            if (OutOfCameraView(enemies[i].transform.position) || removeAll)
            {
                // killerbomb and mambu are kept in play and moved to the other side
                // of the screen unless removeAll is true or they're out of spawn coords
                if (enemies[i].name.Equals("KillerBomb") ||
                    enemies[i].name.Equals("Mambu"))
                {
                    // also remove them if not in spawn coords
                    if (removeAll ||
                        (enemies[i].name.Equals("Mambu") &&
                        playerPosition.x > TileWorldPos(201, 0)))
                    {
                        // can't spawn again until a different coord is reached
                        canSpawnMambu = false;
                        // remove them
                        Destroy(enemies[i]);
                    }
                    else
                    {
                        // move to the other side of the screen
                        MoveGameObjects(enemies[i]);
                    }
                }
                else
                {
                    // remove all other enemies
                    Destroy(enemies[i]);
                }
            }
        }

        // Remove Explosions
        GameObject[] explosions = GameObject.FindGameObjectsWithTag("Explosion");
        for (int i = 0; i < explosions.Length; i++)
        {
            if (OutOfCameraView(explosions[i].transform.position) || removeAll)
            {
                Destroy(explosions[i]);
            }
        }

        // Remove Bonus Items
        ItemScript[] itemScripts = GameObject.FindObjectsOfType<ItemScript>();
        for (int i = 0; i < itemScripts.Length; i++)
        {
            if (OutOfCameraView(itemScripts[i].gameObject.transform.position) || removeAll)
            {
                Destroy(itemScripts[i].gameObject);
            }
        }

        // Remove Platform Beams
        GameObject[] beams = GameObject.FindGameObjectsWithTag("PlatformBeam");
        for (int i = 0; i < beams.Length; i++)
        {
            if (OutOfCameraView(beams[i].gameObject.transform.position) || removeAll)
            {
                // let the player use the magnet beam again
                player.GetComponent<PlayerController>().CanUseWeaponAgain();
                Destroy(beams[i]);
            }
        }

        // Remove Bombs
        GameObject[] bombs = GameObject.FindGameObjectsWithTag("Bomb");
        for (int i = 0; i < bombs.Length; i++)
        {
            if (OutOfCameraView(bombs[i].gameObject.transform.position) || removeAll)
            {
                // let the player throw bombs again
                if (bombs[i].name.Contains("Player"))
                {
                    player.GetComponent<PlayerController>().CanUseWeaponAgain();
                }
                Destroy(bombs[i]);
            }
        }

        // Remove Bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        for (int i = 0; i < bullets.Length; i++)
        {
            if (OutOfCameraView(bullets[i].gameObject.transform.position) || removeAll)
            {
                Destroy(bullets[i]);
            }
        }
    }

    void MoveGameObjects(GameObject go)
    {
        // move killerbomb and mambu to the other side of the view
        if (go.name.Equals("KillerBomb"))
        {
            go.transform.position = new Vector3(worldView.Right, playerPosition.y + tileSizeY);
            // reset killerbomb's following path
            go.GetComponent<KillerBombController>().ResetFollowingPath();
        }
        else if (go.name.Equals("Mambu"))
        {
            go.transform.position = new Vector3(worldView.Right, go.transform.position.y);
            // reset mambu's closed state so timing starts over
            go.GetComponent<MambuController>().SetState(MambuController.MambuState.Closed);
        }
    }

    void InstantiateKamadoma(string name, Vector3 position)
    {
        GameObject kamadoma = Instantiate(prefabKamadoma);
        kamadoma.name = name;
        kamadoma.transform.position = position;
        kamadoma.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Blue);
        kamadoma.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        kamadoma.GetComponent<KamadomaController>().SetColor(KamadomaController.KamadomaColors.Red);
        kamadoma.GetComponent<KamadomaController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateBombombLauncher(string name, Vector3 position)
    {
        GameObject bombombLauncher = Instantiate(prefabBombombLauncher);
        bombombLauncher.name = name;
        bombombLauncher.transform.position = position;
        bombombLauncher.GetComponent<BombombController>().SetLaunchOnStart(true);
        bombombLauncher.GetComponent<BombombController>().SetLaunchDelay(3.5f);
        bombombLauncher.GetComponent<BombombController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateScrewDriver(string name, Vector3 position, float range)
    {
        GameObject screwdriver = Instantiate(prefabScrewDriver);
        screwdriver.name = name;
        screwdriver.transform.position = position;
        screwdriver.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Blue);
        screwdriver.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        screwdriver.GetComponent<ScrewDriverController>().SetColor(ScrewDriverController.ScrewDriverColors.Blue);
        screwdriver.GetComponent<ScrewDriverController>().SetPlayerRange(range);
        screwdriver.GetComponent<ScrewDriverController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateBlaster(string name, Vector3 position, float range, float closedDelay, float startDelay,
        BlasterController.BlasterOrientation orientation)
    {
        GameObject blaster = Instantiate(prefabBlaster);
        blaster.name = name;
        blaster.transform.position = position;
        blaster.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Orange);
        blaster.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        blaster.GetComponent<BlasterController>().SetColor(BlasterController.BlasterColors.Orange);
        blaster.GetComponent<BlasterController>().SetOrientation(orientation);
        blaster.GetComponent<BlasterController>().SetPlayerRange(range);
        blaster.GetComponent<BlasterController>().SetClosedDelay(closedDelay);
        blaster.GetComponent<BlasterController>().SetStartDelay(startDelay);
        blaster.GetComponent<BlasterController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateSniperJoe(string name, Vector3 position, Vector2 velocity)
    {
        GameObject sniperJoe = Instantiate(prefabSniperJoe);
        sniperJoe.name = name;
        sniperJoe.transform.position = position;
        sniperJoe.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Orange);
        sniperJoe.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        sniperJoe.GetComponent<SniperJoeController>().SetJumpVector(velocity);
        sniperJoe.GetComponent<SniperJoeController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateKillerBomb(string name, Vector3 position)
    {
        GameObject killerBomb = Instantiate(prefabKillerBomb);
        killerBomb.name = name;
        killerBomb.transform.position = position;
        killerBomb.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Orange);
        killerBomb.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        killerBomb.GetComponent<KillerBombController>().SetColor(KillerBombController.KillerBombColors.Orange);
        killerBomb.GetComponent<KillerBombController>().SetMoveDirection(KillerBombController.MoveDirections.Left);
        killerBomb.GetComponent<KillerBombController>().EnableAI(true);
    }

    void InstantiateGabyoall(string name, Vector3 position, GabyoallController.MoveDirections direction)
    {
        GameObject gabyoall = Instantiate(prefabGabyoall);
        gabyoall.name = name;
        gabyoall.transform.position = position;
        gabyoall.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Orange);
        gabyoall.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        gabyoall.GetComponent<GabyoallController>().SetColor(GabyoallController.GabyoallColors.Orange);
        gabyoall.GetComponent<GabyoallController>().SetMoveDirection(direction);
        gabyoall.GetComponent<GabyoallController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateMambu(string name, Vector3 position)
    {
        GameObject mambu = Instantiate(prefabMambu);
        mambu.name = name;
        mambu.transform.position = position;
        mambu.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Orange);
        mambu.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        mambu.GetComponent<MambuController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateAdheringSuzy(string name, Vector3 position, bool sleep, float moveSpeed,
        Vector3 stopPosition1, Vector3 stopPosition2, AdheringSuzyController.Directions direction)
    {
        GameObject adheringSuzy = Instantiate(prefabAdheringSuzy);
        adheringSuzy.name = name;
        adheringSuzy.transform.position = position;
        adheringSuzy.GetComponent<EnemyController>().SetBonusBallColor(ItemScript.BonusBallColors.Red);
        adheringSuzy.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Random);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetColor(AdheringSuzyController.AdheringSuzyColors.Red);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetStopMethod(AdheringSuzyController.StopMethods.Position);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetMovement(AdheringSuzyController.Movements.Horizontal);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetStopPositions(stopPosition1, stopPosition2);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetSleepOnStart(sleep);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetDirection(direction);
        adheringSuzy.GetComponent<AdheringSuzyController>().SetMoveSpeed(moveSpeed);
        adheringSuzy.GetComponent<AdheringSuzyController>().EnableAI(true);
        objectActive[name] = true;
    }

    void InstantiateBombMan(string name, Vector3 position)
    {
        bombMan = Instantiate(prefabBombMan);
        bombMan.name = name;
        bombMan.transform.position = position;
        // we drop the weapon part on his defeat separately
        bombMan.GetComponent<EnemyController>().SetBonusItemType(ItemScript.ItemTypes.Nothing);
        // when the boss gets defeated we do stuff and importantly drop the weapon part
        bombMan.GetComponent<EnemyController>().DefeatEvent.AddListener(this.BossDefeated);
        // we enable his AI after the boss fight intro sequence
    }

    void InstantiateExtraLife(string name, Vector3 position)
    {
        // all extra lives have the same properties except for the name and position
        GameObject extraLife = Instantiate(prefabExtraLife);
        extraLife.name = name;
        extraLife.transform.position = position;
        objectActive[name] = true;
    }

    void InstantiateLifeEnergyBig(string name, Vector3 position)
    {
        // all big life energies have the same properties except for the name and position
        GameObject lifeEnergyBig = Instantiate(prefabLifeEnergyBig);
        lifeEnergyBig.name = name;
        lifeEnergyBig.transform.position = position;
        objectActive[name] = true;
    }

    void InstantiateLifeEnergySmall(string name, Vector3 position)
    {
        // all small life energies have the same properties except for the name and position
        GameObject lifeEnergySmall = Instantiate(prefabLifeEnergySmall);
        lifeEnergySmall.name = name;
        lifeEnergySmall.transform.position = position;
        objectActive[name] = true;
    }

    void InstantiateWeaponEnergyBig(string name, Vector3 position)
    {
        // all big weapon energies have the same properties except for the name and position
        GameObject weaponEnergyBig = Instantiate(prefabWeaponEnergyBig);
        weaponEnergyBig.name = name;
        weaponEnergyBig.transform.position = position;
        objectActive[name] = true;
    }

    void InstantiateWeaponPart(string name, Vector3 position, ItemScript.WeaponPartColors color)
    {
        // weapon part after defeating the boss
        GameObject weaponPart = Instantiate(prefabWeaponPart);
        weaponPart.name = name;
        weaponPart.transform.position = position;
        weaponPart.GetComponent<ItemScript>().SetWeaponPartColor(color);
        weaponPart.GetComponent<ItemScript>().SetWeaponPartEnemy(ItemScript.WeaponPartEnemies.BombMan);
        weaponPart.GetComponent<ItemScript>().BonusItemEvent.AddListener(this.WeaponPartCollected);
    }

    void SpawnKamadomas()
    {
        // Kamadoma1
        if ((Between(playerPosition.x, TileWorldPos(1), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(16), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma1") == null && !objectActive["Kamadoma1"])
        {
            InstantiateKamadoma("Kamadoma1", objectPosition["Kamadoma1"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(0), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(17), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma1") == null && objectActive["Kamadoma1"])
        {
            objectActive["Kamadoma1"] = false;
        }

        // Kamadoma2
        if ((Between(playerPosition.x, TileWorldPos(4), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(20), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma2") == null && !objectActive["Kamadoma2"])
        {
            InstantiateKamadoma("Kamadoma2", objectPosition["Kamadoma2"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(3), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(21), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma2") == null && objectActive["Kamadoma2"])
        {
            objectActive["Kamadoma2"] = false;
        }

        // Kamadoma3
        if ((Between(playerPosition.x, TileWorldPos(8), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(23), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma3") == null && !objectActive["Kamadoma3"])
        {
            InstantiateKamadoma("Kamadoma3", objectPosition["Kamadoma3"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(7), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(24), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma3") == null && objectActive["Kamadoma3"])
        {
            objectActive["Kamadoma3"] = false;
        }

        // Kamadoma4
        if ((Between(playerPosition.x, TileWorldPos(11), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(26), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma4") == null && !objectActive["Kamadoma4"])
        {
            InstantiateKamadoma("Kamadoma4", objectPosition["Kamadoma4"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(10), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(27), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma4") == null && objectActive["Kamadoma4"])
        {
            objectActive["Kamadoma4"] = false;
        }

        // Kamadoma5
        if ((Between(playerPosition.x, TileWorldPos(13), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(28), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma5") == null && !objectActive["Kamadoma5"])
        {
            InstantiateKamadoma("Kamadoma5", objectPosition["Kamadoma5"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(12), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(29), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("Kamadoma5") == null && objectActive["Kamadoma5"])
        {
            objectActive["Kamadoma5"] = false;
        }
    }

    void SpawnBombombLaunchers()
    {
        // BombombLauncher1
        if ((Between(playerPosition.x, TileWorldPos(35), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(50), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher1") == null && !objectActive["BombombLauncher1"])
        {
            InstantiateBombombLauncher("BombombLauncher1", objectPosition["BombombLauncher1"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(34), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(51), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher1") == null && objectActive["BombombLauncher1"])
        {
            objectActive["BombombLauncher1"] = false;
        }

        // BombombLauncher2
        if ((Between(playerPosition.x, TileWorldPos(43), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(58), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher2") == null && !objectActive["BombombLauncher2"])
        {
            InstantiateBombombLauncher("BombombLauncher2", objectPosition["BombombLauncher2"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(42), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(59), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher2") == null && objectActive["BombombLauncher2"])
        {
            objectActive["BombombLauncher2"] = false;
        }

        // BombombLauncher3
        if ((Between(playerPosition.x, TileWorldPos(51), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(66), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher3") == null && !objectActive["BombombLauncher3"])
        {
            InstantiateBombombLauncher("BombombLauncher3", objectPosition["BombombLauncher3"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(50), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(67), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher3") == null && objectActive["BombombLauncher3"])
        {
            objectActive["BombombLauncher3"] = false;
        }

        // BombombLauncher4
        if ((Between(playerPosition.x, TileWorldPos(59), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(74), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher4") == null && !objectActive["BombombLauncher4"])
        {
            InstantiateBombombLauncher("BombombLauncher4", objectPosition["BombombLauncher4"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(58), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(75), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("BombombLauncher4") == null && objectActive["BombombLauncher4"])
        {
            objectActive["BombombLauncher4"] = false;
        }
    }

    void SpawnScrewDrivers()
    {
        // ScrewDriver1
        if ((Between(playerPosition.x, TileWorldPos(72), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(87), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("ScrewDriver1") == null && !objectActive["ScrewDriver1"])
        {
            InstantiateScrewDriver("ScrewDriver1", objectPosition["ScrewDriver1"], 1f);
        }

        if ((Between(playerPosition.x, TileWorldPos(71), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(88), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("ScrewDriver1") == null && objectActive["ScrewDriver1"])
        {
            objectActive["ScrewDriver1"] = false;
        }

        // ScrewDriver2
        if (Between(playerPosition.x, TileWorldPos(79), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("ScrewDriver2") == null && !objectActive["ScrewDriver2"])
        {
            InstantiateScrewDriver("ScrewDriver2", objectPosition["ScrewDriver2"], 1f);
        }

        if (Between(playerPosition.x, TileWorldPos(78), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("ScrewDriver2") == null && objectActive["ScrewDriver2"])
        {
            objectActive["ScrewDriver2"] = false;
        }

        // ScrewDriver3
        if (Between(playerPosition.x, TileWorldPos(85), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("ScrewDriver3") == null && !objectActive["ScrewDriver3"])
        {
            InstantiateScrewDriver("ScrewDriver3", objectPosition["ScrewDriver3"], 1.5f);
        }

        if (Between(playerPosition.x, TileWorldPos(84), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("ScrewDriver3") == null && objectActive["ScrewDriver3"])
        {
            objectActive["ScrewDriver3"] = false;
        }
    }

    void SpawnSniperJoes()
    {
        // SniperJoe1
        if (Between(playerPosition.x, TileWorldPos(94), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("SniperJoe1") == null && !objectActive["SniperJoe1"])
        {
            InstantiateSniperJoe("SniperJoe1", objectPosition["SniperJoe1"], new Vector2(-0.8f, 0.25f));
        }

        if (Between(playerPosition.x, TileWorldPos(93), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("SniperJoe1") == null && objectActive["SniperJoe1"])
        {
            objectActive["SniperJoe1"] = false;
        }

        // SniperJoe2
        if (Between(playerPosition.x, TileWorldPos(203), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(60), halfScreenHeight) &&
            GameObject.Find("SniperJoe2") == null && !objectActive["SniperJoe2"])
        {
            InstantiateSniperJoe("SniperJoe2", objectPosition["SniperJoe2"], new Vector2(-0.8f, 0.25f));
        }

        if (Between(playerPosition.x, TileWorldPos(202), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(60), halfScreenHeight) &&
            GameObject.Find("SniperJoe2") == null && objectActive["SniperJoe2"])
        {
            objectActive["SniperJoe2"] = false;
        }

        // SniperJoe3
        if (Between(playerPosition.x, TileWorldPos(235), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(60), halfScreenHeight) &&
            GameObject.Find("SniperJoe3") == null && !objectActive["SniperJoe3"])
        {
            InstantiateSniperJoe("SniperJoe3", objectPosition["SniperJoe3"], new Vector2(-0.8f, 0.25f));
        }

        if (Between(playerPosition.x, TileWorldPos(234), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(60), halfScreenHeight) &&
            GameObject.Find("SniperJoe3") == null && objectActive["SniperJoe3"])
        {
            objectActive["SniperJoe3"] = false;
        }
    }

    void SpawnBlasters()
    {
        // Blaster5
        if ((Between(playerPosition.x, TileWorldPos(120), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(135), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Blaster5") == null && !objectActive["Blaster5"])
        {
            InstantiateBlaster("Blaster5", objectPosition["Blaster5"], 1.35f, 1f, 0f,
                BlasterController.BlasterOrientation.Right);
        }

        if ((Between(playerPosition.x, TileWorldPos(119), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(136), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Blaster5") == null && objectActive["Blaster5"])
        {
            objectActive["Blaster5"] = false;
        }

        // Blaster6
        if ((Between(playerPosition.x, TileWorldPos(128), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(143), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Blaster6") == null && !objectActive["Blaster6"])
        {
            InstantiateBlaster("Blaster6", objectPosition["Blaster6"], 1.35f, 1f, 0f,
                BlasterController.BlasterOrientation.Right);
        }

        if ((Between(playerPosition.x, TileWorldPos(127), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(144), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Blaster6") == null && objectActive["Blaster6"])
        {
            objectActive["Blaster6"] = false;
        }

        // Blaster7
        if ((Between(playerPosition.x, TileWorldPos(136), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(151), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Blaster7") == null && !objectActive["Blaster7"])
        {
            InstantiateBlaster("Blaster7", objectPosition["Blaster7"], 1.35f, 1f, 0f,
                BlasterController.BlasterOrientation.Right);
        }

        if ((Between(playerPosition.x, TileWorldPos(135), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(152), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Blaster7") == null && objectActive["Blaster7"])
        {
            objectActive["Blaster7"] = false;
        }
    }

    void SpawnKillerBombs()
    {
        // one KillerBomb that respawns
        if (!GameManager.Instance.InCameraTransition())
        {
            if (Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) ||
                Between(playerPosition.y, TileWorldPos(45), halfScreenHeight))
            {
                if (playerPosition.x > TileWorldPos(86, 0) &&
                    playerPosition.x < TileWorldPos(182, 0) &&
                    GameObject.Find("KillerBomb") == null && !objectActive["KillerBomb"] &&
                    canSpawnKillerBomb)
                {
                    // spawn KillerBomb after a short delay
                    if (!hasSpawnedKillerBomb)
                    {
                        hasSpawnedKillerBomb = true;
                        Invoke("SpawnKillerBombDelayed", delayKillerBomb);
                    }
                }

                if (playerPosition.x > TileWorldPos(86, 0) &&
                    playerPosition.x < TileWorldPos(182, 0) &&
                    GameObject.Find("KillerBomb") == null && objectActive["KillerBomb"])
                {
                    objectActive["KillerBomb"] = false;
                }

                // the player hitting these coords will make killerbomb spawn again
                if (Between(playerPosition.x, TileWorldPos(118), halfTileX) ||
                    Between(playerPosition.x, TileWorldPos(134), halfTileX))
                {
                    // he can be spawned again
                    canSpawnKillerBomb = true;
                    delayKillerBomb = 0f;
                }
            }
        }
    }

    public void CanSpawnKillerBomb()
    {
        // we can spawn killerbomb (used for the checkpoint too)
        canSpawnKillerBomb = true;
        midStartKillerBomb = true;
        delayKillerBomb = 0f;
    }

    void SpawnKillerBombDelayed()
    {
        // update what his new starting position will be and instantiate
        objectPosition["KillerBomb"] = new Vector3(worldView.Right, midStartKillerBomb ?
            TileWorldPos(45, 0) : playerPosition.y + tileSizeY);
        InstantiateKillerBomb("KillerBomb", objectPosition["KillerBomb"]);
        // reset the flags
        hasSpawnedKillerBomb = false;
        midStartKillerBomb = false;
        delayKillerBomb = 2.0f;
    }

    void SpawnGabyoalls()
    {
        // Gabyoall1
        if ((Between(playerPosition.x, TileWorldPos(155), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(170), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Gabyoall1") == null && !objectActive["Gabyoall1"])
        {
            InstantiateGabyoall("Gabyoall1", objectPosition["Gabyoall1"],
                GabyoallController.MoveDirections.Left);
        }

        if ((Between(playerPosition.x, TileWorldPos(154), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(171), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Gabyoall1") == null && objectActive["Gabyoall1"])
        {
            objectActive["Gabyoall1"] = false;
        }

        // Gabyoall2
        if (Between(playerPosition.x, TileWorldPos(161), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Gabyoall2") == null && !objectActive["Gabyoall2"])
        {
            InstantiateGabyoall("Gabyoall2", objectPosition["Gabyoall2"],
                GabyoallController.MoveDirections.Right);
        }

        if (Between(playerPosition.x, TileWorldPos(160), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Gabyoall2") == null && objectActive["Gabyoall2"])
        {
            objectActive["Gabyoall2"] = false;
        }

        // Gabyoall3
        if (Between(playerPosition.x, TileWorldPos(168), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Gabyoall3") == null && !objectActive["Gabyoall3"])
        {
            InstantiateGabyoall("Gabyoall3", objectPosition["Gabyoall3"],
                GabyoallController.MoveDirections.Left);
        }

        if (Between(playerPosition.x, TileWorldPos(167), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(30), halfScreenHeight) &&
            GameObject.Find("Gabyoall3") == null && objectActive["Gabyoall3"])
        {
            objectActive["Gabyoall3"] = false;
        }
    }

    void SpawnMambus()
    {
        // one Mambu that respawns
        if (Between(playerPosition.y, TileWorldPos(60), halfScreenHeight))
        {
            if (playerPosition.x > TileWorldPos(166, 0) &&
                playerPosition.x < TileWorldPos(201, 0) &&
                GameObject.Find("Mambu") == null && !objectActive["Mambu"] &&
                canSpawnMambu)
            {
                // spawn Mambu after a short delay
                if (!hasSpawnedMambu)
                {
                    hasSpawnedMambu = true;
                    Invoke("SpawnMambuDelayed", delayMambu);
                }
            }

            if (playerPosition.x > TileWorldPos(166, 0) &&
                playerPosition.x < TileWorldPos(201, 0) &&
                GameObject.Find("Mambu") == null && objectActive["Mambu"])
            {
                objectActive["Mambu"] = false;
            }

            // backtracking to this position and he'll spawn again
            if (Between(playerPosition.x, TileWorldPos(188), halfTileX))
            {
                // he can be spawned
                canSpawnMambu = true;
                delayMambu = 0f;
            }
        }
    }

    void SpawnMambuDelayed()
    {
        // update what his new starting X position will be and instantiate
        objectPosition["Mambu"] = new Vector3(worldView.Right, objectPosition["Mambu"].y);
        InstantiateMambu("Mambu", objectPosition["Mambu"]);
        // reset the flag and timer
        hasSpawnedMambu = false;
        delayMambu = 2.0f;
    }

    void SpawnBonusItems()
    {
        // LifeEnergySmall1
        if (Between(playerPosition.x, TileWorldPos(80), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("LifeEnergySmall1") == null && !objectActive["LifeEnergySmall1"])
        {
            InstantiateLifeEnergySmall("LifeEnergySmall1", objectPosition["LifeEnergySmall1"]);
        }

        if (Between(playerPosition.x, TileWorldPos(79), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("LifeEnergySmall1") == null && objectActive["LifeEnergySmall1"])
        {
            objectActive["LifeEnergySmall1"] = false;
        }

        // LifeEnergySmall2
        if (Between(playerPosition.x, TileWorldPos(81), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("LifeEnergySmall2") == null && !objectActive["LifeEnergySmall2"])
        {
            InstantiateLifeEnergySmall("LifeEnergySmall2", objectPosition["LifeEnergySmall2"]);
        }

        if (Between(playerPosition.x, TileWorldPos(80), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("LifeEnergySmall2") == null && objectActive["LifeEnergySmall2"])
        {
            objectActive["LifeEnergySmall2"] = false;
        }

        // WeaponEnergyBig1
        if (Between(playerPosition.x, TileWorldPos(91), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("WeaponEnergyBig1") == null && !objectActive["WeaponEnergyBig1"])
        {
            InstantiateWeaponEnergyBig("WeaponEnergyBig1", objectPosition["WeaponEnergyBig1"]);
        }

        if (Between(playerPosition.x, TileWorldPos(90), halfTileX) &&
            Between(playerPosition.y, TileWorldPos(0), halfScreenHeight) &&
            GameObject.Find("WeaponEnergyBig1") == null && objectActive["WeaponEnergyBig1"])
        {
            objectActive["WeaponEnergyBig1"] = false;
        }

        // ExtraLife1
        if ((Between(playerPosition.x, TileWorldPos(205), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(221), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(60), halfScreenHeight) &&
            GameObject.Find("ExtraLife1") == null && !objectActive["ExtraLife1"])
        {
            InstantiateExtraLife("ExtraLife1", objectPosition["ExtraLife1"]);
        }

        if ((Between(playerPosition.x, TileWorldPos(204), halfTileX) ||
            Between(playerPosition.x, TileWorldPos(222), halfTileX)) &&
            Between(playerPosition.y, TileWorldPos(60), halfScreenHeight) &&
            GameObject.Find("ExtraLife1") == null && objectActive["ExtraLife1"])
        {
            objectActive["ExtraLife1"] = false;
        }
    }

    void RandomizeAdheringSuzys()
    {
        // they switch up their movements in the original and I don't know if
        // there is a pattern to it so for my version I'll just randomize their
        // movements slightly so they aren't too predictable

        if (Between(playerPosition.x, TileWorldPos(253), halfScreenWidth))
        {
            if (!pickRandomSuzyMovement)
            {
                // pick a random delay
                pickRandomSuzyMovement = true;
                randomSuzyDelay = UnityEngine.Random.Range(5f, 10f);
            }
            else
            {
                randomSuzyDelay -= Time.deltaTime;
                if (randomSuzyDelay <= 0)
                {
                    // apply the randomness to the odd number suzy's
                    GameObject adheringSuzy1 = GameObject.Find("AdheringSuzy1");
                    GameObject adheringSuzy3 = GameObject.Find("AdheringSuzy3");
                    GameObject adheringSuzy5 = GameObject.Find("AdheringSuzy5");
                    GameObject adheringSuzy7 = GameObject.Find("AdheringSuzy7");

                    // as long as they haven't been destroyed then if sleeping, wake up
                    if (adheringSuzy1 != null)
                    {
                        if (adheringSuzy1.GetComponent<AdheringSuzyController>().IsSleeping())
                        {
                            adheringSuzy1.GetComponent<AdheringSuzyController>().Wake();
                            pickRandomSuzyMovement = false;
                        }
                    }
                    if (adheringSuzy3 != null)
                    {
                        if (adheringSuzy3.GetComponent<AdheringSuzyController>().IsSleeping())
                        {
                            adheringSuzy3.GetComponent<AdheringSuzyController>().Wake();
                            pickRandomSuzyMovement = false;
                        }
                    }
                    if (adheringSuzy5 != null)
                    {
                        if (adheringSuzy5.GetComponent<AdheringSuzyController>().IsSleeping())
                        {
                            adheringSuzy5.GetComponent<AdheringSuzyController>().Wake();
                            pickRandomSuzyMovement = false;
                        }
                    }
                    if (adheringSuzy7 != null)
                    {
                        if (adheringSuzy7.GetComponent<AdheringSuzyController>().IsSleeping())
                        {
                            adheringSuzy7.GetComponent<AdheringSuzyController>().Wake();
                            pickRandomSuzyMovement = false;
                        }
                    }
                }
            }
        }
    }

    public void PostTransitionEvent()
    {
        // the proper if/else will change their state
        canSpawnKillerBomb = false;
        canSpawnMambu = false;
        midStartKillerBomb = false;

        // (1st Transition) End of the first area - ScrewDrivers and Bonus Items
        if (Between(playerPosition.x, TileWorldPos(93), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(6), halfTileY))
        {
            // instantiate objects
            InstantiateScrewDriver("ScrewDriver2", objectPosition["ScrewDriver2"], 1f);
            InstantiateScrewDriver("ScrewDriver3", objectPosition["ScrewDriver3"], 1.5f);
            InstantiateLifeEnergySmall("LifeEnergySmall1", objectPosition["LifeEnergySmall1"]);
            InstantiateLifeEnergySmall("LifeEnergySmall2", objectPosition["LifeEnergySmall2"]);
            InstantiateWeaponEnergyBig("WeaponEnergyBig1", objectPosition["WeaponEnergyBig1"]);
            // reposition transition triggers
            camTransitions[0].transform.position = new Vector3(camTransitions[0].transform.position.x, 0.986f);
        }
        // (1st Transition) Start of the second area - Blasters and Bonus Item
        else if (Between(playerPosition.x, TileWorldPos(93), halfScreenWidth) &&
            (Between(playerPosition.y, TileWorldPos(7), halfTileY) ||
            Between(playerPosition.y, TileWorldPos(21), halfTileY)))
        {
            // instantiate objects
            InstantiateBlaster("Blaster1", objectPosition["Blaster1"], 2.5f, 1.25f, 0.7f,
                BlasterController.BlasterOrientation.Left);
            InstantiateBlaster("Blaster2", objectPosition["Blaster2"], 2.5f, 1.25f, 0f,
                BlasterController.BlasterOrientation.Left);
            InstantiateBlaster("Blaster3", objectPosition["Blaster3"], 2.5f, 1.25f, 0.7f,
                BlasterController.BlasterOrientation.Left);
            InstantiateBlaster("Blaster4", objectPosition["Blaster4"], 2.5f, 1.25f, 0f,
                BlasterController.BlasterOrientation.Left);
            InstantiateLifeEnergyBig("LifeEnergyBig1", objectPosition["LifeEnergyBig1"]);
            // reposition transition triggers
            camTransitions[0].transform.position = new Vector3(camTransitions[0].transform.position.x, 0.696f);
            camTransitions[1].transform.position = new Vector3(camTransitions[1].transform.position.x, 3.386f);
        }
        // (2nd Transition) Start of the third area - Sniper Joe
        else if (Between(playerPosition.x, TileWorldPos(93), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(22), halfTileY))
        {
            // reposition transition triggers
            camTransitions[1].transform.position = new Vector3(camTransitions[1].transform.position.x, 3.096f);
        }
        // (3rd Transition) End of the third area - Gabyoalls
        else if (Between(playerPosition.x, TileWorldPos(173), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(36), halfTileY))
        {
            // instantiate objects
            InstantiateGabyoall("Gabyoall2", objectPosition["Gabyoall2"],
                GabyoallController.MoveDirections.Right);
            InstantiateGabyoall("Gabyoall3", objectPosition["Gabyoall3"],
                GabyoallController.MoveDirections.Left);
            // reposition transition triggers
            camTransitions[2].transform.position = new Vector3(camTransitions[2].transform.position.x, 5.783f);

        }
        // (3rd Transition) Start of the 4th area - KillerBomb
        else if (Between(playerPosition.x, TileWorldPos(173), halfScreenWidth) &&
            (Between(playerPosition.y, TileWorldPos(37), halfTileY) ||
            Between(playerPosition.y, TileWorldPos(51), halfTileY)))
        {
            // call the spawn killerbomb method
            CanSpawnKillerBomb();
            // reposition transition triggers
            camTransitions[2].transform.position = new Vector3(camTransitions[2].transform.position.x, 5.493f);
            camTransitions[3].transform.position = new Vector3(camTransitions[3].transform.position.x, 8.201f);
        }
        // (4th Transition) Start of the 5th area - Mambu
        else if (Between(playerPosition.x, TileWorldPos(173), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(52), halfTileY))
        {
            // we can spawn mambu
            canSpawnMambu = true;
            delayMambu = 0f;
            // reposition transition triggers
            camTransitions[3].transform.position = new Vector3(camTransitions[3].transform.position.x, 7.911f);
        }
        // (5th Transition) Door to start of Boss area
        // (6th Transition) Boss area 1 - Top of the Ladder
        else if (Between(playerPosition.x, TileWorldPos(253), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(52), halfTileY))
        {
            // reposition transition triggers
            camTransitions[5].transform.position = new Vector3(camTransitions[5].transform.position.x, 7.88f);
        }
        // (6th Transition) Boss area 2 - Adhering Suzy Group 1
        else if (Between(playerPosition.x, TileWorldPos(253), halfScreenWidth) &&
            (Between(playerPosition.y, TileWorldPos(51), halfTileY) ||
            Between(playerPosition.y, TileWorldPos(37), halfTileY)))
        {
            // instantiate objects
            InstantiateAdheringSuzy("AdheringSuzy1", objectPosition["AdheringSuzy1_1"], true, 1.2f,
                objectPosition["AdheringSuzy1_1"], objectPosition["AdheringSuzy1_2"],
                AdheringSuzyController.Directions.Right);
            InstantiateAdheringSuzy("AdheringSuzy2", objectPosition["AdheringSuzy2_2"], false, 1.2f,
                objectPosition["AdheringSuzy2_1"], objectPosition["AdheringSuzy2_2"],
                AdheringSuzyController.Directions.Left);
            InstantiateAdheringSuzy("AdheringSuzy3", objectPosition["AdheringSuzy3_1"], true, 1.2f,
                objectPosition["AdheringSuzy3_1"], objectPosition["AdheringSuzy3_2"],
                AdheringSuzyController.Directions.Right);
            InstantiateAdheringSuzy("AdheringSuzy4", objectPosition["AdheringSuzy4_2"], false, 1.2f,
                objectPosition["AdheringSuzy4_1"], objectPosition["AdheringSuzy4_2"],
                AdheringSuzyController.Directions.Left);
            // reposition transition triggers
            camTransitions[5].transform.position = new Vector3(camTransitions[5].transform.position.x, 8.17f);
            camTransitions[6].transform.position = new Vector3(camTransitions[6].transform.position.x, 5.48f);
        }
        // (7th Transition) Boss area 3 - Adhering Suzy Group 2
        else if (Between(playerPosition.x, TileWorldPos(253), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(36), halfTileY))
        {
            // instantiate objects
            InstantiateAdheringSuzy("AdheringSuzy5", objectPosition["AdheringSuzy5_1"], true, 1.2f,
                objectPosition["AdheringSuzy5_1"], objectPosition["AdheringSuzy5_2"],
                AdheringSuzyController.Directions.Right);
            InstantiateAdheringSuzy("AdheringSuzy6", objectPosition["AdheringSuzy6_2"], false, 1.2f,
                objectPosition["AdheringSuzy6_1"], objectPosition["AdheringSuzy6_2"],
                AdheringSuzyController.Directions.Left);
            InstantiateAdheringSuzy("AdheringSuzy7", objectPosition["AdheringSuzy7_1"], true, 1.2f,
                objectPosition["AdheringSuzy7_1"], objectPosition["AdheringSuzy7_2"],
                AdheringSuzyController.Directions.Right);
            InstantiateAdheringSuzy("AdheringSuzy8", objectPosition["AdheringSuzy8_2"], false, 1.2f,
                objectPosition["AdheringSuzy8_1"], objectPosition["AdheringSuzy8_2"],
                AdheringSuzyController.Directions.Left);
            // reposition transition triggers
            camTransitions[6].transform.position = new Vector3(camTransitions[6].transform.position.x, 5.77f);
        }
        // (8th Transition) BombMan's Lair
        else if (Between(playerPosition.x, TileWorldPos(253), halfScreenWidth) &&
            Between(playerPosition.y, TileWorldPos(22), tileSizeY * 2f))
        {
            // get start time
            startTime = Time.time;
            // player enters lair and switch stage state
            levelState = LevelStates.BossFightIntro;
        }
    }

    bool Between(float a, float b, float margin)
    {
        // test if a is within the range of b
        return (a > (b - margin)) && (a < (b + margin));
    }

    float TileWorldPos(int tilePosition, float offset = 0.5f)
    {
        // the grid cell size is 0.16 for both the X & Y
        return (float)tilePosition * 0.16f + offset * 0.16f;
    }
}