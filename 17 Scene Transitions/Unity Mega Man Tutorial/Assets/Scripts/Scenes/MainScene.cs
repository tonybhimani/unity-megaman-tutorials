using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainScene : MonoBehaviour
{
    // access to our TextMeshPro object
    TextMeshProUGUI screenMessageText;

    // music clip for the scene
    public AudioClip musicClip;

    void Awake()
    {
        // get screen message text tmp object
        screenMessageText = GameObject.Find("ScreenMessage").GetComponent<TextMeshProUGUI>();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    // display a message that a checkpoint has been reached
    // trigger this through a camera transition post delay event
    public void CheckpointReached()
    {
        StartCoroutine(CoCheckpointReached());
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
}
