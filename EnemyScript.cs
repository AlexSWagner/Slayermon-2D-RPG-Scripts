using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls enemy behavior including patrolling, player detection, and attacking
/// </summary>
public class EnemyScript : MonoBehaviour
{
    [Header("References")]
    public Transform enemySprite; // Reference to the enemySprite child object
    public Animator enemyAnimator; // Reference to the animator
    private SpriteRenderer enemySpriteRenderer; // SpriteRenderer component on enemySprite
    private Rigidbody2D rb2d; // Enemy rigidbody component
    private Transform playerTransform; // Reference to the player's transform
    private PlayerScript playerScript; // Reference to the player script to check health
    private BoxCollider2D enemyCollider; // Reference to the enemy's collider

    [Header("Stats")]
    public int enemyHealth = 3; // Enemy health points
    private int maxHealth = 3; // Maximum health to reset to on respawn
    public int damageAmount = 1; // How much damage enemy deals to player
    
    [Header("Movement")]
    public float moveSpeed = 5.0f; // Movement speed
    public float patrolRadius = 4f; // How far from spawn point enemy will patrol
    public float detectionRadius = 5f; // How far enemy can detect player (sight range)
    public float attackRadius = 0.1f; // Drastically reduced to make enemy get much closer (like punching distance)
    private Vector2 startPosition; // Initial spawn position
    private Vector2 patrolPoint; // Current patrol target position
    private bool hasPatrolPoint = false; // Flag to track if patrol point is set
    private int direction = 0; // 0=down, 1=right, 2=left, 3=up (same as player)
    
    [Header("Timers")]
    public float attackCooldown = 0.05f; // Reduced from 0.1f to make attacks faster
    public float attackDuration = 0.2f; // Duration of attack animation
    public float damageDelay = 0.05f; // Time into the attack animation when damage is applied
    public float respawnTime = 5f; // Time until enemy respawns after being hit
    public float stunTime = 0.5f; // Stun time when hit but not killed
    private float attackCooldownTimer = 0f; // Current attack cooldown timer
    
    [Header("Quest Information")]
    public string questTargetID = ""; // Identifier for quest targets, e.g. "Dragon"
    
    // State tracking
    private bool playerInSightRange = false;
    private bool playerInAttackRange = false;
    private bool isAttacking = false;
    private bool hasDealDamage = false;
    private bool isStunned = false;
    private bool isRespawning = false;
    private bool isDead = false;

    // Animator parameter hashes for faster access
    private readonly int MoveXHash = Animator.StringToHash("MoveX");
    private readonly int MoveYHash = Animator.StringToHash("MoveY");
    private readonly int SpeedHash = Animator.StringToHash("Speed");
    private readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        // Get components
        rb2d = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<BoxCollider2D>();
        
        // If enemySprite is null, try to find it
        if (enemySprite == null)
            enemySprite = transform.Find("enemySprite");
            
        // Get animator from parent object (this gameObject) if not assigned
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<Animator>();
            
