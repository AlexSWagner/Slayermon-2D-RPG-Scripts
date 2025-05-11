using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Main player controller script handling movement, combat, inventory and interactions.
/// </summary>
public class PlayerScript : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;
    public Rigidbody2D rb2d;
    public SpriteRenderer swordRenderer;
    public SpriteRenderer bowRenderer;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    public bool movementLocked = false;

    // Direction mapping: 0 = Down, 1 = Right, 2 = Left, 3 = Up
    [HideInInspector]
    public int direction = 0;

    [Header("Combat")]
    public float attackCooldown; // Tracks time until next attack is allowed.
    public float attackDuration = 0.2f; // Base duration of the attack animation. Should match animation length unless modified by speedFactor.
    // public float attackCooldownTime = 0.0f; // Currently unused - cooldown derived from attackDuration.
    public float knockbackForce = 10f;
    private bool isAttacking = false;

    [Header("Weapons")]
    public Transform playerSprite;
    public GameObject swordObj;
    public GameObject bowObj;

    public GameObject arrowPrefab;
    [Tooltip("0 = Sword, 1 = Bow")]
    public int weaponInUse = 0;
    private WeaponScript swordWeaponScript;
    private WeaponScript bowWeaponScript;
    private ArrowManager arrowManager;

    [Header("Health System")]
    [Range(0, 3)]
    public int playerHealth;
    public int maxHealth = 3;
    public Animator gameOver;
    public GameObject heart1;
    public GameObject heart2;
    public GameObject heart3;
    public bool hurting; // Prevents taking multiple damage hits simultaneously.
    public float invulnerabilityTime = 2f; // Duration player is invulnerable after being hit.
    public GameObject continueButton;

    [Header("Inventory")]
    public TextMeshProUGUI inGameCoinText;
    public int coinCount;
    public TextMeshProUGUI inGameHealthPotionText;
    public int healthPotionCount;
    public TextMeshProUGUI inGameArrowText;
    public int arrowCount;
    public int itemCost = 5;

    [Header("Shop")]
    public GameObject shopButtons;

    // Cached animator parameter hashes for performance.
    // TODO: Verify if needed based on profiling.
    private readonly int AnimHorizontalHash = Animator.StringToHash("MoveX");
    private readonly int AnimVerticalHash = Animator.StringToHash("MoveY");
    private readonly int AnimSpeedHash = Animator.StringToHash("Speed");
    private readonly int AnimIsAttackingHash = Animator.StringToHash("IsAttacking");

    void Awake()
    {
        LoadPlayerStats();

        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();
        if (animator == null) FindAnimator();

        SetupWeapons();

        // Ensure player starts with a minimum number of arrows for testing/gameplay.
        if (arrowCount < 50)
        {
            arrowCount = 50;
            PlayerPrefs.SetInt("ArrowCount", arrowCount);
        }
    }

    void Start()
    {
        playerHealth = maxHealth;
        UpdateUI();

        // Attempt to get SpriteRenderers if not assigned.
        if (swordObj != null && swordRenderer == null)
            swordRenderer = swordObj.GetComponent<SpriteRenderer>();
        if (bowObj != null && bowRenderer == null)
            bowRenderer = bowObj.GetComponent<SpriteRenderer>();

        weaponInUse = 0; // Default to sword
        ShowSelectedWeapon();
    }

    void Update()
    {
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;

            if (attackCooldown <= 0)
            {
                // Reset attack state once cooldown finishes.
                if (animator != null)
                {
                    animator.SetBool(AnimIsAttackingHash, false);

                    // Restore movement animation parameters if player was moving before the attack.
                    if (moveInput.sqrMagnitude > 0)
                    {
                        animator.SetFloat(AnimHorizontalHash, moveInput.x);
                        animator.SetFloat(AnimVerticalHash, moveInput.y);
                        animator.SetFloat(AnimSpeedHash, 1f);
                    }
                }
                movementLocked = false;
                isAttacking = false;
                // Weapon visibility is handled by ShowSelectedWeapon and state transitions.
            }
        }

        if (playerHealth > 0)
        {
            if (!movementLocked)
            {
                HandleMovement();
            }
            else
            {
                // Ensure player stops moving during locked states such as attacks
                rb2d.velocity = Vector2.zero;
            }

            if (!isAttacking)
            {
                HandleWeaponSwitching();
            }

            if (attackCooldown <= 0 && !isAttacking)
            {
                HandleAttacking();
            }
        }

        HandleHealthPotion();
        UpdateHealthUI();
        UpdateInventory();
    }

    /// <summary>
    /// Finds the animator component, searching this object, a specific child, or any child.
    /// </summary>
    private void FindAnimator()
    {
        animator = GetComponent<Animator>();
        if (animator == null && playerSprite != null)
        {
            animator = playerSprite.GetComponent<Animator>();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
    }

    /// <summary>
    /// Gets or adds required WeaponScript and ArrowManager components to weapon GameObjects.
    /// </summary>
    private void SetupWeapons()
    {
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

            arrowManager = bowObj.GetComponent<ArrowManager>();
            if (arrowManager == null)
            {
                arrowManager = bowObj.AddComponent<ArrowManager>();
            }

            if (arrowPrefab != null && arrowManager.arrowPrefab == null)
            {
                arrowManager.arrowPrefab = arrowPrefab;
            }
        }
    }

    /// <summary>
    /// Handles player movement input, updates direction, and sets animator parameters.
    /// Enforces non-diagonal movement.
    /// </summary>
    private void HandleMovement()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput.sqrMagnitude > 0)
        {
            // Enforce non-diagonal movement: If both axes have input,
            // prioritize the one with the larger absolute value.
            if (Mathf.Abs(moveInput.x) > 0 && Mathf.Abs(moveInput.y) > 0)
            {
                if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
                {
                    moveInput.y = 0;
                }
                else
                {
                    moveInput.x = 0;
                }
            }

            moveInput.Normalize(); // Prevent faster diagonal speed if enforcement logic changes.

            if (moveInput.x > 0) direction = 1;
            else if (moveInput.x < 0) direction = 2;
            else if (moveInput.y > 0) direction = 3;
            else if (moveInput.y < 0) direction = 0;

            rb2d.velocity = moveInput * moveSpeed;

            if (animator != null)
            {
                animator.SetFloat(AnimHorizontalHash, moveInput.x);
                animator.SetFloat(AnimVerticalHash, moveInput.y);
                animator.SetFloat(AnimSpeedHash, 1f);
            }
        }
        else
        {
            rb2d.velocity = Vector2.zero;
            if (animator != null)
            {
                // Keep last direction for idle animation, but set speed to 0.
                animator.SetFloat(AnimSpeedHash, 0f);
            }
        }
    }

    /// <summary>
    /// Fallback method to directly play specific directional animations by name.
    /// Used if the Animator blend tree isn't behaving as expected.
    /// </summary>
    /// <param name="dir">Direction index (0=D, 1=R, 2=L, 3=U)</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    private void PlayDirectionalAnimation(int dir, bool isMoving)
    {
        if (animator == null) return;

        string[] idleAnims = { "playerIdleD", "playerIdleR", "playerIdleL", "playerIdleU" };
        string[] walkAnims = { "playerWalkD", "playerWalkR", "playerWalkL", "playerWalkU" };

        if (dir < 0 || dir >= idleAnims.Length) dir = 0; // Clamp direction index

        string animName = isMoving ? walkAnims[dir] : idleAnims[dir];
        animator.Play(animName);
    }

    /// <summary>
    /// Handles attack input, triggers animations, applies cooldown, and manages weapon-specific logic.
    /// </summary>
    private void HandleAttacking()
    {
        if (weaponInUse < 0) return; // Should not happen if a weapon is always equipped.

        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && !isAttacking && attackCooldown <= 0)
        {
            isAttacking = true;
            movementLocked = true;
            rb2d.velocity = Vector2.zero; // Stop movement during attack.

            // Adjust attack duration/cooldown by a factor to change attack speed.
            // IMPORTANT: Corresponding animation clips in Unity must also be adjusted!
            float speedFactor = 0.8f; // e.g., 0.8 = 20% faster
            attackCooldown = attackDuration * speedFactor;

            ShowSelectedWeapon();

            if (animator != null)
            {
                animator.SetFloat(AnimSpeedHash, 0f); // Ensure idle state before attack animation.
                animator.SetBool(AnimIsAttackingHash, true);
            }

            if (weaponInUse == 1) // Bow
            {
                StartCoroutine(DelayedBowShot());
            }
            else if (weaponInUse == 0 && swordWeaponScript != null) // Sword
            {
                // Resetting hit enemies allows the sword to hit multiple targets per swing.
                swordWeaponScript.ResetHitEnemies();
            }
        }
    }

    /// <summary>
    /// Coroutine to handle the timing delay for firing the bow, matching the animation.
    /// </summary>
    private IEnumerator DelayedBowShot()
    {
        // Ensure bow is visible; safety check for potential race conditions.
        if (bowRenderer != null) bowRenderer.enabled = true;
        else if (bowObj != null) bowObj.SetActive(true);

        // Wait for a scaled duration to match potentially sped-up animations.
        // IMPORTANT: Corresponding animation clips in Unity must also be adjusted!
        float speedFactor = 0.8f; // Should match the factor used in HandleAttacking
        yield return new WaitForSeconds(0.12f * speedFactor);

        if (arrowManager == null && bowObj != null)
        {
            // Attempt to get manager again if it wasn't ready in SetupWeapons
            arrowManager = bowObj.GetComponent<ArrowManager>();
            if (arrowManager == null)
            {
                arrowManager = bowObj.AddComponent<ArrowManager>();
            }
            if (arrowPrefab != null && arrowManager.arrowPrefab == null)
            {
                arrowManager.arrowPrefab = arrowPrefab;
            }
        }

        if (arrowManager != null)
        {
            arrowManager.FireArrow(transform.position, direction);
        }
        else
        {
            // Optional: Log error if arrow manager is still null here.
        }
    }

    /// <summary>
    /// Ensures only the currently equipped weapon's visual is active.
    /// </summary>
    private void ShowSelectedWeapon()
    {
        HideAllWeapons();

        // Prefer enabling SpriteRenderer if available, otherwise use SetActive.
        if (weaponInUse == 0) // Sword
        {
            if (swordRenderer != null) swordRenderer.enabled = true;
            else if (swordObj != null) swordObj.SetActive(true);
        }
        else if (weaponInUse == 1) // Bow
        {
            if (bowRenderer != null) bowRenderer.enabled = true;
            else if (bowObj != null) bowObj.SetActive(true);
        }
    }

    /// <summary>
    /// Hides visuals for all weapons.
    /// </summary>
    private void HideAllWeapons()
    {
        // Prefer disabling SpriteRenderer if available.
        if (swordRenderer != null) swordRenderer.enabled = false;
        else if (swordObj != null) swordObj.SetActive(false);

        if (bowRenderer != null) bowRenderer.enabled = false;
        else if (bowObj != null) bowObj.SetActive(false);
    }

    /// <summary>
    /// Handles weapon switching input (Tab key).
    /// </summary>
    private void HandleWeaponSwitching()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            weaponInUse = (weaponInUse == 0) ? 1 : 0; // Toggle 0 and 1
            ShowSelectedWeapon();
        }
    }

    /// <summary>
    /// Handles health potion usage input (H key).
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
    /// Updates the heart display UI and handles the game over state.
    /// </summary>
    private void UpdateHealthUI()
    {
        if (heart1 != null) heart1.SetActive(playerHealth >= 3);
        if (heart2 != null) heart2.SetActive(playerHealth >= 2);
        if (heart3 != null) heart3.SetActive(playerHealth >= 1);

        // Trigger game over animation and stop player animation when health is zero.
        if (playerHealth <= 0 && gameOver != null)
        {
            gameOver.Play("gameOverAnim");
            if (animator != null) animator.speed = 0;
            if (continueButton != null) continueButton.SetActive(true);
        }
    }

    /// <summary>
    /// Updates local inventory counts from PlayerPrefs.
    /// </summary>
    private void UpdateInventory()
    {
        coinCount = PlayerPrefs.GetInt("ScoreCount");
        arrowCount = PlayerPrefs.GetInt("ArrowCount");
        healthPotionCount = PlayerPrefs.GetInt("HealthPotionCount");
        UpdateUI();
    }

    /// <summary>
    /// Updates the in-game text displays for inventory items.
    /// </summary>
    public void UpdateUI()
    {
        if (inGameCoinText != null) inGameCoinText.text = coinCount.ToString();
        if (inGameArrowText != null) inGameArrowText.text = arrowCount.ToString();
        if (inGameHealthPotionText != null) inGameHealthPotionText.text = healthPotionCount.ToString();
    }

    /// <summary>
    /// Loads saved inventory counts from PlayerPrefs on startup.
    /// </summary>
    private void LoadPlayerStats()
    {
        coinCount = PlayerPrefs.GetInt("ScoreCount", 0);
        arrowCount = PlayerPrefs.GetInt("ArrowCount", 0);
        healthPotionCount = PlayerPrefs.GetInt("HealthPotionCount", 0);

        // Ensure player starts with a minimum number of arrows for testing/gameplay.
        if (arrowCount < 5)
        {
            arrowCount = 25;
            PlayerPrefs.SetInt("ArrowCount", arrowCount);
        }
    }

    // Note: OnCollisionEnter2D is currently empty but kept for potential future use (e.g., environment interactions).
    public void OnCollisionEnter2D(Collision2D collision)
    {
    }

    /// <summary>
    /// Handles trigger enter events for item pickups and entering the shop zone.
    /// </summary>
    public void OnTriggerEnter2D(Collider2D collision)
    {
        // Handle pickup of health item.
        if (collision.gameObject.CompareTag("Heart") && playerHealth < maxHealth)
        {
            playerHealth++;
            Destroy(collision.gameObject);
            // TODO: Add sound effect for heart pickup.
        }

        // Handle pickup of coins.
        if (collision.gameObject.CompareTag("Coin"))
        {
            coinCount++;
            PlayerPrefs.SetInt("ScoreCount", coinCount);
            Destroy(collision.gameObject);
            // TODO: Add sound effect for coin pickup.
        }

        // Show shop UI when entering shop area.
        if (collision.gameObject.CompareTag("Shop") && shopButtons != null)
        {
            shopButtons.SetActive(true);
        }
    }

    /// <summary>
    /// Handles trigger exit events, specifically for leaving the shop zone.
    /// </summary>
    public void OnTriggerExit2D(Collider2D collision)
    {
        // Hide shop UI when leaving shop area.
        if (collision.gameObject.CompareTag("Shop") && shopButtons != null)
        {
            shopButtons.SetActive(false);
        }
    }

    /// <summary>
    /// Coroutine providing a brief period of invulnerability after taking damage.
    /// </summary>
    IEnumerator InvulnerabilityPeriod()
    {
        yield return new WaitForSeconds(invulnerabilityTime);
        hurting = false;

        // Toggling the collider prevents physics issues if knockback caused weird interactions.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
            col.enabled = true;
        }
    }

    /// <summary>
    /// Processes damage taken by the player, applying health reduction, knockback, and invulnerability.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to take.</param> // Updated param desc
    public void TakeDamage(int damageAmount) // TODO: Use damageAmount if different enemies deal different damage.
    {
        if (!hurting && playerHealth > 0)
        {
            // TODO: Use damageAmount variable instead of decrementing by 1.
            playerHealth--;
            hurting = true; // Prevent immediate re-damage.
            StartCoroutine(InvulnerabilityPeriod());

            // Apply knockback away from the closest enemy.
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length > 0 && playerHealth > 0) // Check health > 0 again in case damage was fatal.
            {
                GameObject closestEnemy = null;
                float closestDistance = float.MaxValue;
                Vector3 currentPosition = transform.position;

                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector2.Distance(currentPosition, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }

                if (closestEnemy != null)
                {
                    Vector2 knockbackDirection = (currentPosition - closestEnemy.transform.position).normalized;
                    rb2d.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }
            // TODO: Add visual effect or sound for taking damage.
        }
    }

    /// <summary>
    /// Restarts the current level.
    /// </summary>
    public void PlayAgain()
    {
        // TODO: Consider resetting PlayerPrefs health/score here if desired, or handle via a GameManager.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void MainMenu()
    {
        // TODO: Replace with actual main menu scene loading, e.g., SceneManager.LoadScene("MainMenu");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Fallback: reload current scene.
    }

    /// <summary>
    /// Handles the purchase of an arrow from the shop.
    /// </summary>
    public void BuyArrow()
    {
        if (coinCount >= itemCost)
        {
            coinCount -= itemCost;
            arrowCount++;
            PlayerPrefs.SetInt("ScoreCount", coinCount);
            PlayerPrefs.SetInt("ArrowCount", arrowCount);
            UpdateUI();
            // TODO: Add sound effect for purchase.
        }
        // else { // Optional: Provide feedback if player cannot afford. }
    }

    /// <summary>
    /// Handles the purchase of a health potion from the shop.
    /// </summary>
    public void BuyHealthPotion()
    {
        if (coinCount >= itemCost)
        {
            coinCount -= itemCost;
            healthPotionCount++;
            PlayerPrefs.SetInt("ScoreCount", coinCount);
            PlayerPrefs.SetInt("HealthPotionCount", healthPotionCount);
            UpdateUI();
            // TODO: Add sound effect for purchase.
        }
        // else { // Optional: Provide feedback if player cannot afford. }
    }

    /// <summary>
    /// Locks or unlocks player movement, typically for UI interactions like menus or dialogues.
    /// </summary>
    /// <param name="locked">True to lock movement, false to unlock.</param>
    public void LockMovementForUI(bool locked)
    {
        movementLocked = locked;
        if (locked)
        {
            rb2d.velocity = Vector2.zero;
        }
    }

    // Alternative names for LockMovementForUI for compatibility with other scripts (e.g., teleport system).
    public void LockMovement()
    {
        LockMovementForUI(true);
    }

    public void UnlockMovement()
    {
        LockMovementForUI(false);
    }

    // Note: This method appears unused or its functionality is handled by the Animator.
    // Consider removing if not needed by external scripts.
    public void PlayIdleAnimation()
    {
    }
}
