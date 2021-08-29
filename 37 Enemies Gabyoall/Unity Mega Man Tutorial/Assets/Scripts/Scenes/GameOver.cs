using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    // flag that we called for the next scene
    bool calledNextScene;

    // game over sound clip
    [SerializeField] AudioClip gameOverClip;

    // text mesh pro score text
    [SerializeField] TextMeshProUGUI playerScoreText;

    // arrow selector (its transform)
    [SerializeField] Transform arrowSelector;

    // arrow position index
    int arrowIndex = 0;

    // arrow screen placement
    Vector2[] arrowPositions = {
        new Vector2(-120f, -32f),
        new Vector2(-120f, -72f)
    };

    void Awake()
    {
        // arrow placement
        arrowSelector.localPosition = arrowPositions[arrowIndex];
        // show player score
        playerScoreText.text = String.Format("<mspace=\"{0}\">{1:0000000}</mspace>",
            playerScoreText.fontSize, GameManager.Instance.GetScorePoints());
    }

    // Start is called before the first frame update
    void Start()
    {
        // set up the game over music - full volume, no loop, and play
        SoundManager.Instance.MusicSource.volume = 1f;
        SoundManager.Instance.PlayMusic(gameOverClip, false);
    }

    // Update is called once per frame
    void Update()
    {
        // keyboard input up arrow
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // cycle around to the end
            if (--arrowIndex < 0)
            {
                arrowIndex = arrowPositions.Length - 1;
            }
            // update the arrow position
            arrowSelector.localPosition = arrowPositions[arrowIndex];
        }

        // keyboard input down arrow
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // cycle around to the beginning
            if (++arrowIndex > arrowPositions.Length - 1)
            {
                arrowIndex = 0;
            }
            // update the arrow position
            arrowSelector.localPosition = arrowPositions[arrowIndex];
        }

        // keyboard input enter
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // tell GameManager to trigger the next scene
            if (!calledNextScene)
            {
                calledNextScene = true;
                // reset player numbers and energies
                GameManager.Instance.ResetPlayerLives();
                GameManager.Instance.ResetPointsCollected(false, true);
                GameManager.Instance.FillWeaponEnergies();
                // what is the arrow pointing to?
                switch (arrowIndex)
                {
                    // continue
                    case 0:
                        // no parameter calls previous scene
                        GameManager.Instance.StartNextScene();
                        break;
                    // stage select
                    case 1:
                        // call the stage select screen
                        GameManager.Instance.StartNextScene(GameManager.GameScenes.StageSelect);
                        break;
                }
            }
        }
    }
}