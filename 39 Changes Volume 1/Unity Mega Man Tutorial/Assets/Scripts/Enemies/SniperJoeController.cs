using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperJoeController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    GameObject player;
    Vector3 playerPosition;

    bool isFacingRight;
    bool isGrounded;
    bool isBlocking;
    bool isJumping;
    bool isShooting;

    // delay for ground check
    bool jumpStarted;

    // flag to allow the next action
    bool doAction;

    // timer for delay between actions
    float actionTimer;

    // assigned from jumping vectors array
    Vector2 jumpVector;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // if you want to put a delay between his actions
    [SerializeField] float actionDelay = 0f;

    [SerializeField]
    Vector2[] jumpingVectors = {
        new Vector2(0, 3.7f),       // jump up
        new Vector2(1.5f, 3.7f)     // jump to get in front of player
    };

    void Awake()
    {
        // get components from EnemyController
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
        box2d = enemyController.GetComponent<BoxCollider2D>();
        rb2d = enemyController.GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // sprite sheet images face right
        isFacingRight = false;
        enemyController.Flip();

        // start picking an action
        doAction = true;
        actionTimer = actionDelay;

        // get player object - used for jumping direction
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void FixedUpdate()
    {
        isGrounded = false;
        Color raycastColor;
        RaycastHit2D raycastHit;
        float raycastDistance = 0.025f;
        int layerMask = 1 << LayerMask.NameToLayer("Ground");
        // ground check
        Vector3 box_origin = box2d.bounds.center;
        box_origin.y = box2d.bounds.min.y + (box2d.bounds.extents.y / 4f);
        Vector3 box_size = box2d.bounds.size;
        box_size.y = box2d.bounds.size.y / 4f;
        raycastHit = Physics2D.BoxCast(box_origin, box_size, 0f, Vector2.down, raycastDistance, layerMask);
        // sniper joe box colliding with ground layer
        if (raycastHit.collider != null && !jumpStarted)
        {
            isGrounded = true;
            // just landed from jumping/falling
            if (isJumping)
            {
                isJumping = false;
                doAction = true;
                actionTimer = actionDelay;
                animator.Play("SniperJoe_Block");
                rb2d.velocity = Vector2.zero;
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
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // do sniper joe's ai logic if it's enabled
        if (enableAI)
        {
            // get player position
            if (player != null) playerPosition = player.transform.position;

            if (isGrounded)
            {
                // player position check
                if (playerPosition.x <= transform.position.x)
                {
                    // player on the left, face that direction
                    if (isFacingRight)
                    {
                        isFacingRight = false;
                        enemyController.Flip();
                    }
                    // jump vector to go upward only
                    jumpVector = jumpingVectors[0];
                    // if we can choose the next action
                    if (doAction)
                    {
                        // if we have a delay set between actions
                        actionTimer -= Time.deltaTime;
                        if (actionTimer <= 0)
                        {
                            // choose the next action
                            ChooseNextAction();
                        }
                    }
                    // stay in the same spot (x is slippery)
                    rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                }
                else
                {
                    // jump to get in front of player
                    if (!isFacingRight)
                    {
                        isFacingRight = true;
                        enemyController.Flip();
                    }
                    // jump vector with x momentum
                    jumpVector = jumpingVectors[1];
                    // do the jump
                    Jump();
                }
            }
            else
            {
                // while not grounded
                isJumping = true;
                animator.Play("SniperJoe_Jump");
                rb2d.velocity = new Vector2(jumpVector.x, rb2d.velocity.y);
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetActionDelay(float delay)
    {
        // override default action delay
        this.actionDelay = delay;
    }

    public void SetJumpingVectors(params Vector2[] vectors)
    {
        // override default jumping vectors
        this.jumpingVectors = vectors;
    }

    private void ShootBullet()
    {
        Vector2 bulletVector = new Vector2(-1f, 0);

        // bullet orient to negative x-axis, rotate bullet direction if facing right
        if (isFacingRight)
        {
            bulletVector = UtilityFunctions.RotateByAngle(bulletVector, 180f);
        }
        // instantiate bullet prefab, set color, damage, speed, and direction
        GameObject bullet = Instantiate(enemyController.bulletPrefab);
        bullet.name = enemyController.bulletPrefab.name;
        bullet.transform.position = enemyController.bulletShootPos.transform.position;
        bullet.GetComponent<BulletScript>().SetBulletType(BulletScript.BulletTypes.MiniGreen);
        bullet.GetComponent<BulletScript>().SetDamageValue(enemyController.bulletDamage);
        bullet.GetComponent<BulletScript>().SetBulletSpeed(enemyController.bulletSpeed);
        bullet.GetComponent<BulletScript>().SetBulletDirection(bulletVector);
        bullet.GetComponent<BulletScript>().SetCollideWithTags("Player");
        bullet.GetComponent<BulletScript>().SetDestroyDelay(5f);
        bullet.GetComponent<BulletScript>().Shoot();

        // play only one bullet sound
        SoundManager.Instance.Play(enemyController.shootBulletClip);
    }

    // block any player attacks
    private void Block()
    {
        animator.Play("SniperJoe_Block", -1, 0);
        isBlocking = true;
        doAction = false;
    }

    // called at the end of the block animation
    private void StopBlockAnimation()
    {
        isBlocking = false;
        doAction = true;
        actionTimer = actionDelay;
    }

    // jump up or to get in front of the player
    private void Jump()
    {
        animator.Play("SniperJoe_Jump");
        isJumping = true;
        doAction = false;
        rb2d.velocity = jumpVector;
        StartCoroutine(JumpCo());
    }

    private IEnumerator JumpCo()
    {
        // delay to give time leaving the ground
        jumpStarted = true;
        yield return new WaitForSeconds(Time.fixedDeltaTime);
        jumpStarted = false;
    }

    // shoot at the player
    private void Shoot()
    {
        animator.Play("SniperJoe_Shoot", -1, 0);
        isShooting = true;
        doAction = false;
    }

    // called at the end of the shoot animation
    private void StopShootAnimation()
    {
        isShooting = false;
        doAction = true;
        actionTimer = actionDelay;
    }

    int PickRandomAction()
    {
        /* 
         * making use of this Unity article again
         * https://docs.unity3d.com/2019.3/Documentation/Manual/RandomNumbers.html
         * 
         * choice between sniper joe's actions weighted by probability
         * 
         * 40% chance of blocking
         * 40% chance of shooting
         * 20% chance of jumping
         * 
         * probabilities array index will be used by the switch in ChooseNextAction()
        */
        float[] probabilities = { 20, 40, 40 };

        float total = 0;

        foreach (float prob in probabilities)
        {
            total += prob;
        }

        float randomPoint = Random.value * total;

        for (int i = 0; i < probabilities.Length; i++)
        {
            if (randomPoint < probabilities[i])
            {
                return i;
            }
            else
            {
                randomPoint -= probabilities[i];
            }
        }
        return probabilities.Length - 1;
    }

    private void ChooseNextAction()
    {
        // probability function returns index for switch
        switch (PickRandomAction())
        {
            case 0:
                Jump();
                break;
            case 1:
                Shoot();
                break;
            case 2:
                Block();
                break;
        }
    }

    // we call these functions from our animations
    // Closed - make'em invincible
    private void StartInvincibleAnimation()
    {
        enemyController.Invincible(true);
    }

    // Open - beware the Mega Buster!
    private void StopInvincibleAnimation()
    {
        enemyController.Invincible(false);
    }
}