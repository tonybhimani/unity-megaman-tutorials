using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PepeController : MonoBehaviour
{
    Animator animator;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    bool isFacingRight;

    bool isFollowingPath;
    Vector3 pathStartPoint;
    Vector3 pathEndPoint;
    Vector3 pathMidPoint;
    float pathTimeStart;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    public float bezierTime = 1f;
    public float bezierDistance = 1f;
    public Vector3 bezierHeight = new Vector3(0, 0.8f, 0);

    public enum MoveDirections { Left, Right };
    [SerializeField] MoveDirections moveDirection = MoveDirections.Left;

    void Awake()
    {
        // get components from EnemyController
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
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
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            // path start time for bezier curve gets annihilated when frozen - compensate
            pathTimeStart += Time.deltaTime;
            return;
        }

        // do Pepe ai logic if it's enabled
        if (enableAI)
        {
            // play animation
            animator.Play("Pepe_Flying");

            // calculate next travel path when previous is completed
            if (!isFollowingPath)
            {
                // distance/length to the next end point, get start point from rigidbody position, 
                // end point is calculated by adding the distance to start point
                // middle point is calculated and the height is applied to form the curve
                // start time is needed to determine the point in the curve we want
                // i.e. this is just like LERPing
                float distance = (isFacingRight) ? bezierDistance : -bezierDistance;
                pathStartPoint = rb2d.transform.position;
                pathEndPoint = new Vector3(pathStartPoint.x + distance, pathStartPoint.y, pathStartPoint.z);
                pathMidPoint = pathStartPoint + (((pathEndPoint - pathStartPoint) / 2) + bezierHeight);
                pathTimeStart = Time.time;
                isFollowingPath = true;
            }
            else
            {
                // percentage is the point in the curve we want and update our rigidbody position
                float percentage = (Time.time - pathTimeStart) / bezierTime;
                rb2d.transform.position = UtilityFunctions.CalculateQuadraticBezierPoint(pathStartPoint, pathMidPoint, pathEndPoint, percentage);
                // end of the curve has been reach
                if (percentage >= 1f)
                {
                    // invert the height - this is what creates the flying wave effect
                    bezierHeight *= -1;
                    isFollowingPath = false;
                }
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetMoveDirection(MoveDirections direction)
    {
        // we can call this to change the moving direction in real-time
        // and it should be followed by calling ResetFollowingPath to
        // calculate new bezier curve control points
        moveDirection = direction;
        // flip the facing side if it's needed
        if (moveDirection == MoveDirections.Left)
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

    public void ResetFollowingPath()
    {
        // if position is manually changed while following curve 
        // then the Y position decays until the path finishes and 
        // new control points are calculated
        // call this function to force calculation of new points
        isFollowingPath = false;
    }
}