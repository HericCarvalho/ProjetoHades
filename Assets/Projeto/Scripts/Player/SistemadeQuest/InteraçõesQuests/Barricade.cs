using UnityEngine;

public class Barricade : MonoBehaviour
{
    public QuestSO questOnBreak;
    public string objectiveId; // "break_barricade"

    public void Break()
    {
        // anim, efeitos, som
        QuestManager.Instance?.MarkObjective(questOnBreak, objectiveId, 1);
        Destroy(gameObject); // ou animar
    }
}
