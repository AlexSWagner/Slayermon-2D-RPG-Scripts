using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Main player controller script handling movement, combat, inventory and interactions
/// </summary>
public class PlayerScript : MonoBehaviour
{
    [Header("Components")]
    public Animator animator; // Animator component reference
    public Rigidbody2D rb2d; // Rigidbody component for movement
    public SpriteRenderer swordRenderer; // Reference to the sword's sprite renderer
    public SpriteRenderer bowRenderer; // Reference to the bow's sprite renderer
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    public bool movementLocked = false;
    
    // Direction: 0 = Down, 1 = Right, 2 = Left, 3 = Up
    [HideInInspector]
    public int direction = 0;

    [Header("Combat")]
    public float attackCooldown; // Current cooldown timer
    public float attackDuration = 0.2f; // Duration of attack animation - matches the animation length exactly
    public float attackCooldownTime = 0.0f; // No additional cooldown needed - let animation control timing
    public float knockbackForce = 10f; // Force for knockback when hit by enemies
    private bool isAttacking = false; // Flag to track if we're currently in an attack
    
    [Header("Weapons")]
    public Transform playerSprite; // Reference to the player sprite object
    public GameObject swordObj; // Reference to the sword object
    public GameObject bowObj; // Reference to the bow object
    
    public GameObject arrowPrefab;
    [Tooltip("0 = Sword, 1 = Bow")]
    public int weaponInUse = 0; // 0 = Sword, 1 = Bow - Start with sword equipped
    private WeaponScript swordWeaponScript; // Reference to the sword's weapon script
    private WeaponScript bowWeaponScript; // Reference to the bow's weapon script
    private ArrowManager arrowManager; // Reference to the arrow manager
    
    [Header("Health System")]
    [Range(0, 3)]
    public int playerHealth;
    public int maxHealth = 3; // Maximum health capacity
    public Animator gameOver;
    public GameObject heart1;
    public GameObject heart2;
    public GameObject heart3;
    public bool hurting; // Flag to prevent taking multiple hits at once
    public float invulnerabilityTime = 2f; // Time player is invulnerable after being hit
    public GameObject continueButton; // Reference to the continue button in the game over screen

    [Header("Inventory")]
    public TextMeshProUGUI inGameCoinText;
    public int coinCount;
    public TextMeshProUGUI inGameHealthPotionText;
    public int healthPotionCount;
    public TextMeshProUGUI inGameArrowText;
    public int arrowCount;
    public int itemCost = 5; // Standard cost for items in the shop

    [Header("Shop")]
    public GameObject shopButtons;

    // Animator parameter names
    private const string ANIM_HORIZONTAL = "MoveX";
    private const string ANIM_VERTICAL = "MoveY";
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_IS_ATTACKING = "IsAttacking";

