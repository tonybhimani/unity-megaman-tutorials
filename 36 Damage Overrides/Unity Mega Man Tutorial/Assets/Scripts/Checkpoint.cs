using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// class to save our checkpoint settings
public class CheckpointSettings
{
    public string name;
    public bool checkpointReached;
}

public class Checkpoint : MonoBehaviour
{
    // Singleton instance for a Manager
    public static Checkpoint Instance = null;

    // one script to handle checkpoints and a manager of them
    public enum CheckpointType { Trigger, Zone, Manager };
    public CheckpointType checkpointType = CheckpointType.Trigger;

    // so we can set the player and camera settings
    GameObject player;
    CameraFollow cameraFollow;

    // use zone coordinates if preferred over a trigger
    [System.Serializable]
    public struct ZoneCoordinates
    {
        public float Top;
        public float Right;
        public float Bottom;
        public float Left;
    }

    // a list to save all our checkpoint settings
    List<CheckpointSettings> checkpointSettingsList =
        new List<CheckpointSettings>();

    // a list to save all our camera transition settings
    List<CameraTransitionSettings> cameraTransitionSettingsList =
        new List<CameraTransitionSettings>();

    // settings for the checkpoints and manager
    [Header("Checkpoint Settings")]
    public bool checkpointReached;
    public string lastCheckpointName;
    public ZoneCoordinates zoneCoordinates;
    public Vector3 playerPosition;
    public Vector3 cameraPosition;
    public Vector2 cameraMinPosition;
    public Vector2 cameraMaxPosition;
    public bool callEventWhenReached;
    public UnityEvent checkpointEvent;

