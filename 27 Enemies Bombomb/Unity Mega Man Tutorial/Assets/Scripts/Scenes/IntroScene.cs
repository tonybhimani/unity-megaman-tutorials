using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroScene : MonoBehaviour
{
    // create an animated storyline
    float startTime;
    float runTime;

    // flag that we called for the next scene
    bool calledNextScene;

    // key press detection flag
    bool inputDetected = false;

    // show runtime text
    [SerializeField] bool showRunTime;

    // progress tracking and fading delay
    float progress;
    float fadeTimer;
    [SerializeField] float fadeDelay = 5f;

    // game objects we use in this scene
    [SerializeField] GameObject outsideLab;
    [SerializeField] GameObject insideLab;
    [SerializeField] GameObject player;

    // music clip for this scene
    public AudioClip musicClip;

    // canvas texts
    Text runTimeText;
    TextMeshProUGUI tmpDialogueText;

    // points our player runs to
    float[] playerRunPoints = {
        0.38f,
        2.45f
    };

    // current music volume
    float musicVolume;

    private enum IntroStates { OutsideLab, ScreenFade1, InsideLab, ScreenFade2, NextScene };
    IntroStates introState = IntroStates.OutsideLab;

    string[] dialogueStrings = {
        "SOMETIME IN THE FUTURE...",
        "DR. LIGHT'S LAB",
        "DR. LIGHT:\n\n\tMEGA MAN, COME OVER HERE PLEASE.",
        "DR. LIGHT:\n\n\tTHERE IS A DISTURBANCE AT THE HIGHWAY.",
        "DR. LIGHT:\n\n\tI NEED YOU TO INVESTIGATE.",
        "MEGA MAN:\n\n\tOF COURSE. I'LL LEAVE RIGHT AWAY.",
        "DR. LIGHT:\n\n\tTHANK YOU, MEGA MAN."
    };

    void Awake()
    {
        // get text objects
        runTimeText = GameObject.Find("RunTime").GetComponent<Text>();
        tmpDialogueText = GameObject.Find("DialogueText").GetComponent<TextMeshProUGUI>();
        // make sure there is no dialogue at start
        tmpDialogueText.text = "";
        // no user control allowed during this scene
        player.GetComponent<PlayerController>().FreezeInput(true);
        // all children in the InsideLab start transparent
        foreach (Transform child in insideLab.transform)
        {
            child.gameObject.GetComponent<SpriteRenderer>().color = Color.clear;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // track when we started
        startTime = Time.time;
        // set up the scene music - 3/4 volume, no loop, and play
        SoundManager.Instance.MusicSource.volume = 0.75f;
        SoundManager.Instance.PlayMusic(musicClip, false);
    }

    // Update is called once per frame
    void Update()
    {
        // how long has the scene been running for
        runTime = Time.time - startTime;

        // this is for seeing the run time of the scene and is helpful for making 
        // the storytelling triggers - use checkbox in inspector to turn it off
        runTimeText.text = showRunTime ? String.Format("RunTime: {0:0.00}", runTime) : "";

        // check for any key input to exit the scene
        //   in case the user wants to skip it :(
        if (Input.anyKey && !inputDetected && introState != IntroStates.ScreenFade2)
        {
            // allow this only once
            inputDetected = true;
            // call init scene exit function (it'll jump to the end state)
            InitSceneExit();
        }

        switch (introState)
        {
            case IntroStates.OutsideLab:
                // sometime in the future...
                if (UtilityFunctions.InTime(runTime, 2.0f))
                {
                    tmpDialogueText.text = dialogueStrings[0];
                }
                // dr. light's lab
                if (UtilityFunctions.InTime(runTime, 5.0f))
                {
                    tmpDialogueText.text = dialogueStrings[1];
                }
                // switch to screen fade / transition state
                if (UtilityFunctions.OverTime(runTime, 8.0f))
                {
                    introState = IntroStates.ScreenFade1;
                }
                break;
            case IntroStates.ScreenFade1:
                // progress of timer with a range of 0 to 1 like LERPing
                progress = Mathf.Clamp(fadeTimer, 0, fadeDelay) / fadeDelay;
                fadeTimer += Time.deltaTime;
                // change color alpha on all children of InsideLab game object
                // to fade in from 0 to 1
                foreach (Transform child in insideLab.transform)
                {
                    child.gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, progress);
                }
                // change color alpha of dialogue text but 1.5 times faster 
                // and opposite of fading out from 1 to 0
                tmpDialogueText.color = new Color(1, 1, 1, 1f - (progress * 1.5f));
                // when progress completes
                if (progress >= 1f)
                {
                    // reset the dialogue text object and set new alignment
                    tmpDialogueText.text = "";
                    tmpDialogueText.color = Color.white;
                    tmpDialogueText.alignment = TextAlignmentOptions.TopLeft;
                    // switch to the InsideLab state
                    introState = IntroStates.InsideLab;
                }
                break;
            case IntroStates.InsideLab:
                // dr. light asks megaman to come over
                if (UtilityFunctions.InTime(runTime, 14.0f))
                {
                    tmpDialogueText.text = dialogueStrings[2];
                }
                // remove the dialogue
                if (UtilityFunctions.InTime(runTime, 17.0f))
                {
                    tmpDialogueText.text = "";
                }
                // megaman runs into the scene to x coordinate and stops
                if (UtilityFunctions.InTime(runTime, 17.0f, 20.0f))
                {
                    if (player.transform.position.x >= playerRunPoints[0])
                    {
                        player.GetComponent<PlayerController>().SimulateMoveLeft();
                    }
                    else
                    {
                        player.GetComponent<PlayerController>().SimulateMoveStop();
                    }
                }
                // dr. light tells megaman there is a disturbance at the highway
                if (UtilityFunctions.InTime(runTime, 20.0f))
                {
                    tmpDialogueText.text = dialogueStrings[3];
                }
                // dr. light tells megaman to go check it out
                if (UtilityFunctions.InTime(runTime, 24.0f))
                {
                    tmpDialogueText.text = dialogueStrings[4];
                }
                // megaman tells dr. light he'll check it out
                if (UtilityFunctions.InTime(runTime, 28.0f))
                {
                    tmpDialogueText.text = dialogueStrings[5];
                }
                // dr. light thanks megaman
                if (UtilityFunctions.InTime(runTime, 32.0f))
                {
                    tmpDialogueText.text = dialogueStrings[6];
                }
                // remove the dialogue
                if (UtilityFunctions.InTime(runTime, 35.0f))
                {
                    tmpDialogueText.text = "";
                }
                // megaman runs out of the scene to x coordinate and stops
                if (UtilityFunctions.InTime(runTime, 32.0f, 35.0f))
                {
                    if (player.transform.position.x <= playerRunPoints[1])
                    {
                        player.GetComponent<PlayerController>().SimulateMoveRight();
                    }
                    else
                    {
                        player.GetComponent<PlayerController>().SimulateMoveStop();
                    }
                }
                // switch to next screen fade state (fade out the scene)
                if (UtilityFunctions.InTime(runTime, 36.0f))
                {
                    // call scene exit function (it'll move to the next state)
                    InitSceneExit();
                }
                break;
            case IntroStates.ScreenFade2:
                // progress of timer with a range of 0 to 1 like LERPing
                progress = Mathf.Clamp(fadeTimer, 0, fadeDelay) / fadeDelay;
                fadeTimer += Time.deltaTime;
                // change color alpha on all children of InsideLab game object
                // to fade out from 1 to 0
                foreach (Transform child in insideLab.transform)
                {
                    child.gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f - progress);
                }
                // fade out music by lowering the volume
                SoundManager.Instance.MusicSource.volume = musicVolume * (1f - progress);
                // when progress completes
                if (progress >= 1f)
                {
                    // make sure music volume is at zero
                    SoundManager.Instance.MusicSource.volume = 0;
                    // switch to the next scene state
                    introState = IntroStates.NextScene;
                }
                break;
            case IntroStates.NextScene:
                // tell GameManager to trigger the next scene
                if (!calledNextScene)
                {
                    GameManager.Instance.StartNextScene();
                    calledNextScene = true;
                }
                break;
        }
    }

    private void InitSceneExit()
    {
        // reset the fade timer
        fadeTimer = 0;
        // clear out the dialogue text
        tmpDialogueText.text = "";
        // hide the outside lab object (and its children) and the player
        outsideLab.SetActive(false);
        player.SetActive(false);
        // get music volume and save it
        musicVolume = SoundManager.Instance.MusicSource.volume;
        // switch to next state
        introState = IntroStates.ScreenFade2;
    }
}