    // Called when the script instance is being loaded
    void Awake()
    {
        // Load saved values from PlayerPrefs at start
        LoadPlayerStats();
        
        // Auto-assign components if not set in inspector
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();
        
        // Try to find the animator if missing
        if (animator == null) FindAnimator();
        
        // Setup weapon scripts
        SetupWeapons();
        
        // Make sure we have arrows for testing
        if (arrowCount < 5)
        {
            arrowCount = 25; // Start with 25 arrows
            PlayerPrefs.SetInt("ArrowCount", arrowCount);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize player health
        playerHealth = maxHealth;
        
        // Update UI with current values
        UpdateUI();
        
        // Hide all weapons initially - use renderer approach
        if (swordObj != null && swordRenderer == null)
            swordRenderer = swordObj.GetComponent<SpriteRenderer>();
        
        if (bowObj != null && bowRenderer == null)
            bowRenderer = bowObj.GetComponent<SpriteRenderer>();
        
        // Set default weapon to sword (0) and show it
        weaponInUse = 0;
        ShowSelectedWeapon();
    }

    // Update is called once per frame
    void Update()
    {
        // Update attack cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            
            // When attack ends, reset the attack state
            if (attackCooldown <= 0)
            {
                if (animator != null)
                {
                    // Reset attack animation parameter
                    animator.SetBool(ANIM_IS_ATTACKING, false);
                    
                    // Re-apply previous movement values if the player was moving
                    // This creates a smoother transition back to movement
                    if (moveInput.sqrMagnitude > 0)
                    {
                        animator.SetFloat(ANIM_HORIZONTAL, moveInput.x);
                        animator.SetFloat(ANIM_VERTICAL, moveInput.y);
                        animator.SetFloat(ANIM_SPEED, 1f);
                    }
                }
                
                movementLocked = false;
                isAttacking = false;
                
                // If we're using a sword, keep it visible after attack
                if (weaponInUse == 0)
                {
                    ShowSelectedWeapon();
                }
                else
                {
                    // For bow, hide weapons when attack animation finishes
                    HideAllWeapons();
                }
            }
        }
        
        // Only process inputs if player is alive
        if (playerHealth > 0)
        {
            // Always allow movement unless locked
            if (!movementLocked)
            {
                HandleMovement();
            }
            else
            {
                // Ensure velocity is zero when movement is locked (during attacks)
                rb2d.velocity = Vector2.zero;
            }
            
            // Handle weapon switching if not attacking
            if (!isAttacking)
            {
                HandleWeaponSwitching();
            }
            
            // Only allow attacking if not on cooldown and not already attacking
            if (attackCooldown <= 0 && !isAttacking)
            {
                HandleAttacking();
            }
        }
        
        // These functions can be executed regardless of player state
        HandleHealthPotion();
        UpdateHealthUI();
        UpdateInventory();
    }
    
