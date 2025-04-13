using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject continueButton;
    public GameObject interactionPrompt;
    
    [Header("Quest Choice UI")]
    public GameObject choicePanel;
    public Button acceptButton;
    public Button declineButton;
    
    [Header("Settings")]
    public float typingSpeed = 0.02f;
    public bool useTypewriterEffect = true;
    
    // Private variables
    private Queue<string> dialogueLines = new Queue<string>();
    private bool isDisplayingDialogue = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Action onDialogueEnd;
    private Action onAcceptQuest;
    private Action onDeclineQuest;
    private bool isQuestDialogue = false;
    private PlayerMovement playerMovement;
    private float lastContinueTime = 0f;
    private const float CONTINUE_COOLDOWN = 0.2f;
    
    private void Awake()
    {
        // Hide UI elements on start
        dialoguePanel.SetActive(false);
        interactionPrompt.SetActive(false);
        
        if (choicePanel != null)
            choicePanel.SetActive(false);
            
        // Find the player movement script
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        
        // Set up button listeners
        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptChoice);
            
        if (declineButton != null)
            declineButton.onClick.AddListener(OnDeclineChoice);
    }
    
    // Start a regular dialogue sequence
    public void StartDialogue(string npcName, string[] lines, Action onEnd = null)
    {
        // Clear any previous dialogue
        dialogueLines.Clear();
        isQuestDialogue = false;
        
        // Setup dialogue
        nameText.text = npcName;
        
        // Add all lines to the queue
        foreach (string line in lines)
        {
            dialogueLines.Enqueue(line);
        }
        
        // Show dialogue panel
        dialoguePanel.SetActive(true);
        isDisplayingDialogue = true;
        
        // Lock player movement
        if (playerMovement != null)
            playerMovement.LockMovementForDialogue(true);
            
        // Store callback
        onDialogueEnd = onEnd;
        
        // Display first line
        DisplayNextLine();
    }
    
    // Start a quest dialogue with choice options
    public void StartQuestDialogue(string npcName, string[] lines, Action onAccept, Action onDecline, Action onEnd = null)
    {
        // Set up regular dialogue first
        StartDialogue(npcName, lines, onEnd);
        
        // Mark this as quest dialogue
        isQuestDialogue = true;
        
        // Store callbacks
        onAcceptQuest = onAccept;
        onDeclineQuest = onDecline;
    }
    
    // Display the next line in the queue
    public void DisplayNextLine()
    {
        // Check for cooldown to prevent rapid clicking
        if (Time.time - lastContinueTime < CONTINUE_COOLDOWN)
            return;
            
        lastContinueTime = Time.time;
        
        // If there's a choice active, don't advance
        if (choicePanel != null && choicePanel.activeSelf)
            return;
            
        // If we're currently typing, complete the line immediately
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
                
            dialogueText.text = dialogueLines.Peek();
            isTyping = false;
            return;
        }
        
        // If there are no more lines, end the dialogue
        if (dialogueLines.Count == 0)
        {
            // If this is quest dialogue and we're at the end, show the choice
            if (isQuestDialogue && !choicePanel.activeSelf)
            {
                ShowChoicePanel();
                return;
            }
            
            EndDialogue();
            return;
        }
        
        // Get the next line and display it
        string line = dialogueLines.Dequeue();
        
        if (useTypewriterEffect)
        {
            // Start typing effect
            typingCoroutine = StartCoroutine(TypeLine(line));
        }
        else
        {
            // Display the full line immediately
            dialogueText.text = line;
        }
    }
    
    // Coroutine for typewriter effect
    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in line.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
    }
    
    // Show the choice panel for quest dialogue
    private void ShowChoicePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            continueButton.SetActive(false);
        }
    }
    
    // Handle accept choice
    private void OnAcceptChoice()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
            
        if (continueButton != null)
            continueButton.SetActive(true);
            
        // Call the accept callback
        onAcceptQuest?.Invoke();
        
        // End dialogue - don't add another "Quest accepted!" line
        // The quest accepted dialogue is already being shown by the NPCScript
        EndDialogue();
    }
    
    // Handle decline choice
    private void OnDeclineChoice()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
            
        if (continueButton != null)
            continueButton.SetActive(true);
            
        // Call the decline callback
        onDeclineQuest?.Invoke();
        
        // End dialogue
        EndDialogue();
    }
    
    // End the dialogue sequence
    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isDisplayingDialogue = false;
        
        // Unlock player movement
        if (playerMovement != null)
            playerMovement.LockMovementForDialogue(false);
            
        // Make sure choice panel is hidden
        if (choicePanel != null)
            choicePanel.SetActive(false);
            
        // Reset quest dialogue flag
        isQuestDialogue = false;
        
        // Call the end callback
        onDialogueEnd?.Invoke();
    }
    
    // Show interaction prompt at a position
    public void ShowInteractionPrompt(Vector3 position)
    {
        // Only show prompt if no dialogue is active
        if (!isDisplayingDialogue)
        {
            interactionPrompt.SetActive(true);
            interactionPrompt.transform.position = Camera.main.WorldToScreenPoint(position);
        }
    }
    
    // Hide interaction prompt
    public void HideInteractionPrompt()
    {
        interactionPrompt.SetActive(false);
    }
} 