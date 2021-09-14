using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KamadomaController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    GameObject player;
    Vector3 playerPosition;

    bool isGrounded;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    float jumpTimer;
    public float jumpDelay = 0.5f;

    Vector2 jumpVector;
    public Vector2[] jumpVectors = {
        new Vector2(1f, 4f),
        new Vector2(2.5f, 3f)
    };

    public enum KamadomaColors { Blue, Red };
    [SerializeField] KamadomaColors kamadomaColor = KamadomaColors.Blue;

    [SerializeField] RuntimeAnimatorController racKamadomaBlue;
    [SerializeField] RuntimeAnimatorController racKamadomaRed;

    // Start is called before the first frame update
    void Start()
    {
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
        box2d = enemyController.GetComponent<BoxCollider2D>();
        rb2d = enemyController.GetComponent<Rigidbody2D>();

        // set kamadoma color of choice
        SetColor(kamadomaColor);

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
        box_origin.y = box2d.bounds.center.y - box2d.bounds.extents.y + (box2d.bounds.extents.y / 4f);
        Vector3 box_size = box2d.bounds.size;
        box_size.y = box2d.bounds.size.y / 4f;
        raycastHit = Physics2D.BoxCast(box_origin, box_size, 0f, Vector2.down, raycastDistance, layerMask);
        // kamadoma box colliding with ground layer
        if (raycastHit.collider != null)
        {
            isGrounded = true;
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

        // do kamadoma ai logic if it's enabled
        if (enableAI)
        {
            // get player position
            if (player != null) playerPosition = player.transform.position;

            if (isGrounded)
            {
                animator.Play("Kamadoma_Grounded");
                rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                jumpTimer -= Time.deltaTime;
                if (jumpTimer < 0)
                {
                    // randomly choose between the two jump vectors
                    jumpVector = jumpVectors[Random.Range(0, 2)];
                    if (playerPosition.x <= transform.position.x)
                    {
                        // player is to the left of the enemy
                        jumpVector.x *= -1;
                    }
                    // apply jump vector and reset jump timer
                    rb2d.velocity = jumpVector;
                    jumpTimer = jumpDelay;
                }
            }
            else
            {
                animator.Play("Kamadoma_Jumping");
                rb2d.velocity = new Vector2(jumpVector.x, rb2d.velocity.y);
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetColor(KamadomaColors color)
    {
        kamadomaColor = color;
        SetAnimatorController();
    }

    void SetAnimatorController()
    {
        // set animator control from color
        switch (kamadomaColor)
        {
            case KamadomaColors.Blue:
                animator.runtimeAnimatorController = racKamadomaBlue;
                break;
            case KamadomaColors.Red:
                animator.runtimeAnimatorController = racKamadomaRed;
                break;
        }
    }
}