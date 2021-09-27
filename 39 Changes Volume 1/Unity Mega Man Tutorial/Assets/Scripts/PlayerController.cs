using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    ColorSwap colorSwap;

    float keyHorizontal;
    float keyVertical;
    bool keyJump;
    bool keyShoot;

    bool isGrounded;
    bool isClimbing;
    bool isJumping;
    bool isShooting;
    bool isThrowing;
    bool isTeleporting;
    bool isTakingDamage;
    bool isInvincible;
    bool isFacingRight;

    bool hitSideRight;

    bool freezeInput;
    bool freezePlayer;
    bool freezeEverything;

    float shootTime;
    float shootTimeLength;
    bool keyShootRelease;
    float keyShootReleaseTimeLength;

    bool canUseWeapon;

    // last animation played
    string lastAnimationName;

    // delay for ground check
    bool jumpStarted;

    // freeze/hide player on screen
    float playerColor;
    RigidbodyConstraints2D rb2dConstraints;

    // ladder climbing variables
    float transformY;
    float transformHY;
    bool isClimbingDown;
    bool atLaddersEnd;
    bool hasStartedClimbing;
    bool startedClimbTransition;
    bool finishedClimbTransition;

    private enum SwapIndex
    {
        Primary = 64,
        Secondary = 128
    }

    public enum WeaponTypes
    {
        HyperBomb,
        ThunderBeam,
        SuperArm,
        IceSlasher,
        RollingCutter,
        FireStorm,
        MagnetBeam,
        MegaBuster
    };
    public WeaponTypes playerWeapon = WeaponTypes.MegaBuster;

    [System.Serializable]
    public struct WeaponsStruct
    {
        public WeaponTypes weaponType;
        public bool enabled;
        public int currentEnergy;
        public int maxEnergy;
        public int energyCost;
        public int weaponDamage;
        public Vector2 weaponVelocity;
        public AudioClip weaponClip;
        public GameObject weaponPrefab;
    }
    public WeaponsStruct[] weaponsData;

    public int currentHealth;
    public int maxHealth = 28;

    [HideInInspector] public LadderScript ladder;

    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float jumpSpeed = 3.7f;
    [SerializeField] float climbSpeed = 0.525f;

    [Header("Audio Clips")]
    [SerializeField] AudioClip teleportClip;
    [SerializeField] AudioClip jumpLandedClip;
    [SerializeField] AudioClip takingDamageClip;
    [SerializeField] AudioClip explodeEffectClip;
    [SerializeField] AudioClip energyFillClip;

    [Header("Positions and Prefabs")]
    [SerializeField] Transform bulletShootPos;
    [SerializeField] GameObject explodeEffectPrefab;

    [Header("Ladder Settings")]
    [SerializeField] float climbSpriteHeight = 0.36f;

    [Header("Teleport Settings")]
    [SerializeField] float teleportSpeed = -10f;
    [SerializeField] float teleportLandingY = 0f;
    public enum TeleportState { Descending, Landed, Idle };
    [SerializeField] TeleportState teleportState;

    void Awake()
    {
        // get handles to components
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // sprite defaults to facing right
        isFacingRight = true;

        // start at full health
        currentHealth = maxHealth;

        // color swap component to change megaman's palette
        colorSwap = GetComponent<ColorSwap>();

        // set player weapon (change color)
        SetWeapon(playerWeapon);

        // fill all weapon energies
        FillWeaponEnergies();

        // restore player weapons saved in game manager
        GameManager.Instance.RestorePlayerWeapons();

#if UNITY_STANDALONE
        // disable screen input canvas if not android or ios
        GameObject inputCanvas = GameObject.Find("InputCanvas");
        if (inputCanvas != null)
        {
            inputCanvas.SetActive(false);
        }
#endif
    }

    private void FixedUpdate()
    {
        isGrounded = false;
        Color raycastColor;
        RaycastHit2D raycastHit;
        float raycastDistance = 0.025f;
        int layerMask = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("MagnetBeam");
        // ground check
        Vector3 box_origin = box2d.bounds.center;
        box_origin.y = box2d.bounds.min.y + (box2d.bounds.extents.y / 4f);
        Vector3 box_size = box2d.bounds.size;
        box_size.y = box2d.bounds.size.y / 4f;
        raycastHit = Physics2D.BoxCast(box_origin, box_size, 0f, Vector2.down, raycastDistance, layerMask);
        // player box colliding with ground layer (ignore if teleport descending)
        if (raycastHit.collider != null && gameObject.layer != LayerMask.NameToLayer("Teleport") && !jumpStarted)
        {
            isGrounded = true;
            // just landed from jumping/falling
            if (isJumping)
            {
                isJumping = false;
                SoundManager.Instance.Play(jumpLandedClip);
            }
        }
        // draw debug lines
        raycastColor = (isGrounded) ? Color.green : Color.red;
        Debug.DrawRay(box_origin + new Vector3(box2d.bounds.extents.x, 0), Vector2.down * (box2d.bounds.extents.y / 4f + raycastDistance), raycastColor);
        Debug.DrawRay(box_origin - new Vector3(box2d.bounds.extents.x, 0), Vector2.down * (box2d.bounds.extents.y / 4f + raycastDistance), raycastColor);
        Debug.DrawRay(box_origin - new Vector3(box2d.bounds.extents.x, box2d.bounds.extents.y / 4f + raycastDistance), Vector2.right * (box2d.bounds.extents.x * 2), raycastColor);
    }

    // Update is called once per frame
    void Update()
    {
        // initial screen animation - teleport from top of screen really fast
        if (isTeleporting)
        {
            switch (teleportState)
            {
                case TeleportState.Descending:
                    // force this to false so the jump landed sound isn't played
                    isJumping = false;
                    if (transform.position.y <= teleportLandingY)
                    {
                        // restore the player tag and layer
                        gameObject.tag = "Player";
                        gameObject.layer = LayerMask.NameToLayer("Player");
                        // zero out the velocity and freeze all the constraints for the landing
                        rb2d.velocity = Vector2.zero;
                        rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
                        // set the position to the teleport landing y
                        transform.position = new Vector3(transform.position.x, teleportLandingY, 0);
                        teleportState = TeleportState.Landed;
                    }
                    break;
                case TeleportState.Landed:
                    // events in the animation will be called
                    animator.speed = 1;
                    break;
                case TeleportState.Idle:
                    Teleport(false);
                    // tell the game manager the teleport is finished
                    GameManager.Instance.TeleportFinished();
                    break;
            }
            return;
        }

        // taking damage from projectiles, touching enemies, or other environment objects
        if (isTakingDamage)
        {
            PlayAnimation("Player_Hit");
            return;
        }

        // player input and movement
        // don't process any input if the game is paused and
        // there is a camera transition happening
        if (!GameManager.Instance.IsGamePaused() &&
            !GameManager.Instance.InCameraTransition())
        {
            PlayerDebugInput();
            PlayerDirectionInput();
            PlayerJumpInput();
            PlayerShootInput();
        }

        // animations and movement from input
        PlayerMovement();

        // fire selected weapon
        FireWeapon();
    }

    void PlayerDebugInput()
    {
        // E for Explosions
        if (Input.GetKeyDown(KeyCode.E))
        {
            Defeat();
            Debug.Log("Defeat()");
        }

        // I for Invincible
        if (Input.GetKeyDown(KeyCode.I))
        {
            Invincible(!isInvincible);
            Debug.Log("Invincible: " + isInvincible);
        }

        // L for Life Energy
        if (Input.GetKeyDown(KeyCode.L))
        {
            ApplyLifeEnergy(10);
            Debug.Log("ApplyLifeEnergy(10)");
        }

        // K for Keyboard
        if (Input.GetKeyDown(KeyCode.K))
        {
            FreezeInput(!freezeInput);
            Debug.Log("Freeze Input: " + freezeInput);
        }

        // J for Player (nope)
        if (Input.GetKeyDown(KeyCode.J))
        {
            FreezePlayer(!freezePlayer);
            Debug.Log("Freeze Player: " + freezePlayer);
        }

        // S for Switch Weapon
        if (Input.GetKeyDown(KeyCode.S))
        {
            int nextWeapon = (int)playerWeapon;
            int maxWeapons = weaponsData.Length;
            while (true)
            {
                // cycle to next weapon index
                if (++nextWeapon > maxWeapons - 1)
                {
                    nextWeapon = 0;
                }
                // if weapon is enabled then use it
                if (weaponsData[nextWeapon].enabled)
                {
                    SwitchWeapon((WeaponTypes)nextWeapon);
                    break;
                }
            }
            Debug.Log("SwitchWeapon()");
        }

        // T for Teleport
        if (Input.GetKeyDown(KeyCode.T))
        {
            //SetWeapon((WeaponTypes)UnityEngine.Random.Range(0,
            //    Enum.GetValues(typeof(WeaponTypes)).Length));
            Teleport(true);
            //CanUseWeaponAgain();
            Debug.Log("Teleport(true)");
        }

        // F for Freeze
        if (Input.GetKeyDown(KeyCode.F))
        {
            freezeEverything = !freezeEverything;
            GameManager.Instance.FreezeEverything(freezeEverything);
        }
    }

    void PlayerDirectionInput()
    {
        if (!freezeInput)
        {
#if UNITY_STANDALONE
            // get keyboard input
            keyHorizontal = Input.GetAxis("Horizontal");
            keyVertical = Input.GetAxisRaw("Vertical");
#endif

#if UNITY_ANDROID || UNITY_IOS
            // get on-screen virtual input
            keyHorizontal = SimpleInput.GetAxisRaw("Horizontal");
            keyVertical = SimpleInput.GetAxisRaw("Vertical");
#endif
        }
    }

    void PlayerJumpInput()
    {
#if UNITY_STANDALONE
        // get keyboard input
        if (!freezeInput)
        {
            keyJump = Input.GetKeyDown(KeyCode.Space);
        }
#endif
    }

    void PlayerShootInput()
    {
#if UNITY_STANDALONE
        // get keyboard input
        if (!freezeInput)
        {
            keyShoot = Input.GetKey(KeyCode.C);
        }
#endif
    }

    void PlayerMovement()
    {
        // these are for the ladder climbing but can be used elsewhere
        // y position and the y position with the climb sprite height
        transformY = transform.position.y;
        transformHY = transformY + climbSpriteHeight;

        // override speed may vary depending on state
        float speed = moveSpeed;

        // ladder climbing part
        if (isClimbing)
        {
            // debug lines for our ladder handling
            Debug.DrawLine(new Vector3(ladder.posX - 2f, ladder.posTopHandlerY, 0),
                new Vector3(ladder.posX + 2f, ladder.posTopHandlerY, 0), Color.blue);
            Debug.DrawLine(new Vector3(ladder.posX - 2f, ladder.posBottomHandlerY, 0),
                new Vector3(ladder.posX + 2f, ladder.posBottomHandlerY, 0), Color.blue);
            Debug.DrawLine(new Vector3(transform.position.x - 2f, transformHY, 0),
                new Vector3(transform.position.x + 2f, transformHY, 0), Color.magenta);
            Debug.DrawLine(new Vector3(transform.position.x - 2f, transformY, 0),
                new Vector3(transform.position.x + 2f, transformY, 0), Color.magenta);

            // we just passed the top ladder handler position
            if (transformHY > ladder.posTopHandlerY)
            {
                // this should only happen when we're not climbing down
                // otherwise we get some real funky results!
                if (!isClimbingDown)
                {
                    // start the climb transition animation
                    if (!startedClimbTransition)
                    {
                        startedClimbTransition = true;
                        ClimbTransition(true);
                    }
                    else if (finishedClimbTransition)
                    {
                        // we only want this block to happen once
                        finishedClimbTransition = false;

                        // we may not be completely touching the ground so setting
                        // this to false will stop the jump landed audio clip
                        isJumping = false;

                        // climb transition has finished now reposition ourself
                        // we kind of dip into the ground so we pad a little on our new y
                        PlayAnimation("Player_Idle");
                        transform.position = new Vector2(ladder.posX, ladder.posPlatformY + 0.005f);

                        // at the top of the ladder
                        if (!atLaddersEnd)
                        {
                            // reset climbing after a short delay
                            // gives the rigidbody and ground check to settle
                            atLaddersEnd = true;
                            Invoke("ResetClimbing", 0.1f);
                        }
                    }
                }
            }
            else if (transformHY < ladder.posBottomHandlerY)
            {
                // reaching this point means we have gone below of bottom handler
                // and haven't touched the ground so we should let go of the ladder
                ResetClimbing();
            }
            else
            {
                // this should only happen when we're not climbing down
                // otherwise we get some real funky results!
                if (!isClimbingDown)
                {
                    // jump off the ladder as long as there is no vertical input
                    if (keyJump && keyVertical == 0)
                    {
                        ResetClimbing();
                    }
                    // reached the ground by climbing down
                    else if (isGrounded && !hasStartedClimbing)
                    {
                        // we may not be completely touching the ground so setting
                        // this to false will stop the jump landed audio clip
                        isJumping = false;

                        // climbing has finished and now reposition ourself
                        // we kind of dip into the ground so we shave a little off our new y
                        PlayAnimation("Player_Idle");
                        transform.position = new Vector2(ladder.posX, ladder.posBottomY - 0.005f);

                        // at the bottom of the ladder
                        if (!atLaddersEnd)
                        {
                            // reset climbing after a short delay
                            // gives the rigidbody and ground check to settle
                            atLaddersEnd = true;
                            Invoke("ResetClimbing", 0.1f);
                        }
                    }
                    // somewhere in between the top and bottom of the ladder
                    else
                    {
                        // added in camera transition checks - when camera transitions
                        // occur we have to NOT modify the animator speed and player
                        // transform position in the climbing code because the transition
                        // will do it -- otherwise we end up having two code blocks fighting
                        // each other -- the FreezePlayer() method won't work here because
                        // it's coded for dynamic rigidbody's and freezing all constraints
                        // -- when we're ladder climbing our rigidbody is set to kinematic
                        //
                        //   camera transitions will control animator speed
                        //   camera transitions will control player movement position

                        // animate if we're moving in either direction
                        if (!GameManager.Instance.InCameraTransition())
                        {
                            animator.speed = Mathf.Abs(keyVertical);
                        }

                        // move on the ladder as long as we're not shooting/throwing
                        if (keyVertical != 0 && !isShooting && !isThrowing &&
                            !GameManager.Instance.InCameraTransition())
                        {
                            // apply the direction and climb speed to our position
                            Vector3 climbDirection = new Vector3(0, climbSpeed) * keyVertical;
                            transform.position = transform.position + climbDirection * Time.deltaTime;
                        }

                        // if we're shooting or throwing then we can change our horizontal direction
                        if (isShooting || isThrowing)
                        {
                            // update the facing direction
                            if (keyHorizontal < 0)
                            {
                                // facing right while shooting left - flip
                                if (isFacingRight)
                                {
                                    Flip();
                                }
                            }
                            else if (keyHorizontal > 0)
                            {
                                // facing left while shooting right - flip
                                if (!isFacingRight)
                                {
                                    Flip();
                                }
                            }
                            // and then choose which animation to play
                            if (isShooting)
                            {
                                // play the shooting climb animation
                                PlayAnimation("Player_ClimbShoot");
                            }
                            else if (isThrowing)
                            {
                                // play the throwing climb animation
                                PlayAnimation("Player_ClimbThrow");
                            }
                        }
                        else
                        {
                            // not shooting or throwing then we play
                            // the regular climbing animation
                            PlayAnimation("Player_Climb");
                        }
                    }
                }
            }
        }
        // not climbing on any ladders
        else
        {
            // left arrow key - moving left
            if (keyHorizontal < 0)
            {
                // facing right while moving left - flip
                if (isFacingRight)
                {
                    Flip();
                }
                // grounded play run animation
                if (isGrounded)
                {
                    // play run shoot or run animation
                    if (isShooting)
                    {
                        PlayAnimation("Player_RunShoot");
                    }
                    else if (isThrowing)
                    {
                        speed = 0f;
                        PlayAnimation("Player_Throw");
                    }
                    else
                    {
                        PlayAnimation("Player_Run");
                    }
                }
            }
            else if (keyHorizontal > 0) // right arrow key - moving right
            {
                // facing left while moving right - flip
                if (!isFacingRight)
                {
                    Flip();
                }
                // grounded play run animation
                if (isGrounded)
                {
                    // play run shoot or run animation
                    if (isShooting)
                    {
                        PlayAnimation("Player_RunShoot");
                    }
                    else if (isThrowing)
                    {
                        speed = 0f;
                        PlayAnimation("Player_Throw");
                    }
                    else
                    {
                        PlayAnimation("Player_Run");
                    }
                }
            }
            else   // no movement
            {
                // grounded play idle animation
                if (isGrounded)
                {
                    // play shoot or idle animation
                    if (isShooting)
                    {
                        PlayAnimation("Player_Shoot");
                    }
                    else if (isThrowing)
                    {
                        PlayAnimation("Player_Throw");
                    }
                    else
                    {
                        PlayAnimation("Player_Idle");
                    }
                }
            }

            // move speed * direction (no movement zero x velocity)
            rb2d.velocity = new Vector2(speed * keyHorizontal, rb2d.velocity.y);

            // pressing jump while grounded - can only jump once
            Jump();

            // while not grounded play jump animation (jumping or falling)
            if (!isGrounded)
            {
                // triggers jump landing sound effect in FixedUpdate
                isJumping = true;
                // jump or jump shoot animation
                if (isShooting)
                {
                    PlayAnimation("Player_JumpShoot");
                }
                else if (isThrowing)
                {
                    PlayAnimation("Player_JumpThrow");
                }
                else
                {
                    PlayAnimation("Player_Jump");
                }
            }

            // start ladder climbing here
            StartClimbingUp();
            StartClimbingDown();
        }
    }

    void Flip()
    {
        // invert facing direction and rotate object 180 degrees on y axis
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    void Jump()
    {
        // pressing jump while grounded and a jump hasn't started yet
        if (keyJump && isGrounded && !jumpStarted)
        {
            // jump velocity - lift off
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpSpeed);
            // call the jump coroutine below
            StartCoroutine(JumpCo());
        }
    }

    private IEnumerator JumpCo()
    {
        // delay to give time leaving the ground
        jumpStarted = true;
        yield return new WaitForSeconds(Time.fixedDeltaTime);
        jumpStarted = false;
    }

    void PlayAnimation(string animationName, int layer = -1, float normalizedTime = float.NegativeInfinity)
    {
        // allow our animations to play through from repeated calls
        if (animationName != lastAnimationName)
        {
            lastAnimationName = animationName;
            animator.Play(animationName, layer, normalizedTime);
        }
    }

    public bool IsGrounded()
    {
        // player's grounded status
        return isGrounded;
    }

    public bool IsInvincible()
    {
        // player's invincibility status
        return isInvincible;
    }

    public void SetWeapon(WeaponTypes weapon)
    {
        /* ColorSwap and Shader to change MegaMan's color scheme
         * 
         * his spritesheets have been altered to greyscale for his outfit
         * Red 64 for the helmet, gloves, boots, etc ( SwapIndex.Primary )
         * Red 128 for his shirt, pants, etc ( SwapIndex.Secondary )
         * 
         * couple ways to code this but I settled on #2
         * 
         * #1 using Lists
         * 
         * var colorIndex = new List<int>();
         * var playerColors = new List<Color>();
         * colorIndex.Add((int)SwapIndex.Primary);
         * colorIndex.Add((int)SwapIndex.Secondary);
         * playerColors.Add(ColorSwap.ColorFromIntRGB(64, 64, 64));
         * playerColors.Add(ColorSwap.ColorFromIntRGB(128, 128, 128));
         * colorSwap.SwapColors(colorIndex, playerColors);
         * 
         * #2 using SwapColor as needed then ApplyColor
         * 
         * colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x0073F7));
         * colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0x00FFFF));
         * colorSwap.ApplyColor();
         * 
         * Also, we'll change the color of our weapon energy bar
         * and adjust the energy value as given in the playerWeaponsStruct
         * 
         */

        // set new selected weapon (determines color scheme)
        playerWeapon = weapon;

        // calculate weapon energy value to adjust the bars
        int currentEnergy = weaponsData[(int)playerWeapon].currentEnergy;
        int maxEnergy = weaponsData[(int)playerWeapon].maxEnergy;
        float weaponEnergyValue = (float)currentEnergy / (float)maxEnergy;

        // apply new selected color scheme with ColorSwap and set weapon energy bar
        switch (playerWeapon)
        {
            case WeaponTypes.MegaBuster:
                // dark blue, light blue
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x0073F7));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0x00FFFF));
                // the player weapon energy doesn't apply but we'll just set the default and hide it
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.PlayerLife);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, false);
                }
                break;
            case WeaponTypes.MagnetBeam:
                // dark blue, light blue
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x0073F7));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0x00FFFF));
                // magnet beam energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.MagnetBeam);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
            case WeaponTypes.HyperBomb:
                // green, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x009400));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // bombman's hyper bomb weapon energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.HyperBomb);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
            case WeaponTypes.RollingCutter:
                // dark gray, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x747474));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // cutman's rolling cutter weapon energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.RollingCutter);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
            case WeaponTypes.ThunderBeam:
                // dark gray, light yellow
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x747474));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCE4A0));
                // elecman's thunderbeam weapon energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.ThunderBeam);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
            case WeaponTypes.FireStorm:
                // dark orange, yellow gold
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0xD82800));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xF0BC3C));
                // fireman's firestorm weapon energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.FireStorm);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
            case WeaponTypes.SuperArm:
                // orange red, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0xC84C0C));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // gutman's super arm weapon energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.SuperArm);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
            case WeaponTypes.IceSlasher:
                // dark blue, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x2038EC));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // iceman's ice slasher weapon energy and set visible
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.IceSlasher);
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                    UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                }
                break;
        }

        // apply the color changes
        colorSwap.ApplyColor();
    }

    public void SwitchWeapon(WeaponTypes weaponType)
    {
        // we can call this function to switch the player to the chosen weapon
        // change color scheme, do the teleport animation, and enable weapon usage
        ResetClimbing();
        SetWeapon(weaponType);
        Teleport(true, false);
        CanUseWeaponAgain();

        // update any in scene bonus item color palettes
        GameManager.Instance.SetBonusItemsColorPalette();
    }

    void FireWeapon()
    {
        // each weapon has its own function for firing
        switch (playerWeapon)
        {
            case WeaponTypes.MegaBuster:
                MegaBuster();
                break;
            case WeaponTypes.MagnetBeam:
                MagnetBeam();
                break;
            case WeaponTypes.HyperBomb:
                HyperBomb();
                break;
            case WeaponTypes.RollingCutter:
                break;
            case WeaponTypes.ThunderBeam:
                break;
            case WeaponTypes.FireStorm:
                break;
            case WeaponTypes.SuperArm:
                break;
            case WeaponTypes.IceSlasher:
                break;
        }
    }

    void MegaBuster()
    {
        shootTimeLength = 0;
        keyShootReleaseTimeLength = 0;

        // shoot key is being pressed and key release flag true
        if (keyShoot && keyShootRelease)
        {
            isShooting = true;
            keyShootRelease = false;
            shootTime = Time.time;
            // Shoot Bullet
            Invoke("ShootBullet", 0.1f);
        }
        // shoot key isn't being pressed and key release flag is false
        if (!keyShoot && !keyShootRelease)
        {
            keyShootReleaseTimeLength = Time.time - shootTime;
            keyShootRelease = true;
        }
        // while shooting limit its duration
        if (isShooting)
        {
            shootTimeLength = Time.time - shootTime;
            if (shootTimeLength >= 0.25f || keyShootReleaseTimeLength >= 0.15f)
            {
                isShooting = false;
            }
        }
    }

    void HyperBomb()
    {
        shootTimeLength = 0;
        keyShootReleaseTimeLength = 0;

        // shoot key is being pressed and key release flag true
        if (keyShoot && keyShootRelease && canUseWeapon)
        {
            // only be able to throw a hyper bomb if there is energy to do so
            // placing the check here so isThrowing can't become true and activate the arm throw animation
            if (weaponsData[(int)WeaponTypes.HyperBomb].currentEnergy > 0)
            {
                isThrowing = true;
                canUseWeapon = false;
                keyShootRelease = false;
                shootTime = Time.time;
                // Throw Bomb
                Invoke("ThrowBomb", 0.1f);
                // spend weapon energy and refresh energy bar
                SpendWeaponEnergy(WeaponTypes.HyperBomb);
                RefreshWeaponEnergyBar(WeaponTypes.HyperBomb);
            }
        }
        // shoot key isn't being pressed and key release flag is false
        if (!keyShoot && !keyShootRelease)
        {
            keyShootReleaseTimeLength = Time.time - shootTime;
            keyShootRelease = true;
        }
        // while shooting limit its duration
        if (isThrowing)
        {
            shootTimeLength = Time.time - shootTime;
            if (shootTimeLength >= 0.25f)
            {
                isThrowing = false;
            }
        }
    }

    void MagnetBeam()
    {
        shootTimeLength = 0;
        keyShootReleaseTimeLength = 0;

        // shoot key is being pressed and key release flag true
        if (keyShoot && keyShootRelease && canUseWeapon)
        {
            // only be able to use the magnet beam if there is energy to do so
            // and haven't hit the maxinum number of beams on screen at a single time (3)
            if (weaponsData[(int)WeaponTypes.MagnetBeam].currentEnergy > 0 &&
                GameObject.FindGameObjectsWithTag("PlatformBeam").Length < 3)
            {
                isShooting = true;
                canUseWeapon = false;
                keyShootRelease = false;
                shootTime = Time.time;
                // Shoot Magnet Beam
                ShootMagnetBeam();
                // spend weapon energy and refresh energy bar
                SpendWeaponEnergy(WeaponTypes.MagnetBeam);
                RefreshWeaponEnergyBar(WeaponTypes.MagnetBeam);
            }
        }
        // shoot key isn't being pressed and key release flag is false
        if (!keyShoot && !keyShootRelease)
        {
            shootTimeLength = Time.time - shootTime;
            keyShootReleaseTimeLength = Time.time - shootTime;
            keyShootRelease = true;
        }
        // shoot key released while shooting
        if (isShooting && !keyShoot)
        {
            isShooting = false;
            GameObject beam = bulletShootPos.transform.GetChild(0).gameObject;
            if (beam != null)
            {
                // just to make sure it is a platform beam
                if (beam.tag.Equals("PlatformBeam"))
                {
                    // lock beam into place
                    beam.GetComponent<MagnetBeamScript>().LockBeam();
                }
            }
        }
    }

    public void ApplyLifeEnergy(int amount)
    {
        // only apply health if we need it
        if (currentHealth < maxHealth)
        {
            int healthDiff = maxHealth - currentHealth;
            if (healthDiff > amount) healthDiff = amount;
            // animate adding health bars via coroutine
            StartCoroutine(AddLifeEnergy(healthDiff));
        }
    }

    private IEnumerator AddLifeEnergy(int amount)
    {
        // loop the energy fill audio clip
        SoundManager.Instance.Play(energyFillClip, true);
        // increment the health bars with a small delay
        for (int i = 0; i < amount; i++)
        {
            currentHealth++;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerHealth, currentHealth / (float)maxHealth);
            yield return new WaitForSeconds(0.05f);
        }
        // done playing energy fill clip
        SoundManager.Instance.Stop();
    }

    public void ApplyWeaponEnergy(int amount)
    {
        // only apply weapon energy if we need it
        int wt = (int)playerWeapon;
        if (weaponsData[wt].currentEnergy < weaponsData[wt].maxEnergy)
        {
            int energyDiff = weaponsData[wt].maxEnergy - weaponsData[wt].currentEnergy;
            if (energyDiff > amount) energyDiff = amount;
            // animate adding energy bars via coroutine
            StartCoroutine(AddWeaponEnergy(energyDiff));
        }
    }

    private IEnumerator AddWeaponEnergy(int amount)
    {
        int wt = (int)playerWeapon;
        // loop the energy fill audio clip
        SoundManager.Instance.Play(energyFillClip, true);
        // increment the energy bars with a small delay
        for (int i = 0; i < amount; i++)
        {
            weaponsData[wt].currentEnergy++;
            weaponsData[wt].currentEnergy = Mathf.Clamp(weaponsData[wt].currentEnergy, 0, weaponsData[wt].maxEnergy);
            UIEnergyBars.Instance.SetValue(
                UIEnergyBars.EnergyBars.PlayerWeapon,
                weaponsData[wt].currentEnergy / (float)weaponsData[wt].maxEnergy);
            yield return new WaitForSeconds(0.05f);
        }
        // done playing energy fill clip
        SoundManager.Instance.Stop();
    }

    public void FillWeaponEnergies()
    {
        // start all energy bars at full
        for (int i = 0; i < weaponsData.Length; i++)
        {
            weaponsData[i].currentEnergy = weaponsData[i].maxEnergy;
        }
    }

    public void EnableMagnetBeam(bool enable)
    {
        // enable/disable the magnet beam
        weaponsData[(int)WeaponTypes.MagnetBeam].enabled = enable;
    }

    public void EnableWeaponPart(ItemScript.WeaponPartEnemies weaponPartEnemy)
    {
        // this will enable the collected weapon part in our weapon struct
        switch (weaponPartEnemy)
        {
            case ItemScript.WeaponPartEnemies.BombMan:
                weaponsData[(int)WeaponTypes.HyperBomb].enabled = true;
                break;
            case ItemScript.WeaponPartEnemies.CutMan:
                weaponsData[(int)WeaponTypes.RollingCutter].enabled = true;
                break;
            case ItemScript.WeaponPartEnemies.ElecMan:
                weaponsData[(int)WeaponTypes.ThunderBeam].enabled = true;
                break;
            case ItemScript.WeaponPartEnemies.FireMan:
                weaponsData[(int)WeaponTypes.FireStorm].enabled = true;
                break;
            case ItemScript.WeaponPartEnemies.GutsMan:
                weaponsData[(int)WeaponTypes.SuperArm].enabled = true;
                break;
            case ItemScript.WeaponPartEnemies.IceMan:
                weaponsData[(int)WeaponTypes.IceSlasher].enabled = true;
                break;
        }
    }

    void ShootBullet()
    {
        // create bullet from prefab gameobject
        GameObject bullet = Instantiate(weaponsData[(int)WeaponTypes.MegaBuster].weaponPrefab);
        // set its name to that of the prefab so it doesn't include "(Clone)" when instantiated
        bullet.name = weaponsData[(int)WeaponTypes.MegaBuster].weaponPrefab.name + "(" + gameObject.name + ")";
        bullet.transform.position = bulletShootPos.position;
        // set bullet damage amount, speed, direction bullet will travel along the x, and fire!
        bullet.GetComponent<BulletScript>().SetDamageValue(weaponsData[(int)WeaponTypes.MegaBuster].weaponDamage);
        bullet.GetComponent<BulletScript>().SetBulletSpeed(weaponsData[(int)WeaponTypes.MegaBuster].weaponVelocity.x);
        bullet.GetComponent<BulletScript>().SetBulletDirection((isFacingRight) ? Vector2.right : Vector2.left);
        bullet.GetComponent<BulletScript>().SetDestroyDelay(5f);
        bullet.GetComponent<BulletScript>().Shoot();
        SoundManager.Instance.Play(weaponsData[(int)WeaponTypes.MegaBuster].weaponClip);
    }

    void ThrowBomb()
    {
        // create bomb from prefab gameobject
        GameObject bomb = Instantiate(weaponsData[(int)WeaponTypes.HyperBomb].weaponPrefab);
        bomb.name = weaponsData[(int)WeaponTypes.HyperBomb].weaponPrefab.name + "(" + gameObject.name + ")";
        bomb.transform.position = bulletShootPos.position;
        // set the bomb properties and throw it
        bomb.GetComponent<BombScript>().SetContactDamageValue(0);
        bomb.GetComponent<BombScript>().SetExplosionDamageValue(weaponsData[(int)WeaponTypes.HyperBomb].weaponDamage);
        bomb.GetComponent<BombScript>().SetExplosionDelay(3f);
        bomb.GetComponent<BombScript>().SetCollideWithTags("Enemy");
        bomb.GetComponent<BombScript>().SetDirection((isFacingRight) ? Vector2.right : Vector2.left);
        bomb.GetComponent<BombScript>().SetVelocity(weaponsData[(int)WeaponTypes.HyperBomb].weaponVelocity);
        bomb.GetComponent<BombScript>().Bounces(true);
        bomb.GetComponent<BombScript>().ExplosionEvent.AddListener(this.CanUseWeaponAgain);
        bomb.GetComponent<BombScript>().Launch(false);
    }

    void ShootMagnetBeam()
    {
        // create magnet beam platform from prefab gameobject
        GameObject beam = Instantiate(weaponsData[(int)WeaponTypes.MagnetBeam].weaponPrefab);
        beam.name = weaponsData[(int)WeaponTypes.MagnetBeam].weaponPrefab.name;
        beam.transform.position = bulletShootPos.position;
        beam.transform.parent = bulletShootPos.transform;
        // set the platform beam properties and play the audio clip
        beam.GetComponent<MagnetBeamScript>().SetDestroyDelay(3f);
        beam.GetComponent<MagnetBeamScript>().SetDirection((isFacingRight) ? Vector2.right : Vector2.left);
        beam.GetComponent<MagnetBeamScript>().SetMaxSegments(30);
        beam.GetComponent<MagnetBeamScript>().LockedEvent.AddListener(this.CanUseWeaponAgain);
        SoundManager.Instance.Play(weaponsData[(int)WeaponTypes.MagnetBeam].weaponClip);
    }

    void SpendWeaponEnergy(WeaponTypes weaponType)
    {
        // deplete the weapon energy and make sure the value is within bounds
        int wt = (int)weaponType;
        weaponsData[wt].currentEnergy -= weaponsData[wt].energyCost;
        weaponsData[wt].currentEnergy = Mathf.Clamp(weaponsData[wt].currentEnergy, 0, weaponsData[wt].maxEnergy);
    }

    void RefreshWeaponEnergyBar(WeaponTypes weaponType)
    {
        // refresh the weapon energy bar (should be called after SpendWeaponEnergy)
        int wt = (int)weaponType;
        if (UIEnergyBars.Instance != null)
        {
            UIEnergyBars.Instance.SetValue(
                UIEnergyBars.EnergyBars.PlayerWeapon,
                weaponsData[wt].currentEnergy / (float)weaponsData[wt].maxEnergy);
        }
    }

    public void CanUseWeaponAgain()
    {
        // many (almost all) of our weapons require they play out their animation or be destroyed
        // before another copy can be used so this function resets the flags to be able to fire again
        canUseWeapon = true;
        isShooting = false;
        isThrowing = false;
    }

    public void HitSide(bool rightSide)
    {
        // determines the push direction of the hit animation
        hitSideRight = rightSide;
    }

    public void Invincible(bool invincibility)
    {
        isInvincible = invincibility;
    }

    public void TakeDamage(int damage)
    {
        // take damage if not invincible
        if (!isInvincible)
        {
            // take damage amount from health and update the health bar
            if (damage > 0)
            {
                currentHealth -= damage;
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                if (UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerHealth, currentHealth / (float)maxHealth);
                }
                // no more health means defeat, otherwise take damage
                if (currentHealth <= 0)
                {
                    Defeat();
                }
                else
                {
                    StartDamageAnimation();
                }
            }
        }
    }

    void StartDamageAnimation()
    {
        // once isTakingDamage is true in the Update function we'll play the Hit animation
        // here we go invincible so we don't repeatedly take damage, determine the X push force
        // depending which side we were hit on, and then apply that force
        if (!isTakingDamage)
        {
            isTakingDamage = true;
            Invincible(true);
            FreezeInput(true);
            ResetClimbing();
            float hitForceX = 0.50f;
            float hitForceY = 1.5f;
            if (hitSideRight) hitForceX = -hitForceX;
            rb2d.velocity = Vector2.zero;
            rb2d.AddForce(new Vector2(hitForceX, hitForceY), ForceMode2D.Impulse);
            SoundManager.Instance.Play(takingDamageClip);
        }
    }

    void StopDamageAnimation()
    {
        // this function is called at the end of the Hit animation
        // and we reset the animation because it doesn't loop otherwise
        // we can end up stuck in it
        isTakingDamage = false;
        FreezeInput(false);
        PlayAnimation("Player_Hit", -1, 0f);
        StartCoroutine(FlashAfterDamage());
    }

    private IEnumerator FlashAfterDamage()
    {
        // hit animation is 12 samples
        // keep flashing consistent with 1/12 secs
        float flashDelay = 0.0833f;
        // get sprite's current material
        //Material material = sprite.material;
        // toggle transparency
        for (int i = 0; i < 10; i++)
        {
            //sprite.enabled = false;
            //sprite.material = null;
            sprite.material.SetFloat("_Transparency", 0f);
            //sprite.color = new Color(1, 1, 1, 0);
            //sprite.color = Color.clear;
            yield return new WaitForSeconds(flashDelay);
            //sprite.enabled = true;
            //sprite.material = material; // new Material(Shader.Find("Sprites/Default"));
            sprite.material.SetFloat("_Transparency", 1f);
            //sprite.color = new Color(1, 1, 1, 1);
            //sprite.color = Color.white;
            yield return new WaitForSeconds(flashDelay);
        }
        // no longer invincible
        Invincible(false);
    }

    private IEnumerator StartDefeatAnimation(bool explode)
    {
        // half second delay before we do it
        yield return new WaitForSeconds(0.5f);
        // freeze player and input and go KABOOM! (if enabled)
        FreezeInput(true);
        FreezePlayer(true);
        if (explode)
        {
            GameObject explodeEffect = Instantiate(explodeEffectPrefab);
            explodeEffect.name = explodeEffectPrefab.name;
            explodeEffect.transform.position = sprite.bounds.center;
            explodeEffect.GetComponent<ExplosionScript>().SetDestroyDelay(5f);
        }
        SoundManager.Instance.Play(explodeEffectClip);
        Destroy(gameObject);
    }

    void StopDefeatAnimation()
    {
        FreezeInput(false);
        FreezePlayer(false);
    }

    public void Defeat(bool explode = true)
    {
        // tell the game manager we died so it can take control
        GameManager.Instance.PlayerDefeated();
        // we died! player defeat animation
        StartCoroutine(StartDefeatAnimation(explode));
    }

    public void FreezeInput(bool freeze)
    {
        // freeze/unfreeze user input
        freezeInput = freeze;
        if (freeze &&
            !GameManager.Instance.InCameraTransition())
        {
            keyHorizontal = 0;
            keyVertical = 0;
            keyJump = false;
            keyShoot = false;
        }
    }

    public void FreezePlayer(bool freeze)
    {
        // freeze/unfreeze the player on screen
        // zero animation speed and freeze XYZ rigidbody constraints
        if (freeze)
        {
            freezePlayer = true;
            rb2dConstraints = rb2d.constraints;
            animator.speed = 0;
            rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            freezePlayer = false;
            animator.speed = 1;
            rb2d.constraints = rb2dConstraints;
        }
    }

    public void HidePlayer(bool hide)
    {
        // hide/show the player on the screen
        // get the current material alpha then set to zero (transparent)
        // restore the material alpha to its previous value
        if (hide)
        {
            playerColor = sprite.material.GetFloat("_Transparency");
            sprite.material.SetFloat("_Transparency", 0f);
        }
        else
        {
            sprite.material.SetFloat("_Transparency", playerColor);
        }
    }

    public void Teleport(bool teleport, bool descend = true)
    {
        if (teleport)
        {
            isTeleporting = true;
            FreezeInput(true);
            PlayAnimation("Player_Teleport");
            rb2dConstraints = rb2d.constraints;
            // descending will override the state and velocity
            teleportState = TeleportState.Landed;
            rb2d.velocity = new Vector2(0, rb2d.velocity.y);
            // we can set the descend flag to false if all we want is to show
            // the teleport animation - we go straight into the Landed state
            // if true then set to Descending state and apply teleport velocity
            if (descend)
            {
                animator.speed = 0;
                gameObject.tag = "Untagged";
                gameObject.layer = LayerMask.NameToLayer("Teleport");
                teleportState = TeleportState.Descending;
                rb2d.velocity = new Vector2(rb2d.velocity.x, teleportSpeed);
            }
        }
        else
        {
            isTeleporting = false;
            rb2d.constraints = rb2dConstraints;
            FreezeInput(false);
        }
    }

    // update the teleport landing position
    public void SetTeleportLanding(float landingY)
    {
        this.teleportLandingY = landingY;
    }

    // play the landing animation sound at the frame of impact otherwise
    // the sounds seems delayed when it originally was played at the animation end
    void TeleportAnimationSound()
    {
        SoundManager.Instance.Play(teleportClip);
    }

    // called at the end of the animation event to move to the Idle state
    // and play the landing sound (once)
    void TeleportAnimationEnd()
    {
        teleportState = TeleportState.Idle;
    }

    // wrapper for the StartedClimbingCo() coroutine below
    void StartedClimbing()
    {
        StartCoroutine(StartedClimbingCo());
    }

    // started climbing coroutine
    // (this gives us a delay from the ground check giving false positives)
    private IEnumerator StartedClimbingCo()
    {
        hasStartedClimbing = true;
        yield return new WaitForSeconds(0.1f);
        hasStartedClimbing = false;
    }

    // start climbing up a nearby ladder
    public void StartClimbingUp()
    {
        if (ladder != null)
        {
            // climbing up
            if (ladder.isNearLadder && keyVertical > 0 && transformHY < ladder.posTopHandlerY)
            {
                isClimbing = true;
                isClimbingDown = false;
                animator.speed = 0;
                rb2d.bodyType = RigidbodyType2D.Kinematic;
                rb2d.velocity = Vector2.zero;
                transform.position = new Vector3(ladder.posX, transformY + 0.025f, 0);
                StartedClimbing();
            }
        }
    }

    // start climbing down a nearby ladder
    public void StartClimbingDown()
    {
        if (ladder != null)
        {
            // climbing down
            if (ladder.isNearLadder && keyVertical < 0 && isGrounded && transformHY > ladder.posTopHandlerY)
            {
                isClimbing = true;
                isClimbingDown = true;
                animator.speed = 0;
                rb2d.bodyType = RigidbodyType2D.Kinematic;
                rb2d.velocity = Vector2.zero;
                transform.position = new Vector3(ladder.posX, transformY, 0);
                ClimbTransition(false);
            }
        }
    }

    // reset our ladder climbing variables and 
    // put back the animator speed and rigidbody type
    public void ResetClimbing()
    {
        // reset climbing if we're climbing
        if (isClimbing)
        {
            isClimbing = false;
            atLaddersEnd = false;
            startedClimbTransition = false;
            finishedClimbTransition = false;
            animator.speed = 1;
            rb2d.bodyType = RigidbodyType2D.Dynamic;
            rb2d.velocity = Vector2.zero;
        }
    }

    // wrapper for the ClimbTransitionCo() coroutine below
    void ClimbTransition(bool movingUp)
    {
        StartCoroutine(ClimbTransitionCo(movingUp));
    }

    // climbing transition animation for when we move to the top of
    // the ladder or when we move down from the top of it
    private IEnumerator ClimbTransitionCo(bool movingUp)
    {
        // we don't want any player input during this
        FreezeInput(true);

        // flag to signal we're not done performing the transition
        finishedClimbTransition = false;

        // there are two positions, going up and going down
        Vector3 newPos = Vector3.zero;
        if (movingUp)
        {
            // moving up we transition the top offset amount
            // (it looks like his body is half above the the ladder top)
            newPos = new Vector3(ladder.posX, transformY + ladder.handlerTopOffset, 0);
        }
        else
        {
            // moving down we first reposition our y (~position at the end of the moving up transition)
            // then we transition down the top offset amount so looks like we're climbing down from the top(ish)
            transform.position = new Vector3(ladder.posX, ladder.posTopHandlerY - climbSpriteHeight + ladder.handlerTopOffset, 0);
            newPos = new Vector3(ladder.posX, ladder.posTopHandlerY - climbSpriteHeight, 0);
        }

        while (transform.position != newPos)
        {
            // we are going to move towards the new position playing our other climb animation (the bent over look)
            transform.position = Vector3.MoveTowards(transform.position, newPos, climbSpeed * Time.deltaTime);
            animator.speed = 1;
            PlayAnimation("Player_ClimbTop");
            yield return null;
        }

        // done climbing down so those other code blocks can work again
        isClimbingDown = false;

        // now we're signaling that we finished the climb transition
        finishedClimbTransition = true;

        // give the player back their input
        FreezeInput(false);
    }

    public void MobileShootWrapper()
    {
        // wrapper function for button handler script
        // can't directly call coroutines
        if (!freezeInput)
        {
            StartCoroutine(MobileShoot());
        }
    }

    private IEnumerator MobileShoot()
    {
        // press shoot and release
        keyShoot = true;
        yield return new WaitForSeconds(0.01f);
        keyShoot = false;
    }

    public void MobileJumpWrapper()
    {
        // wrapper function for button handler script
        // can't directly call coroutines
        if (!freezeInput)
        {
            StartCoroutine(MobileJump());
        }
    }

    private IEnumerator MobileJump()
    {
        // press jump and release
        keyJump = true;
        yield return new WaitForSeconds(0.01f);
        keyJump = false;
    }

    public void SimulateMoveStop()
    {
        // no horizontal input
        keyHorizontal = 0f;
    }

    public void SimulateMoveLeft()
    {
        // value from pressing left on the keyboard
        keyHorizontal = -1.0f;
    }

    public void SimulateMoveRight()
    {
        // value from pressing right on the keyboard
        keyHorizontal = 1.0f;
    }

    public void SimulateShoot()
    {
        // use the existing shoot function
        StartCoroutine(MobileShoot());
    }

    public void SimulateJump()
    {
        // use the existing jump function
        StartCoroutine(MobileJump());
    }
}