using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MagnetBeamScript : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    SpriteRenderer sprite;

    // time vars
    float startTime;
    float beamTime;
    float maxBeamTime = 3f;

    // solid and locked flags
    bool isSolid;
    bool isLocked;

    // access to our player
    GameObject player;

    // event to signal beam is locked in place
    public UnityEvent LockedEvent;

    [Header("Platform Properties")]
    [SerializeField] float destroyDelay = 3f;
    [SerializeField] Vector2 beamDirection = Vector2.right;
    [SerializeField] int MaxSegments = 30;

    void Awake()
    {
        // get components
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // beam started now
        startTime = Time.time;

        // default beam to a single segment
        animator.Play("MagnetBeam1");

        // get the player object
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocked)
        {
            // grow the beam over time and max out at 3 seconds (30 segments)
            // if you go beyond 3 seconds then either lock the beam segments to 30
            // or you'll need to update the sprite sheet and add more animations
            if (beamTime < maxBeamTime)
            {
                beamTime = Mathf.Clamp((Time.time - startTime), 0.1f, maxBeamTime);
                int segments = (int)(beamTime * 10);
                if (segments > MaxSegments) segments = MaxSegments;
                animator.Play("MagnetBeam" + segments.ToString());
            }
        }
        else
        {
            // player object must exist
            if (player != null)
            {
                // if player is above the platform beam then make it solid and apply the layer
                if (player.transform.position.y > transform.position.y + box2d.bounds.extents.y &&
                    player.GetComponent<Rigidbody2D>().velocity.y <= 0)
                {
                    if (!isSolid)
                    {
                        isSolid = true;
                        box2d.isTrigger = false;
                        gameObject.layer = LayerMask.NameToLayer("MagnetBeam");
                    }
                }

                // if the player is below the magnet beam then make it a trigger and set the layer back to default
                if (player.transform.position.y < transform.position.y)
                {
                    if (isSolid)
                    {
                        isSolid = false;
                        box2d.isTrigger = true;
                        gameObject.layer = LayerMask.NameToLayer("Default");
                    }
                }
            }
        }
    }

    public void SetDestroyDelay(float delay)
    {
        // how long the beam platform stick around
        this.destroyDelay = delay;
    }

    public void SetDirection(Vector2 direction)
    {
        this.beamDirection = direction;
        // beam faces right by default, flip if necessary
        if (direction.x < 0)
        {
            transform.Rotate(0, 180f, 0);
        }
    }

    public void SetMaxSegments(int segments)
    {
        // maximum number of platform beam segments
        this.MaxSegments = segments;
    }

    public void LockBeam()
    {
        // detach beam from player, destroy at delay, and invoke event
        isLocked = true;
        gameObject.transform.parent = null;
        if (destroyDelay > 0)
        {
            Destroy(gameObject, destroyDelay);
        }
        LockedEvent.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // if platform beam collides with a wall or floor
        // then it should be locked into place (if it isn't already)
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (!isLocked)
            {
                LockBeam();
            }
        }
    }
}