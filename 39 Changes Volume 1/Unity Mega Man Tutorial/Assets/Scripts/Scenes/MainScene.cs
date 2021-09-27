using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainScene : MonoBehaviour
{
    // create an animated storyline
    float startTime;
    float runTime;

    // flag that we called for the next scene
    bool calledNextScene;

    // canvas stuff
    GameObject dialogueBox;
    Text runTimeText;
    TextMeshProUGUI dialogueText;
    TextMeshProUGUI screenMessageText;

    // general flags for the scene
    bool sniperJoeEnabled;
    bool skipDrLightDialogue;

    // show runtime text
    [SerializeField] bool showRunTime;

    // audio clips for the scene
    public AudioClip musicClip;
    public AudioClip bossFightClip;
    public AudioClip victoryThemeClip;

    // our characters and objects
    [SerializeField] GameObject player;
    [SerializeField] GameObject hologram;
    [SerializeField] GameObject enemy;
    [SerializeField] GameObject weaponPart;

    // x positions for our player
    [SerializeField] float startSniperJoePoint = -14.1f;
    [SerializeField] float startSeqBeginPoint1 = 5.6f;
    [SerializeField] float startSeqEndPoint1 = 7.0f;

    // wall and checkpoint trigger
    [SerializeField] GameObject wallLeft;
    [SerializeField] GameObject checkpointTrigger;
    [SerializeField] float wallLeftXPos1 = 11.5f;

    // boss battle world adjustments
    [SerializeField] float startSeqBeginPoint2 = 20f;
    [SerializeField] float startSeqEndPoint2 = 22.25f;
    [SerializeField] float wallLeftXPos2 = 21.35f;
    [SerializeField] float timeOffset = 0.1f;
    [SerializeField] Vector3 minCamBounds = new Vector3(22f, 0);
    [SerializeField] Vector3 maxCamBounds = new Vector3(22f, 0.3f);

    public enum LevelStates { Exploration, Hologram, KeepLooking, Checkpoint, BossFightIntro, BossFight, PlayerVictory, NextScene };
    public LevelStates levelState = LevelStates.Exploration;

    // scene dialogue strings
    string[] dialogueStrings = {
        "DR. LIGHT:\n\n\tMEGA MAN. I CAN'T STAY HERE LONG.",
        "DR. LIGHT:\n\n\tMY HOLOGRAM IS VERY UNSTABLE.",
        "MEGA MAN:\n\n\tDR. LIGHT, I HAVEN'T SEEN ANY DISTURBANCE.",
        "DR. LIGHT:\n\n\tTHERE HAS TO BE SOMETHING. KEEP LOOKING.",
        "MEGA MAN:\n\n\tOKAY. I'LL CONTACT YOU SOON."
    };

    void Awake()
    {
        // canvas objects
        dialogueBox = GameObject.Find("DialogueBox");
        runTimeText = GameObject.Find("RunTime").GetComponent<Text>();
        dialogueText = GameObject.Find("DialogueText").GetComponent<TextMeshProUGUI>();
        screenMessageText = GameObject.Find("ScreenMessage").GetComponent<TextMeshProUGUI>();
        // hide the dialogue box and empty the text
        dialogueText.text = "";
        dialogueBox.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        // 16:9 resolution bonus items
        GameManager.Instance.SetResolutionScale(GameManager.ResolutionScales.Scale16x9);

        // set palette for weapons menu
        GameManager.Instance.SetWeaponsMenuPalette(WeaponsMenu.MenuPalettes.CutMan);

        // how many points is this level worth
        // this is a placeholder until the stage/level select screen is built
        GameManager.Instance.SetLevelPoints(50000);

        // attach action function (or listening event) to weapon part being collected

        // use this method if the weapon part gets dropped from the enemy being defeated
        //enemy.GetComponent<EnemyController>().BonusItemAction += WeaponPartCollected;

        // we use this method if the weapon part is just a game object in the scene
        weaponPart.GetComponent<ItemScript>().BonusItemEvent.AddListener(WeaponPartCollected);

        // set up the scene music - 3/4 volume, loop, and play
        SoundManager.Instance.MusicSource.volume = 0.75f;
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
            case LevelStates.Exploration:
                if (player != null)
                {
                    if (player.transform.position.x >= startSniperJoePoint && !sniperJoeEnabled)
                    {
                        // find Sniper Joe and enable his AI
                        GameObject sniperJoe = GameObject.Find("SniperJoe");
                        if (sniperJoe != null)
                        {
                            sniperJoeEnabled = true;
                            sniperJoe.GetComponent<SniperJoeController>().EnableAI(true);
                        }
                    }

                    if (player.transform.position.x >= startSeqBeginPoint1 &&
                        player.transform.position.y < 0.3f)
                    {
                        // get start time
                        startTime = Time.time;
                        // freeze the player input and stop movement
                        player.GetComponent<PlayerController>().Invincible(true);
                        player.GetComponent<PlayerController>().FreezeInput(true);
                        Vector2 playerVelocity = player.GetComponent<Rigidbody2D>().velocity;
                        player.GetComponent<Rigidbody2D>().velocity = new Vector2(0, playerVelocity.y);
                        // freeze everything during little cutscene
                        GameManager.Instance.FreezeEverything(true);
                        // don't allow the game to be paused
                        GameManager.Instance.AllowGamePause(false);
                        // go to the dr. light hologram state
                        levelState = LevelStates.Hologram;
                    }

                    // warp ahead to skip the intro to speed along development
                    if (Input.GetKeyDown(KeyCode.W))
                    {
                        // move player, set new camera coords and bounds, advance level state
                        player.transform.position = new Vector2(18f, -1.14f);
                        Camera.main.transform.position = new Vector3(18f, 0, -10f);
                        Camera.main.GetComponent<CameraFollow>().boundsMin = new Vector3(12.2f, 0);
                        Camera.main.GetComponent<CameraFollow>().boundsMax = new Vector3(18f, 0.3f);
                        levelState = LevelStates.Checkpoint;
                    }
                }
                break;
            case LevelStates.Hologram:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // move player forward a bit and stop to show hologram
                if (UtilityFunctions.InTime(runTime, 2.0f, 5.0f))
                {
                    if (player.transform.position.x <= startSeqEndPoint1)
                    {
                        player.GetComponent<PlayerController>().SimulateMoveRight();
                    }
                    else
                    {
                        player.GetComponent<PlayerController>().SimulateMoveStop();
                    }
                }

                // allow the dr. light storyline to be skipped by pressing any key
                // start at the 5 second mark so megaman can run into position
                // advance ahead to 24 seconds where the dialogue is removed
                // and dr. light's hologram will flicker out
                if (UtilityFunctions.InTime(runTime, 5.0f, 24.0f))
                {
                    if (Input.anyKey)
                    {
                        // only allow this one time
                        if (!skipDrLightDialogue)
                        {
                            skipDrLightDialogue = true;
                            // advance the runtime marker
                            // and adjust the start time
                            runTime = 24.0f;
                            startTime = Time.time - runTime;
                        }
                    }
                }

                // dr. light says he can't stay long
                if (UtilityFunctions.InTime(runTime, 5.0f))
                {
                    dialogueBox.SetActive(true);
                    dialogueText.text = dialogueStrings[0];
                }

                // dr. light says his hologram is unstable
                if (UtilityFunctions.InTime(runTime, 9.0f))
                {
                    dialogueText.text = dialogueStrings[1];
                }

                // megaman says he hasn't seen any disturbance
                if (UtilityFunctions.InTime(runTime, 13.0f))
                {
                    dialogueText.text = dialogueStrings[2];
                }

                // dr. light says keep looking
                if (UtilityFunctions.InTime(runTime, 17.0f))
                {
                    dialogueText.text = dialogueStrings[3];
                }

                // megaman says he'll contact dr. light soon
                if (UtilityFunctions.InTime(runTime, 21.0f))
                {
                    dialogueText.text = dialogueStrings[4];
                }

                // hide dialogue box
                if (UtilityFunctions.InTime(runTime, 24.0f))
                {
                    dialogueText.text = "";
                    dialogueBox.SetActive(false);
                }

                // flicker out dr. light hologram
                if (UtilityFunctions.InTime(runTime, 25.0f))
                {
                    StartCoroutine(FlickerOutHologram());
                }

                // give user player control back and move to next state
                if (UtilityFunctions.InTime(runTime, 28.0f))
                {
                    player.GetComponent<PlayerController>().Invincible(false);
                    player.GetComponent<PlayerController>().FreezeInput(false);
                    // unfreeze everything after little cutscene
                    GameManager.Instance.FreezeEverything(false);
                    // allow the game to be paused
                    GameManager.Instance.AllowGamePause(true);
                    // explore some more and proceed to the checkpoint
                    levelState = LevelStates.KeepLooking;
                }
                break;
            case LevelStates.KeepLooking:
                // now out of the hologram state
                // we don't really do anything here 
                // we'll use our checkpoint function to switch states
                break;
            case LevelStates.Checkpoint:
                // player can't move back so we look for the player reaching 
                // x coordinate to activate the boss fight intro state
                if (player != null)
                {
                    if (player.transform.position.x >= startSeqBeginPoint2)
                    {
                        // get start time
                        startTime = Time.time;
                        // freeze the player input and stop movement
                        player.GetComponent<PlayerController>().FreezeInput(true);
                        Vector2 playerVelocity = player.GetComponent<Rigidbody2D>().velocity;
                        player.GetComponent<Rigidbody2D>().velocity = new Vector2(0, playerVelocity.y);
                        // don't allow the game to be paused
                        GameManager.Instance.AllowGamePause(false);
                        // go to the boss fight intro state
                        levelState = LevelStates.BossFightIntro;
                    }
                }
                break;
            case LevelStates.BossFightIntro:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // move player forward a bit and stop in front of the boss
                if (UtilityFunctions.InTime(runTime, 2.0f, 5.0f))
                {
                    if (player.transform.position.x <= startSeqEndPoint2)
                    {
                        player.GetComponent<PlayerController>().SimulateMoveRight();
                    }
                    else
                    {
                        player.GetComponent<PlayerController>().SimulateMoveStop();
                    }
                }

                // move the left wall to block in megaman and the boss plus change the camera bounds
                if (UtilityFunctions.InTime(runTime, 2.0f))
                {
                    // move the left wall
                    Vector3 wallPos = wallLeft.transform.position;
                    wallPos.x = wallLeftXPos2;
                    wallLeft.transform.position = wallPos;
                    // change the camera bounds and speed
                    // snap the camera to our boss fight area
                    Camera.main.GetComponent<CameraFollow>().timeOffset = timeOffset;
                    Camera.main.GetComponent<CameraFollow>().boundsMin = minCamBounds;
                    Camera.main.GetComponent<CameraFollow>().boundsMax = maxCamBounds;
                }

                // start fight music
                if (UtilityFunctions.InTime(runTime, 3.5f))
                {
                    SoundManager.Instance.StopMusic();
                    SoundManager.Instance.MusicSource.volume = 1f;
                    SoundManager.Instance.PlayMusic(bossFightClip);
                }

                // show the enemy health bar
                if (UtilityFunctions.InTime(runTime, 5.0f))
                {
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.EnemyHealth, 0);
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.EnemyHealth, UIEnergyBars.EnergyBarTypes.BombMan);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.EnemyHealth, true);
                }

                // do bombman's pose
                if (UtilityFunctions.InTime(runTime, 6.5f))
                {
                    enemy.GetComponent<BombManController>().Pose();
                }

                // fill enemy health bar and play sound clip
                if (UtilityFunctions.InTime(runTime, 7.0f))
                {
                    StartCoroutine(FillEnemyHealthBar());
                }

                // battle starts, enable boss ai and give player control
                if (UtilityFunctions.InTime(runTime, 8.5f))
                {
                    enemy.GetComponent<BombManController>().EnableAI(true);
                    player.GetComponent<PlayerController>().FreezeInput(false);
                    // allow the game to be paused
                    GameManager.Instance.AllowGamePause(true);
                    // move on to BossFight state
                    levelState = LevelStates.BossFight;
                }
                break;
            case LevelStates.BossFight:
                /*
                 * do stuff during the boss fight state (anything really)
                 *
                 * we have an event function that gets called when the boss is defeated and
                 * there is an action attached to the bonus item event listener (Weapon Part)
                 * when the player captures the weapon part then we can finish the level
                 *
                 * what we'll do during our boss fight is watch the music's time position
                 * and reset it to a position so it will constantly loop while the fight is
                 * going. if you listen to the music it's different in the beginning from
                 * where it loops. an alternative is to break the audio into two clips
                 * and play the "intro" first and then the "loop". I find it a little much.
                 * 
                 * The values I use for the clip loop start and end are guesstimates. Trying 
                 * to figure out precisely where a sound loops in an audio file is like meh.
                 */
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

                // reset the points collected and go to next scene state
                if (UtilityFunctions.InTime(runTime, 15.0f))
                {
                    GameManager.Instance.ResetPointsCollected(true, false);
                    // switch to the next scene state
                    levelState = LevelStates.NextScene;
                }
                break;
            case LevelStates.NextScene:
                // tell GameManager to trigger the next scene
                if (!calledNextScene)
                {
                    GameManager.Instance.StartNextScene(GameManager.GameScenes.MainScene);
                    calledNextScene = true;
                }
                break;
        }
    }

    // run tasks when player reaches Highway0 transition
    // trigger this through a camera transition event
    public void Highway0Reached()
    {
        // find Kamadoma2 and enable its AI
        GameObject kamadoma = GameObject.Find("Kamadoma2");
        if (kamadoma != null)
        {
            kamadoma.GetComponent<KamadomaController>().EnableAI(true);
        }

        // find BombombLauncher and disable its AI
        GameObject bombombLauncher = GameObject.Find("BombombLauncher");
        if (bombombLauncher != null)
        {
            bombombLauncher.GetComponent<BombombController>().EnableAI(false);
        }
    }

    // display a message that a checkpoint has been reached
    // trigger this through a camera transition post delay event
    //
    // *** now it's triggered through the checkpoint system event ***
    public void CheckpointReached()
    {
        StartCoroutine(CoCheckpointReached());
        // switch to the checkpoint state
        levelState = LevelStates.Checkpoint;
        // as an example we'll move a wall to block us in and disable the trigger
        Vector3 wallPos = wallLeft.transform.position;
        wallPos.x = wallLeftXPos1;
        wallLeft.transform.position = wallPos;
        //checkpointTrigger.SetActive(false);
    }

    private IEnumerator CoCheckpointReached()
    {
        // show checkpoint message on screen
        screenMessageText.alignment = TextAlignmentOptions.Center;
        screenMessageText.alignment = TextAlignmentOptions.Top;
        screenMessageText.fontStyle = FontStyles.UpperCase;
        screenMessageText.fontSize = 24;
        screenMessageText.text = "CHECKPOINT REACHED";
        // remove message after 5 seconds
        yield return new WaitForSeconds(5f);
        screenMessageText.text = "";
    }

    private IEnumerator FlickerOutHologram()
    {
        Color hologramColor;
        float delay1 = 0.15f;
        float delay2 = 0.025f;

        // get current color settings
        hologramColor = hologram.GetComponent<SpriteRenderer>().color;

        // first flicker speed
        for (int i = 0; i < 5; i++)
        {
            hologram.GetComponent<SpriteRenderer>().color = Color.clear;
            yield return new WaitForSeconds(delay1);
            hologram.GetComponent<SpriteRenderer>().color = hologramColor;
            yield return new WaitForSeconds(delay1);
        }

        // second flicker speed
        for (int i = 0; i < 10; i++)
        {
            hologram.GetComponent<SpriteRenderer>().color = Color.clear;
            yield return new WaitForSeconds(delay2);
            hologram.GetComponent<SpriteRenderer>().color = hologramColor;
            yield return new WaitForSeconds(delay2);
        }

        // remove hologram from scene
        Destroy(hologram);
    }

    private IEnumerator FillEnemyHealthBar()
    {
        // get enemy max health for bar calculation
        int maxHealth = enemy.GetComponent<EnemyController>().maxHealth;
        // loop the sound and play the repeat clip we generated
        SoundManager.Instance.Play(enemy.GetComponent<EnemyController>().energyFillClip, true);
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
        /* 
         * do anything required when the boss is defeated
         *
         * currently the weapon part is instantiated from the enemy controller
         * we could instantiate it here at a different location much like how in
         * the original game it spawns far above the player and falls to the floor
         *
         * EDIT
         * I have the weapon part as an object in the scene that will be activated
         * and fall to the ground upon the boss defeat - similar to the original game
         */
        // stop the music
        SoundManager.Instance.StopMusic();
        // hide the enemy health bar
        UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.EnemyHealth, false);
        // destroy all weapons
        GameManager.Instance.DestroyWeapons();
        // active weapon part
        weaponPart.SetActive(true);
    }

    private void WeaponPartCollected()
    {
        /*
         * this is our cue that the player has captured the weapon part and we
         * should signal the game manager to end the level, tally up the points, 
         * and move on to the level selection screen to pick another boss
         */
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
}