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

    // canvas stuff
    GameObject dialogueBox;
    Text runTimeText;
    TextMeshProUGUI dialogueText;
    TextMeshProUGUI screenMessageText;

    // show runtime text
    [SerializeField] bool showRunTime;

    // music clip for the scene
    public AudioClip musicClip;

    // our player object
    [SerializeField] GameObject player;
    [SerializeField] GameObject hologram;

    // x positions for our player
    [SerializeField] float startSeqBeginPoint = 5.6f;
    [SerializeField] float startSeqEndPoint = 7.0f;

    // wall and checkpoint trigger
    [SerializeField] GameObject wallLeft;
    [SerializeField] GameObject checkpointTrigger;
    [SerializeField] float wallLeftXPos = 11.5f;

    public enum LevelStates { Exploration, Hologram, KeepLooking, Checkpoint };
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
                    if (player.transform.position.x >= startSeqBeginPoint)
                    {
                        // get start time
                        startTime = Time.time;
                        // freeze the player input and stop movement
                        player.GetComponent<PlayerController>().FreezeInput(true);
                        Vector2 playerVelocity = player.GetComponent<Rigidbody2D>().velocity;
                        player.GetComponent<Rigidbody2D>().velocity = new Vector2(0, playerVelocity.y);
                        // go to the dr. light hologram state
                        levelState = LevelStates.Hologram;
                    }
                }
                break;
            case LevelStates.Hologram:
                // how long has this sequence been running for
                runTime = Time.time - startTime;

                // move player forward a bit and stop to show hologram
                if (UtilityFunctions.InTime(runTime, 2.0f, 5.0f))
                {
                    if (player.transform.position.x <= startSeqEndPoint)
                    {
                        player.GetComponent<PlayerController>().SimulateMoveRight();
                    }
                    else
                    {
                        player.GetComponent<PlayerController>().SimulateMoveStop();
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
                    player.GetComponent<PlayerController>().FreezeInput(false);
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
                // add more stuff if we had a complete level
                break;
        }
    }

    // display a message that a checkpoint has been reached
    // trigger this through a camera transition post delay event
    public void CheckpointReached()
    {
        StartCoroutine(CoCheckpointReached());
        // switch to the checkpoint state
        levelState = LevelStates.Checkpoint;
        // as an example we'll move a wall to block us in and disable the trigger
        Vector3 wallPos = wallLeft.transform.position;
        wallPos.x = wallLeftXPos;
        wallLeft.transform.position = wallPos;
        checkpointTrigger.SetActive(false);
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
}
