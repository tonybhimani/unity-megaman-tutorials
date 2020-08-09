using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb2d;

    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float jumpSpeed = 5f;

    float keyHorizontal;
    bool keyJump;

    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        keyHorizontal = Input.GetAxisRaw("Horizontal");
        keyJump = Input.GetKeyDown(KeyCode.Space);
        rb2d.velocity = new Vector2(keyHorizontal * moveSpeed, rb2d.velocity.y);
        if (keyJump)
        {
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpSpeed);
        }
    }
}
