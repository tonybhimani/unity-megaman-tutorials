using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderScript : MonoBehaviour
{
    // these offsets are for reaching the top (climb up)
    // and bottom (to let go and fall off) of the ladder
    public float handlerTopOffset = 0.04f;
    public float handlerBottomOffset = 0.04f;

    // flag to let the player know that climbing is possible
    [HideInInspector] public bool isNearLadder;

    // these positions are for the player's ladder climbing logic
    [HideInInspector] public float posX;
    [HideInInspector] public float posTopY;
    [HideInInspector] public float posBottomY;
    [HideInInspector] public float posTopHandlerY;
    [HideInInspector] public float posBottomHandlerY;
    [HideInInspector] public float posPlatformY;

    void Awake()
    {
        // set up the positioning
        GetComponent<BoxCollider2D>().offset = Vector2.zero;
        Vector2 size = GetComponent<BoxCollider2D>().size;
        Transform ladderTop = transform.GetChild(0).transform;
        Transform ladderBottom = transform.GetChild(1).transform;
        Transform ladderPlatform = transform.GetChild(2).transform;
        ladderTop.position = new Vector3(transform.position.x, transform.position.y + (size.y / 2), 0);
        ladderBottom.position = new Vector3(transform.position.x, transform.position.y - (size.y / 2), 0);

        // set up the variables for the climber to use
        posX = transform.position.x;
        posTopY = ladderTop.transform.position.y;
        posBottomY = ladderBottom.transform.position.y;
        posPlatformY = ladderPlatform.transform.position.y;
        posTopHandlerY = posTopY + handlerTopOffset;
        posBottomHandlerY = posBottomY + handlerBottomOffset;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // only ladder climber we have is the player
        if (other.gameObject.CompareTag("Player"))
        {
            // if the player is within the range (center-ish of the ladder)
            isNearLadder = (other.gameObject.transform.position.x > (posX - 0.05f) &&
                    other.gameObject.transform.position.x < (posX + 0.05f));
            other.gameObject.GetComponent<PlayerController>().ladder = this;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // outside of the trigger then no ladder to climb
            isNearLadder = false;
            other.gameObject.GetComponent<PlayerController>().ladder = null;
        }
    }
}