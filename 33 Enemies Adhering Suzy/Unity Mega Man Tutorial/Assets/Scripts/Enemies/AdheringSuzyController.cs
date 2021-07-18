using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdheringSuzyController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;
    EnemyController enemyController;

    bool isSleeping;

    float suzySpeed;
    float sleepTimer;

    // for stopping by position
    Vector3 moveStopPos;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // settings for adhering suzy
    [SerializeField] bool sleepOnStart;
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float approachSpeed = 0.5f;
    [SerializeField] float raycastDistance = 0.2f;
    [SerializeField] float sleepDelay = 2f;

    // stop positions if not using raycast/collision detection
    // use indexes as left[0]/right[1], up[0]/down[1]
    [SerializeField] Vector3[] stopPositions = new Vector3[2];

    public enum Movements { Horizontal, Vertical };
    [SerializeField] Movements movement = Movements.Horizontal;

    public enum Directions { Left, Right, Up, Down };
    [SerializeField] Directions direction = Directions.Left;

    public enum StopMethods { Collision, Position };
    [SerializeField] StopMethods stopMethod = StopMethods.Collision;

    // get the vector from the int value of our direction enum
    Vector3[] directionVectors =
    {
        Vector3.left,
        Vector3.right,
        Vector3.up,
        Vector3.down
    };

    public enum AdheringSuzyColors { Blue, Red };
    [SerializeField] AdheringSuzyColors adheringSuzyColor = AdheringSuzyColors.Blue;

    [SerializeField] RuntimeAnimatorController racAdheringSuzyBlue;
    [SerializeField] RuntimeAnimatorController racAdheringSuzyRed;

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
        // initialize adhering suzy
        isSleeping = sleepOnStart;
        animator.speed = isSleeping ? 0 : 1;
        suzySpeed = moveSpeed;
        sleepTimer = sleepDelay;
        UpdateStopPosition();

        // set adhering suzy color of choice
        SetColor(adheringSuzyColor);
    }

    void FixedUpdate()
    {
        /*
         * at faster movement speeds we can travel into the ground/wall before
         * OnCollisionEnter2D() responds to stop us so to try and remedy that
         * we project a raycast to alert of us of the approaching layer then
         * change to a slower movement speed -- the faster the movement the 
         * greater the raycast distance should be to compensate.
         * 
         * the alternative is to use the other method I have implemented which
         * is to stop movement by reaching a position vector. Suzy will travel 
         * between the two points and you don't have to worry about collisions
         * or which layer that triggers it. I have just the ground layer here 
         * but maybe you'd need to collide with others which means updating the 
         * layerMask variable or adding in the serialization of collision layers.
         * also, sometimes you may want to just move between points and there 
         * are no layers involved. I prefer this approach because it appears
         * to be more precise in reaching the stop location.
         * 
         * the drawback is you have to figure out the position vectors in advance 
         * but it isn't like you don't have to apply other setting changes, right?
         */
        if (moveSpeed > approachSpeed)
        {
            // ground check
            int layerMask = 1 << LayerMask.NameToLayer("Ground");
            Vector3 origin = box2d.bounds.center;
            Vector3 raycastDirection = directionVectors[(int)direction];
            RaycastHit2D raycastHit = Physics2D.Raycast(origin, raycastDirection, raycastDistance, layerMask);
            // suzy raycast is touching the ground layer
            if (raycastHit.collider != null)
            {
                // use the slower speed before we collide
                suzySpeed = approachSpeed;
            }
            // draw debug line
            Debug.DrawRay(origin, raycastDirection * raycastDistance, Color.magenta);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // do suzy ai logic if it's enabled
        if (enableAI)
        {
            if (isSleeping)
            {
                // sleep until time to wake up
                sleepTimer -= Time.deltaTime;
                if (sleepTimer <= 0)
                {
                    // suzy wake up
                    Wake();
                }
            }
            else
            {
                // stop method collision - suzy just keeps moving until a collision is detected
                if (stopMethod == StopMethods.Collision)
                {
                    // move at the speed and direction
                    transform.position += suzySpeed * directionVectors[(int)direction] * Time.deltaTime;
                }
                else
                {
                    // stop method position - suzy will travel between the two vector points
                    transform.position = Vector3.MoveTowards(
                        transform.position, moveStopPos, suzySpeed * Time.deltaTime);
                    if (transform.position == moveStopPos)
                    {
                        // sleep now dear suzy
                        Sleep();
                    }
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // stop method should be collision detection here
        if (stopMethod == StopMethods.Collision)
        {
            // collided with the ground layer
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // suzy go to sleep
                Sleep();
            }
        }
    }

    public void Wake()
    {
        // wake up from slumber
        isSleeping = false;
        animator.speed = 1;
        animator.Play("AdheringSuzy_Wake");
    }

    public void Sleep()
    {
        // go back to sleep and change direction
        isSleeping = true;
        sleepTimer = sleepDelay;
        suzySpeed = moveSpeed;
        direction = GetNextDirection();
        UpdateStopPosition();
        animator.Play("AdheringSuzy_Sleep");
    }

    public Directions GetNextDirection()
    {
        // get the next direction to travel
        return (movement == Movements.Horizontal) ?
            ((direction == Directions.Left) ? Directions.Right : Directions.Left) :
            ((direction == Directions.Up) ? Directions.Down : Directions.Up);
    }

    public void UpdateStopPosition()
    {
        // update the movement stop position
        moveStopPos = (movement == Movements.Horizontal) ?
           ((direction == Directions.Left) ? stopPositions[0] : stopPositions[1]) :
           ((direction == Directions.Up) ? stopPositions[0] : stopPositions[1]);
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetColor(AdheringSuzyColors color)
    {
        adheringSuzyColor = color;
        SetAnimatorController();
    }

    void SetAnimatorController()
    {
        // set animator control from color
        switch (adheringSuzyColor)
        {
            case AdheringSuzyColors.Blue:
                animator.runtimeAnimatorController = racAdheringSuzyBlue;
                break;
            case AdheringSuzyColors.Red:
                animator.runtimeAnimatorController = racAdheringSuzyRed;
                break;
        }
    }

    public void SetSleepOnStart(bool sleep)
    {
        // set to sleep on startup
        this.sleepOnStart = sleep;
    }

    public void SetMoveSpeed(float speed)
    {
        // set movement speed
        this.moveSpeed = speed;
    }

    public void SetApproachSpeed(float speed)
    {
        // set approach speed when raycast touches ground
        this.approachSpeed = speed;
    }

    public void SetRaycastDistance(float distance)
    {
        // set raycast distance for ground check
        this.raycastDistance = distance;
    }

    public void SetSleepDelay(float delay)
    {
        // set sleep delay between movements
        this.sleepDelay = delay;
    }

    public void SetStopPositions(Vector3 p0, Vector3 p1)
    {
        // set the vector stop positions
        this.stopPositions[0] = p0;
        this.stopPositions[1] = p1;
    }

    public void SetMovement(Movements movement)
    {
        // set movement type of horizontal or vertical
        this.movement = movement;
    }

    public void SetDirection(Directions direction)
    {
        // set movement direction for movement type
        this.direction = direction;
    }

    public void SetStopMethod(StopMethods stopMethod)
    {
        // set the movement stop method
        this.stopMethod = stopMethod;
    }
}