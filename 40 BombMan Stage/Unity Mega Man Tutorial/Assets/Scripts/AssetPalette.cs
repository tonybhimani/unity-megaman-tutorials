using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetPalette : MonoBehaviour
{
    // enemy prefabs (resolution scales)
    //   prefabs for 16:9, 4:3, ...
    public GameObject[] enemyPrefabs_16x9 = new GameObject[13];
    public GameObject[] enemyPrefabs_4x3 = new GameObject[13];
    public enum EnemyList
    {
        AdheringSuzy,
        BigEye,
        Blaster,
        Bombomb,
        Gabyoall,
        Kamadoma,
        KillerBomb,
        Mambu,
        Metall,
        Pepe,
        ScrewDriver,
        SniperJoe,
        BombMan
    };
    public EnemyList enemyList;

    // item prefabs (resolution scales)
    //   prefabs for 16:9, 4:3, ...
    public GameObject[] itemPrefabs_16x9 = new GameObject[9];
    public GameObject[] itemPrefabs_4x3 = new GameObject[9];
    public enum ItemList
    {
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
    public ItemList itemList;

    // game pause/menu audio
    public AudioClip pauseMenuClip;

    // point tally audio
    [SerializeField] AudioClip pointTallyClip;
    public AudioClip pointTallyLoopClip;
    public AudioClip pointTallyEndClip;

    // Start is called before the first frame update
    void Start()
    {
        // make the point tally sub clips (loop and end)
        CreatePointTallySubClips();
    }

    void CreatePointTallySubClips()
    {
        if (pointTallyClip != null)
        {
            // create new audio clips with properties of the original clip we're sampling from
            pointTallyLoopClip = AudioClip.Create("PointTallyLoop", 2465, pointTallyClip.channels, pointTallyClip.frequency, false);
            pointTallyEndClip = AudioClip.Create("PointTallyEnd", 5938, pointTallyClip.channels, pointTallyClip.frequency, false);

            // get and set the audio data for the loop clip from the original clip
            float[] loopSamples = new float[2465 * pointTallyClip.channels];
            pointTallyClip.GetData(loopSamples, 2158);
            pointTallyLoopClip.SetData(loopSamples, 0);

            // get and set the audio data for the end clip from the original clip
            float[] endSamples = new float[5938 * pointTallyClip.channels];
            pointTallyClip.GetData(endSamples, 2158);
            pointTallyEndClip.SetData(endSamples, 0);
        }
    }
}