using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Teleports the player to a target location when they enter the trigger area
/// </summary>
public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform destination; // The target location to teleport to
    public float teleportDelay = 0.1f; // Small delay before teleporting
    
    [Header("Transition Effects")]
    public bool useFadeEffect = true;
    public float fadeDuration = 0.5f;
    
    [Header("Advanced Settings")]
    public bool requireButtonPress = false; // Whether to require button press or auto-teleport on enter
    public KeyCode activationKey = KeyCode.E; // Key to press if requireButtonPress is true
    public string promptMessage = "Press E to enter"; // Message to show if requireButtonPress is true
    
    // References
    private DialogueManager dialogueManager;
    private bool playerInTrigger = false;
    private Transform playerTransform;
    private bool isTeleporting = false;
    
    // If true, will face player in specific direction after teleport
    [Header("Post-Teleport Direction")]
    public bool setPlayerDirection = true;
    public enum FacingDirection { Down, Right, Left, Up }
    public FacingDirection playerFacingAfterTeleport = FacingDirection.Down;
    
    [Header("Anti-Loop Protection")]
    public float teleportCooldown = 1.0f; // Time before player can teleport again
    private static float lastTeleportTime = 0f; // Static to be shared across all teleporters
    
    private void Start()
    {
        // Try to find dialogue manager for prompting
        if (requireButtonPress)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
            if (dialogueManager == null && requireButtonPress)
            {
                Debug.LogWarning("No DialogueManager found. Prompt won't be displayed.");
            }
        }
        
        // Ensure destination is set
        if (destination == null)
        {
            Debug.LogError("Teleport destination not set for " + gameObject.name);
        }
    }
    
    private void Update()
    {
        // If we require a button press and player is in trigger, check for input
        if (requireButtonPress && playerInTrigger && !isTeleporting)
        {
            if (Input.GetKeyDown(activationKey))
            {
                StartCoroutine(TeleportPlayer());
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            playerInTrigger = true;
            
            // Show prompt if using button press
            if (requireButtonPress && dialogueManager != null)
            {
                dialogueManager.ShowInteractionPrompt(transform.position + Vector3.up * 0.5f);
            }
            else if (!isTeleporting && !requireButtonPress)
            {
                // Auto-teleport
                StartCoroutine(TeleportPlayer());
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            
            // Hide prompt if using button press
            if (requireButtonPress && dialogueManager != null)
            {
                dialogueManager.HideInteractionPrompt();
            }
        }
    }
    
    private IEnumerator TeleportPlayer()
    {
        // Check if we're in cooldown period to prevent teleport loops
        if (Time.time < lastTeleportTime + teleportCooldown || 
            isTeleporting || destination == null || playerTransform == null) 
            yield break;
        
        // Set the last teleport time
        lastTeleportTime = Time.time;
        
        isTeleporting = true;
        
        // Lock player movement during teleport
        PlayerMovement playerMovement = playerTransform.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.LockMovement();
        }
        
        // Hide prompt if shown
        if (dialogueManager != null)
        {
            dialogueManager.HideInteractionPrompt();
        }
        
        // Handle fade effect if enabled
        if (useFadeEffect)
        {
            // Find or create a fade system
            ScreenFader fader = FindObjectOfType<ScreenFader>();
            if (fader != null)
            {
                // Fade out
                yield return fader.FadeOut(fadeDuration);
            }
            else
            {
                Debug.LogWarning("ScreenFader component not found, but useFadeEffect is enabled.");
            }
        }
        
        // Small delay
        yield return new WaitForSeconds(teleportDelay);
        
        // Perform teleport
        playerTransform.position = destination.position;
        
        // Set player direction if enabled
        if (setPlayerDirection && playerMovement != null)
        {
            playerMovement.direction = (int)playerFacingAfterTeleport;
            playerMovement.PlayIdleAnimation(); // Update animation to match new direction
        }
        
        // Handle fade effect if enabled
        if (useFadeEffect)
        {
            ScreenFader fader = FindObjectOfType<ScreenFader>();
            if (fader != null)
            {
                // Fade in
                yield return fader.FadeIn(fadeDuration);
            }
        }
        
        // Small delay before unlocking movement
        yield return new WaitForSeconds(0.1f);
        
        // Unlock player movement
        if (playerMovement != null)
        {
            playerMovement.UnlockMovement();
        }
        
        isTeleporting = false;
    }
    
    private void OnDrawGizmos()
    {
        // Draw a line showing the teleport destination
        if (destination != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, destination.position);
            Gizmos.DrawWireSphere(destination.position, 0.3f);
        }
    }
} 