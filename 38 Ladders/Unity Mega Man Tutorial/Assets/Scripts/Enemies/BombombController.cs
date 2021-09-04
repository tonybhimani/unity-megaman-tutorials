using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombombController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;
    EnemyController enemyController;

    // time between bomb launches
    float launchTimer;

    // flag to enable enemy ai logic
    [SerializeField] bool enableAI;

    // bombomb properties
    public enum BombombTypes { Launcher, Bomb, Shrapnel };
    [SerializeField] BombombTypes bombombType = BombombTypes.Launcher;

    public enum BombombColors { Blue, Red };
    [SerializeField] BombombColors bombombColor = BombombColors.Blue;

    [Header("Timers & Collision")]
    [SerializeField] float launchDelay = 5f;

    [SerializeField] string[] collideWithTags;

    [Header("Velocity Vectors")]
    [SerializeField] Vector2 bombVelocity = new Vector2(0, 5f);

    [SerializeField]
    Vector2[] shrapnelVectors =
    {
        new Vector2(-0.75f, 2f),
        new Vector2(-1.5f, 2.5f),
        new Vector2(1.5f, 2.5f),
        new Vector2(0.75f, 2f)
    };

    [Header("Prefabs")]
    [SerializeField] GameObject bombPrefab;
    [SerializeField] GameObject shrapnelPrefab;

    [Header("Audio Clips")]
    [SerializeField] AudioClip explosionClip;

    [Header("Animator Controllers")]
    [SerializeField] RuntimeAnimatorController racBombombBlue;
    [SerializeField] RuntimeAnimatorController racBombombRed;

    void Awake()
    {
        // get components from EnemyController
        enemyController = GetComponent<EnemyController>();
        animator = enemyController.GetComponent<Animator>();
        box2d = enemyController.GetComponent<BoxCollider2D>();
        rb2d = enemyController.GetComponent<Rigidbody2D>();
        sprite = enemyController.GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        switch (bombombType)
        {
            case BombombTypes.Launcher:
                // only the launcher uses the timer
                launchTimer = launchDelay;
                break;
            case BombombTypes.Bomb:
            case BombombTypes.Shrapnel:
                // always invincible
                enemyController.Invincible(true);
                // set bomb & shrapnel color
                SetColor(bombombColor);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyController.freezeEnemy)
        {
            // add anything here to happen while frozen i.e. time compensations
            return;
        }

        // do bombomb ai logic if it's enabled
        if (enableAI)
        {
            switch (bombombType)
            {
                case BombombTypes.Launcher:
                    launchTimer -= Time.deltaTime;
                    if (launchTimer <= 0)
                    {
                        // create the bomb and launch it
                        GameObject bomb = Instantiate(bombPrefab);
                        bomb.name = bombPrefab.name;
                        bomb.transform.position = transform.position;
                        bomb.GetComponent<BombombController>().EnableAI(this.enableAI);
                        bomb.GetComponent<BombombController>().SetColor(this.bombombColor);
                        bomb.GetComponent<BombombController>().SetCollideWithTags(this.collideWithTags);
                        bomb.GetComponent<BombombController>().SetExplosionDamage(enemyController.explosionDamage);
                        bomb.GetComponent<BombombController>().SetShrapnelVectors(this.shrapnelVectors);
                        bomb.GetComponent<Rigidbody2D>().velocity = this.bombVelocity;
                        // reset the launch timer
                        launchTimer = launchDelay;
                    }
                    break;
                case BombombTypes.Bomb:
                    // once it peaks let it drop a bit
                    if (rb2d.velocity.y <= -1.0f)
                    {
                        // create explosion animation
                        GameObject explodeEffect = Instantiate(enemyController.explodeEffectPrefab);
                        explodeEffect.name = enemyController.explodeEffectPrefab.name;
                        explodeEffect.transform.position = sprite.bounds.center;
                        explodeEffect.GetComponent<ExplosionScript>().SetCollideWithTags(this.collideWithTags);
                        explodeEffect.GetComponent<ExplosionScript>().SetDamageValue(enemyController.explosionDamage);
                        explodeEffect.GetComponent<ExplosionScript>().SetDestroyDelay(enemyController.explodeEffectDestroyDelay);

                        // launch the shrapnel
                        GameObject[] shrapnel = new GameObject[4];
                        for (int i = 0; i < shrapnel.Length; i++)
                        {
                            shrapnel[i] = Instantiate(shrapnelPrefab);
                            shrapnel[i].name = shrapnelPrefab.name;
                            shrapnel[i].transform.position = sprite.bounds.center;
                            shrapnel[i].GetComponent<BombombController>().EnableAI(this.enableAI);
                            shrapnel[i].GetComponent<BombombController>().SetColor(this.bombombColor);
                            shrapnel[i].GetComponent<BombombController>().SetCollideWithTags(this.collideWithTags);
                            shrapnel[i].GetComponent<BombombController>().SetExplosionDamage(enemyController.explosionDamage);
                            shrapnel[i].GetComponent<Rigidbody2D>().velocity = this.shrapnelVectors[i];
                        }

                        // destroy the bomb
                        Destroy(gameObject);
                    }
                    break;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (bombombType == BombombTypes.Shrapnel)
        {
            // check for shrapnel colliding with the ground layer
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // create explosion animation
                GameObject explodeEffect = Instantiate(enemyController.explodeEffectPrefab);
                explodeEffect.name = enemyController.explodeEffectPrefab.name;
                explodeEffect.transform.position = sprite.bounds.center;
                explodeEffect.GetComponent<ExplosionScript>().SetCollideWithTags(this.collideWithTags);
                explodeEffect.GetComponent<ExplosionScript>().SetDamageValue(enemyController.explosionDamage);
                explodeEffect.GetComponent<ExplosionScript>().SetDestroyDelay(enemyController.explodeEffectDestroyDelay);

                // hide the shrapnel but leave it on screen long enough to play the audio
                // also disable the hitbox because even though it's invisible the player can
                // still touch it and show he's taking damage
                sprite.color = Color.clear;
                gameObject.transform.Find("HitBox").gameObject.SetActive(false);
                Destroy(gameObject, 1f);

                // play explosion audio clip
                SoundManager.Instance.Play(explosionClip);
            }
        }
    }

    public void EnableAI(bool enable)
    {
        // enable enemy ai logic
        this.enableAI = enable;
    }

    public void SetColor(BombombColors color)
    {
        // set color and update animator controllers
        bombombColor = color;
        SetAnimatorController();
    }

    void SetAnimatorController()
    {
        // set animator control from color
        switch (bombombColor)
        {
            case BombombColors.Blue:
                animator.runtimeAnimatorController = racBombombBlue;
                break;
            case BombombColors.Red:
                animator.runtimeAnimatorController = racBombombRed;
                break;
        }
    }

    public void SetLaunchDelay(float delay)
    {
        // time between bomb launches
        this.launchDelay = delay;
    }

    public void SetCollideWithTags(params string[] tags)
    {
        // can collide with these tags
        this.collideWithTags = tags;
    }

    public void SetExplosionDamage(int damage)
    {
        // override enemy controller explosion damage
        enemyController.explosionDamage = damage;
    }

    public void SetBombVelocity(Vector2 velocity)
    {
        // override default bomb velocity
        this.bombVelocity = velocity;
    }

    public void SetShrapnelVectors(params Vector2[] vectors)
    {
        // override default shrapnel vectors
        this.shrapnelVectors = vectors;
    }
}