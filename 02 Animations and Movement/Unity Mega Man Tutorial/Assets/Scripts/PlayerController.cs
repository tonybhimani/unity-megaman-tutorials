using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;

    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float jumpSpeed = 3.7f;

    float keyHorizontal;
    bool keyJump;
    bool keyShoot;

    bool isGrounded;
    bool isShooting;
    bool isFacingRight;

    float shootTime;
    bool keyShootRelease;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();

        // sprite defaults to facing right
        isFacingRight = true;
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
        PlayerDirectionInput();
        PlayerShootInput();
        PlayerMovement();
    }

    void PlayerDirectionInput()
    {
        // get keyboard input
        keyHorizontal = Input.GetAxisRaw("Horizontal");
        keyJump = Input.GetKeyDown(KeyCode.Space);
        keyShoot = Input.GetKey(KeyCode.C);
    }

    void PlayerShootInput()
    {
        float shootTimeLength = 0;
        float keyShootReleaseTimeLength = 0;

        // shoot key is being pressed and key release flag true
        if (keyShoot && keyShootRelease)
        {
            isShooting = true;
            keyShootRelease = false;
            shootTime = Time.time;
            // Shoot Bullet
            Debug.Log("Shoot Bullet");
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
}
