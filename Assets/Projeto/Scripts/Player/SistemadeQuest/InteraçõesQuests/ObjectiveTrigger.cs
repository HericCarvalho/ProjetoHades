using UnityEngine;

public class ObjectiveTrigger : MonoBehaviour
{
    public QuestSO quest;
    public string objectiveId;
    public bool singleUse = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.MarkObjective(quest, objectiveId, 1);
        if (singleUse) Destroy(this);
    }
}
