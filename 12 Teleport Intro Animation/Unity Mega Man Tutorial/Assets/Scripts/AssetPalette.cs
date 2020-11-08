using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetPalette : MonoBehaviour
{
    public GameObject[] enemyPrefabs = new GameObject[4];

    public enum EnemyList { BigEye, KillerBomb, Mambu, Pepe };
    public EnemyList enemyList;
}
