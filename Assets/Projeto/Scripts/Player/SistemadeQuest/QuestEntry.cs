using System;
using System.Collections.Generic;

[Serializable]
public class QuestEntry
{
    public QuestSO quest;
    public bool completed = false;
    public System.DateTime addedTime;
    public DateTime? completedTime;

    // progresso por objective id
    public Dictionary<string, int> objectiveProgress = new Dictionary<string, int>();
    public HashSet<string> objectivesCompleted = new HashSet<string>();

    public QuestEntry(QuestSO q)
    {
        quest = q;
        completed = false;
        addedTime = System.DateTime.Now;
        completedTime = null;

        // inicializa progresso se houver dados no questSO
        if (q != null && q.objectives != null)
        {
            foreach (var o in q.objectives)
                objectiveProgress[o.id] = 0;
        }
    }

    public bool IsObjectiveComplete(string id, QuestObjectiveData od = null)
    {
        if (objectivesCompleted.Contains(id)) return true;
        if (od != null)
            return objectiveProgress.TryGetValue(id, out int p) && p >= od.targetAmount;
        if (objectiveProgress.TryGetValue(id, out int val)) return val > 0;
        return false;
    }
    public void MarkCompleted()
    {
        completed = true;
        completedTime = DateTime.Now;
    }
}
