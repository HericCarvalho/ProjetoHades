using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    public QuestSO questSO;
    public bool shouldGiveOnlyOnce = true;

    [TextArea] public string messageOnGive = "Nova tarefa registrada.";

    private bool given = false;

    public bool TryGive()
    {
        if (questSO == null) return false;
        if (given && shouldGiveOnlyOnce) return false;

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestGiver] QuestManager não encontrado na cena.");
            return false;
        }

        if (!QuestManager.Instance.HasQuest(questSO))
        {
            var entry = QuestManager.Instance.AddQuest(questSO);
            if (entry != null)
            {
                if (!string.IsNullOrEmpty(messageOnGive))
                    HUD_Interacao.instancia?.MostrarMensagem(messageOnGive);

                given = true;
                return true;
            }
        }
        else
        {
            // já tem a quest
            return false;
        }

        return false;
    }
}
