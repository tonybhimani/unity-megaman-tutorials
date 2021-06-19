using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    Animator animator;
    CircleCollider2D cc2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    float destroyTimer;

    // freeze bullet on screen
    bool freezeBullet;
    RigidbodyConstraints2D rb2dConstraints;

    public int damage = 1;

    [SerializeField] float bulletSpeed;
    [SerializeField] Vector2 bulletDirection;
    [SerializeField] float destroyDelay;

    [SerializeField] string[] collideWithTags = { "Enemy" };

    public enum BulletTypes { Default, MiniBlue, MiniGreen, MiniOrange, MiniPink, MiniRed };
    [SerializeField] BulletTypes bulletType = BulletTypes.Default;

    [System.Serializable]
    public struct BulletStruct
    {
        public Sprite sprite;
        public float radius;
        public Vector3 scale;
    }
    [SerializeField] BulletStruct[] bulletData;

    // Start is called before the first frame update
    void Awake()
    {
        // get handles to components
        animator = GetComponent<Animator>();
        cc2d = GetComponent<CircleCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        // set default bullet sprite & radius
        SetBulletType(bulletType);
    }

    // Update is called once per frame
    void Update()
    {
        // if the bullet is frozen then don't allow it to destroy
        if (freezeBullet) return;

        // remove this bullet once its time is up
        if (destroyDelay > 0)
        {
            destroyTimer -= Time.deltaTime;
            if (destroyTimer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void SetBulletType(BulletTypes type)
    {
        // set sprite image & collider radius
        sprite.sprite = bulletData[(int)type].sprite;
        cc2d.radius = bulletData[(int)type].radius;
        transform.localScale = bulletData[(int)type].scale;
    }

    public void SetBulletSpeed(float speed)
    {
        // set bullet speed
        this.bulletSpeed = speed;
    }

    public void SetBulletDirection(Vector2 direction)
    {
        // set bullet direction vector
        this.bulletDirection = direction;
        // rotate the bullet for the highlight pixels
        if (direction.x > 0)
        {
            transform.Rotate(0, 180f, 0);
        }
    }

    public void SetDamageValue(int damage)
    {
        // how much damage does this bullet cause
        this.damage = damage;
    }

    public void SetDestroyDelay(float delay)
    {
        // the time this bullet will last if it doesn't collide
        this.destroyDelay = delay;
    }

    public void SetCollideWithTags(params string[] tags)
    {
        // set game object tags bullet can collide with
        this.collideWithTags = tags;
    }

    public void Shoot()
    {
        // give it speed and how long it'll last
        rb2d.velocity = bulletDirection * bulletSpeed;
        destroyTimer = destroyDelay;
    }

    public void FreezeBullet(bool freeze)
    {
        // freeze/unfreeze the bullets on screen
        // zero animation speed and freeze XYZ rigidbody constraints
        // NOTE: this will be called from the GameManager but could be used in other scripts
        if (freeze)
        {
            freezeBullet = true;
            rb2dConstraints = rb2d.constraints;
            animator.speed = 0;
            rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
            rb2d.velocity = Vector2.zero;
        }
        else
        {
            freezeBullet = false;
            animator.speed = 1;
            rb2d.constraints = rb2dConstraints;
            rb2d.velocity = bulletDirection * bulletSpeed;
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
                        // enemy controller will apply the damage player bullet can cause
                        EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(this.damage);
                        }
                        break;
                    case "Player":
                        // player controller will apply the damage enemy bullet can cause
                        PlayerController player = other.gameObject.GetComponent<PlayerController>();
                        if (player != null)
                        {
                            player.HitSide(transform.position.x > player.transform.position.x);
                            player.TakeDamage(this.damage);
                        }
                        break;
                }
                // remove the bullet - just not immediately
                Destroy(gameObject, 0.01f);
            }
        }
    }
}