    /// <summary>
    /// Finds the animator component in the hierarchy
    /// </summary>
    private void FindAnimator()
    {
        // First try on this GameObject
        animator = GetComponent<Animator>();
        
        // Then try on child named playerSprite
        if (animator == null && playerSprite != null)
        {
            animator = playerSprite.GetComponent<Animator>();
        }
        
        // Last resort - look in children
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
    
    /// <summary>
    /// Sets up weapon references and scripts
    /// </summary>
    private void SetupWeapons()
    {
        // Get weapon scripts
        if (swordObj != null)
        {
            swordWeaponScript = swordObj.GetComponent<WeaponScript>();
            if (swordWeaponScript == null)
            {
                swordWeaponScript = swordObj.AddComponent<WeaponScript>();
                swordWeaponScript.isSword = true;
            }
        }
        
        if (bowObj != null)
        {
            bowWeaponScript = bowObj.GetComponent<WeaponScript>();
            if (bowWeaponScript == null)
            {
                bowWeaponScript = bowObj.AddComponent<WeaponScript>();
                bowWeaponScript.isSword = false;
            }
            
            // Setup arrow manager
            arrowManager = bowObj.GetComponent<ArrowManager>();
            if (arrowManager == null)
            {
                arrowManager = bowObj.AddComponent<ArrowManager>();
            }
            
            // Set arrow prefab reference
            if (arrowPrefab != null && arrowManager.arrowPrefab == null)
            {
                arrowManager.arrowPrefab = arrowPrefab;
            }
        }
    }
    
    /// <summary>
    /// Handles player movement and animation
    /// </summary>
    private void HandleMovement()
    {
        // Get input for horizontal and vertical movement
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        
        // Handle movement input
        if (moveInput.sqrMagnitude > 0)
        {
            // Prioritize movement direction - enforce non-diagonal movement
            // If player presses both horizontal and vertical keys, prioritize based on key pressed first or stronger axis
            if (Mathf.Abs(moveInput.x) > 0 && Mathf.Abs(moveInput.y) > 0)
            {
                // Both axes have input - need to choose one
                // Prioritize horizontal movement if its magnitude is greater
                if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
                {
                    moveInput.y = 0; // Zero out vertical movement
                }
                else
                {
                    moveInput.x = 0; // Zero out horizontal movement
                }
            }
            
            // Normalize to ensure consistent speed (although we've eliminated diagonals, this is good practice)
            moveInput.Normalize();
            
            // Update direction based on final movement
            if (moveInput.x > 0)
                direction = 1; // Right
            else if (moveInput.x < 0)
                direction = 2; // Left
            else if (moveInput.y > 0)
                direction = 3; // Up
            else if (moveInput.y < 0)
                direction = 0; // Down
            
            // Apply movement
            rb2d.velocity = moveInput * moveSpeed;
            
            // Update animator parameters for blend tree
            if (animator != null)
            {
                animator.SetFloat(ANIM_HORIZONTAL, moveInput.x);
                animator.SetFloat(ANIM_VERTICAL, moveInput.y);
                animator.SetFloat(ANIM_SPEED, 1f); // Set speed to 1 for walking
            }
        }
        else
        {
            // No input, stop movement
            rb2d.velocity = Vector2.zero;
            
            // Update animator parameters
            if (animator != null)
            {
                // Keep the directional values but set speed to 0 for idle
                animator.SetFloat(ANIM_SPEED, 0f);
            }
        }
    }
    
    /// <summary>
    /// Directly plays animation if Animator parameters aren't working
    /// </summary>
    private void PlayDirectionalAnimation(int dir, bool isMoving)
    {
        if (animator == null) return;
        
        // Animation clip names
        string[] idleAnims = { "playerIdleD", "playerIdleR", "playerIdleL", "playerIdleU" };
        string[] walkAnims = { "playerWalkD", "playerWalkR", "playerWalkL", "playerWalkU" };
        
        // Ensure dir is valid
        if (dir < 0 || dir > 3) dir = 0;
        
        // Play appropriate animation
        string animName = isMoving ? walkAnims[dir] : idleAnims[dir];
        animator.Play(animName);
    }
    
    /// <summary>
    /// Handles attack input and execution
    /// </summary>
    private void HandleAttacking()
    {
        // Only process if a weapon is selected (should always be true now)
        if (weaponInUse < 0) return;
        
        // Add a stricter check to prevent attacking too quickly
        // This ensures the animation has time to complete fully
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && !isAttacking && attackCooldown <= 0)
        {
            // Set attacking flag to prevent multiple attacks
            isAttacking = true;
            
            // Start attack with cooldown (animation duration only)
            // Add a small buffer to ensure animations complete
            attackCooldown = attackDuration + 0.1f;
            movementLocked = true;
            
            // Stop movement immediately
            rb2d.velocity = Vector2.zero;
            
            // Make sure only the selected weapon is visible during attack
            ShowSelectedWeapon();
            
            // Set attack animation parameter - preserve direction values for blend tree
            if (animator != null)
            {
                // Make sure Speed is 0 first to ensure we're properly in idle state
                animator.SetFloat(ANIM_SPEED, 0f);
                
                // Reset the attack bool first to ensure clean state
                animator.SetBool(ANIM_IS_ATTACKING, false);
                
                // Trigger attack animation on the next frame
                StartCoroutine(TriggerAttackAnimation());
            }
            
            // For bow attacks, delay the arrow firing to match animation
            if (weaponInUse == 1) // Bow
            {
                StartCoroutine(DelayedBowShot());
            }
            else if (weaponInUse == 0 && swordWeaponScript != null) // Sword
            {
                swordWeaponScript.ResetHitEnemies();
            }
        }
    }
    
