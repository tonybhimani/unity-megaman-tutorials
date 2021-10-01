using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeEffect3 : MonoBehaviour
{
    public float explosionSpeed = 0.75f;
    GameObject[] explosions = new GameObject[12];
    Vector3[] explosionVectors = {
        new Vector3(-1f, 0, 0),             // West - Full Speed
        new Vector3(1f, 0, 0),              // East - Full Speed
        new Vector3(0, -1f, 0),             // South - Full Speed
        new Vector3(0, 1f, 0),              // North - Full Speed
        new Vector3(-0.75f, -0.75f, 0),     // Southwest - Full Speed
        new Vector3(-0.75f, 0.75f, 0),      // Northwest - Full Speed
        new Vector3(0.75f, -0.75f, 0),      // Southeast - Full Speed
        new Vector3(0.75f, 0.75f, 0),       // Northeast - Full Speed
        new Vector3(-0.5f, 0, 0),           // West - Half Speed
        new Vector3(0.5f, 0, 0),            // East - Half Speed
        new Vector3(0, -0.5f, 0),           // South - Half Speed
        new Vector3(0, 0.5f, 0)             // North - Half Speed
    };

    // Start is called before the first frame update
    void Start()
    {
        // populate explosions array with child gameobjects
        for (int i = 0; i < explosions.Length; i++)
        {
            string explosionName = "Explosion" + (i + 1).ToString();
            explosions[i] = transform.Find(explosionName).gameObject;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // move each explosion by speed and vector
        for (int i = 0; i < explosions.Length; i++)
        {
            Vector3 position = explosions[i].transform.position;
            position.x += explosionVectors[i].x * explosionSpeed * Time.deltaTime;
            position.y += explosionVectors[i].y * explosionSpeed * Time.deltaTime;
            position.z += explosionVectors[i].z * explosionSpeed * Time.deltaTime;
            explosions[i].transform.position = position;
        }
    }
}