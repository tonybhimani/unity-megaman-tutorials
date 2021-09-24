using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionScript : MonoBehaviour
{
    Animator animator;
    SpriteRenderer sprite;

    int damage = 0;

    float destroyTimer;
    float destroyDelay = 2f;

    // freeze/hide explosion on screen
    bool freezeExplosion;
    bool wasFrozen;
    float animatorSpeed;
    Color explosionColor;

    string[] collideWithTags = { "Player" };

    // override name from weapon creating explosion
    string damageOverrideName = null;

    void Awake()
    {
        // get components
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // init the timer with the delay
        SetDestroyDelay(destroyDelay);
    }

    // Update is called once per frame
    void Update()
    {
        // if the explosion is frozen then don't allow it to destroy
        if (freezeExplosion) return;

        // countdown to destroy
        if (destroyDelay > 0)
        {
            destroyTimer -= Time.deltaTime;
            if (destroyTimer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void SetDamageValue(int damage)
    {
        this.damage = damage;
    }

    public void SetDestroyDelay(float delay)
    {
        this.destroyDelay = delay;
        // set the timer in motion here
        // nothing triggers the timer to start elsewhere
        this.destroyTimer = delay;
    }

    public void SetCollideWithTags(params string[] tags)
    {
        this.collideWithTags = tags;
    }

    public void SetDamageOverrideName(string damageOverrideName)
    {
        this.damageOverrideName = damageOverrideName;
    }

    public void FreezeExplosion(bool freeze)
    {
        // freeze/unfreeze the explosions on screen
        // NOTE: this will be called from the GameManager but could be used in other scripts
        if (freeze)
        {
            freezeExplosion = true;
            wasFrozen = true;
            animatorSpeed = animator.speed;
            animator.speed = 0;
        }
        else
        {
            // only unfreeze if was frozen otherwise expect weird results
            if (wasFrozen)
            {
                freezeExplosion = false;
                animator.speed = animatorSpeed;
            }
        }
    }

    public void HideExplosion(bool hide)
    {
        // hide/show the explosions on the screen
        // get the current color then set to transparent
        // restore the explosion to its saved color
        if (hide)
        {
            explosionColor = sprite.color;
            sprite.color = Color.clear;
        }
        else
        {
            sprite.color = explosionColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // we only apply damage when this value is set
        // not all explosions affect the player i.e. smaller ones
        // KillerBomb uses a large explosion and does damage the player
        // that is when this script and collision would apply
        if (this.damage > 0)
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
                                enemy.TakeDamage(this.damage, this.damageOverrideName);
                            }
                            break;
                        case "Player":
                            // player controller will apply the damage the bomb can cause
                            PlayerController player = other.gameObject.GetComponent<PlayerController>();
                            if (player != null)
                            {
                                player.HitSide(transform.position.x > player.transform.position.x);
                                player.TakeDamage(this.damage);
                            }
                            break;
                    }
                }
            }
        }
    }
}