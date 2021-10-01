using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigEyeController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    GameObject player;
    Vector3 playerPosition;

    bool isFacingRight;
    bool isGrounded;
    bool isJumping;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    float jumpTimer;
    public float jumpDelay = 0.25f;

    int jumpPatternIndex;
    int[] jumpPattern;
    int[][] jumpPatterns = new int[][] {
        new int[1] { 1 },           // High Jump
        new int[2] { 0, 1 },        // Low Jump, High Jump
        new int[3] { 0, 0, 1 }      // Low Jump, Low Jump, High Jump
    };

    int jumpVelocityIndex;
    Vector2 jumpVelocity;
    public Vector2[] jumpVelocities = {
        new Vector2(1.0f, 3.0f),    // Low Jump
        new Vector2(0.75f, 4.0f)    // High Jump
    };

    public AudioClip jumpLandedClip;

    public enum BigEyeColors { Blue, Orange, Red };
    [SerializeField] BigEyeColors bigEyeColor = BigEyeColors.Blue;

    [SerializeField] RuntimeAnimatorController racBigEyeBlue;
    [SerializeField] RuntimeAnimatorController racBigEyeOrange;
    [SerializeField] RuntimeAnimatorController racBigEyeRed;

    public enum MoveDirections { Left, Right };
    [SerializeField] MoveDirections moveDirection = MoveDirections.Left;

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
        // switch to facing left if it's set
        isFacingRight = true;
        if (moveDirection == MoveDirections.Left)
        {
            isFacingRight = false;
            enemyController.Flip();
        }

        // set big eye color of choice
        SetColor(bigEyeColor);

        // start with no pattern
        jumpPattern = null;

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
        // big eye box colliding with ground layer
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
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // do Big Eye ai logic if it's enabled
        if (enableAI)
        {
            /*
             * Big Eye - the cyclops jumping robot springs in the direction of Mega Man trying to squash him. 
             * In the NES game I believe his jumping between low and high is random but this replica follows 
             * the jump patterns published in the Mega Man Maker wiki. Reason I chose patterns has to do with 
             * potential obstacles in his path. He could randomly pick low jumps many times and not clear the 
             * objects making it look uhhh ooookay. By using the patterns at most it could be two low jumps 
             * then finally being a high jump and clearing the object in question (unless it's taller than 
             * his jump height of course). 
             * 
             * How this works
             * 
             * While grounded he has no velocity therefore stands still. There is a jump timer that counts 
             * down and once times up looks into performing the jump, however before doing so he checks whether 
             * he is already following a pattern or is it time to pick a new one at random. Once the pattern 
             * is chosen the velocity for that jump is fetched. The player's x position is used to determine 
             * whether he needs to jump left or right. Right of the player, jump left x *= -1, right being 
             * positive, no change in x velocity. Increment through the pattern index each jump and if the 
             * last is reached then null the pattern telling Big Eye on next pass to pick a new random pattern.
             * 
             * While jumping he uses a constant x velocity to be able to pass objects that initially block his 
             * path. Originally I used AddForce but as soon as something opposes his forward momentum it's lost. 
             * Now constant velocity is used to fix that issue. The isJumping flag is for playing the jump landed 
             * sound in FixedUpdate once isGrounded is set from raycast collision with the Ground layer. This is 
             * exactly like in the Player code for Mega Man's jump landing audio. Last he checks whether he needs 
             * to face the opposite direction. Unlike the other enemies that face a constant direction, Big Eye 
             * checks where the player is and jumps in his direction, so no SetMovementDirection function. The 
             * x velocity is tested for being less than zero, meaning, he's jumping left. We use the same logic 
             * as with the other enemies to change the facing direction if needed.
             */

            // get player position
            if (player != null) playerPosition = player.transform.position;

            if (isGrounded)
            {
                animator.Play("BigEye_Grounded");
                rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                jumpTimer -= Time.deltaTime;
                if (jumpTimer < 0)
                {
                    if (jumpPattern == null)
                    {
                        jumpPatternIndex = 0;
                        jumpPattern = jumpPatterns[Random.Range(0, jumpPatterns.Length)];
                    }
                    jumpVelocityIndex = jumpPattern[jumpPatternIndex];
                    jumpVelocity = jumpVelocities[jumpVelocityIndex];
                    if (playerPosition.x <= transform.position.x)
                    {
                        jumpVelocity.x *= -1;
                    }
                    rb2d.velocity = new Vector2(rb2d.velocity.x, jumpVelocity.y);
                    jumpTimer = jumpDelay;
                    if (++jumpPatternIndex > jumpPattern.Length - 1)
                    {
                        jumpPattern = null;
                    }
                }
            }
            else
            {
                animator.Play("BigEye_Jumping");
                rb2d.velocity = new Vector2(jumpVelocity.x, rb2d.velocity.y);
                isJumping = true;
                if (jumpVelocity.x <= 0)
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
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetColor(BigEyeColors color)
    {
        bigEyeColor = color;
        SetAnimatorController();
    }

    void SetAnimatorController()
    {
        // set animator control from color
        switch (bigEyeColor)
        {
            case BigEyeColors.Blue:
                animator.runtimeAnimatorController = racBigEyeBlue;
                break;
            case BigEyeColors.Orange:
                animator.runtimeAnimatorController = racBigEyeOrange;
                break;
            case BigEyeColors.Red:
                animator.runtimeAnimatorController = racBigEyeRed;
                break;
        }
    }
}