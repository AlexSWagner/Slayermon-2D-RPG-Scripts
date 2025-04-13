using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls weapon collisions and damage dealing to enemies
/// </summary>
public class WeaponScript : MonoBehaviour
{
    [Header("Weapon Settings")]
    public int weaponDamage = 1; // How much damage this weapon deals
    public bool isSword = true; // Is this a sword (true) or arrow (false)
    
    private bool canDealDamage = true; // Flag to prevent hitting the same enemy multiple times per attack
    private List<GameObject> hitEnemies = new List<GameObject>(); // Tracks which enemies have been hit during this attack
    
    /// <summary>
    /// Resets the hit enemies list when a new attack starts
    /// </summary>
    public void ResetHitEnemies()
    {
        hitEnemies.Clear();
        canDealDamage = true;
    }
    
    /// <summary>
    /// Handle collisions with enemies
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if we hit an enemy
        if (collision.CompareTag("Enemy") && canDealDamage)
        {
            // Make sure we haven't already hit this enemy in this attack
            if (!hitEnemies.Contains(collision.gameObject))
            {
                // Get the enemy script
                EnemyScript enemy = collision.GetComponent<EnemyScript>();
                if (enemy != null)
                {
                    // Deal damage to the enemy
                    enemy.TakeDamage(weaponDamage);
                    
                    // Add to list of hit enemies to prevent hitting multiple times in same attack
                    hitEnemies.Add(collision.gameObject);
                    
                    // If this is an arrow, destroy it after hitting an enemy
                    if (!isSword)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
} 