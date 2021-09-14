using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// all enemies will require these components
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]

public class EnemyController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    bool isInvincible;

    GameObject explodeEffect;

    // freeze/hide enemy on the screen
    float animatorSpeed;
    Color enemyColor;
    Vector2 freezeVelocity;
    RigidbodyConstraints2D rb2dConstraints;

    public bool freezeEnemy;

    public bool hasHealthBar;

    [Header("Enemy Settings")]
    public int scorePoints = 500;
    public int currentHealth;
    public int maxHealth = 1;
    public int contactDamage = 1;
    public int explosionDamage = 0;
    public int bulletDamage = 1;
    public float bulletSpeed = 3f;

    [Header("Bonus Item Settings")]
    public ItemScript.ItemTypes bonusItemType;
    public ItemScript.BonusBallColors bonusBallColor;
    public ItemScript.WeaponPartColors weaponPartColor;
    public float bonusDestroyDelay = 5f;
    public Vector2 bonusVelocity = new Vector2(0, 3f);
    public UnityAction BonusItemAction;

    [Header("Audio Clips")]
    public AudioClip damageClip;
    public AudioClip blockAttackClip;
    public AudioClip shootBulletClip;
    public AudioClip energyFillClip;

    [Header("Positions and Prefabs")]
    public GameObject bulletShootPos;
    public GameObject bulletPrefab;
    public GameObject explodeEffectPrefab;
    public float explodeEffectDestroyDelay = 2f;

    [Header("Enemy Events")]
    public UnityEvent TakeDamageEvent;
    public UnityEvent DefeatEvent;

    [System.Serializable]
    public struct DamageOverridesStruct
    {
        public bool ignoreInvincibility;
        public int damageAmount;
        public string overrideName;
        public UnityEvent overrideEvent;
    }
    public DamageOverridesStruct[] damageOverrides;

    void Awake()
    {
        // get handles to components
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // start at full health
        currentHealth = maxHealth;
    }

    public void Flip()
    {
        transform.Rotate(0, 180f, 0);
    }

    public void Invincible(bool invincibility)
    {
        isInvincible = invincibility;
    }

    public void TakeDamage(int damage, string overrideName = null)
    {
        // apply damage overrides
        bool ignoreInvincibility = false;
        for (int i = 0; i < damageOverrides.Length; i++)
        {
            // the override name matches
            if (damageOverrides[i].overrideName == overrideName)
            {
                // override the damage amount and get ignore invincibility
                damage = damageOverrides[i].damageAmount;
                ignoreInvincibility = damageOverrides[i].ignoreInvincibility;
                // check for override event and invoke if set
                if (damageOverrides[i].overrideEvent != null)
                {
                    damageOverrides[i].overrideEvent.Invoke();
                }
                // override found, exit the loop early
                break;
            }
        }

        // take damage if not invincible
        if (!isInvincible || ignoreInvincibility)
        {
            // take damage amount from health and call defeat if no health
            if (damage > 0)
            {
                // invoke take damage event
                TakeDamageEvent.Invoke();
                // update health value and energy bar
                currentHealth -= damage;
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                if (hasHealthBar && UIEnergyBars.Instance != null)
                {
                    UIEnergyBars.Instance.SetValue(UIEnergyBars.EnergyBars.EnemyHealth, currentHealth / (float)maxHealth);
                }
                // play taking damage sound clip
                if (damageClip != null)
                {
                    SoundManager.Instance.Play(damageClip);
                }
            }
            // no more health means defeat
            if (currentHealth <= 0)
            {
                Defeat();
            }
        }
        else
        {
            // block attack sound - dink!
            if (blockAttackClip != null)
            {
                SoundManager.Instance.Play(blockAttackClip);
            }
        }
    }

    public void SetBonusItemType(ItemScript.ItemTypes itemType)
    {
        // set bonus item type
        this.bonusItemType = itemType;
    }

    public void SetBonusBallColor(ItemScript.BonusBallColors color)
    {
        // set bonus ball color
        this.bonusBallColor = color;
    }

    public void SetWeaponPartColor(ItemScript.WeaponPartColors color)
    {
        // set weapon part color
        this.weaponPartColor = color;
    }

    public void SetBonusDestroyDelay(float delay)
    {
        // set bonus item destroy delay
        this.bonusDestroyDelay = delay;
    }

    public void SetBonusVelocity(Vector2 velocity)
    {
        // set bonus item velocity
        this.bonusVelocity = velocity;
    }

    void StartDefeatAnimation()
    {
        // play explosion animation
        //   create copy of prefab, place its spawn location at center of sprite, 
        //   set explosion damage value (if any), destroy after explodeEffectDestroyDelay
        explodeEffect = Instantiate(explodeEffectPrefab);
        explodeEffect.name = explodeEffectPrefab.name;
        explodeEffect.transform.position = sprite.bounds.center;
        explodeEffect.GetComponent<ExplosionScript>().SetDamageValue(this.explosionDamage);
        explodeEffect.GetComponent<ExplosionScript>().SetDestroyDelay(explodeEffectDestroyDelay);

        // get the bonus item prefab
        GameObject bonusItemPrefab = GameManager.Instance.GetBonusItem(bonusItemType);
        if (bonusItemPrefab != null)
        {
            // instantiate the bonus item
            GameObject bonusItem = Instantiate(bonusItemPrefab);
            bonusItem.name = bonusItemPrefab.name;
            bonusItem.transform.position = explodeEffect.transform.position;
            bonusItem.GetComponent<ItemScript>().Animate(true);
            bonusItem.GetComponent<ItemScript>().SetDestroyDelay(bonusDestroyDelay);
            bonusItem.GetComponent<ItemScript>().SetBonusBallColor(bonusBallColor);
            bonusItem.GetComponent<ItemScript>().SetWeaponPartColor(weaponPartColor);
            if (BonusItemAction != null)
            {
                // add bonus item action(s) to event
                bonusItem.GetComponent<ItemScript>().BonusItemEvent.AddListener(BonusItemAction);
            }
            // give the bonus item a bounce effect
            bonusItem.GetComponent<Rigidbody2D>().velocity = bonusVelocity;
        }
    }

    void StopDefeatAnimation()
    {
        // we have this function in case we want to remove the explosion before it finishes
        Destroy(explodeEffect);
    }

    void Defeat()
    {
        // invoke defeat event
        DefeatEvent.Invoke();
        // play explosion animation, remove enemy, give player score points
        StartDefeatAnimation();
        Destroy(gameObject);
        GameManager.Instance.AddScorePoints(this.scorePoints);
    }

    public void FreezeEnemy(bool freeze)
    {
        // freeze/unfreeze the enemy on screen
        // zero animation speed and freeze XYZ rigidbody constraints
        // NOTE: this will be called from the GameManager but could be used in other scripts
        if (freeze)
        {
            freezeEnemy = true;
            animatorSpeed = animator.speed;
            rb2dConstraints = rb2d.constraints;
            freezeVelocity = rb2d.velocity;
            animator.speed = 0;
            rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            freezeEnemy = false;
            animator.speed = animatorSpeed;
            rb2d.constraints = rb2dConstraints;
            rb2d.velocity = freezeVelocity;
        }
    }

    public void HideEnemy(bool hide)
    {
        // hide/show the enemy on the screen
        // get the current color then set to transparent
        // restore the explosion to its saved color
        if (hide)
        {
            enemyColor = sprite.color;
            sprite.color = Color.clear;
        }
        else
        {
            sprite.color = enemyColor;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // check for collision with player
        if (other.gameObject.CompareTag("Player"))
        {
            // colliding with player inflicts damage and takes contact damage away from health
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.HitSide(transform.position.x > player.transform.position.x);
            player.TakeDamage(this.contactDamage);
        }
    }
}