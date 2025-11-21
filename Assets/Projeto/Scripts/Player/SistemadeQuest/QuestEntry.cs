using System;

[Serializable]
public class QuestEntry
{
    public QuestSO quest;
    public bool completed = false;
    public DateTime addedTime;
    public DateTime completedTime;

    public QuestEntry(QuestSO q)
    {
        quest = q;
        completed = false;
        addedTime = DateTime.Now;
    }
}
