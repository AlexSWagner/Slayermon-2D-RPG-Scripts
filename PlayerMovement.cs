using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player movement and animations in a 4-directional top-down game
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    public Rigidbody2D rb2d;
    
    [Header("Animation")]
    public Animator playerAnim;
    
    // Direction: 0 = Down, 1 = Right, 2 = Left, 3 = Up
    [HideInInspector]
    public int direction = 0;
    
    // Animation clip names
    private const string ANIM_IDLE_DOWN = "playerIdleD";
    private const string ANIM_IDLE_RIGHT = "playerIdleR";
    private const string ANIM_IDLE_LEFT = "playerIdleL";
    private const string ANIM_IDLE_UP = "playerIdleU";
    
    private const string ANIM_WALK_DOWN = "playerWalkD";
    private const string ANIM_WALK_RIGHT = "playerWalkR";
    private const string ANIM_WALK_LEFT = "playerWalkL";
    private const string ANIM_WALK_UP = "playerWalkU";
    
    // Flag to indicate if movement is locked by another system (like attacking or dialogue)
    [HideInInspector]
    public bool movementLocked = false;

    void Awake()
    {
        // Find required components
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();
        if (playerAnim == null) FindAnimator();
    }

    void Start()
    {
        // Make sure we have all required components
        if (playerAnim == null) FindAnimator();
        
        // Set initial animation
        PlayIdleAnimation();
    }

    /// <summary>
    /// Finds the animator component in the hierarchy
    /// </summary>
    private void FindAnimator()
    {
        if (playerAnim != null) return;
        
        // Try to find on this object first
        playerAnim = GetComponent<Animator>();
        
        // If not found on this object, try to find on child named "playerSprite"
        if (playerAnim == null)
        {
            Transform spriteChild = transform.Find("playerSprite");
            if (spriteChild != null)
            {
                playerAnim = spriteChild.GetComponent<Animator>();
            }
        }
        
        // If still not found, search all children
        if (playerAnim == null)
        {
            playerAnim = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // Get input in Update (better for responsiveness)
        if (!movementLocked)
        {
            GetMovementInput();
        }
    }
    
    void FixedUpdate()
    {
        // Apply movement in FixedUpdate (better for physics)
        if (!movementLocked)
        {
            ApplyMovement();
        }
        else
        {
            // Stop movement when locked
            rb2d.velocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Gets player input and determines movement direction
    /// </summary>
    private void GetMovementInput()
    {
        // Get input for horizontal and vertical movement
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        
        // Restrict to 4-directional movement (no diagonals)
        if (moveInput.sqrMagnitude > 0)
        {
            // If horizontal movement is stronger or equal to vertical, only move horizontally
            if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
            {
                moveInput.y = 0;
                
                // Update direction based on movement
                direction = moveInput.x > 0 ? 1 : 2; // Right or Left
            }
            else
            {
                moveInput.x = 0;
                
                // Update direction based on movement
                direction = moveInput.y > 0 ? 3 : 0; // Up or Down
            }
            
            // Normalize to ensure consistent speed
            moveInput.Normalize();
            
            // Update animation
            PlayWalkAnimation();
        }
        else
        {
            // No input, play idle
            PlayIdleAnimation();
        }
    }
    
    /// <summary>
    /// Applies movement to the rigidbody
    /// </summary>
    private void ApplyMovement()
    {
        rb2d.velocity = moveInput * moveSpeed;
    }
    
    /// <summary>
    /// Plays the appropriate idle animation based on current direction
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (playerAnim == null) return;
        
        string animName = "";
        
        switch (direction)
        {
            case 0: // Down
                animName = ANIM_IDLE_DOWN;
                break;
            case 1: // Right
                animName = ANIM_IDLE_RIGHT;
                break;
            case 2: // Left
                animName = ANIM_IDLE_LEFT;
                break;
            case 3: // Up
                animName = ANIM_IDLE_UP;
                break;
        }
        
        playerAnim.Play(animName);
    }
    
    /// <summary>
    /// Plays the appropriate walking animation based on current direction
    /// </summary>
    private void PlayWalkAnimation()
    {
        if (playerAnim == null) return;
        
        string animName = "";
        
        switch (direction)
        {
            case 0: // Down
                animName = ANIM_WALK_DOWN;
                break;
            case 1: // Right
                animName = ANIM_WALK_RIGHT;
                break;
            case 2: // Left
                animName = ANIM_WALK_LEFT;
                break;
            case 3: // Up
                animName = ANIM_WALK_UP;
                break;
        }
        
        playerAnim.Play(animName);
    }
    
    /// <summary>
    /// Locks player movement temporarily
    /// </summary>
    public void LockMovement()
    {
        movementLocked = true;
        rb2d.velocity = Vector2.zero;
    }
    
    /// <summary>
    /// Unlocks player movement
    /// </summary>
    public void UnlockMovement()
    {
        movementLocked = false;
    }

    /// <summary>
    /// Locks player movement for dialogue
    /// </summary>
    /// <param name="locked">Whether to lock (true) or unlock (false) movement</param>
    public void LockMovementForDialogue(bool locked)
    {
        if (locked)
        {
            LockMovement();
        }
        else
        {
            UnlockMovement();
        }
    }
}