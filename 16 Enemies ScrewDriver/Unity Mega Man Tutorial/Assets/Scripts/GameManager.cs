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

    AssetPalette assetPalette;
    int enemyPrefabCount;

    bool showMessage;
    bool firstMessage;
    int messageIndex;
    string messageText;
    float messageTimer;
    float messageDelay = 2.5f;

    bool isGameOver;
    bool playerReady;
    bool initReadyScreen;

    int playerScore;
    int bonusCount;
    int bonusScore;

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

        // set the asset palette up only one time
        if (assetPalette == null)
        {
            assetPalette = GetComponent<AssetPalette>();
            enemyPrefabCount = Enum.GetNames(typeof(AssetPalette.EnemyList)).Length;
        }
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
                TeleportPlayer(true);
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
            //ShowMessage();
            UpdateScore();
            //SpawnEnemies();
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
        firstMessage = true;
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

    // keep track of how many bonus balls are collected and add up the points
    // at the end of a boss battle the bonus score would be added to the overall player score
    public void AddBonusPoints(int points)
    {
        bonusCount++;
        bonusScore += points;
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

    private void TeleportPlayer(bool teleport)
    {
        // teleport player - happens after READY screen
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<PlayerController>().Teleport(teleport);
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

    private void ShowMessage()
    {
        // words of encouragement :)
        string[] messages = {
            "GO GO GO",
            "YOU'RE TOO GOOD",
            "GREAT JOB PLAYER",
            "RACK UP THAT SCORE",
            "THIS MIGHT BE TOUGH"
        };

        // pick a random message when there are no enemies
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            messageIndex = firstMessage ? 0 : UnityEngine.Random.Range(0, messages.Length);
            messageText = messages[messageIndex];
            messageTimer = messageDelay;
            showMessage = true;
        }

        if (showMessage)
        {
            // show the message on screen
            screenMessageText.alignment = TextAlignmentOptions.Center;
            screenMessageText.alignment = TextAlignmentOptions.Top;
            screenMessageText.fontStyle = FontStyles.UpperCase;
            screenMessageText.fontSize = 24;
            screenMessageText.text = messageText;

            // show the message for the timer duration
            messageTimer -= Time.deltaTime;
            if (messageTimer < 0)
            {
                screenMessageText.text = "";
                firstMessage = false;
                showMessage = false;
            }
        }
    }

    private void UpdateScore()
    {
        // update the player score each wave by adding on any bonus points
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            playerScore += bonusScore;
            bonusCount = 0;
            bonusScore = 0;
        }
    }

    private void SpawnEnemies()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            // 5 enemies at most on screen at one time
            int randomEnemyCount = UnityEngine.Random.Range(1, 6);
            GameObject[] randomEnemies = new GameObject[randomEnemyCount];
            for (int i = 0; i < randomEnemyCount; i++)
            {
                // pick a random enemy and position it
                int enemyIndex = UnityEngine.Random.Range(0, enemyPrefabCount);
                randomEnemies[i] = Instantiate(assetPalette.enemyPrefabs[enemyIndex]);
                randomEnemies[i].name = assetPalette.enemyPrefabs[enemyIndex].name;
                randomEnemies[i].transform.position = new Vector3(worldViewCoords.Right + UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(-1f, 1f), 0);
                // for enemies with colors, randomize them
                switch (randomEnemies[i].name)
                {
                    case "BigEye":
                        // and dropping big eye into the scene inside camera view
                        randomEnemies[i].transform.position = new Vector3(worldViewCoords.Right - UnityEngine.Random.Range(0.75f, 1.5f), 2f, 0);
                        randomEnemies[i].GetComponent<BigEyeController>().SetColor((BigEyeController.BigEyeColors)UnityEngine.Random.Range(0, Enum.GetNames(typeof(BigEyeController.BigEyeColors)).Length));
                        break;
                    case "KillerBomb":
                        randomEnemies[i].GetComponent<KillerBombController>().SetColor((KillerBombController.KillerBombColors)UnityEngine.Random.Range(0, Enum.GetNames(typeof(KillerBombController.KillerBombColors)).Length));
                        break;
                }
            }
        }
    }

    private void RepositionEnemies()
    {
        // check all enemies in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            // outside the bottom view (grounded enemies)
            if (enemy.transform.position.y < worldViewCoords.Bottom)
            {
                switch (enemy.name)
                {
                    case "BigEye":
                        // because he uses velocity while in the air for jumping it's possible 
                        // he can go outside the right boundary, reposition more to the left
                        enemy.transform.position = new Vector3(worldViewCoords.Right - 2.5f, 2f, 0);
                        break;
                }
            }
            // outside the left view (flying enemies)
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

    // display a message that a checkpoint has been reached
    // trigger this through a camera transition post delay event
    public void CheckpointReached()
    {
        StartCoroutine(CoCheckpointReached());
    }

    private IEnumerator CoCheckpointReached()
    {
        // show checkpoint message on screen
        screenMessageText.alignment = TextAlignmentOptions.Center;
        screenMessageText.alignment = TextAlignmentOptions.Top;
        screenMessageText.fontStyle = FontStyles.UpperCase;
        screenMessageText.fontSize = 24;
        screenMessageText.text = "CHECKPOINT REACHED";
        // remove message after 5 seconds
        yield return new WaitForSeconds(5f);
        screenMessageText.text = "";
    }

    // internal to the game manager to pick a random bonus item
    // use the function below GetBonusItem in enemies and objects
    // that need to generate a bonus item drop upon their explosion
    private ItemScript.ItemTypes PickRandomBonusItem()
    {
        /*
		 * Item info from this source
		 * http://tasvideos.org/GameResources/NES/Rockman.html
		 * 
		 * Type					MM1 (random seeds)
		 * extra life			1/128 (63)
		 * big weapon refill	2/128 (5F and 60)
		 * big energy refill	2/128 (61 and 62)
		 * small weapon refill	15/128 (41-4F)
		 * small energy refill	15/128 (50-5E)
		 * bonus pearl			69/128 (0C-40 and 70-7F)
		 * nothing				24/128 (00-0B and 64-6F)
		 * 
		 * Array indexes based on the random seeds
		 * Index 0		nothing
		 * Index 1		bonus pearl
		 * Index 2		small weapon refill
		 * Index 3		small energy refill
		 * Index 4		big weapon refill
		 * Index 5		big energy refill
		 * Index 6		extra life
		 * Index 7		nothing
		 * Index 8		bonus pearl
		 * 
		 * 
		 * This function is based off the Unity documentation
		 * 
		 * Choosing Items with Different Probabilities
		 * https://docs.unity3d.com/2019.3/Documentation/Manual/RandomNumbers.html
		 */
        // probabilities (random seeds) in decimal values
        float[] probabilities = {
            12, 53, 15, 15, 2, 2, 1, 12, 16
        };

        // sum of all probabilities
        float total = 0;

        // item types indexed to match probabilities
        ItemScript.ItemTypes[] items = {
            ItemScript.ItemTypes.Nothing,
            ItemScript.ItemTypes.BonusBall,
            ItemScript.ItemTypes.WeaponEnergySmall,
            ItemScript.ItemTypes.LifeEnergySmall,
            ItemScript.ItemTypes.WeaponEnergyBig,
            ItemScript.ItemTypes.LifeEnergyBig,
            ItemScript.ItemTypes.ExtraLife,
            ItemScript.ItemTypes.Nothing,
            ItemScript.ItemTypes.BonusBall
        };

        // add up all the probability values to get the total
        foreach (float prob in probabilities)
        {
            total += prob;
        }

        // pick a point in the total
        float randomPoint = UnityEngine.Random.value * total;

        // use the chosen index value to get the item type to return
        for (int i = 0; i < probabilities.Length; i++)
        {
            if (randomPoint < probabilities[i])
            {
                //return i;
                return items[i];
            }
            else
            {
                randomPoint -= probabilities[i];
            }
        }
        //return probabilities.Length - 1;
        return items[probabilities.Length - 1];
    }

    // enemies and objects that offer bonus items should call this function
    public GameObject GetBonusItem(ItemScript.ItemTypes itemType)
    {
        GameObject bonusItem = null;

        // pick a random bonus item
        if (itemType == ItemScript.ItemTypes.Random)
        {
            itemType = PickRandomBonusItem();
        }

        // get bonus item prefab from type
        switch (itemType)
        {
            case ItemScript.ItemTypes.Nothing:
                bonusItem = null;
                break;
            case ItemScript.ItemTypes.BonusBall:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.BonusBall];
                break;
            case ItemScript.ItemTypes.ExtraLife:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.ExtraLife];
                break;
            case ItemScript.ItemTypes.LifeEnergyBig:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.LifeEnergyBig];
                break;
            case ItemScript.ItemTypes.LifeEnergySmall:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.LifeEnergySmall];
                break;
            case ItemScript.ItemTypes.WeaponEnergyBig:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.WeaponEnergyBig];
                break;
            case ItemScript.ItemTypes.WeaponEnergySmall:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.WeaponEnergySmall];
                break;
            case ItemScript.ItemTypes.MagnetBeam:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.MagnetBeam];
                break;
            case ItemScript.ItemTypes.WeaponPart:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.WeaponPart];
                break;
            case ItemScript.ItemTypes.Yashichi:
                bonusItem = assetPalette.itemPrefabs[(int)AssetPalette.ItemList.Yashichi];
                break;
        }

        // return bonus item prefab
        return bonusItem;
    }
}