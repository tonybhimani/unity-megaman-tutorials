using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraTransition : MonoBehaviour
{
    public enum TransitionEntry { Enter, Exit }
    public enum TransitionState { PreDelay, Transition, PostDelay }
    public enum TransitionDirection { Horizontal, Vertical }
    public enum TransitionEventCall
    {
        BothBeforeDelay,
        BothAfterDelay,
        OnEnterBeforeDelay,
        OnEnterAfterDelay,
        OnExitBeforeDelay,
        OnExitAfterDelay
    }

    [SerializeField] bool onlyMoveCamera;
    [SerializeField] TransitionEntry entry = TransitionEntry.Enter;
    [SerializeField] TransitionState state = TransitionState.PreDelay;
    [SerializeField] TransitionDirection direction = TransitionDirection.Horizontal;
    [SerializeField] TransitionEventCall eventCallPreDelay = TransitionEventCall.BothBeforeDelay;
    [SerializeField] TransitionEventCall eventCallPostDelay = TransitionEventCall.BothBeforeDelay;

    [Header("Timers")]
    [SerializeField] float transitionDelay = 1f;
    [SerializeField] float preTransitionDelay = 1f;
    [SerializeField] float postTransitionDelay = 1f;

    [Header("Positions")]
    [SerializeField] Vector2 cameraMinPosition;
    [SerializeField] Vector2 cameraMaxPosition;
    [SerializeField] Vector2 playerChange;

    [Header("Events")]
    public UnityEvent preTransitionEvent;
    public UnityEvent postTransitionEvent;
    public UnityEvent onlyMoveCameraEvent;

    // camera follow script and player references
    CameraFollow cam;
    GameObject player;

    // calculated camera and player movement positions
    Vector2 cameraMinPrevious;
    Vector2 cameraMaxPrevious;
    Vector2 cameraMoveStart;
    Vector2 cameraMoveFinish;
    Vector2 cameraMoveProgress;
    Vector2 playerMoveStart;
    Vector2 playerMoveFinish;

    // transition timers
    float progress;
    float transitionTimer;

    // transition flags
    bool transition;
    bool getCamPrevious = true;
    bool callPreTransitionEvent = true;
    bool callPostTranisionEvent = true;

    // Start is called before the first frame update
    void Start()
    {
        // get follow script component from main camera
        cam = Camera.main.GetComponent<CameraFollow>();
    }

    void Update()
    {
        if (transition)
        {
            switch (state)
            {
                case TransitionState.PreDelay:
                    // run pre transition event before delay
                    CallEventBeforePreDelay();
                    // wait out the pre delay if set
                    transitionTimer -= Time.deltaTime;
                    if (transitionTimer <= 0)
                    {
                        // run pre transition event after delay
                        CallEventAfterPreDelay();
                        // start player animation
                        player.GetComponent<Animator>().speed = 1;
                        // set transition state and timer
                        state = TransitionState.Transition;
                        transitionTimer = 0;
                    }
                    break;
                case TransitionState.Transition:
                    // track transition progress for lerp
                    progress = Mathf.Clamp(transitionTimer, 0, transitionDelay) / transitionDelay;
                    transitionTimer += Time.deltaTime;
                    // move camera and player - animate player during transition
                    cameraMoveProgress = Vector2.Lerp(cameraMoveStart, cameraMoveFinish, progress);
                    cam.transform.position = new Vector3(cameraMoveProgress.x, cameraMoveProgress.y, cam.transform.position.z);
                    player.transform.position = Vector2.Lerp(playerMoveStart, playerMoveFinish, progress);
                    // end of transition
                    if (progress >= 1)
                    {
                        // stop player animation
                        player.GetComponent<Animator>().speed = 0;
                        // set camera positions
                        cam.boundsMin = (entry == TransitionEntry.Enter) ? cameraMinPosition : cameraMinPrevious;
                        cam.boundsMax = (entry == TransitionEntry.Enter) ? cameraMaxPosition : cameraMaxPrevious;
                        player.transform.position = playerMoveFinish;
                        // attach players transform back on the camera
                        cam.player = player.transform;
                        // set transition state and timer
                        state = TransitionState.PostDelay;
                        transitionTimer = postTransitionDelay;
                    }
                    break;
                case TransitionState.PostDelay:
                    // run post transition event before delay
                    CallEventBeforePostDelay();
                    // wait out the post delay if set
                    transitionTimer -= Time.deltaTime;
                    if (transitionTimer <= 0)
                    {
                        // run post transition event after delay
                        CallEventAfterPostDelay();
                        // toggle transition entry
                        entry = (entry == TransitionEntry.Enter) ? TransitionEntry.Exit : TransitionEntry.Enter;
                        // end the transition
                        transition = false;
                        // give control back to the player
                        player.GetComponent<PlayerController>().FreezeInput(false);
                        player.GetComponent<PlayerController>().FreezePlayer(false);
                    }
                    break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // only move the camera positions
            // this is best used for falls where the player clears the trigger
            // otherwise you'll experience issues with double-triggering
            if (onlyMoveCamera)
            {
                cam.boundsMin = cameraMinPosition;
                cam.boundsMax = cameraMaxPosition;
                // new event for only camera movement
                onlyMoveCameraEvent.Invoke();
                return;
            }

            // perform camera transition
            if (!transition)
            {
                transition = true;
                player = other.gameObject;
                // detach players transform from camera
                // manipulate the camera position without min/max bounds interfering
                cam.player = null;
                // set transition state and timer
                state = TransitionState.PreDelay;
                transitionTimer = preTransitionDelay;
                // allow the transition events to fire
                callPreTransitionEvent = true;
                callPostTranisionEvent = true;
                // camera start and player positions
                cameraMoveStart = cam.transform.position;
                playerMoveStart = player.transform.position;
                if (entry == TransitionEntry.Enter)
                {
                    // save previous camera min/max positions the first time
                    if (getCamPrevious)
                    {
                        getCamPrevious = false;
                        cameraMinPrevious = cam.boundsMin;
                        cameraMaxPrevious = cam.boundsMax;
                    }
                    // player end position
                    playerMoveFinish = playerMoveStart + playerChange;
                }
                else
                {
                    // player end position
                    playerMoveFinish = playerMoveStart - playerChange;
                }
                // camera finish positions
                if (direction == TransitionDirection.Horizontal)
                {
                    float cameraMinPosX = cameraMinPosition.x;
                    if (entry == TransitionEntry.Exit)
                    {
                        cameraMinPosX = (playerChange.x > 0) ? cameraMaxPrevious.x : cameraMinPrevious.x;
                    }
                    cameraMoveFinish = new Vector2(cameraMinPosX, cam.transform.position.y);
                }
                else
                {
                    float cameraMinPosY = cameraMinPosition.y;
                    if (entry == TransitionEntry.Exit)
                    {
                        cameraMinPosY = (playerChange.y > 0) ? cameraMaxPrevious.y : cameraMinPrevious.y;
                    }
                    cameraMoveFinish = new Vector2(cam.transform.position.x, cameraMinPosY);
                }
                // stop player animation
                player.GetComponent<Animator>().speed = 0;
                // freeze player and input
                player.GetComponent<PlayerController>().FreezeInput(true);
                player.GetComponent<PlayerController>().FreezePlayer(true);
            }
        }
    }

    private void CallPreTransitionEvent()
    {
        // call pre transition event
        if (callPreTransitionEvent)
        {
            callPreTransitionEvent = false;
            preTransitionEvent.Invoke();
        }
    }

    private void CallEventBeforePreDelay()
    {
        // run event before delay
        switch (eventCallPreDelay)
        {
            case TransitionEventCall.BothBeforeDelay:
                CallPreTransitionEvent();
                break;
            case TransitionEventCall.OnEnterBeforeDelay:
                if (entry == TransitionEntry.Enter)
                {
                    CallPreTransitionEvent();
                }
                break;
            case TransitionEventCall.OnExitBeforeDelay:
                if (entry == TransitionEntry.Exit)
                {
                    CallPreTransitionEvent();
                }
                break;
        }
    }

    private void CallEventAfterPreDelay()
    {
        // run event after delay
        switch (eventCallPreDelay)
        {
            case TransitionEventCall.BothAfterDelay:
                CallPreTransitionEvent();
                break;
            case TransitionEventCall.OnEnterAfterDelay:
                if (entry == TransitionEntry.Enter)
                {
                    CallPreTransitionEvent();
                }
                break;
            case TransitionEventCall.OnExitAfterDelay:
                if (entry == TransitionEntry.Exit)
                {
                    CallPreTransitionEvent();
                }
                break;
        }
    }

    private void CallPostTransitionEvent()
    {
        // call post transition event
        if (callPostTranisionEvent)
        {
            callPostTranisionEvent = false;
            postTransitionEvent.Invoke();
        }
    }

    private void CallEventBeforePostDelay()
    {
        // run event before delay
        switch (eventCallPostDelay)
        {
            case TransitionEventCall.BothBeforeDelay:
                CallPostTransitionEvent();
                break;
            case TransitionEventCall.OnEnterBeforeDelay:
                if (entry == TransitionEntry.Enter)
                {
                    CallPostTransitionEvent();
                }
                break;
            case TransitionEventCall.OnExitBeforeDelay:
                if (entry == TransitionEntry.Exit)
                {
                    CallPostTransitionEvent();
                }
                break;
        }
    }

    private void CallEventAfterPostDelay()
    {
        // run event after delay
        switch (eventCallPostDelay)
        {
            case TransitionEventCall.BothAfterDelay:
                CallPostTransitionEvent();
                break;
            case TransitionEventCall.OnEnterAfterDelay:
                if (entry == TransitionEntry.Enter)
                {
                    CallPostTransitionEvent();
                }
                break;
            case TransitionEventCall.OnExitAfterDelay:
                if (entry == TransitionEntry.Exit)
                {
                    CallPostTransitionEvent();
                }
                break;
        }
    }
}
