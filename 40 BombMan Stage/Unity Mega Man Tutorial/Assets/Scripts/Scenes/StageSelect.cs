using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSelect : MonoBehaviour
{
    // create an animated storyline
    float startTime;
    float runTime;
    float thisTime;

    // flag that we called for the next scene
    bool calledNextScene;

    // timer for use in different places
    float animationTimer;

    // number of blocks that are on the screen
    int maxEnemyBlocks;

    // flag for when text an blocks are white
    bool isWhite = true;

    // Dr. Wily stuff
    bool playDrWily;
    Vector3 wilyShipTopPos;
    float wilyShipTopClosingY;
    float wilyShipTopOpeningY = 62f;
    float wilyShipFlySpeed = 100f;
    float wilyShipLandSpeed = 50f;
    float wilyShipTopSpeed = 25f;
    enum DrWilyAnimation2States
    {
        HailingShip,
        ShipIncoming,
        OpenShip,
        LoadWily1,
        LoadWily2,
        CloseShip,
        ShipOutgoing
    };
    DrWilyAnimation2States drWilyAnimation2State = DrWilyAnimation2States.HailingShip;

    // index to move through animation waypoints
    int waypointIndex = 0;

    // allow stepping to prevent animation time overlaps
    int animationStep = 0;

    // bomb's original position
    Vector3 bombPosition;

    // choose from the available stage points
    int stagePointsSelection;
    int[] stagePointsArray = { 50000, 60000, 70000, 80000, 90000, 100000 };

    [Header("Scene Settings")]
    [SerializeField] CanvasScaler canvasScaler;
    [SerializeField] int enemySelection = 0;
    [SerializeField] float blockFlashDelay = 0.2f;
    [SerializeField] float whiteScreenFlashDelay = 0.15f;
    [SerializeField] float typewriterTextDelay = 0.05f;
    [SerializeField] float animationMoveSpeed = 200f;
    [SerializeField] float jumpLandingPosY = -16f;
    [SerializeField] Vector2 bombTossVelocity = new Vector2(0, 2f);

    [Header("Audio Clips")]
    [SerializeField] AudioClip sceneMusicClip;
    [SerializeField] AudioClip bossMusicClip;
    [SerializeField] AudioClip drWilyShipClip;
    [SerializeField] AudioClip menuSelectClip;
    [SerializeField] AudioClip gameStartClip;

    [Header("Background Objects")]
    [SerializeField] GameObject bomb;
    [SerializeField] GameObject wilyShip;
    [SerializeField] GameObject wilyShipGroup;
    [SerializeField] GameObject wilyShipTop;
    [SerializeField] GameObject wilyShipBottom;
    [SerializeField] GameObject blueBar;
    [SerializeField] GameObject whiteFlash;

    [Header("Enemies & Blocks")]
    [SerializeField] GameObject[] enemyChars;
    [SerializeField] GameObject[] enemyBlocks;

    [SerializeField] Sprite[] blocksActiveBlue;
    [SerializeField] Sprite[] blocksActiveWhite;
    [SerializeField] Sprite[] blocksInactiveBlue;
    [SerializeField] Sprite[] blocksInactiveWhite;

    [Header("Waypoints & Jump Vectors")]
    [SerializeField] Transform[] WilyShipWaypoints;

    [SerializeField] Vector2[] enemyJumpVectors = new Vector2[7];
    /* 
     * default jump vectors for 16:9 / orthographic 1.2
     * CutMan : (0, 2.8f)
     * GutsMan: (-0.83f, 2.1f)
     * IceMan : (-1.24f, 3.1f)
     * BombMan: (-0.74f, 4.1f)
     * FireMan: (0, 4f)
     * ElecMan: (0.38f, 3.3f)
     * Dr.Wily: (-0.38f, 3.3f)
     */

    [Header("Stage Select Text")]
    [SerializeField] TextMeshProUGUI tmpSelectText;
    [SerializeField]
    [TextArea(5, 10)]
    string strSelectText =
@"<mspace=""{0}""><color={1}>SELECT
STAGE

PRESS
ENTER</color></mspace>";
    [SerializeField] string hexBlue = "#9CFCF0";
    [SerializeField] string hexWhite = "#FFFFFF";

    [Header("Points Text")]
    [SerializeField] TextMeshProUGUI tmpEnemyClearPoints;
    [SerializeField]
    [TextArea(5, 10)]
    string strEnemyClearPoints =
@"<mspace=""{0}""><color={1}>{2}</color></mspace>";
    [SerializeField] TextMeshProUGUI tmpPointsTallyText;
    [SerializeField]
    [TextArea(5, 10)]
    string strPointsTallyText =
@"<mspace=""{0}""><color={1}>{2}</color></mspace>";

    private enum SceneStates { EnemySelection, IntroAnimation, ExitAnimation, NextScene };
    private SceneStates sceneState = SceneStates.EnemySelection;

    void Awake()
    {
        // adjust settings based on set resolution scale
        switch (GameManager.Instance.GetResolutionScale())
        {
            case GameManager.ResolutionScales.Scale4x3:
                // set canvas scaler resolution
                canvasScaler.referenceResolution = new Vector2(480, 360);
                // move bomb to new position and set new velocity
                bomb.transform.localPosition = new Vector3(-38f, 9.5f);
                bombTossVelocity = new Vector2(0, 2.3f);
                // update all enemy jump vectors
                enemyJumpVectors[0] = new Vector2(0, 2.8f);
                enemyJumpVectors[1] = new Vector2(-0.95f, 2.3f);
                enemyJumpVectors[2] = new Vector2(-1.363f, 3.5f);
                enemyJumpVectors[3] = new Vector2(-0.875f, 4.5f);
                enemyJumpVectors[4] = new Vector2(0, 4.4f);
                enemyJumpVectors[5] = new Vector2(0.453f, 3.6f);
                enemyJumpVectors[6] = new Vector2(-0.453f, 3.6f);
                // move ship and start and end positions
                wilyShip.transform.localPosition = new Vector3(-285f, 41.75f);
                WilyShipWaypoints[0].transform.localPosition = new Vector3(-285f, 41.75f);
                WilyShipWaypoints[4].transform.localPosition = new Vector3(285f, 41.75f);
                break;
        }

        // initialize timer for alternating block coloring/flashing
        animationTimer = blockFlashDelay;

        // assume Dr. Wily is available and initialize his stuff
        playDrWily = true;
        maxEnemyBlocks = (int)GameManager.StagesList.DrWily1;
        enemyChars[maxEnemyBlocks].SetActive(true);
        enemyChars[maxEnemyBlocks].GetComponent<Animator>().speed = 0;
        enemyChars[maxEnemyBlocks].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        enemyBlocks[maxEnemyBlocks].SetActive(true);
        enemyBlocks[maxEnemyBlocks].GetComponent<Image>().sprite = blocksActiveBlue[maxEnemyBlocks];

        // hide the bomb and Dr. Wily's ship (and I ended up not using the ship bottom)
        bomb.SetActive(false);
        wilyShip.SetActive(false);
        wilyShipGroup.SetActive(false);
        wilyShipBottom.SetActive(false);

        // blue bar and white flash are hidden
        blueBar.GetComponent<CanvasGroup>().alpha = 0f;
        whiteFlash.GetComponent<CanvasGroup>().alpha = 0f;

        // init the stage select text and hide it
        tmpSelectText.text = String.Format(strSelectText, tmpSelectText.fontSize, hexBlue);
        tmpSelectText.gameObject.SetActive(false);

        // these texts should not be visible until the intro animations
        tmpEnemyClearPoints.gameObject.SetActive(false);
        tmpPointsTallyText.gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        // run through the stages and setup
        for (int i = 0; i <= (int)GameManager.StagesList.ElecMan; i++)
        {
            // set enemy animator speed to zero and freeze all rigidbody constraints
            enemyChars[i].GetComponent<Animator>().speed = 0;
            enemyChars[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            // check for level completion
            if (GameManager.Instance.GameStages[i].Completed)
            {
                // initialize with blue inactive block
                enemyBlocks[i].GetComponent<Image>().sprite = (i == enemySelection) ?
                    blocksInactiveWhite[i] : blocksInactiveBlue[i];
            }
            else
            {
                // initialize with blue active block
                enemyBlocks[i].GetComponent<Image>().sprite = (i == enemySelection) ?
                    blocksActiveWhite[i] : blocksActiveBlue[i];
                // last boss index (ElecMan should be before Dr. Wily)
                // disable Dr. Wily and enable the stage select text
                playDrWily = false;
                maxEnemyBlocks = (int)GameManager.StagesList.ElecMan;
                enemyChars[(int)GameManager.StagesList.DrWily1].SetActive(false);
                enemyBlocks[(int)GameManager.StagesList.DrWily1].SetActive(false);
                // show the stage select text and set it to white
                tmpSelectText.gameObject.SetActive(true);
                tmpSelectText.text = String.Format(strSelectText, tmpSelectText.fontSize, hexWhite);
            }
        }

        // start up the scene background music
        SoundManager.Instance.MusicSource.volume = 1.0f;
        SoundManager.Instance.PlayMusic(sceneMusicClip);
    }

    // Update is called once per frame
    void Update()
    {
        switch (sceneState)
        {
            case SceneStates.EnemySelection:
                EnemySelection();
                break;
            case SceneStates.IntroAnimation:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // prepare the intro and flash the screen white
                if (UtilityFunctions.InTime(runTime, 0.001f))
                {
                    PrepareAnimatedIntro();
                    StartCoroutine(FlashWhiteScreen());
                }

                // during these ~3 seconds play the selected enemy animation
                if (UtilityFunctions.UntilTime(runTime, 3.2f))
                {
                    PlayEnemyAnimations();
                }

                // do the typewriter text and stage points tally
                if (UtilityFunctions.InTime(runTime, 3.2f))
                {
                    StartCoroutine(PlayScoreAnimations());
                }

                // music is done playing
                if (UtilityFunctions.InTime(runTime, 7.5f))
                {
                    // music stopped playing @ ~7.5s
                    if (playDrWily && (enemySelection == (int)GameManager.StagesList.DrWily1))
                    {
                        // Dr. Wily has an exit animation so we switch to that
                        sceneState = SceneStates.ExitAnimation;
                        // reset the start time for the exit animation sequence
                        startTime = Time.time;
                    }
                    else
                    {
                        // all other the enemies end the scene
                        sceneState = SceneStates.NextScene;
                    }
                }
                break;
            case SceneStates.ExitAnimation:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // play the second Dr. Wily animation sequence
                PlayDrWilyAnimation2();
                break;
            case SceneStates.NextScene:
                // tell GameManager to trigger the next scene
                if (!calledNextScene)
                {
                    // it's important to note that if you get to Wily Stage 2 and up and run out of lives
                    // if you choose Stage Select on the Game Over screen, if you select Dr. Wily again you
                    // start all over on Wily Stage 1 -- it makes it easier because now we don't have to
                    // figure out which Wily Stage we left off on -- always start at Stage 1
                    GameManager.Instance.SetLevelPoints(stagePointsSelection);
                    GameManager.Instance.StartNextScene(GameManager.Instance.GameStages[enemySelection].GameScene);
                    calledNextScene = true;
                }
                break;
        }
    }

    string GetEnemyName(int stage)
    {
        // as long as the stage names match the enemy names (Dr. Wily the exception here)
        string enemyName = Enum.GetName(typeof(GameManager.StagesList), stage).ToUpper();
        if (enemyName == "DRWILY1") enemyName = "DR.WILY";
        return enemyName;
    }

    string GetEnemyName(GameManager.StagesList stage)
    {
        // as long as the stage names match the enemy names (Dr. Wily the exception here)
        string enemyName = stage.ToString().ToUpper();
        if (enemyName == "DRWILY1") enemyName = "DR.WILY";
        return enemyName;
    }

    int PickRandomStagePoints()
    {
        // random point selection except for Dr. Wily
        if (playDrWily && (enemySelection == (int)GameManager.StagesList.DrWily1))
        {
            // return 200k for him (although you don't get it until defeating his last stage)
            return 200000;
        }
        else
        {
            // return random stage points from array
            return stagePointsArray[UnityEngine.Random.Range(0, stagePointsArray.Length)];
        }
    }

    void EnemySelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // stay within range
            if (enemySelection++ >= maxEnemyBlocks)
            {
                enemySelection = 0;
            }
            // update all the enemy blocks and stage select text
            UpdateEnemyBlocksAndText();
            // reset the block flash timer
            animationTimer = blockFlashDelay;
            // play the menu select clip
            SoundManager.Instance.Play(menuSelectClip);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // stay within range
            if (--enemySelection < 0)
            {
                enemySelection = maxEnemyBlocks;
            }
            // update all the enemy blocks and stage select text
            UpdateEnemyBlocksAndText();
            // reset the block flash timer
            animationTimer = blockFlashDelay;
            // play the menu select clip
            SoundManager.Instance.Play(menuSelectClip);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            // track when we started
            startTime = Time.time;
            // choose random stage points
            stagePointsSelection = PickRandomStagePoints();
            // switch to the animations state
            sceneState = SceneStates.IntroAnimation;
        }

        // here we flash the currently selected enemy block
        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0)
        {
            FlashEnemyBlockAndText();
        }
    }

    void UpdateEnemyBlocksAndText()
    {
        // first run through all the blocks to set the base as blue outline -- yellow(active) or black(inactive)
        for (int i = 0; i <= maxEnemyBlocks; i++)
        {
            // completed stages will have the black(inactive) block
            enemyBlocks[i].GetComponent<Image>().sprite =
                GameManager.Instance.GameStages[i].Completed ? blocksInactiveBlue[i] : blocksActiveBlue[i];
            // Dr. Wily is always active regardless of any of his stages being completed
            if (playDrWily && (i == (int)GameManager.StagesList.DrWily1))
            {
                // blue outline -- yellow(active) block
                enemyBlocks[i].GetComponent<Image>().sprite = blocksActiveBlue[i];
            }
        }

        // now the update the currently selected enemy's block to have the white outline version
        // the timer will flash between the white and blue outline blocks and the stage select text
        isWhite = true;
        enemyBlocks[enemySelection].GetComponent<Image>().sprite =
            (GameManager.Instance.GameStages[enemySelection].Completed) ?
            blocksInactiveWhite[enemySelection] : blocksActiveWhite[enemySelection];
        // Dr. Wily is always active regardless of any of his stages being completed
        if (playDrWily && (enemySelection == (int)GameManager.StagesList.DrWily1))
        {
            // white outline -- yellow(active) block
            enemyBlocks[enemySelection].GetComponent<Image>().sprite = blocksActiveWhite[enemySelection];
        }
        else
        {
            // no Dr. Wily means we're using the stage select text
            tmpSelectText.text = String.Format(strSelectText, tmpSelectText.fontSize, hexWhite);
        }
    }

    void FlashEnemyBlockAndText()
    {
        // flip white
        isWhite = !isWhite;
        // get the blue and white outline blocks and either the yellow(active) or black(inactive)
        // depending on whether the enemy level has been completed and save them in a couplee sprites
        Sprite blockBlue = (GameManager.Instance.GameStages[enemySelection].Completed) ?
            blocksInactiveBlue[enemySelection] : blocksActiveBlue[enemySelection];
        Sprite blockWhite = (GameManager.Instance.GameStages[enemySelection].Completed) ?
            blocksInactiveWhite[enemySelection] : blocksActiveWhite[enemySelection];
        // now update the sprite to be either the white or blue outline block
        enemyBlocks[enemySelection].GetComponent<Image>().sprite = isWhite ? blockWhite : blockBlue;
        // Dr. Wily is always active regardless of any of his stages being completed
        if (playDrWily && (enemySelection == (int)GameManager.StagesList.DrWily1))
        {
            // show either the white outline block or the blue one for Dr. Wily
            enemyBlocks[enemySelection].GetComponent<Image>().sprite = isWhite ?
                blocksActiveWhite[enemySelection] : blocksActiveBlue[enemySelection];
        }
        else
        {
            // no Dr. Wily so we show change the color of the stage select text 
            tmpSelectText.text = String.Format(strSelectText, tmpSelectText.fontSize, isWhite ? hexWhite : hexBlue);
        }
        // reset the block flash timer
        animationTimer = blockFlashDelay;
    }

    void PrepareAnimatedIntro()
    {
        // set animation step to zero
        animationStep = 0;
        // hide all the blocks and all the enemies but the one we selected
        for (int i = 0; i <= maxEnemyBlocks; i++)
        {
            enemyBlocks[i].SetActive(false);
            if (i != enemySelection)
            {
                enemyChars[i].SetActive(false);
            }
        }
        // hide the selection text
        tmpSelectText.gameObject.SetActive(false);
        // show the blue bar
        blueBar.GetComponent<CanvasGroup>().alpha = 1f;
        // play boss select music and game start clip
        SoundManager.Instance.PlayMusic(bossMusicClip, false);
        SoundManager.Instance.Play(gameStartClip);
    }

    private IEnumerator FlashWhiteScreen()
    {
        // flash the white overlay to cover the screen except for the enemy
        for (int i = 0; i < 5; i++)
        {
            whiteFlash.GetComponent<CanvasGroup>().alpha = 1f;
            yield return new WaitForSeconds(whiteScreenFlashDelay);
            whiteFlash.GetComponent<CanvasGroup>().alpha = 0f;
            yield return new WaitForSeconds(whiteScreenFlashDelay);
        }
    }

    void PlayEnemyAnimations()
    {
        // each enemy animation is in its own function so this doesn't get too cluttered
        // and it'll check whether it needs to do its thing by checking the enemy selection
        PlayCutManAnimation();
        PlayGutsManAnimation();
        PlayIceManAnimation();
        PlayBombManAnimation();
        PlayFireManAnimation();
        PlayElecManAnimation();
        // play the first Dr. Wily animation
        PlayDrWilyAnimation1();
    }

    void PlayCutManAnimation()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.CutMan)
        {
            // cutman animation has 16 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the jump (frame #2)
                enemyChars[enemySelection].GetComponent<Animator>().Play("CutMan_Intro", 0, 0.066f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // play rest of his animation (frame #3+)
                        enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                        enemyChars[enemySelection].GetComponent<Animator>().Play("CutMan_Intro", 0, 0.133f);
                        // set the next animation step (there isn't one for him)
                        animationStep = 2;
                    }
                }
            }
        }
    }

    void PlayGutsManAnimation()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.GutsMan)
        {
            // gutsman animation has 13 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the initial jump (frame #2)
                enemyChars[enemySelection].GetComponent<Animator>().Play("GutsMan_Intro", 0, 0.0833f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // have him face the other direction
                        enemyChars[enemySelection].transform.Rotate(0, 180f, 0);
                        // show animation (frame #4)
                        enemyChars[enemySelection].GetComponent<Animator>().Play("GutsMan_Intro", 0, 0.25f);
                        // set the next animation step
                        animationStep = 2;
                        // capture the current run time to finish the animation from this point
                        thisTime = runTime;
                    }
                }
            }
            // this will happen one time while he is jumping before he lands
            if (UtilityFunctions.InTime(runTime, 0.1f))
            {
                // show the actual jump (frame #3)
                enemyChars[enemySelection].GetComponent<Animator>().Play("GutsMan_Intro", 0, 0.166f);
            }
            // he's landed now play this part of the animation when the time comes
            if (animationStep == 2)
            {
                // stand up from landing
                if (UtilityFunctions.InTime(runTime, thisTime + 0.2f))
                {
                    // show animation (frame #5)
                    enemyChars[enemySelection].GetComponent<Animator>().Play("GutsMan_Intro", 0, 0.333f);
                    // set the next animation step
                    animationStep = 3;
                }
            }
            // reached the last part of his animation
            if (animationStep == 3)
            {
                // laughing and shoulder movement
                if (UtilityFunctions.InTime(runTime, thisTime + 0.7f))
                {
                    // play the rest of his animation (frame #5+)
                    enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                    enemyChars[enemySelection].GetComponent<Animator>().Play("GutsMan_Intro", 0, 0.333f);
                }
            }
        }
    }

    void PlayIceManAnimation()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.IceMan)
        {
            // iceman animation has 4 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the jump (frame #2)
                enemyChars[enemySelection].GetComponent<Animator>().Play("IceMan_Intro", 0, 0.333f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // have him face the other direction
                        enemyChars[enemySelection].transform.Rotate(0, 180f, 0);
                        // show animation (frame #1)
                        enemyChars[enemySelection].GetComponent<Animator>().Play("IceMan_Intro", 0, 0f);
                        // set the next animation step
                        animationStep = 2;
                        // capture the current run time to finish the animation from this point
                        thisTime = runTime;
                    }
                }
            }
            // he's landed now play this part of the animation when the time comes
            if (animationStep == 2)
            {
                // throwing his arm up
                if (UtilityFunctions.InTime(runTime, thisTime + 1f))
                {
                    // play the rest of his animation (frame #3+)
                    enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                    enemyChars[enemySelection].GetComponent<Animator>().Play("IceMan_Intro", 0, 0.666f);
                }
            }
        }
    }

    void PlayBombManAnimation()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.BombMan)
        {
            // bombman animation has 5 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the jump (frame #2)
                enemyChars[enemySelection].GetComponent<Animator>().Play("BombMan_Intro", 0, 0.25f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // have him face the other direction
                        enemyChars[enemySelection].transform.Rotate(0, 180f, 0);
                        // show animation (frame #3)
                        enemyChars[enemySelection].GetComponent<Animator>().Play("BombMan_Intro", 0, 0.5f);
                        // set the next animation step
                        animationStep = 2;
                        // capture the current run time to finish the animation from this point
                        thisTime = runTime;
                    }
                }
            }
            // he's landed now play this part of the animation when the time comes
            if (animationStep == 2)
            {
                // show the bomb now
                if (UtilityFunctions.InTime(runTime, thisTime + 0.5f))
                {
                    // get bomb original position and set it active
                    bomb.SetActive(true);
                    bombPosition = bomb.transform.localPosition;
                }
                // throwing his arm up pose
                if (UtilityFunctions.InTime(runTime, thisTime + 1.25f))
                {
                    // play the rest of his animation (frame #3+)
                    enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                    enemyChars[enemySelection].GetComponent<Animator>().Play("BombMan_Intro", 0, 0.5f);
                }
                // toss the bomb up
                if (UtilityFunctions.InTime(runTime, thisTime + 1.35f))
                {
                    // unfreeze the bomb and give it a velocity to lift
                    bomb.GetComponent<Rigidbody2D>().constraints =
                        RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                    bomb.GetComponent<Rigidbody2D>().velocity = bombTossVelocity;
                }
                // watch for the bomb coming back down
                if (UtilityFunctions.OverTime(runTime, thisTime + 1.5f))
                {
                    // move the bomb back to its original position and freeze it
                    if (bomb.transform.localPosition.y <= bombPosition.y)
                    {
                        bomb.transform.localPosition = bombPosition;
                        bomb.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    }
                }
            }
        }
    }

    void PlayFireManAnimation()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.FireMan)
        {
            // fireman animation has 12 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the jump (frame #2)
                enemyChars[enemySelection].GetComponent<Animator>().Play("FireMan_Intro", 0, 0.0909f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // play rest of his animation (frame #3+)
                        enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                        enemyChars[enemySelection].GetComponent<Animator>().Play("FireMan_Intro", 0, 0.1818f);
                        // set the next animation step (there isn't one for him)
                        animationStep = 2;
                    }
                }
            }
        }
    }

    void PlayElecManAnimation()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.ElecMan)
        {
            // elecman animation has 6 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the jump (frame #2)
                enemyChars[enemySelection].GetComponent<Animator>().Play("ElecMan_Intro", 0, 0.2f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // show animation (frame #3)
                        enemyChars[enemySelection].GetComponent<Animator>().Play("ElecMan_Intro", 0, 0.4f);
                        // set the next animation step
                        animationStep = 2;
                        // capture the current run time to finish the animation from this point
                        thisTime = runTime;
                    }
                }
            }
            // he's landed now play this part of the animation when the time comes
            if (animationStep == 2)
            {
                // throwing his arm up once
                if (UtilityFunctions.InTime(runTime, thisTime + 1f))
                {
                    // show animation (frame #3)
                    enemyChars[enemySelection].GetComponent<Animator>().Play("ElecMan_Intro", 0, 0.4f);
                }
                // throwing his arms up all the way through
                if (UtilityFunctions.InTime(runTime, thisTime + 1.2f))
                {
                    // play the rest of his animation (frame #3+)
                    enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                    enemyChars[enemySelection].GetComponent<Animator>().Play("ElecMan_Intro", 0, 0.4f);
                }
            }
        }
    }

    void PlayDrWilyAnimation1()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.DrWily1)
        {
            // Dr. Wily animation has 8 frames
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // show the jump aka idle (frame #1)
                enemyChars[enemySelection].GetComponent<Animator>().Play("DrWily_Intro", 0, 0.1428f);
                // set him in motion - use the jump velocity vector
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity = enemyJumpVectors[enemySelection];
                // set the first animation step
                animationStep = 1;
            }
            // in his jump sequence - watch for the landing
            if (UtilityFunctions.InTime(runTime, 0.001f, 3.0f))
            {
                if (animationStep == 1)
                {
                    // watch his Y position
                    if (enemyChars[enemySelection].GetComponent<Rigidbody2D>().velocity.y <= 0 &&
                        enemyChars[enemySelection].transform.localPosition.y < jumpLandingPosY)
                    {
                        // set him to the Y position
                        enemyChars[enemySelection].transform.localPosition =
                            new Vector3(enemyChars[enemySelection].transform.localPosition.x, jumpLandingPosY);
                        enemyChars[enemySelection].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        // have him face the other direction
                        enemyChars[enemySelection].transform.Rotate(0, 180f, 0);
                        // set the next animation step (there isn't one for him)
                        animationStep = 2;
                    }
                }
            }
        }
    }

    void PlayDrWilyAnimation2()
    {
        // find frame in normalization time
        // Normal = (currentValue – minValue) / (maxValue – minvalue)
        if (enemySelection == (int)GameManager.StagesList.DrWily1)
        {
            // first thing we do is hide the clear points, score tally, flip Dr. Wily
            if (UtilityFunctions.InTime(runTime, 0.001f))
            {
                // hide the texts
                tmpEnemyClearPoints.gameObject.SetActive(false);
                tmpPointsTallyText.gameObject.SetActive(false);
                // flip Dr. Wily to face the other direction
                enemyChars[enemySelection].transform.Rotate(0, 180f, 0);
                // play the animation (frame #1+) 0/1
                enemyChars[enemySelection].GetComponent<Animator>().speed = 1;
                enemyChars[enemySelection].GetComponent<Animator>().Play("DrWily_Intro", 0, 0.1482f);
                // Dr. Wily hails his ship
                drWilyAnimation2State = DrWilyAnimation2States.HailingShip;
            }
            // play out the UFO Ship animation
            if (UtilityFunctions.OverTime(runTime, 0.001f))
            {
                switch (drWilyAnimation2State)
                {
                    case DrWilyAnimation2States.HailingShip:
                        // hailing his UFO Ship
                        if (enemyChars[enemySelection].GetComponent<Animator>().
                            GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.14f)
                        {
                            // done hailing Wily Ship
                            drWilyAnimation2State = DrWilyAnimation2States.ShipIncoming;
                            // reset the wayoint index
                            waypointIndex = 0;
                            // use a slower speed for the ship flying
                            animationMoveSpeed = wilyShipFlySpeed;
                            // stop his animation
                            enemyChars[enemySelection].GetComponent<Animator>().speed = 0;
                            enemyChars[enemySelection].GetComponent<Animator>().Play("DrWily_Intro", 0, 0.1482f);
                            // enable Dr. Wily's Ship
                            wilyShip.SetActive(true);
                            // capture the current run time to finish the animation from this point
                            thisTime = runTime;
                            // play the UFO Ship clip
                            SoundManager.Instance.PlayMusic(drWilyShipClip);
                        }
                        break;
                    case DrWilyAnimation2States.ShipIncoming:
                        // ship hailed, fly in for the landing (brief delay)
                        if (UtilityFunctions.OverTime(runTime, thisTime + 0.1f))
                        {
                            // move through the fly in and land waypoints
                            if (waypointIndex < 3)
                            {
                                wilyShip.transform.localPosition = Vector3.MoveTowards(
                                    wilyShip.transform.localPosition,
                                    WilyShipWaypoints[waypointIndex].transform.localPosition,
                                    animationMoveSpeed * Time.deltaTime);
                                if (wilyShip.transform.localPosition ==
                                    WilyShipWaypoints[waypointIndex].transform.localPosition)
                                {
                                    // move to the next point
                                    waypointIndex++;
                                }
                                // prepare for landing
                                if (waypointIndex == 2)
                                {
                                    // use a slower speed for landing
                                    animationMoveSpeed = wilyShipLandSpeed;
                                }
                                // ship has landed now switch to the ship that
                                // is broken into its top and bottom pieces
                                if (waypointIndex == 3)
                                {
                                    // activate the ship group (top of the ship)
                                    wilyShipGroup.SetActive(true);
                                    // set the animation speed to zero
                                    wilyShip.GetComponent<Animator>().speed = 0;
                                    // move the grouped ship into position
                                    wilyShipGroup.transform.localPosition = wilyShip.transform.localPosition;
                                    // get wily ship top position, retain the X and set the Y
                                    wilyShipTopPos = wilyShipTop.transform.localPosition;
                                    wilyShipTopClosingY = wilyShipTopPos.y;
                                    wilyShipTopPos.y = wilyShipTopOpeningY;
                                    // hide Dr. Wily (the loading and blinking animations have him)
                                    enemyChars[enemySelection].SetActive(false);
                                    // switch to the Dr. Wily getting into his ship animation
                                    wilyShip.GetComponent<Animator>().speed = 0;
                                    wilyShip.GetComponent<Animator>().Play("WilyShip_Loading");
                                    // capture the current run time to finish the animation from this point
                                    thisTime = runTime;
                                    // ship has landed, now load Dr. Wily
                                    drWilyAnimation2State = DrWilyAnimation2States.OpenShip;
                                }
                            }
                        }
                        break;
                    case DrWilyAnimation2States.OpenShip:
                        // ship landed, open and load Dr. Wily
                        if (UtilityFunctions.OverTime(runTime, thisTime))
                        {
                            // check if the ship's top hasn't reached the desired Y position
                            if (wilyShipTop.transform.localPosition.y < wilyShipTopPos.y)
                            {
                                wilyShipTop.transform.localPosition = Vector3.MoveTowards(
                                    wilyShipTop.transform.localPosition, wilyShipTopPos,
                                    wilyShipTopSpeed * Time.deltaTime);
                                if (wilyShipTop.transform.localPosition == wilyShipTopPos)
                                {
                                    // set the Y position for closing
                                    wilyShipTopPos.y = wilyShipTopClosingY;
                                    // once the ship opens we play his loading animation
                                    wilyShip.GetComponent<Animator>().speed = 1;
                                    // capture the current run time to finish the animation from this point
                                    thisTime = runTime;
                                    // move on to loading Dr. Wily
                                    drWilyAnimation2State = DrWilyAnimation2States.LoadWily1;
                                }
                            }
                        }
                        break;
                    case DrWilyAnimation2States.LoadWily1:
                        // load Dr. Wily onboard (brief delay)
                        if (UtilityFunctions.OverTime(runTime, thisTime + 0.1f))
                        {
                            // check for the end of the animation
                            if (wilyShip.GetComponent<Animator>().
                                GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.333f)
                            {
                                // switch to his sit down blinking/squinting animation 
                                wilyShip.GetComponent<Animator>().Play("WilyShip_Blinking");
                                // capture the current run time to finish the animation from this point
                                thisTime = runTime;
                                // go to the next part of him getting into his ship
                                drWilyAnimation2State = DrWilyAnimation2States.LoadWily2;
                            }
                        }
                        break;
                    case DrWilyAnimation2States.LoadWily2:
                        // Dr. Wily sits down and starts blinking/squinting
                        if (UtilityFunctions.OverTime(runTime, thisTime + 0.1f))
                        {
                            // animation is 0.333 in length, let him blink four times
                            if (wilyShip.GetComponent<Animator>().
                                GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.332f)
                            {
                                // capture the current run time to finish the animation from this point
                                thisTime = runTime;
                                // time to bring down the top of the ship
                                drWilyAnimation2State = DrWilyAnimation2States.CloseShip;
                            }
                        }
                        break;
                    case DrWilyAnimation2States.CloseShip:
                        // close the ship and prepare for take off
                        if (UtilityFunctions.OverTime(runTime, thisTime))
                        {
                            // check if the ship's top hasn't reached the desired Y position
                            if (wilyShipTop.transform.localPosition.y > wilyShipTopPos.y)
                            {
                                wilyShipTop.transform.localPosition = Vector3.MoveTowards(
                                    wilyShipTop.transform.localPosition, wilyShipTopPos,
                                    wilyShipTopSpeed * Time.deltaTime);
                                if (wilyShipTop.transform.localPosition == wilyShipTopPos)
                                {
                                    // hide Dr. Wily
                                    enemyChars[enemySelection].SetActive(false);
                                    // disable the ship group (top of the ship)
                                    wilyShipGroup.SetActive(false);
                                    // switch back to the ship animation
                                    wilyShip.GetComponent<Animator>().speed = 1;
                                    wilyShip.GetComponent<Animator>().Play("WilyShip");
                                    // capture the current run time to finish the animation from this point
                                    thisTime = runTime;
                                    // now fly fly away
                                    drWilyAnimation2State = DrWilyAnimation2States.ShipOutgoing;
                                }
                            }
                        }
                        break;
                    case DrWilyAnimation2States.ShipOutgoing:
                        if (UtilityFunctions.OverTime(runTime, thisTime + 0.1f))
                        {
                            // move through the (rest of) fly out waypoints
                            if (waypointIndex < 6)
                            {
                                wilyShip.transform.localPosition = Vector3.MoveTowards(
                                    wilyShip.transform.localPosition,
                                    WilyShipWaypoints[waypointIndex].transform.localPosition,
                                    animationMoveSpeed * Time.deltaTime);
                                if (wilyShip.transform.localPosition ==
                                    WilyShipWaypoints[waypointIndex].transform.localPosition)
                                {
                                    // move to the next point
                                    waypointIndex++;
                                }
                                // time to take off
                                if (waypointIndex == 4)
                                {
                                    // use a faster speed for leaving
                                    animationMoveSpeed = wilyShipFlySpeed;
                                }
                                // ship is gone and so is this scene
                                if (waypointIndex == 5)
                                {
                                    // now we can move to the next scene
                                    // FINALLY DONE with his exit animation sequence! :)
                                    sceneState = SceneStates.NextScene;
                                }
                            }
                        }
                        break;
                }
            }
        }
    }

    private IEnumerator PlayScoreAnimations()
    {
        // first part is to typewriter effect the enemy name and clear points text

        // string insert for textmesh and clear points text
        string strInsert = "";
        string strClearPoints = GetEnemyName(enemySelection) + "\n\n" + "CLEAR POINTS";
        // clear the textmesh text and activate it
        tmpEnemyClearPoints.text = "";
        tmpEnemyClearPoints.gameObject.SetActive(true);
        // for each character in the clear points string
        for (int i = 0; i < strClearPoints.Length; i++)
        {
            // get the char at current position, append, and update textmesh text
            char strChar = strClearPoints[i];
            strInsert += strChar;
            tmpEnemyClearPoints.text = String.Format(strEnemyClearPoints, tmpEnemyClearPoints.fontSize, hexWhite, strInsert);
            // no delays if we encounter newlines or spaces
            if (strChar != '\n' && strChar != ' ')
            {
                // little between the string building out
                yield return new WaitForSeconds(typewriterTextDelay);
            }
        }

        // second part is to animate the score before settling on it

        // clear the textmesh text and activate it
        tmpPointsTallyText.text = "";
        tmpPointsTallyText.gameObject.SetActive(true);

        // will do eight full passes before we settle on the selected points
        int pointsIndex = 0;
        int iterations = stagePointsArray.Length * 8;
        if (!playDrWily)
        {
            // after the full iterations then this gets us to the points we selected
            // Dr. Wily is a solid 200k and is not in the array so just show it at the end
            iterations += Array.IndexOf(stagePointsArray, stagePointsSelection);
        }

        // play the point tally sound loop
        SoundManager.Instance.Play(GameManager.Instance.assetPalette.pointTallyLoopClip, true);

        // need to limit to five digits during the loop
        int iterationPoints;
        string formattedPoints = "";
        string strIterationPoints = " {0:00000}";

        // now loop through the score points array
        for (int i = 0; i < iterations; i++)
        {
            // stay within range
            if (pointsIndex > stagePointsArray.Length - 1)
            {
                pointsIndex = 0;
            }
            // get the current points from the index
            // to keep it within five digits, the 100k will be replaced with a 0
            iterationPoints = stagePointsArray[pointsIndex++];
            if (iterationPoints == 100000) iterationPoints = 0;
            formattedPoints = String.Format(strIterationPoints, iterationPoints);
            // now show the iterations points in the textmesh
            tmpPointsTallyText.text = String.Format(strPointsTallyText,
                tmpPointsTallyText.fontSize, hexWhite, formattedPoints);
            // brief delay before next update
            yield return new WaitForSeconds(0.001f);
        }

        // selected stage points (if six digit then trim the leading space)
        if (stagePointsSelection >= 100000) strIterationPoints = strIterationPoints.Trim();
        formattedPoints = String.Format(strIterationPoints, stagePointsSelection);
        tmpPointsTallyText.text = String.Format(strPointsTallyText,
            tmpPointsTallyText.fontSize, hexWhite, formattedPoints);

        // play the point tally end clip
        SoundManager.Instance.Play(GameManager.Instance.assetPalette.pointTallyEndClip);
    }
}