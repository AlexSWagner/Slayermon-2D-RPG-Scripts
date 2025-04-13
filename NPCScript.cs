using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCScript : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Villager";
    public float interactionRadius = 1.5f;

    [Header("Dialogue")]
    [TextArea(3, 5)]
    public string[] dialogueLines;
    
    [Header("Quest")]
    public bool hasQuest = false;
    [TextArea(2, 3)]
    public string questTitle = "";
    [TextArea(3, 5)]
    public string questDescription = "";
    public QuestType questType = QuestType.Fetch;
    public int questTargetAmount = 1;
    public string questTargetID = ""; // ID of target (e.g., "Dragon" for kill quests)

    [Header("Quest Objectives")]
    [TextArea(3, 5)]
    public string initialObjective = ""; // Default shown in first stage (will be auto-generated if empty)
    [TextArea(3, 5)]
    public string returnObjective = "Return to quest giver"; // Shown after primary objective is complete
    
    [Header("Quest Status")]
    [SerializeField] private bool questCompleted = false;
    [TextArea(3, 5)]
    public string[] questCompletedDialogue; // Dialogue when turning in quest
    [TextArea(3, 5)]
    public string[] postQuestDialogue; // Dialogue after quest is completed and reward received
    [TextArea(3, 5)]
    public string[] questAcceptedDialogue;
    [TextArea(3, 5)]
    public string[] questDeclinedDialogue;
    
    // References
    private Transform playerTransform;
    private bool playerInRange = false;
    private DialogueManager dialogueManager;
    private QuestManager questManager;
    private bool questActive = false;
    
    // Optional components
    private Animator npcAnimator;
    private SpriteRenderer npcRenderer;
    private int direction = 0; // 0=down, 1=right, 2=left, 3=up
    
    // Animation hashes (only needed if using animations)
    private readonly int MoveXHash = Animator.StringToHash("MoveX");
    private readonly int MoveYHash = Animator.StringToHash("MoveY");
    private readonly int TalkingHash = Animator.StringToHash("Talking");
    
    void Awake()
    {
        // Try to get components if they exist
        npcAnimator = GetComponent<Animator>();
        npcRenderer = GetComponent<SpriteRenderer>();
        
        // Find the dialogue manager
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogWarning("No DialogueManager found in scene. Please add a DialogueManager to use dialogue features.");
        }
        
        // Find the quest manager
        questManager = FindObjectOfType<QuestManager>();
        if (questManager == null && hasQuest)
        {
            Debug.LogWarning("No QuestManager found in scene. Please add a QuestManager to use quest features.");
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }
    
    void Start()
    {
        // Check if the quest is already active
        if (hasQuest && questManager != null)
        {
            questActive = questManager.IsQuestActive(questTitle);
            questCompleted = questManager.IsQuestCompleted(questTitle);
        }
    }
    
    void Update()
    {
        // Skip if no player or dialogue manager
        if (playerTransform == null || dialogueManager == null) return;
        
        // Check if player is in range
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = distanceToPlayer <= interactionRadius;
        
        // Show/hide interaction prompt when player enters/exits range
        if (playerInRange && !wasInRange)
        {
            dialogueManager.ShowInteractionPrompt(transform.position + Vector3.up * 0.5f);
        }
        else if (!playerInRange && wasInRange)
        {
            dialogueManager.HideInteractionPrompt();
        }
        
        // If player is in range and presses interaction key, start dialogue
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            StartInteraction();
        }
        
        // If player is in range, face the player
        if (playerInRange && npcAnimator != null)
        {
            FacePlayer();
        }
    }
    
    private void FacePlayer()
    {
        // Calculate direction to player
        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        
        // Determine dominant axis
        float absX = Mathf.Abs(dirToPlayer.x);
        float absY = Mathf.Abs(dirToPlayer.y);
        
        if (absX >= absY)
        {
            // Horizontal is dominant
            direction = dirToPlayer.x >= 0 ? 1 : 2; // 1=right, 2=left
        }
        else
        {
            // Vertical is dominant
            direction = dirToPlayer.y >= 0 ? 3 : 0; // 3=up, 0=down
        }
        
        // Update animator
        if (npcAnimator != null)
        {
            Vector2 dirVector = GetDirectionVector(direction);
            npcAnimator.SetFloat(MoveXHash, dirVector.x);
            npcAnimator.SetFloat(MoveYHash, dirVector.y);
        }
    }
    
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
    
    private void StartInteraction()
    {
        // Different dialogue based on quest status
        if (hasQuest)
        {
            if (questCompleted)
            {
                // Quest already completed
                StartRegularDialogue(questCompletedDialogue.Length > 0 ? questCompletedDialogue : dialogueLines);
            }
            else if (questActive)
            {
                // Quest is active but not completed
                CheckQuestProgress();
            }
            else
            {
                // Offer quest
                StartQuestOffer();
            }
        }
        else
        {
            // Regular NPC with no quest
            StartRegularDialogue(dialogueLines);
        }
    }
    
    private void StartRegularDialogue(string[] lines)
    {
        if (dialogueManager != null && lines != null && lines.Length > 0)
        {
            // Start talking animation if we have one
            if (npcAnimator != null)
            {
                npcAnimator.SetBool(TalkingHash, true);
            }
            
            // Start dialogue
            dialogueManager.StartDialogue(npcName, lines, OnDialogueEnd);
            
            // Hide interaction prompt while talking
            dialogueManager.HideInteractionPrompt();
        }
    }
    
    private void StartQuestOffer()
    {
        if (dialogueManager != null && dialogueLines.Length > 0)
        {
            // Start talking animation if we have one
            if (npcAnimator != null)
            {
                npcAnimator.SetBool(TalkingHash, true);
            }
            
            // Start quest dialogue with choice at the end
            dialogueManager.StartQuestDialogue(
                npcName,
                dialogueLines, 
                AcceptQuest, 
                DeclineQuest, 
                OnDialogueEnd);
            
            // Hide interaction prompt while talking
            dialogueManager.HideInteractionPrompt();
        }
    }
    
    private void CheckQuestProgress()
    {
        // Check if quest is now ready to complete (target reached but not turned in)
        if (questManager != null)
        {
            Quest quest = questManager.GetQuest(questTitle);
            
            if (quest != null)
            {
                if (quest.currentStage == Quest.QuestStage.ReadyToComplete)
                {
                    // Quest is ready to turn in - complete it and give reward
                    questManager.CompleteQuest(questTitle);
                    questCompleted = true;
                    
                    // Show completion dialogue
                    StartRegularDialogue(questCompletedDialogue.Length > 0 ? 
                        questCompletedDialogue : 
                        new string[] { "Thank you for completing my quest! Here's your reward of 100 gold coins." });
                }
                else if (quest.completed || questCompleted)
                {
                    // Quest was already completed - show post-completion dialogue
                    questCompleted = true;
                    StartRegularDialogue(postQuestDialogue.Length > 0 ? 
                        postQuestDialogue : 
                        new string[] { "Thanks again for your help! The village is much safer now." });
                }
                else
                {
                    // Quest still in progress
                    StartRegularDialogue(questAcceptedDialogue.Length > 0 ? 
                        questAcceptedDialogue : 
                        new string[] { $"Have you {(questType == QuestType.Kill ? "defeated" : "found")} the {questTargetID} yet?" });
                }
            }
        }
    }
    
    private void AcceptQuest()
    {
        if (questManager != null)
        {
            // Add quest to player's quest log with target ID and custom objectives
            questManager.AcceptQuest(
                questTitle, 
                questDescription, 
                questType, 
                questTargetAmount,
                questTargetID,
                string.IsNullOrEmpty(initialObjective) ? 
                    $"{(questType == QuestType.Kill ? "Kill" : "Collect")} {questTargetAmount} {questTargetID}" : 
                    initialObjective,
                returnObjective
            );
            
            questActive = true;
            
            // Show acceptance dialogue if available
            if (questAcceptedDialogue.Length > 0)
            {
                StartRegularDialogue(questAcceptedDialogue);
            }
        }
    }
    
    private void DeclineQuest()
    {
        // Show decline dialogue if available
        if (questDeclinedDialogue.Length > 0)
        {
            StartRegularDialogue(questDeclinedDialogue);
        }
    }
    
    private void OnDialogueEnd()
    {
        // Stop talking animation
        if (npcAnimator != null)
        {
            npcAnimator.SetBool(TalkingHash, false);
        }
        
        // Show interaction prompt again if player is still in range
        if (playerInRange)
        {
            dialogueManager.ShowInteractionPrompt(transform.position + Vector3.up * 0.5f);
        }
    }
    
    // Use this to complete a quest from elsewhere in code (e.g., when player delivers an item)
    public void CompleteQuest()
    {
        if (questManager != null && !questCompleted && hasQuest)
        {
            questManager.CompleteQuest(questTitle);
            questCompleted = true;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw interaction radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
} 