using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BombManController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;
    EnemyController enemyController;

    bool isFacingRight;
    bool isGrounded;
    bool isJumping;
    bool isTakingDamage;

    // controls when bombman can jump, throw, 
    // and how many bombs to throw between jumps
    bool canJump;
    bool canThrow;
    int bombThrowCount;

    // X direction multiplier and tracking
    float jumpDirection;
    int lastJumpDirection;
    int lastJumpDirectionCount;

    // holds the jump animation name
    // three styles Jump1, Jump2, Jump3
    // i.e. upward, forward, backward
    string jumpAnimation;

    // handles to the bomb and player objects
    GameObject bomb;
    GameObject player;

    // player position
    Vector3 playerPosition;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // hand position where the bomb will rest
    // prefab of the bomb to be instantiated
    // hit bubble sprite for flashing when taking damage
    [SerializeField] Transform handPos;
    [SerializeField] GameObject bombPrefab;
    [SerializeField] SpriteRenderer damageSprite;

    // distance from player to attack (throw bombs)
    [SerializeField] float attackDistance = 1f;

    // bomb launch height
    [SerializeField] float bombHeight = 1f;

    // target offset for where the bomb will land
    [SerializeField] float targetOffset = 0.25f;

    // posing bomb toss velocity
    [SerializeField] Vector2 tossVelocity = new Vector2(0, 3f);

    // we use some fixed jump velocities
    [SerializeField]
    Vector2[] jumpVelocities = {
        new Vector2(1f, 4f),
        new Vector2(2f, 4.5f),
        new Vector2(3f, 5f)
    };

    public enum MoveDirections { Left, Right };
    [SerializeField] MoveDirections moveDirection = MoveDirections.Left;

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
        box2d = enemyController.GetComponent<BoxCollider2D>();
        rb2d = enemyController.GetComponent<Rigidbody2D>();
        sprite = enemyController.GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // sprite sheet images face left
        // switch to facing left if it's set
        isFacingRight = false;
        if (moveDirection == MoveDirections.Right)
        {
            isFacingRight = true;
            enemyController.Flip();
        }

        // give bombman his weapon
        NewBomb();

        // start with being able to jump or throw bombs
        canJump = true;
        canThrow = true;
        bombThrowCount = ChooseBombCount();

        // get a reference to the player to watch his position
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
        // bombman box colliding with ground layer
        if (raycastHit.collider != null)
        {
            isGrounded = true;
            if (isJumping)
            {
                // allow jumps, throws, when landing set to idle animation, 
                // and zero velocity so there is no sliding from the force of landing
                canJump = true;
                canThrow = true;
                isJumping = false;
                bombThrowCount = ChooseBombCount();
                animator.Play("BombMan_Idle");
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

        // get player position
        if (player != null) playerPosition = player.transform.position;

        // change facing direction depending where player is
        if (playerPosition.x < transform.position.x)
        {
            if (isFacingRight)
            {
                isFacingRight = !isFacingRight;
                enemyController.Flip();
            }
        }
        else
        {
            if (!isFacingRight)
            {
                isFacingRight = !isFacingRight;
                enemyController.Flip();
            }
        }

        // distance between bombman and the player
        float playerDistance = Vector2.Distance(playerPosition, transform.position);

        // do bombman ai logic if it's enabled
        if (enableAI)
        {
            if (isGrounded)
            {
                if (playerDistance >= attackDistance)
                {
                    // throw bombs when distant from player
                    if (bombThrowCount > 0)
                    {
                        if (canThrow)
                        {
                            // throw the bomb
                            Throw();
                        }
                    }
                    else
                    {
                        // jump away after bomb throwing is done
                        Jump();
                    }
                }
                else
                {
                    // jump away if close to the player
                    Jump();
                }
            }
            else
            {
                // play the current jump animation
                isJumping = true;
                animator.Play(jumpAnimation);
            }
        }

        // just to see our cool bomb toss and catch pose
        // it'll be removed when we do the boss fight animation intro
        /*
        if (Input.GetKeyDown(KeyCode.R))
        {
            Pose();
        }
        */
    }

    void Launch()
    {
        // we don't want the bomb to land exactly in the player's position
        if (playerPosition.x > transform.position.x) targetOffset *= -1f;
        // set up the bomb properties and launch it
        bomb.GetComponent<BombScript>().SetContactDamageValue(4);
        bomb.GetComponent<BombScript>().SetExplosionDamageValue(4);
        bomb.GetComponent<BombScript>().SetExplosionDelay(0);
        bomb.GetComponent<BombScript>().SetCollideWithTags("Player");
        bomb.GetComponent<BombScript>().SetSourcePosition(handPos.position);
        bomb.GetComponent<BombScript>().SetTargetPosition(playerPosition);
        bomb.GetComponent<BombScript>().SetTargetOffset(targetOffset);
        bomb.GetComponent<BombScript>().SetHeight(bombHeight);
        bomb.GetComponent<BombScript>().Bounces(false);
        bomb.GetComponent<BombScript>().Launch();
        /* 
         * this block of code will be for how to set up MegaMan's hyperbomb usage
         * and moved out when we get to the boss fight and weapon part collection
         * 
        bomb.GetComponent<BombScript>().SetContactDamageValue(0);
        bomb.GetComponent<BombScript>().SetExplosionDamageValue(4);
        bomb.GetComponent<BombScript>().SetExplosionDelay(3f);
        bomb.GetComponent<BombScript>().SetCollideWithTags("Enemy");
        bomb.GetComponent<BombScript>().SetDirection(Vector2.right);
        bomb.GetComponent<BombScript>().SetVelocity(new Vector2(2f, 1.5f));
        bomb.GetComponent<BombScript>().Bounces(true);
        bomb.GetComponent<BombScript>().Launch();
        */
        // detach the bomb from the hand position
        bomb.transform.parent = null;
    }

    public void Pose()
    {
        // the animation event calls for tossing the bomb up - TossBomb()
        animator.Play("BombMan_Pose");
        animator.speed = 1;
    }

    void TossBomb()
    {
        // set the bomb to solid and a dynamic rigidbody
        bomb.GetComponent<CircleCollider2D>().isTrigger = false;
        bomb.GetComponent<Rigidbody2D>().isKinematic = false;
        // give the bomb some vertical push to leave his hand
        bomb.GetComponent<Rigidbody2D>().velocity = tossVelocity;
        // detach the bomb from the hand position
        bomb.transform.parent = null;
        // start the coroutine to catch the bomb
        StartCoroutine(CatchBombCo());
    }

    IEnumerator CatchBombCo()
    {
        bool caughtBomb = false;

        // give the bomb a chance to leave his hand
        yield return new WaitForSeconds(0.1f);

        while (!caughtBomb)
        {
            // if the bomb falls below or at his hand position
            if (bomb.transform.position.y <= handPos.position.y)
            {
                caughtBomb = true;
                // reset the animation
                animator.Play("BombMan_Pose", -1, 0);
                animator.speed = 0;
                // set the bomb back to a trigger and a kinematic rigidbody
                bomb.GetComponent<CircleCollider2D>().isTrigger = true;
                bomb.GetComponent<Rigidbody2D>().isKinematic = true;
                // set the bomb position and parent to the hand
                bomb.transform.parent = handPos.transform;
                bomb.transform.position = handPos.position;
            }
            // yield until the bomb is caught
            yield return new WaitForEndOfFrame();
        }
    }

    void PrepareThrow()
    {
        // reset the animation
        animator.Play("BombMan_Throw", -1, 0);
        animator.speed = 0;
        // instantiate a new bomb
        NewBomb();
        // bombman can start throwing or jumping
        canThrow = true;
        canJump = true;
    }

    public void Throw()
    {
        // the animaion event calls for launching the bomb - Launch()
        animator.Play("BombMan_Throw");
        animator.speed = 1;
        // bombman has to wait until the bomb explodes before doing anything else
        canThrow = false;
        canJump = false;
        // one less bomb to throw
        bombThrowCount--;
    }

    void NewBomb()
    {
        // create a new bomb and set its parent and position
        bomb = Instantiate(bombPrefab);
        bomb.name = bombPrefab.name + "(" + gameObject.name + ")";
        bomb.transform.parent = handPos.transform;
        bomb.transform.position = handPos.position;
        // set bomb contact damage and to collide with player tag
        bomb.GetComponent<BombScript>().SetContactDamageValue(4);
        bomb.GetComponent<BombScript>().SetCollideWithTags("Player");
        // set function to be called for explosion event
        bomb.GetComponent<BombScript>().ExplosionEvent.AddListener(this.PrepareThrow);
        // flip bomb if bombman is facing right
        if (isFacingRight)
        {
            bomb.transform.Rotate(0, 180f, 0);
        }
    }

    int ChooseBombCount()
    {
        /* 
         * making use of this Unity article again
         * https://docs.unity3d.com/2019.3/Documentation/Manual/RandomNumbers.html
         * 
         * maximum of three bomb throws in a session but the choice is weighted by probability
         * 
         * 70% chance of choosing 3 bombs
         * 20% chance of choosing 2 bombs
         * 10% chance of choosing 1 bomb
         * 
         * probabilities array index + 1 will be the bomb count and returned by this function
        */
        float[] probabilities = { 10, 20, 70 };

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
                return i + 1;
            }
            else
            {
                randomPoint -= probabilities[i];
            }
        }
        return probabilities.Length;
    }

    public void Jump()
    {
        // only jump if we are allowed to
        if (canJump)
        {
            // randomly pick a jump velocity
            int jumpIndex = Random.Range(0, jumpVelocities.Length);
            Vector2 jumpVelocity = jumpVelocities[jumpIndex];
            // randomly pick a jump direction (multiply it against the X velocity)
            // but we don't want to use the same direction one more than twice
            while (true)
            {
                float[] xDirections = { -1f, 1f };
                int jumpDirectionIndex = Random.Range(0, xDirections.Length);
                jumpDirection = xDirections[jumpDirectionIndex];
                // last jump direction index is different
                if (jumpDirectionIndex != lastJumpDirection)
                {
                    // save and reset counter
                    lastJumpDirection = jumpDirectionIndex;
                    lastJumpDirectionCount = 1;
                    // exit while loop
                    break;
                }
                else
                {
                    // last jump direction index is the same
                    // if this is the second use then we exit the loop
                    // or we stay and keep trying until it's different
                    if (++lastJumpDirectionCount <= 2) break;
                }
            }
            // default to these jump styles (Jump2 for forward, Jump3 for backward)
            jumpAnimation =
                ((playerPosition.x <= transform.position.x && jumpDirection == -1f) ||
                (transform.position.x <= playerPosition.x && jumpDirection == 1f))
                ? "BombMan_Jump2" : "BombMan_Jump3";
            // jump style Jump1 for shortest jump
            if (jumpIndex == 0) jumpAnimation = "BombMan_Jump1";
            // set jump velocity
            jumpVelocity.x *= jumpDirection;
            rb2d.velocity = jumpVelocity;
            // now we jump but only at this time
            canJump = false;
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetAttackDistance(float distance)
    {
        // distance from player to start throwing bombs
        // closer than this distance cause bombman to jump away
        this.attackDistance = distance;
    }

    public void SetBombHeight(float height)
    {
        // set the bomb height when thrown
        this.bombHeight = height;
    }

    public void SetTargetOffset(float offset)
    {
        // set bomb target offset
        this.targetOffset = offset;
    }

    public void SetTossVelocity(Vector2 velocity)
    {
        // set velocity for posing bomb toss
        this.tossVelocity = velocity;
    }

    public void SetJumpVectors(Vector2 v0, Vector2 v1, Vector2 v2)
    {
        // set jump velocity vectors
        this.jumpVelocities[0] = v0;
        this.jumpVelocities[1] = v1;
        this.jumpVelocities[2] = v2;
    }

    public void TakeDamage()
    {
        // event is called from enemy controller
        if (!isTakingDamage)
        {
            // allow flash damage to do its full sequence
            isTakingDamage = true;
            enemyController.Invincible(true);
            StartCoroutine(FlashAfterDamageCo());
        }
    }

    IEnumerator FlashAfterDamageCo()
    {
        // alternate between damage sprite and current bombman animaton frame
        for (int i = 0; i < 8; i++)
        {
            // show damage sprite
            sprite.color = Color.clear;
            damageSprite.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            // back to bombman current animation frame
            sprite.color = Color.white;
            damageSprite.color = Color.clear;
            yield return new WaitForSeconds(0.05f);
        }
        // done flashing so we can do it again on the next hit
        isTakingDamage = false;
        enemyController.Invincible(false);
    }

    public void Defeat()
    {
        // event is called from enemy controller
        // do something when this enemy is defeated
        // as an example if a bomb happens to be in motion then destroy it
        bomb = GameObject.Find(bombPrefab.name + "(" + gameObject.name + ")");
        if (bomb != null)
        {
            Destroy(bomb);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // including this in case we want to detect the following...
            if (isJumping)
            {
                // landing on the player from a jump
            }
            else
            {
                // or touching the player while grounded
            }
        }
    }
}