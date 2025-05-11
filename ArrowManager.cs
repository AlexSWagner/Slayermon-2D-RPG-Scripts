using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages arrow instantiation and firing logic, calculating position relative to the player.
/// </summary>
public class ArrowManager : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 10f;
    public float arrowLifetime = 5f;
    
    [Header("References")]
    [SerializeField] private PlayerScript playerScript; // Reference to the player for inventory checks.
    
    void Start()
    {
        if (playerScript == null)
        {
            playerScript = GetComponentInParent<PlayerScript>();
            if (playerScript == null)
            {
                // This component requires a PlayerScript in its parent hierarchy to function.
                Debug.LogError("ArrowManager requires a PlayerScript in its parent hierarchy!");
            }
        }
        
        if (arrowPrefab == null)
        {
            Debug.LogWarning("ArrowManager: No arrow prefab assigned in the Inspector!");
        }
    }
    
    /// <summary>
    /// Creates and launches an arrow based on the player's position and direction.
    /// Decrements the player's arrow count.
    /// </summary>
    /// <param name="playerPosition">The player's current world position.</param>
    /// <param name="playerDirection">The direction the player is facing (0=Down, 1=Right, 2=Left, 3=Up).</param>
    /// <returns>True if the arrow was fired successfully, false otherwise (e.g., no arrows left).</returns>
    public bool FireArrow(Vector3 playerPosition, int playerDirection)
    {
        if (arrowPrefab == null)
        {
            // Error logged in Start, but double-check here.
            return false;
        }
        
        if (playerScript == null)
        {
            // Error logged in Start, but double-check here.
            return false;
        }
        
        if (playerScript.arrowCount <= 0)
        {
            return false; // Player is out of arrows
        }
        
        // Consume an arrow
        playerScript.arrowCount--;
        PlayerPrefs.SetInt("ArrowCount", playerScript.arrowCount);
        playerScript.UpdateUI();
        
        Vector2 shootDirection = Vector2.zero;
        Vector3 spawnOffset = Vector3.zero;
        Quaternion arrowRotation = Quaternion.identity;
        float angle = 0f;
        
        // Define spawn offsets relative to the player's center.
        // These values determine where the arrow appears relative to the player sprite
        // and may need fine-tuning based on the sprite's dimensions and pivot point.
        float horizontalOffsetX = 0.4f;
        float horizontalOffsetY = -0.1f;
        float verticalOffsetY = 0.4f;
        
        switch (playerDirection)
        {
            case 0: // Down
                shootDirection = Vector2.down;
                spawnOffset = new Vector3(0, -verticalOffsetY, 0);
                angle = 270f;
                break;
            case 1: // Right
                shootDirection = Vector2.right;
                spawnOffset = new Vector3(horizontalOffsetX, horizontalOffsetY, 0);
                angle = 0f;
                break;
            case 2: // Left
                shootDirection = Vector2.left;
                spawnOffset = new Vector3(-horizontalOffsetX, horizontalOffsetY, 0);
                angle = 180f;
                break;
            case 3: // Up
                shootDirection = Vector2.up;
                spawnOffset = new Vector3(0, verticalOffsetY, 0);
                angle = 90f;
                break;
        }
        arrowRotation = Quaternion.Euler(0, 0, angle);
        
        Vector3 spawnPosition = playerPosition + spawnOffset;
        
        GameObject arrow = Instantiate(arrowPrefab, spawnPosition, arrowRotation);
        
        // Ensure the arrow is active if the prefab might be inactive by default.
        if (!arrow.activeInHierarchy)
        {
            arrow.SetActive(true);
        }
        
        // --- Component Setup on Instantiated Arrow --- 
        // Note: Prefer adding these components directly to the arrow Prefab in the editor.
        
        // Rigidbody2D for movement.
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb == null)
        {
            arrowRb = arrow.AddComponent<Rigidbody2D>();
            arrowRb.gravityScale = 0; // Assuming top-down, no gravity needed.
        }
        arrowRb.velocity = shootDirection * arrowSpeed;
        
        // WeaponScript for damage dealing.
        WeaponScript weaponScript = arrow.GetComponent<WeaponScript>();
        if (weaponScript == null)
        {
            weaponScript = arrow.AddComponent<WeaponScript>();
            weaponScript.isSword = false;
            weaponScript.weaponDamage = 1; // Default damage if added dynamically.
        }
        // Sync damage with ArrowScript if present (ArrowScript might define specific damage).
        ArrowScript arrowScriptComponent = arrow.GetComponent<ArrowScript>();
        if (arrowScriptComponent != null)
        {
            weaponScript.weaponDamage = arrowScriptComponent.arrowDamage;
        }
        
        // BoxCollider2D for collision detection.
        BoxCollider2D collider = arrow.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = arrow.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; // Ensure it's a trigger for OnTrigger events.
            // Set a reasonable default size based on orientation.
            if (shootDirection.x != 0) { collider.size = new Vector2(0.5f, 0.1f); }
            else { collider.size = new Vector2(0.1f, 0.5f); }
        }
        
        // ArrowScript for self-destruction and potentially other arrow-specific logic.
        if (arrowScriptComponent == null) // Check the variable we already fetched.
        {
            arrow.AddComponent<ArrowScript>();
        }
        
        Destroy(arrow, arrowLifetime); // Arrow self-destructs after a set time.
        
        return true;
    }
} 