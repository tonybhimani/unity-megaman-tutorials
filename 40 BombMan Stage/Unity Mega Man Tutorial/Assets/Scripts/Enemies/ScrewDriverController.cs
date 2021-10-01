using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrewDriverController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    GameObject player;
    Vector3 playerPosition;

    // color determines the bullet type
    BulletScript.BulletTypes bulletType;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // delay before screwdriver opens
    float openTimer;
    public float openDelay = 0.5f;

    // perform attack if player is within range
    bool doAttack;
    public float playerRange = 2f;

    public enum ScrewDriverColors { Blue, Orange };
    [SerializeField] ScrewDriverColors screwDriverColor = ScrewDriverColors.Blue;

    public enum ScrewDriverState { Closed, Open };
    [SerializeField] ScrewDriverState screwDriverState = ScrewDriverState.Closed;

    public enum ScrewDriverOrientation { Bottom, Top, Left, Right };
    [SerializeField] ScrewDriverOrientation screwDriverOrientation = ScrewDriverOrientation.Bottom;

    [SerializeField] RuntimeAnimatorController racScrewDriverBlue;
    [SerializeField] RuntimeAnimatorController racScrewDriverOrange;

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
        SetColor(screwDriverColor);
        SetOrientation();

        // get player object - used for distance check
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // do ScrewDriver ai logic if it's enabled
        if (enableAI)
        {
            // get player position
            if (player != null) playerPosition = player.transform.position;

            // screwdriver has only two states - closed and open
            // while closed he's below standing megaman's bullet height
            // while open he shoots bullet spreads two times
            switch (screwDriverState)
            {
                case ScrewDriverState.Closed:
                    animator.Play("ScrewDriver_Closed");
                    // check distance to player
                    if (player != null && !doAttack)
                    {
                        float distance = Vector2.Distance(transform.position, playerPosition);
                        if (distance <= playerRange)
                        {
                            doAttack = true;
                            openTimer = openDelay;
                        }
                    }
                    // within distance and can attack
                    if (doAttack)
                    {
                        // delay before opening to attack
                        openTimer -= Time.deltaTime;
                        if (openTimer <= 0)
                        {
                            // switch to open state
                            // NOTE: animation has events that shoot the bullets and goes back to closed state
                            screwDriverState = ScrewDriverState.Open;
                        }
                    }
                    break;
                case ScrewDriverState.Open:
                    // firing of the bullets is performed via animation events calling ShootBullet() 
                    animator.Play("ScrewDriver_Open");
                    break;
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetColor(ScrewDriverColors color)
    {
        screwDriverColor = color;
        SetBulletType();
        SetAnimatorController();
    }

    public void SetPlayerRange(float range)
    {
        // set the player range to start attacking
        this.playerRange = range;
    }

    void SetAnimatorController()
    {
        // set animator controller from color
        switch (screwDriverColor)
        {
            case ScrewDriverColors.Blue:
                animator.runtimeAnimatorController = racScrewDriverBlue;
                break;
            case ScrewDriverColors.Orange:
                animator.runtimeAnimatorController = racScrewDriverOrange;
                break;
        }
    }

    void SetBulletType()
    {
        // set bullet type/color
        switch (screwDriverColor)
        {
            case ScrewDriverColors.Blue:
                bulletType = BulletScript.BulletTypes.MiniBlue;
                break;
            case ScrewDriverColors.Orange:
                bulletType = BulletScript.BulletTypes.MiniPink;
                break;
        }
    }

    void SetOrientation()
    {
        // reset rotation
        transform.rotation = Quaternion.identity;
        // rotate orientation
        switch (screwDriverOrientation)
        {
            case ScrewDriverOrientation.Bottom:
                transform.Rotate(0, 0, 0);
                break;
            case ScrewDriverOrientation.Top:
                transform.Rotate(180f, 0, 0);
                break;
            case ScrewDriverOrientation.Left:
                transform.Rotate(0, 0, -90f);
                break;
            case ScrewDriverOrientation.Right:
                transform.Rotate(0, 0, 90f);
                break;
        }
    }

    // NOTE: animation events call this function to fire the bullets
    private void ShootBullet()
    {
        GameObject[] bullets = new GameObject[5];
        Vector2[] bulletVectors = {
            new Vector2(-1f, 0),
            new Vector2(1f, 0),
            new Vector2(0, 1f),
            new Vector2(-0.75f, 0.75f),
            new Vector2(0.75f, 0.75f)
        };
        // initialize and shoot all the bullets
        for (int i = 0; i < bullets.Length; i++)
        {
            // adjust bullet orientation
            switch (screwDriverOrientation)
            {
                case ScrewDriverOrientation.Bottom:
                    // this is default, screwdriver is on the floor firing up, so make no changes
                    // 0 degrees - up
                    break;
                case ScrewDriverOrientation.Top:
                    // flip 180 degrees - down
                    bulletVectors[i] = UtilityFunctions.RotateByAngle(bulletVectors[i], 180f);
                    break;
                case ScrewDriverOrientation.Left:
                    // rotate clockwise
                    bulletVectors[i] = UtilityFunctions.RotateByAngle(bulletVectors[i], -90f);
                    break;
                case ScrewDriverOrientation.Right:
                    // rotate counter-clockwise
                    bulletVectors[i] = UtilityFunctions.RotateByAngle(bulletVectors[i], 90f);
                    break;
            }
            // instantiate each bullet prefab, set type, damage, speed, direction, collision with and destroy time
            bullets[i] = Instantiate(enemyController.bulletPrefab);
            bullets[i].name = enemyController.bulletPrefab.name;
            bullets[i].transform.position = enemyController.bulletShootPos.transform.position;
            bullets[i].GetComponent<BulletScript>().SetBulletType(bulletType);
            bullets[i].GetComponent<BulletScript>().SetDamageValue(enemyController.bulletDamage);
            bullets[i].GetComponent<BulletScript>().SetBulletSpeed(enemyController.bulletSpeed);
            bullets[i].GetComponent<BulletScript>().SetBulletDirection(bulletVectors[i]);
            bullets[i].GetComponent<BulletScript>().SetCollideWithTags("Player");
            bullets[i].GetComponent<BulletScript>().SetDestroyDelay(5f);
            bullets[i].GetComponent<BulletScript>().Shoot();
        }
        // play only one bullet sound
        SoundManager.Instance.Play(enemyController.shootBulletClip);
    }

    // called from the last animation event in the ScrewDriver_Open animation
    private void OpenAnimationStop()
    {
        doAttack = false;
        screwDriverState = ScrewDriverState.Closed;
    }
}