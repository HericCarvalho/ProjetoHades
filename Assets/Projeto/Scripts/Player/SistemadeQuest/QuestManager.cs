using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public event Action<QuestEntry> OnQuestAdded;
    public event Action<QuestEntry> OnQuestCompleted;
    public event Action<QuestEntry> OnQuestRemoved;
    public event Action<QuestEntry, string, int, int> OnObjectiveUpdated;

    private readonly List<QuestEntry> activeQuests = new List<QuestEntry>();
    private readonly List<QuestEntry> completedQuests = new List<QuestEntry>();
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

        // marca como completa
        entry.MarkCompleted();

        // remove das listas ativas e lookup, adiciona em completed
        lookup.Remove(quest);
        activeQuests.Remove(entry);
        completedQuests.Add(entry);

        OnQuestCompleted?.Invoke(entry);
        Debug.Log($"[QuestManager] Quest completada: {quest.name} at {entry.completedTime}");
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
    public bool CompleteQuestByName(string questName)
    {
        var e = activeQuests.Find(x => x.quest != null && x.quest.name == questName);
        if (e == null) return false;
        return CompleteQuest(e);
    }
    public bool CompleteQuest(QuestEntry entry)
    {
        if (entry == null || entry.completed) return false;

        entry.MarkCompleted();

        // remove das listas ativas e lookup se existir
        if (entry.quest != null)
        {
            lookup.Remove(entry.quest);
        }
        activeQuests.Remove(entry);
        completedQuests.Add(entry);

        OnQuestCompleted?.Invoke(entry);
        Debug.Log($"[QuestManager] Quest completada: {entry?.quest?.name ?? "null"} at {entry.completedTime}");
        return true;
    }
    public bool IsQuestCompleted(QuestSO quest)
    {
        if (quest == null) return false;
        // procura por quest nas completedQuests
        for (int i = 0; i < completedQuests.Count; i++)
        {
            var e = completedQuests[i];
            if (e != null && e.quest == quest) return true;
        }
        return false;
    }
    public bool MarkObjective(QuestSO quest, string objectiveId, int amount = 1)
    {
        if (!lookup.TryGetValue(quest, out QuestEntry entry)) return false;
        // garante que exista
        if (!entry.objectiveProgress.ContainsKey(objectiveId))
            entry.objectiveProgress[objectiveId] = 0;

        entry.objectiveProgress[objectiveId] += amount;

        // pega objetivo do QuestSO
        var od = quest.objectives?.Find(x => x.id == objectiveId);
        int target = od != null ? od.targetAmount : 1;
        int progress = entry.objectiveProgress[objectiveId];

        // se atingiu o target, marca como completed
        if (progress >= target)
            entry.objectivesCompleted.Add(objectiveId);

        OnObjectiveUpdated?.Invoke(entry, objectiveId, progress, target);

        // checa se TODOS objetivos completos -> completa quest
        bool allDone = true;
        if (quest.objectives != null)
        {
            foreach (var o in quest.objectives)
            {
                if (!entry.objectivesCompleted.Contains(o.id))
                {
                    allDone = false;
                    break;
                }
            }
        }
        if (allDone)
            CompleteQuest(quest);

        return true;
    }

}
