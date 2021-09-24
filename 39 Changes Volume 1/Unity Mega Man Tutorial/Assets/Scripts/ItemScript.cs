using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// all items will require these components
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]

public class ItemScript : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    ColorSwap colorSwap;

    float destroyTimer;

    // freeze/hide bonus item on screen
    bool freezeItem;
    bool wasFrozen;
    bool animateItem;
    float itemAlpha;
    Color itemColor;
    Vector2 freezeVelocity;
    RigidbodyConstraints2D rb2dConstraints;

    private enum SwapIndex
    {
        Primary = 64,
        Secondary = 128
    }

    public enum ItemTypes
    {
        Nothing,
        Random,
        BonusBall,
        ExtraLife,
        LifeEnergyBig,
        LifeEnergySmall,
        WeaponEnergyBig,
        WeaponEnergySmall,
        MagnetBeam,
        WeaponPart,
        Yashichi
    };

    [SerializeField] ItemTypes itemType;

    [SerializeField] bool animate;
    [SerializeField] float destroyDelay;
    [SerializeField] int lifeEnergy;
    [SerializeField] int weaponEnergy;
    [SerializeField] int bonusPoints;

    [Header("Audio Clips")]
    [SerializeField] AudioClip itemClip;

    [Header("Bonus Ball Settings")]
    [SerializeField] RuntimeAnimatorController racBonusBallBlue;
    [SerializeField] RuntimeAnimatorController racBonusBallGray;
    [SerializeField] RuntimeAnimatorController racBonusBallGreen;
    [SerializeField] RuntimeAnimatorController racBonusBallOrange;
    [SerializeField] RuntimeAnimatorController racBonusBallRed;
    public enum BonusBallColors { Random, Blue, Gray, Green, Orange, Red };
    [SerializeField] BonusBallColors bonusBallColor = BonusBallColors.Blue;

    [Header("Weapon Part Settings")]
    [SerializeField] RuntimeAnimatorController racWeaponPartBlue;
    [SerializeField] RuntimeAnimatorController racWeaponPartOrange;
    [SerializeField] RuntimeAnimatorController racWeaponPartRed;
    public enum WeaponPartColors { Random, Blue, Orange, Red };
    [SerializeField] WeaponPartColors weaponPartColor = WeaponPartColors.Blue;
    public enum WeaponPartEnemies { None, BombMan, CutMan, ElecMan, FireMan, GutsMan, IceMan };
    [SerializeField] WeaponPartEnemies weaponPartEnemy = WeaponPartEnemies.None;

    [Header("Bonus Item Events")]
    public UnityEvent BonusItemEvent;

    void Awake()
    {
        // get components
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // color swap component to change item's palette
        colorSwap = GetComponent<ColorSwap>();

        // set the color swap palette
        SetColorPalette();

        // if no animation then default to the first frame
        Animate(animate);

        // if there is a delay set then apply it
        SetDestroyDelay(destroyDelay);

        // set bonus ball color
        if (itemType == ItemTypes.BonusBall)
        {
            SetBonusBallColor(bonusBallColor);
        }

        // set weapon part color
        if (itemType == ItemTypes.WeaponPart)
        {
            SetWeaponPartColor(weaponPartColor);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // if the bous item is frozen then don't allow it to destroy
        if (freezeItem) return;

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

    public void Animate(bool animate)
    {
        if (animate)
        {
            animator.Play("Default");
            animator.speed = 1;
        }
        else
        {
            animator.Play("Default", 0, 0);
            animator.speed = 0;
        }
    }

    public void SetDestroyDelay(float delay)
    {
        destroyDelay = delay;
        // set the timer in motion here
        // nothing triggers the timer to start elsewhere
        destroyTimer = delay;
    }

    public void SetBonusBallColor(BonusBallColors color)
    {
        if (itemType == ItemTypes.BonusBall)
        {
            bonusBallColor = color;
            SetBonusBallAnimatorController();
        }
    }

    void SetBonusBallAnimatorController()
    {
        // get bonus ball color
        BonusBallColors color = bonusBallColor;

        // if random pick a bonus ball color
        if (color == BonusBallColors.Random)
        {
            color = (BonusBallColors)UnityEngine.Random.Range(
                1, Enum.GetNames(typeof(BonusBallColors)).Length);
        }

        // set color animator controller
        switch (color)
        {
            case BonusBallColors.Blue:
                animator.runtimeAnimatorController = racBonusBallBlue;
                break;
            case BonusBallColors.Gray:
                animator.runtimeAnimatorController = racBonusBallGray;
                break;
            case BonusBallColors.Green:
                animator.runtimeAnimatorController = racBonusBallGreen;
                break;
            case BonusBallColors.Orange:
                animator.runtimeAnimatorController = racBonusBallOrange;
                break;
            case BonusBallColors.Red:
                animator.runtimeAnimatorController = racBonusBallRed;
                break;
        }
    }

    public void SetWeaponPartColor(WeaponPartColors color)
    {
        if (itemType == ItemTypes.WeaponPart)
        {
            weaponPartColor = color;
            SetWeaponPartAnimatorController();
        }
    }

    void SetWeaponPartAnimatorController()
    {
        // get weapon part color
        WeaponPartColors color = weaponPartColor;

        // if random pick a weapon part color
        if (color == WeaponPartColors.Random)
        {
            color = (WeaponPartColors)UnityEngine.Random.Range(
                1, Enum.GetNames(typeof(WeaponPartColors)).Length);
        }

        // set color animator controller
        switch (color)
        {
            case WeaponPartColors.Blue:
                animator.runtimeAnimatorController = racWeaponPartBlue;
                break;
            case WeaponPartColors.Orange:
                animator.runtimeAnimatorController = racWeaponPartOrange;
                break;
            case WeaponPartColors.Red:
                animator.runtimeAnimatorController = racWeaponPartRed;
                break;
        }
    }

    public void SetColorPalette()
    {
        // not all bonus items have the ColorSwap component
        // only the Extra Life, Magnet Beam and Weapon Energies
        if (colorSwap != null)
        {
            // default to megabuster / magnetbeam colors
            // dark blue, light blue
            colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x0073F7));
            colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0x00FFFF));

            // find the player's controller to access the weapon type
            PlayerController player = GameObject.FindObjectOfType<PlayerController>();
            if (player != null)
            {
                // apply new selected color scheme with ColorSwap
                switch (player.playerWeapon)
                {
                    case PlayerController.WeaponTypes.HyperBomb:
                        // green, light gray
                        colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x009400));
                        colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                        break;
                    case PlayerController.WeaponTypes.RollingCutter:
                        // dark gray, light gray
                        colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x747474));
                        colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                        break;
                    case PlayerController.WeaponTypes.ThunderBeam:
                        // dark gray, light yellow
                        colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x747474));
                        colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCE4A0));
                        break;
                    case PlayerController.WeaponTypes.FireStorm:
                        // dark orange, yellow gold
                        colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0xD82800));
                        colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xF0BC3C));
                        break;
                    case PlayerController.WeaponTypes.SuperArm:
                        // orange red, light gray
                        colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0xC84C0C));
                        colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                        break;
                    case PlayerController.WeaponTypes.IceSlasher:
                        // dark blue, light gray
                        colorSwap.SwapColor((int)SwapIndex.Primary, ColorSwap.ColorFromInt(0x2038EC));
                        colorSwap.SwapColor((int)SwapIndex.Secondary, ColorSwap.ColorFromInt(0xFCFCFC));
                        break;
                }
            }

            // apply the color changes
            colorSwap.ApplyColor();
        }
    }

    public void FreezeItem(bool freeze)
    {
        // freeze/unfreeze the bonus item on screen
        // NOTE: this will be called from the GameManager but could be used in other scripts
        if (freeze)
        {
            freezeItem = true;
            wasFrozen = true;
            animateItem = animate;
            if (animateItem) Animate(false);
            rb2dConstraints = rb2d.constraints;
            freezeVelocity = rb2d.velocity;
            rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            // only unfreeze if was frozen otherwise expect weird results
            if (wasFrozen)
            {
                freezeItem = false;
                wasFrozen = false;
                if (animateItem) Animate(true);
                rb2d.constraints = rb2dConstraints;
                rb2d.velocity = freezeVelocity;
            }
        }
    }

    public void HideItem(bool hide)
    {
        // hide/show the bonus item on the screen
        // get the current material alpha then set to zero (transparent)
        // restore the material alpha to its previous value
        if (hide)
        {
            if (colorSwap != null)
            {
                itemAlpha = sprite.material.GetFloat("_Transparency");
                sprite.material.SetFloat("_Transparency", 0f);
            }
            else
            {
                itemColor = sprite.color;
                sprite.color = Color.clear;
            }
        }
        else
        {
            if (colorSwap != null)
            {
                sprite.material.SetFloat("_Transparency", itemAlpha);
            }
            else
            {
                sprite.color = itemColor;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (lifeEnergy > 0)
            {
                // add to player health energy
                player.ApplyLifeEnergy(lifeEnergy);
            }

            if (weaponEnergy > 0)
            {
                // add to current weapon energy
                player.ApplyWeaponEnergy(weaponEnergy);
            }

            if (bonusPoints > 0)
            {
                // call game manager to add bonus points
                GameManager.Instance.AddBonusPoints(bonusPoints);
            }

            if (itemType == ItemTypes.ExtraLife)
            {
                // call game manager to add an extra life
                GameManager.Instance.AddPlayerLives(1);
            }

            if (itemType == ItemTypes.MagnetBeam)
            {
                // collected the magnet beam item
                player.EnableMagnetBeam(true);
            }

            if (itemType == ItemTypes.WeaponPart)
            {
                // collected a weapon part from a defeated boss
                player.EnableWeaponPart(weaponPartEnemy);
            }

            // play item sound
            if (itemClip != null)
            {
                SoundManager.Instance.Play(itemClip);
            }

            // invoke the bonus item event
            if (BonusItemEvent != null)
            {
                BonusItemEvent.Invoke();
            }

            // remove the item
            Destroy(gameObject);
        }
    }
}