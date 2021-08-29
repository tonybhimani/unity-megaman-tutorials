using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GabyoallController : MonoBehaviour
{
    Rigidbody2D rb2d;
    Animator animator;
    BoxCollider2D box2d;
    SpriteRenderer sprite;
    EnemyController enemyController;

    GameObject player;

    bool isStunned;
    bool isFacingRight;

    float gabyoallSpeed;
    float stunTimer;

    // movement direction
    Vector2 direction;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // gabyoall settings
    [SerializeField] float patrolSpeed = 1f;
    [SerializeField] float chaseSpeed = 2f;
    [SerializeField] float stunDelay = 3f;
    [SerializeField] float searchDistance = 3f;
    [SerializeField] float playerHitPointY = 0.05f;

    // sound clip when getting stunned
    [SerializeField] AudioClip damageClip;

    // transforms for the ground and wall check positions
    [SerializeField] Transform groundCheck;
    [SerializeField] Transform wallCheck;

    public enum GabyoallColors { Blue, Orange };
    [SerializeField] GabyoallColors gabyoallColor = GabyoallColors.Blue;

    [SerializeField] RuntimeAnimatorController racGabyoallBlue;
    [SerializeField] RuntimeAnimatorController racGabyoallOrange;

    public enum MoveDirections { Left, Right };
    [SerializeField] MoveDirections moveDirection = MoveDirections.Left;

    void Awake()
    {
        // get components from EnemyController
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
        box2d = enemyController.GetComponent<BoxCollider2D>();
        rb2d = enemyController.GetComponent<Rigidbody2D>();
        sprite = enemyController.GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // sprite sheet images face right
        // switch to facing left if it's set
        isFacingRight = true;
        direction = Vector2.right;
        if (moveDirection == MoveDirections.Left)
        {
            isFacingRight = false;
            direction = Vector2.left;
            enemyController.Flip();
        }

        // initialize gabyoall
        gabyoallSpeed = patrolSpeed;
        stunTimer = stunDelay;

        // set gabyoall color of choice
        SetColor(gabyoallColor);

        // get the player (need for grounded status)
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void FixedUpdate()
    {
        // raycast checks
        GroundWallCheck();
        PlayerCheck();
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // do gabyoall ai logic if it's enabled
        if (enableAI)
        {
            // stunned?
            if (isStunned)
            {
                // stop moving and wait until it ends
                rb2d.velocity = Vector2.zero;
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0)
                {
                    // start moving again
                    Stun(false);
                }
            }
            else
            {
                // move at the speed and direction
                rb2d.velocity = gabyoallSpeed * direction;
            }
        }
    }

    void GroundWallCheck()
    {
        // ground check
        float raycastDistance = 0.025f;
        int layerMask = 1 << LayerMask.NameToLayer("Ground");
        // ground/wall is the same layer but use two rays for checking
        RaycastHit2D groundRaycastHit = Physics2D.Raycast(groundCheck.position, Vector2.down, raycastDistance, layerMask);
        RaycastHit2D wallRaycastHit = Physics2D.Raycast(wallCheck.position, direction, raycastDistance, layerMask);
        // draw debug lines first (because we may change the direction)
        Debug.DrawRay(groundCheck.position, Vector2.down * raycastDistance, Color.magenta);
        Debug.DrawRay(wallCheck.position, direction * raycastDistance, Color.magenta);
        // looking for no grounds or there is a wall
        if (groundRaycastHit.collider == null || wallRaycastHit.collider != null)
        {
            // ground or wall found so change direction
            isFacingRight = !isFacingRight;
            enemyController.Flip();
            direction = (direction == Vector2.left) ? Vector2.right : Vector2.left;
        }
    }

    void PlayerCheck()
    {
        // player check
        gabyoallSpeed = patrolSpeed;
        int layerMask = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Player");
        Vector3 origin = sprite.bounds.center;
        // check for hits against the ground and player layers
        // the ground layer is included so that will register the hit first if the player is behind it
        // i.e. gabyoall can't see through grounds/walls - maybe serialize if many layers need to be checked
        RaycastHit2D raycastHit1 = Physics2D.Raycast(origin, Vector2.left, searchDistance, layerMask);
        RaycastHit2D raycastHit2 = Physics2D.Raycast(origin, Vector2.right, searchDistance, layerMask);
        // check if the raycasts hit the layers
        PlayerRaycastHit(raycastHit1);
        PlayerRaycastHit(raycastHit2);
        // draw debug lines
        Debug.DrawRay(origin, Vector2.left * searchDistance, Color.magenta);
        Debug.DrawRay(origin, Vector2.right * searchDistance, Color.magenta);
    }

    void PlayerRaycastHit(RaycastHit2D raycastHit)
    {
        // check for raycast hit against the player
        if (raycastHit.collider != null && player != null)
        {
            // hit the player?
            if (raycastHit.collider.CompareTag("Player"))
            {
                // transform world point to local point
                Vector3 playerHitPoint = player.transform.InverseTransformPoint(raycastHit.point);
                // go faster gabyoall!
                if (playerHitPoint.y <= playerHitPointY &&
                    player.GetComponent<PlayerController>().IsGrounded())
                {
                    // chase/dash speed
                    gabyoallSpeed = chaseSpeed;
                }
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetColor(GabyoallColors color)
    {
        gabyoallColor = color;
        SetAnimatorController();
    }

    void SetAnimatorController()
    {
        // set animator control from color
        switch (gabyoallColor)
        {
            case GabyoallColors.Blue:
                animator.runtimeAnimatorController = racGabyoallBlue;
                break;
            case GabyoallColors.Orange:
                animator.runtimeAnimatorController = racGabyoallOrange;
                break;
        }
    }

    public void Stun(bool stun = true)
    {
        // gabyoall is stunned
        if (stun)
        {
            isStunned = true;
            stunTimer = stunDelay;
            animator.speed = 0;
            // play the damage sound clip
            SoundManager.Instance.Play(damageClip);
        }
        else
        {
            // gabyoall is back in play
            isStunned = false;
            animator.speed = 1;
        }
    }
}