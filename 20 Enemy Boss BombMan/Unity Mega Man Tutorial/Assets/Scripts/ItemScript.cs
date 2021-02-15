using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (delay > 0)
        {
            Destroy(gameObject, delay);
        }
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

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            if (lifeEnergy > 0)
            {
                player.ApplyLifeEnergy(lifeEnergy);
            }

            if (weaponEnergy > 0)
            {
                player.ApplyWeaponEnergy(weaponEnergy);
            }

            if (bonusPoints > 0)
            {
                // call game manager to add bonus points
                GameManager.Instance.AddBonusPoints(bonusPoints);
            }

            // play item sound
            if (itemClip != null)
            {
                SoundManager.Instance.Play(itemClip);
            }

            // remove the item
            Destroy(gameObject);
        }
    }
}
