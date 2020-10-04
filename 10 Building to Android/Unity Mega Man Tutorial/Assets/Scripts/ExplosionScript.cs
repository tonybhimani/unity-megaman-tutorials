using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionScript : MonoBehaviour
{
    int damage = 0;

    public void SetDamageValue(int damage)
    {
        this.damage = damage;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // we only apply damage when this value is set
        // not all explosions affect the player i.e. smaller ones
        // KillerBomb uses a large explosion and does damage the player
        // that is when this script and collision would apply
        if (this.damage > 0)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                player.HitSide(transform.position.x > player.transform.position.x);
                player.TakeDamage(this.damage);
            }
        }
    }

}
