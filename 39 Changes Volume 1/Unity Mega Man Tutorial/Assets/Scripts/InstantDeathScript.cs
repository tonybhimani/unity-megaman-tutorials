using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantDeathScript : MonoBehaviour
{
    // flag to show player death explosion
    public bool showExplosion;

    // flag to destroy any enemies that collide
    public bool destroyEnemies;

    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.gameObject.tag)
        {
            case "Player":
                // call player's defeat and pass the explosion flag
                other.gameObject.GetComponent<PlayerController>().Defeat(showExplosion);
                break;
            case "Enemy":
                // destroy enemy game objects that collide with the trigger
                if (destroyEnemies)
                {
                    Destroy(other.gameObject);
                }
                break;
        }
    }
}