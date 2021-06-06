using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlasterController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    // track which bullet is being fired
    int bulletIndex = 0;

    // color determines the bullet type
    BulletScript.BulletTypes bulletType;

    // how long blaster will stay closed and invincible
    float closedTimer;
    public float closedDelay = 2f;

    // perform attack if player is within range
    bool doAttack;
    public float playerRange = 2f;

    public enum BlasterColors { Blue, Orange, Red };
    [SerializeField] BlasterColors blasterColor = BlasterColors.Blue;

    public enum BlasterState { Closed, Open };
    [SerializeField] BlasterState blasterState = BlasterState.Closed;

    public enum BlasterOrientation { Bottom, Top, Left, Right };
    [SerializeField] BlasterOrientation blasterOrientation = BlasterOrientation.Left;

    [SerializeField] RuntimeAnimatorController racBlasterBlue;
    [SerializeField] RuntimeAnimatorController racBlasterOrange;
    [SerializeField] RuntimeAnimatorController racBlasterRed;

    void Awake()
    {
        // get components from EnemyController
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
        box2d = enemyController.GetComponent<BoxCollider2D>();
        rb2d = enemyController.GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // set color and orientation
        SetColor(blasterColor);
        SetOrientation();
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // get player object - used for distance check
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // blaster has only two states - closed and open
        // while closed he's invicible and when open vulnerable
        // and fires four bullets at different vectors
        switch (blasterState)
        {
            case BlasterState.Closed:
                animator.Play("Blaster_Closed");
                // check distance to player
                if (player != null && !doAttack)
                {
                    float distance = Vector2.Distance(transform.position, player.transform.position);
                    if (distance <= playerRange)
                    {
                        doAttack = true;
                        closedTimer = closedDelay;
                    }
                }
                // within distance and can attack
                if (doAttack)
                {
                    // delay before opening to attack
                    closedTimer -= Time.deltaTime;
                    if (closedTimer <= 0)
                    {
                        // switch to open state
                        // NOTE: animation has events that shoot the bullets and goes back to closed state
                        blasterState = BlasterState.Open;
                    }
                }
                break;
            case BlasterState.Open:
                // firing of the bullets is performed via animation events calling ShootBullet() 
                animator.Play("Blaster_Open");
                break;
        }
    }

    public void SetColor(BlasterColors color)
    {
        blasterColor = color;
        SetBulletType();
        SetAnimatorController();
    }

    void SetAnimatorController()
    {
        // set animator controller from color
        switch (blasterColor)
        {
            case BlasterColors.Blue:
                animator.runtimeAnimatorController = racBlasterBlue;
                break;
            case BlasterColors.Orange:
                animator.runtimeAnimatorController = racBlasterOrange;
                break;
            case BlasterColors.Red:
                animator.runtimeAnimatorController = racBlasterRed;
                break;
        }
    }

    void SetBulletType()
    {
        // set bullet type/color
        switch (blasterColor)
        {
            case BlasterColors.Blue:
                bulletType = BulletScript.BulletTypes.MiniBlue;
                break;
            case BlasterColors.Orange:
                bulletType = BulletScript.BulletTypes.MiniPink;
                break;
            case BlasterColors.Red:
                bulletType = BulletScript.BulletTypes.MiniRed;
                break;
        }
    }

    void SetOrientation()
    {
        // reset rotation
        transform.rotation = Quaternion.identity;
        // rotate orientation
        switch (blasterOrientation)
        {
            case BlasterOrientation.Bottom:
                transform.Rotate(0, 0, 90f);
                break;
            case BlasterOrientation.Top:
                transform.Rotate(0, 0, -90f);
                break;
            case BlasterOrientation.Left:
                transform.Rotate(0, 0, 0);
                break;
            case BlasterOrientation.Right:
                transform.Rotate(0, 180f, 0);
                break;
        }
    }

    // NOTE: animation events call this function to fire the bullets
    private void ShootBullet()
    {
        GameObject bullet;
        Vector2[] bulletVectors = {
            new Vector2(0.75f, 0.75f),
            new Vector2(1f, 0.15f),
            new Vector2(1f, -0.15f),
            new Vector2(0.75f, -0.75f)
        };
        // bulletIndex determines which bullet to fire
        // adjust bullet orientation
        switch (blasterOrientation)
        {
            case BlasterOrientation.Left:
                // this is default, blaster fires along postive x-axis, so make no changes
                break;
            case BlasterOrientation.Right:
                // rotating causes the bullets to invert (firing from bottom to up)
                // times x by -1 - fires left - negative x-axis
                bulletVectors[bulletIndex].x *= -1;
                break;
            case BlasterOrientation.Bottom:
                // rotate counter-clockwise - fires up - positive y-axis
                bulletVectors[bulletIndex] = UtilityFunctions.RotateByAngle(bulletVectors[bulletIndex], 90f);
                break;
            case BlasterOrientation.Top:
                // rotate clockwise - fires down - negative y-axis
                bulletVectors[bulletIndex] = UtilityFunctions.RotateByAngle(bulletVectors[bulletIndex], -90f);
                break;
        }
        // instantiate bullet prefab, set type, damage, speed, direction, collision with and destroy time
        bullet = Instantiate(enemyController.bulletPrefab);
        bullet.name = enemyController.bulletPrefab.name;
        bullet.transform.position = enemyController.bulletShootPos.transform.position;
        bullet.GetComponent<BulletScript>().SetBulletType(bulletType);
        bullet.GetComponent<BulletScript>().SetDamageValue(enemyController.bulletDamage);
        bullet.GetComponent<BulletScript>().SetBulletSpeed(enemyController.bulletSpeed);
        bullet.GetComponent<BulletScript>().SetBulletDirection(bulletVectors[bulletIndex]);
        bullet.GetComponent<BulletScript>().SetCollideWithTags("Player");
        bullet.GetComponent<BulletScript>().SetDestroyDelay(5f);
        bullet.GetComponent<BulletScript>().Shoot();
        // increment/reset bulletIndex for next firing sequence
        if (++bulletIndex > bulletVectors.Length - 1)
        {
            // reset bulletIndex
            bulletIndex = 0;
        }
        // play only one bullet sound
        SoundManager.Instance.Play(enemyController.shootBulletClip);
    }

    // called from the animation event in Blaster_Closed animation
    private void InvincibleAimationStart()
    {
        enemyController.Invincible(true);
    }

    // called from the first animation event in the Blaster_Open animation
    private void OpenAnimationStart()
    {
        enemyController.Invincible(false);
    }

    // called from the last animation event in the Blaster_Open animation
    private void OpenAnimationStop()
    {
        doAttack = false;
        blasterState = BlasterState.Closed;
    }
}
