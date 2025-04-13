using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages quest tracking across the game
/// </summary>
public class QuestManager : MonoBehaviour
{
    // Singleton instance
    public static QuestManager Instance { get; private set; }
    
    // Quest events that other systems can subscribe to
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestUpdated;
    public event Action<Quest> OnQuestCompleted;
    
    // List of all active quests
    private List<Quest> activeQuests = new List<Quest>();
    // List of completed quests
    private List<Quest> completedQuests = new List<Quest>();
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Accept a new quest
    /// </summary>
    public void AcceptQuest(string title, string description, QuestType type, int targetAmount, string targetID = "", string initialObjective = "", string returnObjective = "")
    {
        // Check if quest is already active
        if (IsQuestActive(title))
            return;
        
        // Check if quest is already completed
        if (IsQuestCompleted(title))
            return;
        
        // Create new quest
        Quest newQuest = new Quest(title, description, type, targetAmount);
        
        // Set additional parameters if provided
        if (!string.IsNullOrEmpty(targetID))
            newQuest.targetID = targetID;
        
        if (!string.IsNullOrEmpty(initialObjective))
            newQuest.initialObjective = initialObjective;
        
        if (!string.IsNullOrEmpty(returnObjective))
            newQuest.returnObjective = returnObjective;
        
        // Update the objective text
        newQuest.UpdateObjective();
        
        // Add to active quests
        activeQuests.Add(newQuest);
        
        // Trigger event
        OnQuestAccepted?.Invoke(newQuest);
        
        Debug.Log($"Quest accepted: {title}");
    }
    
    /// <summary>
    /// Update the progress of a quest
    /// </summary>
    public void UpdateQuestProgress(string questTitle, int amount)
    {
        Quest quest = activeQuests.Find(q => q.title == questTitle);
        
        if (quest != null)
        {
            quest.currentAmount += amount;
            
            // Cap at target amount
            if (quest.currentAmount > quest.targetAmount)
            {
                quest.currentAmount = quest.targetAmount;
            }
            
            // Check if objective is complete but quest isn't turned in yet
            if (quest.currentAmount >= quest.targetAmount && quest.currentStage == Quest.QuestStage.InProgress)
            {
                // Change to ready to complete
                quest.currentStage = Quest.QuestStage.ReadyToComplete;
                quest.UpdateObjective();
            }
            else
            {
                // Just update the progress text
                quest.UpdateObjective();
            }
            
            // Trigger update event
            OnQuestUpdated?.Invoke(quest);
        }
    }
    
    /// <summary>
    /// Complete a quest
    /// </summary>
    public void CompleteQuest(string questTitle)
    {
        Quest quest = activeQuests.Find(q => q.title == questTitle);
        
        if (quest != null)
        {
            // Mark as completed
            quest.completed = true;
            quest.currentStage = Quest.QuestStage.Completed;
            quest.UpdateObjective();
            
            // Move to completed quests
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
            
            // Trigger event
            OnQuestCompleted?.Invoke(quest);
            
            Debug.Log($"Quest completed: {questTitle}");
            
            // You could add reward distribution logic here
            GiveQuestReward(quest);
        }
    }
    
    /// <summary>
    /// Check if a quest is active
    /// </summary>
    public bool IsQuestActive(string questTitle)
    {
        return activeQuests.Exists(q => q.title == questTitle);
    }
    
    /// <summary>
    /// Check if a quest is completed
    /// </summary>
    public bool IsQuestCompleted(string questTitle)
    {
        return completedQuests.Exists(q => q.title == questTitle);
    }
    
    /// <summary>
    /// Get a quest by title (from active or completed quests)
    /// </summary>
    public Quest GetQuest(string questTitle)
    {
        Quest quest = activeQuests.Find(q => q.title == questTitle);
        if (quest != null)
            return quest;
        
        return completedQuests.Find(q => q.title == questTitle);
    }
    
    /// <summary>
    /// Get all active quests
    /// </summary>
    public List<Quest> GetActiveQuests()
    {
        return new List<Quest>(activeQuests);
    }
    
    /// <summary>
    /// Get all completed quests
    /// </summary>
    public List<Quest> GetCompletedQuests()
    {
        return new List<Quest>(completedQuests);
    }
    
    /// <summary>
    /// Get progress on a specific quest
    /// </summary>
    public float GetQuestProgress(string questTitle)
    {
        Quest quest = GetQuest(questTitle);
        if (quest != null)
            return quest.GetProgress();
        
        return 0f;
    }

    // Handle enemy kills for quests
    public void NotifyEnemyKilled(string enemyID)
    {
        // Find all active kill quests targeting this enemy
        foreach (Quest quest in activeQuests)
        {
            if (quest.type == QuestType.Kill && 
                quest.targetID.Equals(enemyID, StringComparison.OrdinalIgnoreCase) &&
                quest.currentStage == Quest.QuestStage.InProgress)
            {
                // Update progress
                UpdateQuestProgress(quest.title, 1);
            }
        }
    }

    // Method to give quest rewards
    private void GiveQuestReward(Quest quest)
    {
        // Find player to give rewards
        PlayerScript player = FindObjectOfType<PlayerScript>();
        if (player != null)
        {
            // Give a fixed reward of 100 coins
            int coinReward = 100;
            
            // Add coins to player
            int currentCoins = PlayerPrefs.GetInt("ScoreCount", 0);
            PlayerPrefs.SetInt("ScoreCount", currentCoins + coinReward);
            
            // Update player UI
            player.UpdateUI();
            
            Debug.Log($"Rewarded player with {coinReward} coins for completing quest: {quest.title}");
        }
    }
}

/// <summary>
/// Types of quests available in the game
/// </summary>
public enum QuestType
{
    Fetch,       // Collect or find items
    Kill,        // Defeat enemies
    Talk,        // Talk to NPCs
    Escort,      // Escort an NPC to a location
    Explore     // Discover a location
}

/// <summary>
/// Represents a quest with progress tracking
/// </summary>
[System.Serializable]
public class Quest
{
    public string title;
    public string description;
    public QuestType type;
    public int targetAmount;
    public int currentAmount;
    public bool completed;
    
    // For tracking quest progress stages
    public enum QuestStage
    {
        NotStarted,
        InProgress,
        ReadyToComplete,
        Completed
    }
    
    public QuestStage currentStage = QuestStage.NotStarted;
    
    // For kill quests
    public string targetID = "";
    
    // Current objective text - changes based on stage
    public string currentObjective;
    public string initialObjective;
    public string returnObjective;
    
    public Quest(string title, string description, QuestType type, int targetAmount)
    {
        this.title = title;
        this.description = description;
        this.type = type;
        this.targetAmount = targetAmount;
        this.currentAmount = 0;
        this.completed = false;
        this.currentStage = QuestStage.InProgress;
        
        // Set default objectives
        this.initialObjective = $"Complete task ({currentAmount}/{targetAmount})";
        this.returnObjective = "Return to quest giver";
        this.currentObjective = this.initialObjective;
    }
    
    // Updated progress calculation
    public float GetProgress()
    {
        return Mathf.Clamp01((float)currentAmount / targetAmount);
    }
    
    // Update the objective text based on current stage
    public void UpdateObjective()
    {
        switch (currentStage)
        {
            case QuestStage.InProgress:
                currentObjective = $"{initialObjective} ({currentAmount}/{targetAmount})";
                break;
            case QuestStage.ReadyToComplete:
                currentObjective = returnObjective;
                break;
            case QuestStage.Completed:
                currentObjective = "Completed!";
                break;
        }
    }
} 