        // Get sprite renderer
        if (enemySprite != null)
            enemySpriteRenderer = enemySprite.GetComponent<SpriteRenderer>();
            
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<PlayerScript>();
        }
        
        // Set start position
        startPosition = transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize any starting settings
        if (enemySpriteRenderer != null)
            enemySpriteRenderer.color = Color.white;
            
        // Generate first patrol point
        SearchPatrolPoint();
    }

    // Update is called once per frame
    void Update()
    {
        // Don't process AI if dead, stunned or respawning
        if (isDead || isStunned || isRespawning)
            return;

        // Update attack cooldown
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // Check if player is in sight or attack range
        if (playerTransform != null && playerScript != null && playerScript.playerHealth > 0)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            playerInSightRange = distanceToPlayer <= detectionRadius;
            playerInAttackRange = distanceToPlayer <= attackRadius;
            
            // Simple state machine like in the provided example
            if (!playerInSightRange && !playerInAttackRange)
            {
                Patrolling();
            }
            else if (playerInSightRange && !playerInAttackRange)
            {
                ChasePlayer();
            }
            else if (playerInAttackRange)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Player is null or dead, just patrol
            Patrolling();
        }
    }
    
    /// <summary>
    /// Handles patrol behavior - simplified from example script
    /// </summary>
    private void Patrolling()
    {
        if (!hasPatrolPoint)
        {
            SearchPatrolPoint();
        }

        if (hasPatrolPoint)
        {
            // Move towards patrol point
            MoveTowardsPosition(patrolPoint);
            
            // Check if we reached the patrol point
            if (Vector2.Distance(transform.position, patrolPoint) < 0.1f)
            {
                hasPatrolPoint = false;
                
                // Wait briefly at patrol point (handled by stopping movement)
                StopMovement();
                
                // Find a new patrol point after a delay
                Invoke(nameof(SearchPatrolPoint), 2f);
            }
        }
    }

    /// <summary>
    /// Generates a new random patrol point - similar to SearchWalkPoint in example
    /// </summary>
    private void SearchPatrolPoint()
    {
        // Generate random direction
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Generate random distance within patrol radius
        float randomDistance = Random.Range(patrolRadius * 0.5f, patrolRadius);
        
        // Calculate new position
        float x = Mathf.Cos(randomAngle) * randomDistance;
        float y = Mathf.Sin(randomAngle) * randomDistance;
        
        // Set new patrol point within patrol radius of start position
        patrolPoint = startPosition + new Vector2(x, y);
        hasPatrolPoint = true;
    }

    /// <summary>
    /// Chase the player - similar to ChasePlayer in example
    /// </summary>
    private void ChasePlayer()
    {
        if (playerTransform != null)
        {
            // Move directly towards player
            MoveTowardsPosition(playerTransform.position);
        }
    }

    /// <summary>
    /// Attack the player - similar to AttackPlayer in example
    /// </summary>
    private void AttackPlayer()
    {
        // Stop movement when attacking
        StopMovement();
        
        // Update facing to look at player
        if (playerTransform != null)
        {
            Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
            UpdateDirectionTowardPlayer(dirToPlayer);
        }
        
        // Start attack if cooldown is complete and not already attacking
        if (attackCooldownTimer <= 0 && !isAttacking)
        {
            Attack();
        }
    }
    
    /// <summary>
    /// Execute attack against player
    /// </summary>
    private void Attack()
    {
        // Set up attack
        isAttacking = true;
        hasDealDamage = false;
        
        // Face the player
        if (playerTransform != null)
        {
            Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
            UpdateDirectionTowardPlayer(dirToPlayer);
        }
        
        // Trigger attack animation immediately
        enemyAnimator.SetBool(IsAttackingHash, true);
        
        // Apply damage during attack
        StartCoroutine(ApplyDamageDuringAttack());
        
        // Reset attack after animation completes
        StartCoroutine(ResetAttack());
    }
    
    /// <summary>
    /// Reset attack state after animation - similar to ResetAttack in example
    /// </summary>
    private IEnumerator ResetAttack()
    {
        // Wait for attack animation to complete
        yield return new WaitForSeconds(attackDuration);
        
        // Reset animation state
        enemyAnimator.SetBool(IsAttackingHash, false);
        
        // Reset attack flags
        isAttacking = false;
        
        // Set cooldown
        attackCooldownTimer = attackCooldown;
        
        // Immediately check if we should attack again - no extra yield
        if (playerTransform != null && 
            Vector2.Distance(transform.position, playerTransform.position) <= attackRadius)
        {
            // If we're in range, attack as soon as the cooldown is over
            // The Update function will handle this automatically next frame
        }
    }

    /// <summary>
    /// Apply damage to player during attack animation
    /// </summary>
    private IEnumerator ApplyDamageDuringAttack()
    {
        // Wait for damage timing
        yield return new WaitForSeconds(damageDelay);
        
        // Check if player is within attack range and we haven't dealt damage yet
        if (!hasDealDamage && playerTransform != null && playerScript != null && 
            playerScript.playerHealth > 0 && !playerScript.hurting)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            // Only deal damage if player is within attack range
            if (distanceToPlayer <= attackRadius)
            {
                // Deal damage to player
                playerScript.TakeDamage(damageAmount);
                hasDealDamage = true;
                
                // Apply stronger knockback if player is still alive
                if (playerScript.playerHealth > 0)
                {
                    Vector2 knockbackDirection = (playerTransform.position - transform.position).normalized;
                    Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        // Increased knockback force from 7f to 12f for more noticeable effect
                        playerRb.AddForce(knockbackDirection * 12f, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Helper method to move towards a position in 2D
    /// </summary>
    private void MoveTowardsPosition(Vector2 targetPos)
    {
        // Calculate direction to target
        Vector2 moveDirection = (targetPos - (Vector2)transform.position).normalized;
        
        // Stop if too close to player to prevent pushing (only when in sight)
        float distanceToPlayer = (playerTransform != null) ? 
            Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
            
        // Reduced the minimum distance to let enemy get very close
        if (distanceToPlayer < 0.1f && playerInSightRange) // Reduced from 0.3f to allow very close approach
        {
            StopMovement();
            
            // Face the player even when stopped
            UpdateDirectionTowardPlayer((playerTransform.position - transform.position).normalized);
            return;
        }
        
        // Apply movement
        rb2d.velocity = moveDirection * moveSpeed;
        
        // Update animator parameters
        enemyAnimator.SetFloat(MoveXHash, moveDirection.x);
        enemyAnimator.SetFloat(MoveYHash, moveDirection.y);
        enemyAnimator.SetFloat(SpeedHash, rb2d.velocity.magnitude);
        
        // Update facing direction based on movement
        UpdateDirectionTowardPlayer(moveDirection);
    }
    
    /// <summary>
    /// Helper method to stop all movement
    /// </summary>
    private void StopMovement()
    {
        rb2d.velocity = Vector2.zero;
        enemyAnimator.SetFloat(SpeedHash, 0);
    }
    
    /// <summary>
    /// Update direction toward the player for consistent direction handling
    /// </summary>
    private void UpdateDirectionTowardPlayer(Vector2 dirToPlayer)
    {
        // Use absolute values to determine dominant axis
        float absX = Mathf.Abs(dirToPlayer.x);
        float absY = Mathf.Abs(dirToPlayer.y);
        
        // Determine direction based on dominant axis
        if (absX >= absY) // Use >= to prioritize horizontal on exact diagonals
        {
            // Horizontal movement is dominant
            direction = dirToPlayer.x >= 0 ? 1 : 2; // 1=right, 2=left
        }
        else
        {
            // Vertical movement is dominant
            direction = dirToPlayer.y > 0 ? 3 : 0; // 3=up, 0=down
        }
        
        // Update animator directly with direction
        Vector2 dirVector = GetDirectionVector(direction);
        enemyAnimator.SetFloat(MoveXHash, dirVector.x);
        enemyAnimator.SetFloat(MoveYHash, dirVector.y);
    }
    
    /// <summary>
    /// Converts direction index to a vector2 for animator parameters
    /// </summary>
    private Vector2 GetDirectionVector(int dir)
    {
        switch (dir)
        {
            case 0: return new Vector2(0, -1); // Down
            case 1: return new Vector2(1, 0);  // Right
            case 2: return new Vector2(-1, 0); // Left
            case 3: return new Vector2(0, 1);  // Up
            default: return Vector2.zero;
        }
    }
    
    /// <summary>
    /// Handle taking damage from player - similar to TakeDamage in example
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        // Don't take damage if already stunned or dead
        if (isStunned || isDead)
            return;
            
        // Reduce health
        enemyHealth -= damageAmount;
        
        // Check if dead
        if (enemyHealth <= 0)
        {
            Die();
        }
        else
        {
            // Only stun the enemy briefly when hit
            StartCoroutine(StunEnemy());
        }
    }
    
    /// <summary>
    /// Stun the enemy for a short time when hit
    /// </summary>
    private IEnumerator StunEnemy()
    {
        // Set stunned state
        isStunned = true;
        
        // Stop all movement
        StopMovement();
        
        // Turn enemy red to indicate hit
        if (enemySpriteRenderer != null)
            enemySpriteRenderer.color = Color.red;
            
        // Wait for stun duration
        yield return new WaitForSeconds(stunTime);
        
        // Return to normal color
        if (enemySpriteRenderer != null)
            enemySpriteRenderer.color = Color.white;
        
        // Reset stun state
        isStunned = false;
    }
    
    /// <summary>
    /// Handle enemy death and respawn - similar to DestroyEnemy in example
    /// but with respawn instead of destruction
    /// </summary>
    private void Die()
    {
        // Set state to dead
        isDead = true;
        
        // Stop all movement
        StopMovement();
        
        // Disable sprite and collider
        if (enemySpriteRenderer != null)
            enemySpriteRenderer.enabled = false;
            
        if (enemyCollider != null)
            enemyCollider.enabled = false;
        
        // Start respawn timer
        StartCoroutine(RespawnAfterDeath());
        
        // Notify quest system if this is a quest target
        if (!string.IsNullOrEmpty(questTargetID))
        {
            QuestManager questManager = FindObjectOfType<QuestManager>();
            if (questManager != null)
            {
                // Update any kill quests targeting this enemy type
                questManager.NotifyEnemyKilled(questTargetID);
            }
        }
    }
    
    /// <summary>
    /// Respawn enemy after death
    /// </summary>
    private IEnumerator RespawnAfterDeath()
    {
        // Set respawning flag
        isRespawning = true;
        
        // Wait for respawn time
        yield return new WaitForSeconds(respawnTime);
        
        // Reset enemy health
        enemyHealth = maxHealth;
        
        // Reset position to spawn point
        transform.position = startPosition;
        
        // Re-enable sprite and collider
        if (enemySpriteRenderer != null)
        {
            enemySpriteRenderer.enabled = true;
            enemySpriteRenderer.color = Color.white;
        }
            
        if (enemyCollider != null)
            enemyCollider.enabled = true;
        
        // Reset state
        isDead = false;
        isRespawning = false;
        
        // Find a new patrol point
        SearchPatrolPoint();
    }
    
    /// <summary>
    /// Draw gizmos for visualization in editor - same as example
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw patrol radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : (Vector2)transform.position, patrolRadius);
        
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
} 