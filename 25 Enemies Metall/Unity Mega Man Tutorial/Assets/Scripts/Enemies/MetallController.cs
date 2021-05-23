using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetallController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    EnemyController enemyController;

    bool isFacingRight;
    bool doAttack;
    bool isShooting;

    float openTimer;
    float closedTimer;
    float shootTimer;
    float sleepTimer;

    // track the shooting sequence
    int shootSequenceNum;
    int shootSequenceCount;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // control his timing and shooting
    public float openDelay = 0.5f;
    public float closedDelay = 0.5f;
    public float shootDelay = 0.25f;
    public float sleepDelay = 2f;
    public float viewDistance = 2f;
    public int shootSequenceMax = 5;

    public enum MetallState { Closed, Open };
    public MetallState metallState = MetallState.Closed;

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
        // default to facing left
        isFacingRight = false;
        enemyController.Flip();

        // default to not shooting
        isShooting = false;

        // set up the state timers
        openTimer = openDelay;
        closedTimer = closedDelay;
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // get player distance
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float playerDistance = Vector2.Distance(player.transform.position, transform.position);

        // do Metall ai logic if it's enabled
        if (enableAI)
        {
            // state machine
            switch (metallState)
            {
                case MetallState.Closed:
                    // only update facing direction when closed
                    // because direction can change in the middle of shooting bullets
                    // and it looks like they're coming from the back of the hat instead of its front
                    bool currentFace = isFacingRight;
                    isFacingRight = (player.transform.position.x > transform.position.x);
                    if (currentFace != isFacingRight)
                    {
                        enemyController.Flip();
                    }

                    // play closed animation
                    animator.Play("Metall_Closed");

                    // player is within view distance for attacking
                    if (playerDistance < viewDistance)
                    {
                        // not attacking yet, set it up
                        if (!doAttack)
                        {
                            shootSequenceCount = 0;
                            shootSequenceNum = Random.Range(1, shootSequenceMax + 1);
                            closedTimer = closedDelay;
                            doAttack = true;
                        }
                        else
                        {
                            // haven't reached the selected number of shots
                            if (shootSequenceCount < shootSequenceNum)
                            {
                                // helmet is down until time is up then go to open
                                closedTimer -= Time.deltaTime;
                                if (closedTimer < 0)
                                {
                                    isShooting = false;
                                    openTimer = openDelay;
                                    shootTimer = shootDelay;
                                    metallState = MetallState.Open;
                                }
                            }
                            else
                            {
                                // took all the shots now sleep
                                sleepTimer -= Time.deltaTime;
                                if (sleepTimer < 0)
                                {
                                    doAttack = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        // player out of view so don't attack
                        doAttack = false;
                    }
                    break;
                case MetallState.Open:
                    // play open animation
                    animator.Play("Metall_Open");

                    // haven't taken all the shots yet
                    if (shootSequenceCount < shootSequenceNum)
                    {
                        openTimer -= Time.deltaTime;
                        shootTimer -= Time.deltaTime;

                        // time to shoot
                        if (shootTimer < 0 && !isShooting)
                        {
                            ShootBullet();
                            isShooting = true;
                        }

                        // time is up on staying open
                        if (openTimer < 0)
                        {
                            isShooting = false;
                            shootSequenceCount++;
                            closedTimer = closedDelay;
                            metallState = MetallState.Closed;
                            // add a little randomness to the close timer value
                            float variance = Random.Range(0, 5) / 10f;
                            if (variance > 0f)
                            {
                                closedTimer -= variance;
                            }
                            // end of open/firing sequence, go to sleep in close state
                            if (shootSequenceCount == shootSequenceNum)
                            {
                                sleepTimer = sleepDelay;
                            }
                        }
                    }
                    break;
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    private void ShootBullet()
    {
        GameObject[] bullets = new GameObject[3];
        Vector2[] bulletVectors = {
            new Vector2(-0.75f, 0.75f),
            new Vector2(-1f, 0),
            new Vector2(-0.75f, -0.75f)
        };
        // initialize and shoot all the bullets
        for (int i = 0; i < bullets.Length; i++)
        {
            // bullet orient to negative x-axis, rotate bullet direction if facing right
            if (isFacingRight)
            {
                bulletVectors[i] = UtilityFunctions.RotateByAngle(bulletVectors[i], 180f);
            }
            // instantiate bullet prefab, set color, damage, speed, and direction
            bullets[i] = Instantiate(enemyController.bulletPrefab);
            bullets[i].name = enemyController.bulletPrefab.name;
            bullets[i].transform.position = enemyController.bulletShootPos.transform.position;
            bullets[i].GetComponent<SpriteRenderer>().sortingOrder = 11;
            bullets[i].GetComponent<BulletScript>().SetBulletType(BulletScript.BulletTypes.MiniOrange);
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

    // we call these functions from our two animations
    // Closed - make'em invincible
    private void StartInvincibleAnimation()
    {
        enemyController.Invincible(true);
    }

    // Open - beware the Mega Buster!
    private void StopInvincibleAnimation()
    {
        enemyController.Invincible(false);
    }
}