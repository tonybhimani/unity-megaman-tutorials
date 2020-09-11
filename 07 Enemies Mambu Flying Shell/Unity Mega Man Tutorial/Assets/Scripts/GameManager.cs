using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance = null;

    bool isGameOver;
    bool playerReady;
    bool initReadyScreen;

    int playerScore;

    float gameRestartTime;
    float gamePlayerReadyTime;

    public float gameRestartDelay = 5f;
    public float gamePlayerReadyDelay = 3f;

    public struct WorldViewCoordinates
    {
        public float Top;
        public float Right;
        public float Bottom;
        public float Left;
    }
    public WorldViewCoordinates worldViewCoords;

    TextMeshProUGUI playerScoreText;
    TextMeshProUGUI screenMessageText;

    // Initialize the singleton instance
    private void Awake()
    {
        // If there is not already an instance of SoundManager, set it to this
        if (Instance == null)
        {
            Instance = this;
        }
        // If an instance already exists, destroy whatever this object is to enforce the singleton
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        // Set GameManager to DontDestroyOnLoad so that it won't be destroyed when reloading our scene
        DontDestroyOnLoad(gameObject);
    }

    // called third
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        // player ready screen - wait the delay time and show READY on screen
        if (playerReady)
        {
            // initialize objects and set READY text
            if (initReadyScreen)
            {
                FreezePlayer(true);
                FreezeEnemies(true);
                screenMessageText.alignment = TextAlignmentOptions.Center;
                screenMessageText.alignment = TextAlignmentOptions.Top;
                screenMessageText.fontStyle = FontStyles.UpperCase;
                screenMessageText.fontSize = 24;
                screenMessageText.text = "\n\n\n\nREADY";
                initReadyScreen = false;
            }
            // countdown READY screen pause
            gamePlayerReadyTime -= Time.deltaTime;
            if (gamePlayerReadyTime < 0)
            {
                FreezePlayer(false);
                FreezeEnemies(false);
                screenMessageText.text = "";
                playerReady = false;
            }
            return;
        }

        // show player score
        if (playerScoreText != null)
        {
            playerScoreText.text = String.Format("<mspace=\"{0}\">{1:0000000}</mspace>", playerScoreText.fontSize, playerScore);
        }

        // if the game isn't over then spawn enemies
        if (!isGameOver)
        {
            // here is where we can do things while the game is running
            GetWorldViewCoordinates();
            RepositionEnemies();
            DestroyStrayBullets();
        }
        else
        {
            // game over, wait delay then reload scene
            gameRestartTime -= Time.deltaTime;
            if (gameRestartTime < 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    // called first
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // called when the game is terminated
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // called second
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Do Startup Functions - Scene has to be fully loaded
        // otherwise we can't get a handle on the player score text object
        StartGame();
    }

    // initializes and starts the game
    private void StartGame()
    {
        isGameOver = false;
        playerReady = true;
        initReadyScreen = true;
        gamePlayerReadyTime = gamePlayerReadyDelay;
        playerScoreText = GameObject.Find("PlayerScore").GetComponent<TextMeshProUGUI>();
        screenMessageText = GameObject.Find("ScreenMessage").GetComponent<TextMeshProUGUI>();
        SoundManager.Instance.MusicSource.Play();
    }

    // objects that offer score points should call this method upon their defeat to add to the player's score
    public void AddScorePoints(int points)
    {
        playerScore += points;
    }

    private void FreezePlayer(bool freeze)
    {
        // freeze player and input
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<PlayerController>().FreezeInput(freeze);
            player.GetComponent<PlayerController>().FreezePlayer(freeze);
        }
    }

    private void FreezeEnemies(bool freeze)
    {
        // freeze all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyController>().FreezeEnemy(freeze);
        }
    }

    private void FreezeBullets(bool freeze)
    {
        // freeze all bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            bullet.GetComponent<BulletScript>().FreezeBullet(freeze);
        }
    }

    public void PlayerDefeated()
    {
        // game over :(
        isGameOver = true;
        gameRestartTime = gameRestartDelay;
        // stop all sounds
        SoundManager.Instance.Stop();
        SoundManager.Instance.StopMusic();
        // freeze player and input
        FreezePlayer(true);
        // freeze all enemies
        FreezeEnemies(true);
        // remove all bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            Destroy(bullet);
        }
        // remove all explosions
        GameObject[] explosions = GameObject.FindGameObjectsWithTag("Explosion");
        foreach (GameObject explosion in explosions)
        {
            Destroy(explosion);
        }
    }

    private void GetWorldViewCoordinates()
    {
        // get camera world coordinates just outside the left-bottom and top-right views
        Vector3 wv0 = Camera.main.ViewportToWorldPoint(new Vector3(-0.1f, -0.1f, 0));
        Vector3 wv1 = Camera.main.ViewportToWorldPoint(new Vector3(1.1f, 1.1f, 0));
        // and then update the world view coords var so it can used when needed
        worldViewCoords.Left = wv0.x;
        worldViewCoords.Bottom = wv0.y;
        worldViewCoords.Right = wv1.x;
        worldViewCoords.Top = wv1.y;
    }

    private void RepositionEnemies()
    {
        // check all enemies in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            // outside the left view
            if (enemy.transform.position.x < worldViewCoords.Left)
            {
                switch (enemy.name)
                {
                    case "Mambu":
                        enemy.transform.position = new Vector3(worldViewCoords.Right, UnityEngine.Random.Range(-0.9f, 0.7f), 0);
                        break;
                    case "KillerBomb":
                        enemy.transform.position = new Vector3(worldViewCoords.Right, UnityEngine.Random.Range(-1.5f, 1.5f), 0);
                        enemy.GetComponent<KillerBombController>().ResetFollowingPath();
                        break;
                    case "Pepe":
                        enemy.transform.position = new Vector3(worldViewCoords.Right, UnityEngine.Random.Range(-1f, 1f), 0);
                        enemy.GetComponent<PepeController>().ResetFollowingPath();
                        break;
                }
            }
        }
    }

    private void DestroyStrayBullets()
    {
        // destroy all bullets that are outside the camera world view
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            // bullet is out of view so destroy it
            if (bullet.transform.position.x < worldViewCoords.Left ||
                bullet.transform.position.x > worldViewCoords.Right ||
                bullet.transform.position.y > worldViewCoords.Top ||
                bullet.transform.position.y < worldViewCoords.Bottom)
            {
                // buh bye bullet
                Destroy(bullet);
            }
        }
    }
}