    /// <summary>
    /// Delays the bow shot to match the animation timing
    /// </summary>
    private IEnumerator DelayedBowShot()
    {
        // Force bow visibility immediately at the start
        if (bowRenderer != null)
            bowRenderer.enabled = true;
        else if (bowObj != null)
            bowObj.SetActive(true);
        
        // Wait a short time for the animation to start
        yield return new WaitForSeconds(0.05f);
        
        // Double-check that arrowManager exists and is properly initialized
        if (arrowManager == null && bowObj != null)
        {
            arrowManager = bowObj.GetComponent<ArrowManager>();
            if (arrowManager == null)
            {
                arrowManager = bowObj.AddComponent<ArrowManager>();
                if (arrowPrefab != null)
                    arrowManager.arrowPrefab = arrowPrefab;
            }
        }
        
        // Update firePoint position based on player direction
        if (arrowManager != null)
        {
            arrowManager.UpdateFirePointPosition(direction);
        }
        
        // Wait a bit more for the "draw" part of the animation
        yield return new WaitForSeconds(0.1f);
        
        // Check again to make sure bow is still visible
        if (bowRenderer != null)
            bowRenderer.enabled = true;
        else if (bowObj != null)
            bowObj.SetActive(true);
        
        // Use the new ArrowManager system with fallback
        if (arrowManager != null && arrowManager.enabled)
        {
            // Fire the arrow using ArrowManager
            arrowManager.FireArrow();
        }
        else
        {
            // Fallback to direct method with improved positioning
            ShootArrow();
        }
        
        // Keep bow visible for the remainder of the attack
        yield return new WaitForSeconds(attackDuration - 0.15f);
        
        // Final check to ensure bow is visible until attack ends
        if (bowRenderer != null)
            bowRenderer.enabled = true;
        else if (bowObj != null)
            bowObj.SetActive(true);
    }
    
    /// <summary>
    /// Shows only the currently selected weapon
    /// </summary>
    private void ShowSelectedWeapon()
    {
        // First, ensure all weapons are hidden
        HideAllWeapons();
        
        // Then show only the selected weapon
        if (weaponInUse == 0) // Sword
        {
            if (swordRenderer != null)
                swordRenderer.enabled = true;
            else if (swordObj != null)
                swordObj.SetActive(true);
        }
        else if (weaponInUse == 1) // Bow
        {
            if (bowRenderer != null)
                bowRenderer.enabled = true;
            else if (bowObj != null)
                bowObj.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hides all weapons
    /// </summary>
    private void HideAllWeapons()
    {
        // Use sprite renderers if available (preferred method)
        if (swordRenderer != null)
            swordRenderer.enabled = false;
        else if (swordObj != null)
            swordObj.SetActive(false);
        
        if (bowRenderer != null)
            bowRenderer.enabled = false;
        else if (bowObj != null)
            bowObj.SetActive(false);
    }
    
    /// <summary>
    /// Handles weapon switching with tab key
    /// </summary>
    private void HandleWeaponSwitching()
    {
        // Use Tab key to cycle between weapons
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Toggle between sword (0) and bow (1)
            weaponInUse = (weaponInUse == 0) ? 1 : 0;
            
            // Show the current weapon
            ShowSelectedWeapon();
            
            // Update firePoint when switching to bow
            if (weaponInUse == 1 && arrowManager != null)
            {
                arrowManager.UpdateFirePointPosition(direction);
            }
        }
    }
    
    /// <summary>
    /// Handles arrow shooting
    /// </summary>
    private void ShootArrow()
    {
        if (arrowCount <= 0 || arrowPrefab == null) return;
        
        // Decrease arrow count
        arrowCount--;
        PlayerPrefs.SetInt("ArrowCount", arrowCount);
        
        // Set rotation based on direction
        Quaternion arrowRotation = Quaternion.identity;
        Vector2 arrowVelocity = Vector2.zero;
        float arrowSpeed = 10f;
        
        switch (direction)
        {
            case 0: // Down
                arrowRotation = Quaternion.Euler(0, 0, 270);
                arrowVelocity = new Vector2(0, -arrowSpeed);
                break;
            case 1: // Right
                arrowRotation = Quaternion.Euler(0, 0, 0);
                arrowVelocity = new Vector2(arrowSpeed, 0);
                break;
            case 2: // Left
                arrowRotation = Quaternion.Euler(0, 0, 180);
                arrowVelocity = new Vector2(-arrowSpeed, 0);
                break;
            case 3: // Up
                arrowRotation = Quaternion.Euler(0, 0, 90);
                arrowVelocity = new Vector2(0, arrowSpeed);
                break;
        }
        
        // Calculate spawn position in front of player based on direction
        // Adjust these values to ensure arrow fires from center of bow
        Vector3 spawnOffset = Vector3.zero;
        switch (direction)
        {
            case 0: // Down
                spawnOffset = new Vector3(0, -0.3f, 0);
                break;
            case 1: // Right
                spawnOffset = new Vector3(0.3f, 0, 0);
                break;
            case 2: // Left
                spawnOffset = new Vector3(-0.3f, 0, 0);
                break;
            case 3: // Up
                spawnOffset = new Vector3(0, 0.3f, 0);
                break;
        }
        
        // Instantiate arrow
        GameObject arrow = Instantiate(arrowPrefab, transform.position + spawnOffset, arrowRotation);
        
        // Add velocity to arrow if it has a Rigidbody2D
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb != null)
        {
            arrowRb.velocity = arrowVelocity;
        }
    }
    
