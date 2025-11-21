using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public event Action<QuestEntry> OnQuestAdded;
    public event Action<QuestEntry> OnQuestCompleted;
    public event Action<QuestEntry> OnQuestRemoved; // opcional

    private readonly List<QuestEntry> activeQuests = new List<QuestEntry>();
    private readonly Dictionary<QuestSO, QuestEntry> lookup = new Dictionary<QuestSO, QuestEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // opcional
    }

    public QuestEntry AddQuest(QuestSO quest)
    {
        Debug.Log($"[QuestManager] AddQuest called for: {(quest != null ? quest.name : "NULL")}");
        if (quest == null)
        {
            Debug.LogWarning("[QuestManager] AddQuest aborted: quest == null");
            return null;
        }

        if (lookup.ContainsKey(quest))
        {
            Debug.LogWarning("[QuestManager] AddQuest aborted: quest already in lookup -> " + quest.name);
            return null;
        }

        var entry = new QuestEntry(quest);
        activeQuests.Add(entry);
        lookup.Add(quest, entry);

        Debug.Log("[QuestManager] Quest added to lists: " + quest.name + " activeCount=" + activeQuests.Count);
        OnQuestAdded?.Invoke(entry);
        Debug.Log("[QuestManager] OnQuestAdded invoked for: " + quest.name);
        return entry;
    }
    // FORÇAR: debug apenas
    public QuestEntry AddQuest_Force(QuestSO quest)
    {
        if (quest == null) return null;
        if (lookup.ContainsKey(quest))
        {
            Debug.Log("[QuestManager] Forçando re-add: removendo existente primeiro.");
            var existing = lookup[quest];
            activeQuests.Remove(existing);
            lookup.Remove(quest);
        }
        return AddQuest(quest); // chama o método normal depois de remover
    }


    public bool HasQuest(QuestSO quest)
    {
        if (quest == null) return false;
        return lookup.ContainsKey(quest);
    }

    public bool CompleteQuest(QuestSO quest)
    {
        if (quest == null) return false;
        if (!lookup.TryGetValue(quest, out QuestEntry entry)) return false;
        if (entry.completed) return false;

        entry.completed = true;
        entry.completedTime = DateTime.Now;
        OnQuestCompleted?.Invoke(entry);
        Debug.Log($"[QuestManager] Quest completada: {quest.name}");
        return true;
    }

    public bool RemoveQuest(QuestSO quest)
    {
        if (quest == null) return false;
        if (!lookup.TryGetValue(quest, out QuestEntry entry)) return false;

        lookup.Remove(quest);
        activeQuests.Remove(entry);
        OnQuestRemoved?.Invoke(entry);
        Debug.Log($"[QuestManager] Quest removida: {quest.name}");
        return true;
    }

    public List<QuestEntry> GetActiveQuests()
    {
        return new List<QuestEntry>(activeQuests);
    }

    // utilitário (opcional)
    public bool CompleteQuestByName(string questName)
    {
        var e = activeQuests.Find(x => x.quest != null && x.quest.name == questName);
        return e != null && CompleteQuest(e);
    }

    public bool CompleteQuest(QuestEntry entry)
    {
        if (entry == null || entry.completed) return false;
        entry.completed = true;
        entry.completedTime = DateTime.Now;
        OnQuestCompleted?.Invoke(entry);
        return true;
    }

}
