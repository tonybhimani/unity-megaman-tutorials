using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BombScript : MonoBehaviour
{
    Animator animator;
    CircleCollider2D circle2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    public UnityEvent ExplosionEvent;

    // trigger and time keeper for the bomb explosion
    bool startTimer;
    float explodeTimer;

    // default the settings for use by the player
    [Header("Bomb Damage")]
    [SerializeField] int contactDamage = 0;
    [SerializeField] int explosionDamage = 4;

    [Header("Audio Clips")]
    [SerializeField] AudioClip explosionClip;

    [Header("Timers & Collision")]
    [SerializeField] float explodeDelay = 3f;

    [SerializeField] string[] collideWithTags;

    [Header("Positions & Physics")]
    [SerializeField] float gravity;
    [SerializeField] float height = 1f;
    [SerializeField] float targetOffset = 0.15f;

    [SerializeField] Vector3 sourcePosition;
    [SerializeField] Vector3 targetPosition;

    [SerializeField] Vector2 bombDirection = Vector2.right;
    [SerializeField] Vector2 launchVelocity = new Vector3(2f, 1.5f);

    [Header("Materials & Prefabs")]
    [SerializeField] PhysicsMaterial2D bounceMaterial;
    [SerializeField] GameObject explodeEffectPrefab;

    void Awake()
    {
        // get the attached components
        animator = GetComponent<Animator>();
        circle2d = GetComponent<CircleCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // make bomb as a static type of object
        circle2d.isTrigger = true;
        rb2d.isKinematic = true;
    }

    // Update is called once per frame
    void Update()
    {
        // colliding with the ground and having a delay 
        // activates the timer for the bomb explosion
        if (startTimer)
        {
            explodeTimer -= Time.deltaTime;
            if (explodeTimer <= 0)
            {
                // just once until next time
                startTimer = false;

                // KABOOM! the bomb explodes
                Explode();
            }
        }
    }

    public void SetContactDamageValue(int damage)
    {
        // damage value for when the bomb touches the player/enemy (no explosion)
        this.contactDamage = damage;
    }

    public void SetExplosionDamageValue(int damage)
    {
        // damage value for when the bomb explodes and the player/enemy is hit
        this.explosionDamage = damage;
    }

    public void SetExplosionDelay(float delay)
    {
        // explosion delay for the bomb - starts after the bomb touches the ground
        this.explodeDelay = delay;
    }

    public void SetVelocity(Vector2 velocity)
    {
        // if no target is set then this velocity is used for launching the bomb
        this.launchVelocity = velocity;
    }

    public void SetDirection(Vector2 direction)
    {
        // if no target is set then this direction is used in conjunction with the velocity
        this.bombDirection = direction;
    }

    public void SetHeight(float height)
    {
        // height is used for the LaunchData calculation
        this.height = height;
    }

    public void SetSourcePosition(Vector3 position)
    {
        // source position for the LaunchData calculation
        this.sourcePosition = position;
    }

    public void SetTargetPosition(Vector3 position)
    {
        // target position for the LaunchData calculation
        this.targetPosition = position;
    }

    public void SetTargetOffset(float offset)
    {
        // offset to apply if LaunchData target exact hit isn't desired
        this.targetOffset = offset;
    }

    public void Bounces(bool bounce)
    {
        // apply bounce material or set to null
        rb2d.sharedMaterial = bounce ? bounceMaterial : null;
    }

    public void SetCollideWithTags(params string[] tags)
    {
        // the bomb can collide with these tags
        this.collideWithTags = tags;
    }

    // launch the bomb has two scenarios
    // 1st case - target position isn't null means the velocity and direction vectors won't be used
    // the velocity is calculated using the kinematic equation and gravity and offset will be used
    // this case is for BombMan however could be used by other characters
    // 2nd case - target position is null so use straight velocity and direction to launch the bomb
    // this case is for MegaMan however could be used by other characters
    public void Launch()
    {
        // make the bomb solid and have a dynamic rigidbody
        circle2d.isTrigger = false;
        rb2d.isKinematic = false;

        if (targetPosition != null)
        {
            // launch bomb to target and apply offset if any
            if (gravity == 0) gravity = Physics2D.gravity.y;
            Vector3 bombPos = sourcePosition;
            Vector3 playerPos = targetPosition;
            if (targetOffset != 0) playerPos.x += targetOffset;
            rb2d.velocity = UtilityFunctions.CalculateLaunchData(bombPos, playerPos, height, gravity).initialVelocity;
        }
        else
        {
            // no target set - use launch velocity instead
            Vector2 velocity = this.launchVelocity;
            velocity.x *= this.bombDirection.x;
            rb2d.AddForce(velocity, ForceMode2D.Impulse);
        }
    }

    // called when the bomb is ready to explode
    // create a copy of the explosion prefab, set the collision tag(s), destroy the bomb
    private void Explode()
    {
        GameObject explodeEffect = Instantiate(explodeEffectPrefab);
        explodeEffect.name = explodeEffectPrefab.name;
        explodeEffect.transform.position = sprite.bounds.center;
        explodeEffect.GetComponent<ExplosionScript>().SetCollideWithTags(this.collideWithTags);
        explodeEffect.GetComponent<ExplosionScript>().SetDamageValue(this.explosionDamage);
        Destroy(explodeEffect, 2f);

        // for the audio clip to play we need the gameobject to stick around
        // so we're going to set the sprite to transparent and destroy it after 
        // a delay to give the audio time to play
        sprite.color = Color.clear;
        Destroy(gameObject, 1f);

        // play bomb explosion audio clip
        SoundManager.Instance.Play(explosionClip);

        // invoke explosion event
        ExplosionEvent.Invoke();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // check for bomb colliding with the ground layer
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // look for an explosion delay and start the timer
            if (!startTimer && explodeDelay > 0)
            {
                startTimer = true;
                explodeTimer = explodeDelay;
            }

            // if there is no delay then destroy the bomb immediately
            if (explodeDelay == 0)
            {
                Explode();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        foreach (string tag in collideWithTags)
        {
            // check for collision with this tag
            if (other.gameObject.CompareTag(tag))
            {
                switch (tag)
                {
                    case "Enemy":
                        // enemy controller will apply the damage the bomb can cause
                        EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(this.contactDamage);
                        }
                        break;
                    case "Player":
                        // player controller will apply the damage the bomb can cause
                        PlayerController player = other.gameObject.GetComponent<PlayerController>();
                        if (player != null)
                        {
                            player.HitSide(transform.position.x > player.transform.position.x);
                            player.TakeDamage(this.contactDamage);
                        }
                        break;
                }
            }
        }
    }
}