    /// <summary>
    /// Handles health potion usage with H key
    /// </summary>
    private void HandleHealthPotion()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (healthPotionCount > 0 && playerHealth < maxHealth)
            {
                playerHealth++;
                healthPotionCount--;
                PlayerPrefs.SetInt("HealthPotionCount", healthPotionCount);
                UpdateUI();
            }
        }
    }
    
    /// <summary>
    /// Updates the health UI based on current player health
    /// </summary>
    private void UpdateHealthUI()
    {
        // Update heart UI based on current health
        if (heart1 != null) heart1.SetActive(playerHealth >= 3);
        if (heart2 != null) heart2.SetActive(playerHealth >= 2);
        if (heart3 != null) heart3.SetActive(playerHealth >= 1);
        
        // If player is dead, show game over screen
        if (playerHealth <= 0 && gameOver != null)
        {
            gameOver.Play("gameOverAnim");
            if (animator != null) animator.speed = 0;
            
            // Make sure the continue button is active
            if (continueButton != null)
            {
                continueButton.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Updates inventory and UI elements with values from PlayerPrefs
    /// </summary>
    private void UpdateInventory()
    {
        // Get current counts from PlayerPrefs
        coinCount = PlayerPrefs.GetInt("ScoreCount");
        arrowCount = PlayerPrefs.GetInt("ArrowCount");
        healthPotionCount = PlayerPrefs.GetInt("HealthPotionCount");
        
        // Update UI
        UpdateUI();
    }
    
    /// <summary>
    /// Updates all UI text elements with current values
    /// </summary>
    public void UpdateUI()
    {
        // Update UI text
        if (inGameCoinText != null) inGameCoinText.text = coinCount.ToString();
        if (inGameArrowText != null) inGameArrowText.text = arrowCount.ToString();
        if (inGameHealthPotionText != null) inGameHealthPotionText.text = healthPotionCount.ToString();
    }
    
    /// <summary>
    /// Loads player stats from PlayerPrefs
    /// </summary>
    private void LoadPlayerStats()
    {
        coinCount = PlayerPrefs.GetInt("ScoreCount", 0);
        arrowCount = PlayerPrefs.GetInt("ArrowCount", 0);
        healthPotionCount = PlayerPrefs.GetInt("HealthPotionCount", 0);
        
        // Ensure we have arrows for testing
        if (arrowCount < 5)
        {
            arrowCount = 25;
            PlayerPrefs.SetInt("ArrowCount", arrowCount);
        }
    }

    /// <summary>
    /// Called when this collider/rigidbody has begun touching another rigidbody/collider
    /// </summary>
    public void OnCollisionEnter2D(Collision2D collision)
    {
        // This is kept for non-enemy collision handling
    }

    /// <summary>
    /// Called when the Collider enters a trigger
    /// </summary>
    public void OnTriggerEnter2D(Collider2D collision)
    {
        // Handle pickup of health item
        if (collision.gameObject.CompareTag("Heart") && playerHealth < maxHealth)
        {
            playerHealth++;
            Destroy(collision.gameObject);
        }
        
        // Handle pickup of coins
        if (collision.gameObject.CompareTag("Coin"))
        {
            coinCount++;
            PlayerPrefs.SetInt("ScoreCount", coinCount);
            Destroy(collision.gameObject);
        }
        
        // Show shop UI when entering shop area
        if (collision.gameObject.CompareTag("Shop") && shopButtons != null)
        {
            shopButtons.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the Collider exits a trigger
    /// </summary>
    public void OnTriggerExit2D(Collider2D collision)
    {
        // Hide shop UI when leaving shop area
        if (collision.gameObject.CompareTag("Shop") && shopButtons != null)
        {
            shopButtons.SetActive(false);
        }
    }

    /// <summary>
    /// Coroutine that handles invulnerability period after taking damage
    /// </summary>
    IEnumerator InvulnerabilityPeriod()
    {
        // Wait for invulnerability time
        yield return new WaitForSeconds(invulnerabilityTime);
        
        // Reset hurting flag
        hurting = false;
        
        // Reset collider to ensure collision detection works properly
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
            col.enabled = true;
        }
    }

    /// <summary>
    /// Handles damage taken from enemies or other sources
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        // Only take damage if not already hurting and still alive
        if (!hurting && playerHealth > 0)
        {
            // Decrease health
            playerHealth--;
            
            // Start invulnerability period
            StartCoroutine(InvulnerabilityPeriod());
            
            // Set hurting flag to prevent multiple hits
            hurting = true;
            
            // Add knockback in the direction away from the enemy
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length > 0 && playerHealth > 0)
            {
                // Find the closest enemy
                GameObject closestEnemy = null;
                float closestDistance = float.MaxValue;
                
                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
                
                // Apply knockback away from the closest enemy
                if (closestEnemy != null)
                {
                    Vector2 knockbackDirection = (transform.position - closestEnemy.transform.position).normalized;
                    rb2d.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// Restarts the game from the beginning
    /// </summary>
    public void PlayAgain()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Returns to the main menu
    /// </summary>
    public void MainMenu()
    {
        // Reload the current scene as a fallback
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Purchases an arrow from the shop
    /// </summary>
    public void BuyArrow()
    {
        if (coinCount >= itemCost)
        {
            // Deduct coins and add arrow
            coinCount -= itemCost;
            arrowCount++;
            
            // Save to PlayerPrefs
            PlayerPrefs.SetInt("ScoreCount", coinCount);
            PlayerPrefs.SetInt("ArrowCount", arrowCount);
            
            // Update UI
            UpdateUI();
        }
    }
    
    /// <summary>
    /// Purchases a health potion from the shop
    /// </summary>
    public void BuyHealthPotion()
    {
        if (coinCount >= itemCost)
        {
            // Deduct coins and add health potion
            coinCount -= itemCost;
            healthPotionCount++;
            
            // Save to PlayerPrefs
            PlayerPrefs.SetInt("ScoreCount", coinCount);
            PlayerPrefs.SetInt("HealthPotionCount", healthPotionCount);
            
            // Update UI
            UpdateUI();
        }
    }

    /// <summary>
    /// Coroutine that triggers attack animation
    /// </summary>
    private IEnumerator TriggerAttackAnimation()
    {
        yield return null; // Wait for the next frame
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_ATTACKING, true);
        }
    }

    /// <summary>
    /// Locks or unlocks player movement for UI interaction
    /// </summary>
    /// <param name="locked">True to lock movement, false to unlock</param>
    public void LockMovementForUI(bool locked)
    {
        movementLocked = locked;
        
        // Stop movement immediately if being locked
        if (locked)
        {
            rb2d.velocity = Vector2.zero;
        }
    }

    // You already have LockMovementForUI, but the teleport script is looking for these methods:
    public void LockMovement()
    {
        movementLocked = true;
        rb2d.velocity = Vector2.zero;
    }

    public void UnlockMovement()
    {
        movementLocked = false;
    }

    public void PlayIdleAnimation()
    {
        // This method already exists in your code
    }
}
