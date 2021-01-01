using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetPalette : MonoBehaviour
{
    // enemy prefabs
    public GameObject[] enemyPrefabs = new GameObject[4];
    public enum EnemyList
    {
        BigEye,
        KillerBomb,
        Mambu,
        Pepe
    };
    public EnemyList enemyList;

    // item prefabs
    public GameObject[] itemPrefabs = new GameObject[9];
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
}
