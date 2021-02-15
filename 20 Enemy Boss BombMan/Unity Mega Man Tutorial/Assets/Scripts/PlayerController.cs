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
    bool isJumping;
    bool isShooting;
    bool isTeleporting;
    bool isTakingDamage;
    bool isInvincible;
    bool isFacingRight;

    bool hitSideRight;

    bool freezeInput;
    bool freezePlayer;
    bool freezeBullets;

    float shootTime;
    bool keyShootRelease;

    RigidbodyConstraints2D rb2dConstraints;

    private enum SwapIndex
    {
        Primary = 64,
        Secondary = 128
    }

    public enum PlayerWeapons { Default, MagnetBeam, BombMan, CutMan, ElecMan, FireMan, GutsMan, IceMan };
    public PlayerWeapons playerWeapon = PlayerWeapons.Default;

    [System.Serializable]
    public struct PlayerWeaponsStruct
    {
        public PlayerWeapons weapon;
        public int currentEnergy;
        public int maxEnergy;
        public int energyCost;
        public int weaponDamage;
        public GameObject weaponPrefab;
    }
    public PlayerWeaponsStruct[] playerWeaponStructs;

    public int currentHealth;
    public int maxHealth = 28;

    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float jumpSpeed = 3.7f;

    [SerializeField] int bulletDamage = 1;
    [SerializeField] float bulletSpeed = 5f;

    [Header("Audio Clips")]
    [SerializeField] AudioClip teleportClip;
    [SerializeField] AudioClip jumpLandedClip;
    [SerializeField] AudioClip shootBulletClip;
    [SerializeField] AudioClip takingDamageClip;
    [SerializeField] AudioClip explodeEffectClip;
    [SerializeField] AudioClip energyFillClip;

    [Header("Positions and Prefabs")]
    [SerializeField] Transform bulletShootPos;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] GameObject explodeEffectPrefab;

    [Header("Teleport Settings")]
    [SerializeField] float teleportSpeed = -10f;
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

        // start all energy bars at full
        for (int i = 0; i < playerWeaponStructs.Length; i++)
        {
            playerWeaponStructs[i].currentEnergy = playerWeaponStructs[i].maxEnergy;
        }

        // color swap component to change megaman's palette
        colorSwap = GetComponent<ColorSwap>();

        // set player weapon (change color)
        SetWeapon(playerWeapon);

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
        float raycastDistance = 0.05f;
        int layerMask = 1 << LayerMask.NameToLayer("Ground");
        // ground check
        Vector3 box_origin = box2d.bounds.center;
        box_origin.y = box2d.bounds.min.y + (box2d.bounds.extents.y / 4f);
        Vector3 box_size = box2d.bounds.size;
        box_size.y = box2d.bounds.size.y / 4f;
        raycastHit = Physics2D.BoxCast(box_origin, box_size, 0f, Vector2.down, raycastDistance, layerMask);
        // player box colliding with ground layer
        if (raycastHit.collider != null)
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
                    if (isGrounded)
                    {
                        teleportState = TeleportState.Landed;
                    }
                    break;
                case TeleportState.Landed:
                    // events in the animation will be called
                    animator.speed = 1;
                    break;
                case TeleportState.Idle:
                    Teleport(false);
                    break;
            }
            return;
        }

        // taking damage from projectiles, touching enemies, or other environment objects
        if (isTakingDamage)
        {
            animator.Play("Player_Hit");
            return;
        }

        PlayerDebugInput();
        PlayerDirectionInput();
        PlayerJumpInput();
        PlayerShootInput();
        PlayerMovement();
    }

    void PlayerDebugInput()
    {
        // B for Bullets
        if (Input.GetKeyDown(KeyCode.B))
        {
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
            if (bullets.Length > 0)
            {
                freezeBullets = !freezeBullets;
                foreach (GameObject bullet in bullets)
                {
                    bullet.GetComponent<BulletScript>().FreezeBullet(freezeBullets);
                }
            }
            Debug.Log("Freeze Bullets: " + freezeBullets);
        }

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

        // P for Player
        if (Input.GetKeyDown(KeyCode.P))
        {
            FreezePlayer(!freezePlayer);
            Debug.Log("Freeze Player: " + freezePlayer);
        }

        // T for Teleport
        if (Input.GetKeyDown(KeyCode.T))
        {
            SetWeapon((PlayerWeapons)UnityEngine.Random.Range(0,
                Enum.GetValues(typeof(PlayerWeapons)).Length));
            Teleport(true);
            Debug.Log("Teleport(true)");
        }

        // #1 for random Enemy Health energy bar color
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UIEnergyBars.Instance.SetImage(
                UIEnergyBars.EnergyBars.EnemyHealth,
                (UIEnergyBars.EnergyBarTypes)UnityEngine.Random.Range(0,
                    Enum.GetValues(typeof(UIEnergyBars.EnergyBarTypes)).Length));
        }
    }

    void PlayerDirectionInput()
    {
        if (!freezeInput)
        {
#if UNITY_STANDALONE
            // get keyboard input
            keyHorizontal = Input.GetAxisRaw("Horizontal");
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
        float shootTimeLength = 0;
        float keyShootReleaseTimeLength = 0;

#if UNITY_STANDALONE
        // get keyboard input
        if (!freezeInput)
        {
            keyShoot = Input.GetKey(KeyCode.C);
        }
#endif

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

    void PlayerMovement()
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
                    animator.Play("Player_RunShoot");
                }
                else
                {
                    animator.Play("Player_Run");
                }
            }
            // negative move speed to go left
            rb2d.velocity = new Vector2(-moveSpeed, rb2d.velocity.y);
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
                    animator.Play("Player_RunShoot");
                }
                else
                {
                    animator.Play("Player_Run");
                }
            }
            // positive move speed to go right
            rb2d.velocity = new Vector2(moveSpeed, rb2d.velocity.y);
        }
        else   // no movement
        {
            // grounded play idle animation
            if (isGrounded)
            {
                // play shoot or idle animation
                if (isShooting)
                {
                    animator.Play("Player_Shoot");
                }
                else
                {
                    animator.Play("Player_Idle");
                }
            }
            // no movement zero x velocity
            rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
        }

        // pressing jump while grounded - can only jump once
        if (keyJump && isGrounded)
        {
            // play jump/jump shoot animation and jump speed on y velocity
            if (isShooting)
            {
                animator.Play("Player_JumpShoot");
            }
            else
            {
                animator.Play("Player_Jump");
            }
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpSpeed);
        }

        // while not grounded play jump animation (jumping or falling)
        if (!isGrounded)
        {
            // triggers jump landing sound effect in FixedUpdate
            isJumping = true;
            // jump or jump shoot animation
            if (isShooting)
            {
                animator.Play("Player_JumpShoot");
            }
            else
            {
                animator.Play("Player_Jump");
            }
        }
    }

    void Flip()
    {
        // invert facing direction and rotate object 180 degrees on y axis
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    public void SetWeapon(PlayerWeapons weapon)
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
        int currentEnergy = playerWeaponStructs[(int)playerWeapon].currentEnergy;
        int maxEnergy = playerWeaponStructs[(int)playerWeapon].maxEnergy;
        float weaponEnergyValue = (float)currentEnergy / (float)maxEnergy;

        // apply new selected color scheme with ColorSwap and set weapon energy bar
        switch (playerWeapon)
        {
            case PlayerWeapons.Default:
                // dark blue, light blue
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x0073F7));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0x00FFFF));
                // the player weapon energy doesn't apply but we'll just set the default and hide it
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.PlayerLife);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, false);
                break;
            case PlayerWeapons.MagnetBeam:
                // dark blue, light blue
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x0073F7));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0x00FFFF));
                // magnet beam energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.MagnetBeam);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
            case PlayerWeapons.BombMan:
                // green, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x009400));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // bombman's hyper bomb weapon energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.HyperBomb);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
            case PlayerWeapons.CutMan:
                // dark gray, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x747474));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // cutman's rolling cutter weapon energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.RollingCutter);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
            case PlayerWeapons.ElecMan:
                // dark gray, light yellow
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x747474));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCE4A0));
                // elecman's thunderbeam weapon energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.ThunderBeam);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
            case PlayerWeapons.FireMan:
                // dark orange, yellow gold
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0xD82800));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xF0BC3C));
                // fireman's firestorm weapon energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.FireStorm);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
            case PlayerWeapons.GutsMan:
                // orange red, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0xC84C0C));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // gutman's super arm weapon energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.SuperArm);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
            case PlayerWeapons.IceMan:
                // dark blue, light gray
                colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x2038EC));
                colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                // iceman's ice slasher weapon energy and set visible
                UIEnergyBars.Instance.SetImage(UIEnergyBars.EnergyBars.PlayerWeapon, UIEnergyBars.EnergyBarTypes.IceSlasher);
                UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerWeapon, weaponEnergyValue);
                UIEnergyBars.Instance.SetVisibility(UIEnergyBars.EnergyBars.PlayerWeapon, true);
                break;
        }

        // apply the color changes
        colorSwap.ApplyColor();
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
        // define this function when we get to boss battles and acquiring their weapons
    }

    void ShootBullet()
    {
        // create bullet from prefab gameobject
        GameObject bullet = Instantiate(bulletPrefab, bulletShootPos.position, Quaternion.identity);
        // set its name to that of the prefab so it doesn't include "(Clone)" when instantiated
        bullet.name = bulletPrefab.name;
        // set bullet damage amount, speed, direction bullet will travel along the x, and fire!
        bullet.GetComponent<BulletScript>().SetDamageValue(bulletDamage);
        bullet.GetComponent<BulletScript>().SetBulletSpeed(bulletSpeed);
        bullet.GetComponent<BulletScript>().SetBulletDirection((isFacingRight) ? Vector2.right : Vector2.left);
        bullet.GetComponent<BulletScript>().SetDestroyDelay(5f);
        bullet.GetComponent<BulletScript>().Shoot();
        SoundManager.Instance.Play(shootBulletClip);
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
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.PlayerHealth, currentHealth / (float)maxHealth);
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
        animator.Play("Player_Hit", -1, 0f);
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

    void StartDefeatAnimation()
    {
        // freeze player and input and go KABOOM!
        FreezeInput(true);
        FreezePlayer(true);
        GameObject explodeEffect = Instantiate(explodeEffectPrefab);
        explodeEffect.name = explodeEffectPrefab.name;
        explodeEffect.transform.position = sprite.bounds.center;
        SoundManager.Instance.Play(explodeEffectClip);
        Destroy(gameObject);
    }

    void StopDefeatAnimation()
    {
        FreezeInput(false);
        FreezePlayer(false);
    }

    void Defeat()
    {
        // tell the game manager we died so it can take control
        GameManager.Instance.PlayerDefeated();
        // we died! player defeat animation - half second delay
        Invoke("StartDefeatAnimation", 0.5f);
    }

    public void FreezeInput(bool freeze)
    {
        // freeze/unfreeze user input
        freezeInput = freeze;
        if (freeze)
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

    public void Teleport(bool teleport)
    {
        if (teleport)
        {
            isTeleporting = true;
            FreezeInput(true);
            animator.Play("Player_Teleport");
            animator.speed = 0;
            teleportState = TeleportState.Descending;
            rb2d.velocity = new Vector2(rb2d.velocity.x, teleportSpeed);
        }
        else
        {
            isTeleporting = false;
            FreezeInput(false);
        }
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
