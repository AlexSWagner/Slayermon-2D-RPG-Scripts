using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages arrow shooting functionality with a firePoint system
/// </summary>
public class ArrowManager : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowSpeed = 10f;
    public float arrowLifetime = 5f;
    
    [Header("References")]
    [SerializeField] private PlayerScript playerScript;
    
    private Vector2 shootDirection = Vector2.right; // Default direction
    
    void Start()
    {
        // If firePoint not assigned, create one
        if (firePoint == null)
        {
            GameObject newFirePoint = new GameObject("FirePoint");
            newFirePoint.transform.parent = transform;
            newFirePoint.transform.localPosition = new Vector3(0.5f, 0, 0); // Default to right
            firePoint = newFirePoint.transform;
            
            // Add a visual indicator in debug mode
            #if UNITY_EDITOR
            GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.transform.parent = firePoint;
            debugSphere.transform.localPosition = Vector3.zero;
            debugSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            debugSphere.GetComponent<Renderer>().material.color = Color.red;
            Destroy(debugSphere.GetComponent<Collider>());
            debugSphere.name = "DebugPoint";
            #endif
        }
        
        // Get player script reference if not set
        if (playerScript == null)
        {
            playerScript = GetComponentInParent<PlayerScript>();
            if (playerScript == null)
            {
                Debug.LogError("ArrowManager: No PlayerScript found!");
            }
        }
        
        // Find or assign arrow prefab
        if (arrowPrefab == null)
        {
            Debug.LogWarning("ArrowManager: No arrow prefab assigned!");
        }
        
        // Log setup
        Debug.Log("ArrowManager initialized. FirePoint: " + (firePoint != null) + ", Arrow Prefab: " + (arrowPrefab != null));
    }
    
    /// <summary>
    /// Updates the firePoint position based on player direction
    /// </summary>
    public void UpdateFirePointPosition(int playerDirection)
    {
        if (firePoint == null)
        {
            Debug.LogError("ArrowManager: FirePoint is null when trying to update position!");
            return;
        }
        
        switch (playerDirection)
        {
            case 0: // Down
                shootDirection = Vector2.down;
                firePoint.localPosition = new Vector3(0, -0.5f, 0);
                break;
            case 1: // Right
                shootDirection = Vector2.right;
                firePoint.localPosition = new Vector3(0.5f, 0, 0);
                break;
            case 2: // Left
                shootDirection = Vector2.left;
                firePoint.localPosition = new Vector3(-0.5f, 0, 0);
                break;
            case 3: // Up
                shootDirection = Vector2.up;
                firePoint.localPosition = new Vector3(0, 0.5f, 0);
                break;
        }
        
        Debug.Log("Updated firePoint position for direction: " + playerDirection + 
                  " - Position: " + firePoint.localPosition + 
                  " - Shoot Direction: " + shootDirection);
    }
    
    /// <summary>
    /// Fires an arrow in the current direction
    /// </summary>
    public bool FireArrow()
    {
        Debug.Log("FireArrow called - Attempting to fire arrow");
        
        // Make sure we have what we need
        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowManager: Cannot fire - no arrow prefab assigned");
            return false;
        }
        
        if (playerScript == null)
        {
            Debug.LogError("ArrowManager: Cannot fire - no PlayerScript reference");
            return false;
        }
        
        if (firePoint == null)
        {
            Debug.LogError("ArrowManager: Cannot fire - no firePoint assigned");
            return false;
        }
        
        // Check if player has arrows
        if (playerScript.arrowCount <= 0)
        {
            Debug.Log("No arrows remaining!");
            return false;
        }
        
        // Decrease arrow count
        playerScript.arrowCount--;
        PlayerPrefs.SetInt("ArrowCount", playerScript.arrowCount);
        playerScript.UpdateUI(); // Update UI immediately
        
        // Calculate rotation based on direction
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        Quaternion arrowRotation = Quaternion.Euler(0, 0, angle);
        
        // Log detailed spawn info
        Debug.Log("Arrow spawn at: " + firePoint.position + " with rotation: " + angle + "° - Direction: " + shootDirection);
        
        // Instantiate arrow at firePoint
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, arrowRotation);
        arrow.name = "Arrow_" + Random.Range(1000, 9999); // Give a unique name for debugging
        
        // Make sure it's active and visible
        if (!arrow.activeInHierarchy)
        {
            Debug.LogWarning("Arrow instantiated but not active in hierarchy!");
            arrow.SetActive(true);
        }
        
        // Add components if needed
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb == null)
        {
            arrowRb = arrow.AddComponent<Rigidbody2D>();
            arrowRb.gravityScale = 0;
        }
        
        // Set arrow velocity
        arrowRb.velocity = shootDirection * arrowSpeed;
        Debug.Log("Arrow velocity set to: " + (shootDirection * arrowSpeed));
        
        // Ensure it has a WeaponScript for damage
        WeaponScript weaponScript = arrow.GetComponent<WeaponScript>();
        if (weaponScript == null)
        {
            weaponScript = arrow.AddComponent<WeaponScript>();
            weaponScript.isSword = false;
            weaponScript.weaponDamage = 1;
        }
        
        // Add collider if needed
        BoxCollider2D collider = arrow.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = arrow.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            
            // Set size based on direction
            if (shootDirection.x != 0) // Horizontal
            {
                collider.size = new Vector2(0.5f, 0.1f);
            }
            else // Vertical
            {
                collider.size = new Vector2(0.1f, 0.5f);
            }
        }
        
        // Make sure it has an ArrowScript
        ArrowScript arrowScript = arrow.GetComponent<ArrowScript>();
        if (arrowScript == null)
        {
            arrow.AddComponent<ArrowScript>();
        }
        
        // Destroy arrow after lifetime
        Destroy(arrow, arrowLifetime);
        
        Debug.Log("Arrow successfully fired: " + arrow.name);
        return true;
    }
} 