    private void Awake()
    {
        // The Checkpoint(Manger) will be a singleton
        if (checkpointType == CheckpointType.Manager)
        {
            // If there is not already an instance of Checkpoint(Manager), set it to this
            if (Instance == null)
            {
                Instance = this;
            }
            // If an instance already exists, destroy whatever this object is to enforce the singleton
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
            // Set Checkpoint(Manager) to DontDestroyOnLoad so that it won't be destroyed when reloading our scene
            DontDestroyOnLoad(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // only the checkpoints need the objects
        if (checkpointType != CheckpointType.Manager)
        {
            // get needed game objects
            GetGameObjects();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // checkpoint zones will look for the player within coordinates
        //   and we only want it to trigger once
        if (checkpointType == CheckpointType.Zone && !checkpointReached)
        {
            if (player != null)
            {
                // check if player is within the zone's coordinates
                if (player.transform.position.x > zoneCoordinates.Left &&
                    player.transform.position.x < zoneCoordinates.Right &&
                    player.transform.position.y > zoneCoordinates.Bottom &&
                    player.transform.position.y < zoneCoordinates.Top)
                {
                    // checkpoint reached
                    CheckpointReached();
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // handle this only if the checkpoint type is trigger
        //   and we only want it to trigger once
        if (checkpointType == CheckpointType.Trigger && !checkpointReached)
        {
            // the player can trigger the checkpoint being reached
            if (other.gameObject.CompareTag("Player"))
            {
                // checkpoint reached
                CheckpointReached();
            }
        }
    }

    private void GetGameObjects()
    {
        // get the player and camera follow objects
        player = GameObject.FindGameObjectWithTag("Player");
        cameraFollow = GameObject.FindObjectOfType<CameraFollow>();
    }

    public void CheckpointReached()
    {
        // actual checkpoints that haven't been triggered yet
        if (checkpointType != CheckpointType.Manager && !checkpointReached)
        {
            // checkpoint reached
            checkpointReached = true;
            // save the settings data
            Instance.SaveSettings();
            // send the settings to the checkpoint manger
            Instance.checkpointReached = true;
            Instance.lastCheckpointName = this.name;
            Instance.playerPosition = this.playerPosition;
            Instance.cameraPosition = this.cameraPosition;
            Instance.cameraMinPosition = this.cameraMinPosition;
            Instance.cameraMaxPosition = this.cameraMaxPosition;
            // invoke the checkpoint event if allowed
            if (this.callEventWhenReached) this.checkpointEvent.Invoke();
        }
    }

    public void UpdateScene()
    {
        // the checkpoint manager and any checkpoint has been reached
        if (checkpointType == CheckpointType.Manager && this.checkpointReached)
        {
            // get the player and camera follow
            GetGameObjects();
            // restore the saved settings
            RestoreSettings();
            // update the player's position (respawn position)
            if (player != null)
            {
                player.transform.position = this.playerPosition;
            }
            // update the camera position and follow script's bounds
            if (cameraFollow != null)
            {
                cameraFollow.boundsMin = this.cameraMinPosition;
                cameraFollow.boundsMax = this.cameraMaxPosition;
                cameraFollow.gameObject.transform.position = this.cameraPosition;
            }
            // call the last checkpoint's event (if any)
            GameObject lastCheckpoint = GameObject.Find(lastCheckpointName);
            if (lastCheckpoint != null)
            {
                lastCheckpoint.GetComponent<Checkpoint>().checkpointEvent.Invoke();
            }
        }
    }

    public void ResetManager()
    {
        // reset all the saved settings in the manager
        if (checkpointType == CheckpointType.Manager)
        {
            this.player = null;
            this.cameraFollow = null;
            this.checkpointReached = false;
            this.lastCheckpointName = "";
            this.playerPosition = Vector3.zero;
            this.cameraPosition = Vector3.zero;
            this.cameraMinPosition = Vector2.zero;
            this.cameraMaxPosition = Vector2.zero;
            this.checkpointSettingsList.Clear();
            this.cameraTransitionSettingsList.Clear();
        }
    }

    public void SaveSettings()
    {
        // only the manager will save settings
        if (checkpointType == CheckpointType.Manager)
        {
            // just checkpoints and camera transitions
            //   however this can be expanded upon
            SaveCheckpoints();
            SaveCameraTransitions();
        }
    }

    private void SaveCheckpoints()
    {
        // start fresh
        checkpointSettingsList.Clear();
        // save all the checkpoint settings
        Checkpoint[] checkpoints = GameObject.FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in checkpoints)
        {
            // saving only actual checkpoints (not the manager)
            if (checkpoint.checkpointType != CheckpointType.Manager)
            {
                CheckpointSettings checkpointSettings = new CheckpointSettings();
                checkpointSettings.name = checkpoint.name;
                checkpointSettings.checkpointReached = checkpoint.checkpointReached;
                checkpointSettingsList.Add(checkpointSettings);
            }
        }
    }

    private void SaveCameraTransitions()
    {
        // start fresh
        cameraTransitionSettingsList.Clear();
        // save all camera transition settings
        CameraTransition[] cameraTransitions = GameObject.FindObjectsOfType<CameraTransition>();
        foreach (CameraTransition cameraTransition in cameraTransitions)
        {
            CameraTransitionSettings cameraTransitionSettings = new CameraTransitionSettings();
            cameraTransition.GetSettings(ref cameraTransitionSettings);
            cameraTransitionSettingsList.Add(cameraTransitionSettings);
        }
    }

    public void RestoreSettings()
    {
        // only the manager will restore settings
        if (checkpointType == CheckpointType.Manager)
        {
            RestoreCheckpoints();
            RestoreCameraTransitions();
        }
    }

    private void RestoreCheckpoints()
    {
        // restore checkpoints
        Checkpoint[] checkpoints = GameObject.FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in checkpoints)
        {
            CheckpointSettings savedCheckpointSettings =
                checkpointSettingsList.Find(item => item.name == checkpoint.name);
            if (savedCheckpointSettings != null)
            {
                checkpoint.checkpointReached = savedCheckpointSettings.checkpointReached;
            }
        }
    }

    private void RestoreCameraTransitions()
    {
        // restore camera transitions
        CameraTransition[] cameraTransitions = GameObject.FindObjectsOfType<CameraTransition>();
        foreach (CameraTransition cameraTransition in cameraTransitions)
        {
            CameraTransitionSettings savedCameraTransitionSettings =
                cameraTransitionSettingsList.Find(item => item.name == cameraTransition.name);
            if (savedCameraTransitionSettings != null)
            {
                cameraTransition.PutSettings(savedCameraTransitionSettings);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // draw lines in unity scene view to outline the zone visually
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(zoneCoordinates.Left, zoneCoordinates.Top),
            new Vector3(zoneCoordinates.Left, zoneCoordinates.Bottom));
        Gizmos.DrawLine(new Vector3(zoneCoordinates.Left, zoneCoordinates.Bottom),
            new Vector3(zoneCoordinates.Right, zoneCoordinates.Bottom));
        Gizmos.DrawLine(new Vector3(zoneCoordinates.Right, zoneCoordinates.Bottom),
            new Vector3(zoneCoordinates.Right, zoneCoordinates.Top));
        Gizmos.DrawLine(new Vector3(zoneCoordinates.Right, zoneCoordinates.Top),
            new Vector3(zoneCoordinates.Left, zoneCoordinates.Top));
    }
}