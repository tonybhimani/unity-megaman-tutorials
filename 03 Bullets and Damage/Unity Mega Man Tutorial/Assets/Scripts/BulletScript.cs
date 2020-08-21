using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    float destroyTime;

    public int damage = 1;

    [SerializeField] float bulletSpeed;
    [SerializeField] Vector2 bulletDirection;
    [SerializeField] float destroyDelay;

    // Start is called before the first frame update
    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // remove this bullet once its time is up
        destroyTime -= Time.deltaTime;
        if (destroyTime < 0)
        {
            Destroy(gameObject);
        }
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

    public void Shoot()
    {
        // flip the bullet sprite for the highlight pixels
        sprite.flipX = (bulletDirection.x < 0);
        // give it speed and how long it'll last
        rb2d.velocity = bulletDirection * bulletSpeed;
        destroyTime = destroyDelay;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // check for collision with enemy
        if (other.gameObject.CompareTag("Enemy"))
        {
            // enemy controller will apply the damage our bullet can cause
            EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(this.damage);
            }
            // remove the bullet - just not immediately
            Destroy(gameObject, 0.01f);
        }
    }
}
