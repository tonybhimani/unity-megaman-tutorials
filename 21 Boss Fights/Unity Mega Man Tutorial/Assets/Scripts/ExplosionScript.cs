using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionScript : MonoBehaviour
{
    int damage = 0;

    string[] collideWithTags = { "Player" };

    public void SetDamageValue(int damage)
    {
        this.damage = damage;
    }

    public void SetCollideWithTags(params string[] tags)
    {
        this.collideWithTags = tags;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // we only apply damage when this value is set
        // not all explosions affect the player i.e. smaller ones
        // KillerBomb uses a large explosion and does damage the player
        // that is when this script and collision would apply
        if (this.damage > 0)
        {
            foreach (string tag in collideWithTags)
            {
                // check for collision with this tag
                if (other.gameObject.CompareTag(tag))
                {
                    switch (tag)
                    {
                        case "Enemy":
                            // enemy controller will apply the damage the bomb can cause
                            EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
                            if (enemy != null)
                            {
                                enemy.TakeDamage(this.damage);
                            }
                            break;
                        case "Player":
                            // player controller will apply the damage the bomb can cause
                            PlayerController player = other.gameObject.GetComponent<PlayerController>();
                            if (player != null)
                            {
                                player.HitSide(transform.position.x > player.transform.position.x);
                                player.TakeDamage(this.damage);
                            }
                            break;
                    }
                }
            }
        }
    }

}
