using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestJournal : MonoBehaviour
{
    [Header("UI References")]
    public GameObject journalPanel;               // Main journal panel
    public GameObject questEntryPrefab;           // Prefab for quest entries
    public Transform questListContent;            // Parent transform for quest entries
    public TextMeshProUGUI journalTitle;          // Title text
    public TextMeshProUGUI emptyJournalText;      // Text to show when no quests are active
    
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.J;         // Key to toggle journal visibility
    
    // References
    private QuestManager questManager;
    private Dictionary<string, GameObject> questEntries = new Dictionary<string, GameObject>();
    
    // Internal state
    private bool isInitialized = false;
    
    void Start()
    {
        // Find the quest manager
        questManager = FindObjectOfType<QuestManager>();
        if (questManager == null)
        {
            Debug.LogError("QuestJournal could not find QuestManager!");
            return;
        }
        
        // Hide journal initially
        if (journalPanel != null)
            journalPanel.SetActive(false);
            
        // Subscribe to quest events
        questManager.OnQuestAccepted += OnQuestAccepted;
        questManager.OnQuestUpdated += OnQuestUpdated;
        questManager.OnQuestCompleted += OnQuestCompleted;
        
        // Initialize interface
        InitializeJournal();
    }
    
    void Update()
    {
        // Toggle journal with J key
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleJournal();
        }
    }
    
    private void InitializeJournal()
    {
        if (isInitialized || questManager == null)
            return;
            
        // Add any active quests
        List<Quest> activeQuests = questManager.GetActiveQuests();
        foreach (Quest quest in activeQuests)
        {
            AddQuestToJournal(quest);
        }
        
        // Update "no quests" message visibility
        UpdateEmptyJournalText();
        
        isInitialized = true;
    }
    
    public void ToggleJournal()
    {
        if (journalPanel == null) return;
        
        // Toggle visibility
        journalPanel.SetActive(!journalPanel.activeSelf);
        
        // If opening, refresh quest list
        if (journalPanel.activeSelf)
        {
            RefreshQuestList();
        }
        
        // Lock player movement when journal is open
        PlayerScript player = FindObjectOfType<PlayerScript>();
        if (player != null)
        {
            // Alternative way to lock movement without direct method call
            player.movementLocked = journalPanel.activeSelf;
            
            // Stop movement immediately if locking
            if (journalPanel.activeSelf)
            {
                player.rb2d.velocity = Vector2.zero;
            }
        }
    }
    
    private void RefreshQuestList()
    {
        // Clear existing entries
        foreach (GameObject entry in questEntries.Values)
        {
            Destroy(entry);
        }
        questEntries.Clear();
        
        // Add current quests
        List<Quest> activeQuests = questManager.GetActiveQuests();
        foreach (Quest quest in activeQuests)
        {
            AddQuestToJournal(quest);
        }
        
        // Update empty journal text
        UpdateEmptyJournalText();
    }
    
    private void AddQuestToJournal(Quest quest)
    {
        if (questEntryPrefab == null || questListContent == null) return;
        
        // Create a new entry
        GameObject entryObj = Instantiate(questEntryPrefab, questListContent);
        
        // Set up the entry UI
        TextMeshProUGUI titleText = entryObj.transform.Find("QuestTitle")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI objectiveText = entryObj.transform.Find("QuestObjective")?.GetComponent<TextMeshProUGUI>();
        Image progressBar = entryObj.transform.Find("ProgressBar")?.GetComponent<Image>();
        
        if (titleText != null)
            titleText.text = quest.title;
            
        if (objectiveText != null)
            objectiveText.text = quest.currentObjective;
            
        if (progressBar != null)
            progressBar.fillAmount = quest.GetProgress();
            
        // Store the entry
        questEntries[quest.title] = entryObj;
        
        // Update empty journal text
        UpdateEmptyJournalText();
    }
    
    private void UpdateQuestInJournal(Quest quest)
    {
        if (!questEntries.TryGetValue(quest.title, out GameObject entryObj))
        {
            // If the entry doesn't exist, add it
            AddQuestToJournal(quest);
            return;
        }
        
        // Update the existing entry
        TextMeshProUGUI objectiveText = entryObj.transform.Find("QuestObjective")?.GetComponent<TextMeshProUGUI>();
        Image progressBar = entryObj.transform.Find("ProgressBar")?.GetComponent<Image>();
        
        if (objectiveText != null)
            objectiveText.text = quest.currentObjective;
            
        if (progressBar != null)
        {
            // Ensure progress bar is filled when the quest is ready to complete
            if (quest.currentStage == Quest.QuestStage.ReadyToComplete)
                progressBar.fillAmount = 1.0f;
            else
                progressBar.fillAmount = quest.GetProgress();
        }
    }
    
    private void RemoveQuestFromJournal(Quest quest)
    {
        if (!questEntries.TryGetValue(quest.title, out GameObject entryObj))
            return;
            
        // Remove the entry
        Destroy(entryObj);
        questEntries.Remove(quest.title);
        
        // Update empty journal text
        UpdateEmptyJournalText();
    }
    
    private void UpdateEmptyJournalText()
    {
        if (emptyJournalText == null) return;
        
        // Show "No active quests" text if there are no quests
        emptyJournalText.gameObject.SetActive(questEntries.Count == 0);
    }
    
    // Event handlers
    private void OnQuestAccepted(Quest quest)
    {
        AddQuestToJournal(quest);
    }
    
    private void OnQuestUpdated(Quest quest)
    {
        UpdateQuestInJournal(quest);
        
        // Force refresh if journal is open
        if (journalPanel != null && journalPanel.activeSelf)
        {
            RefreshQuestList();
        }
    }
    
    private void OnQuestCompleted(Quest quest)
    {
        RemoveQuestFromJournal(quest);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (questManager != null)
        {
            questManager.OnQuestAccepted -= OnQuestAccepted;
            questManager.OnQuestUpdated -= OnQuestUpdated;
            questManager.OnQuestCompleted -= OnQuestCompleted;
        }
    }
} 