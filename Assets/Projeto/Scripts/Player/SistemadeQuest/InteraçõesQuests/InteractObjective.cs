using UnityEngine;

public class InteractObjective : MonoBehaviour
{
    [Header("Objetivo principal")]
    public QuestSO quest;             
    public string objectiveId;        

    [Header("Comportamento")]
    public bool requireQuestToBeActive = true;
    public bool completeDirectly = false;

    [Header("Follow-up (adicionar outra quest)")]
    public QuestSO questToAddAfter = null;
    public bool onlyAddAfterCompletion = true;

    [Header("Debug")]
    public bool debugLogs = true;

    public void OnInteracted()
    {
        if (QuestManager.Instance == null)
        {
            if (debugLogs) Debug.LogWarning("[InteractObjective] QuestManager.Instance == null");
            return;
        }

        if (debugLogs) Debug.Log($"[InteractObjective] OnInteracted() called on '{gameObject.name}' - quest={(quest != null ? quest.name : "NULL")} objectiveId='{objectiveId}'");

        bool progressed = false;
        bool completed = false;

        // Se não há quest principal configurada, possivelmente só queremos adicionar followup (raro)
        if (quest == null)
        {
            if (debugLogs) Debug.Log("[InteractObjective] Nenhuma quest principal configurada. Apenas tratar follow-up se configurada.");
            TryAddFollowupIfNeeded(proceeded: false, completed: false);
            return;
        }

        // Se exigir que a quest esteja ativa, valida
        if (requireQuestToBeActive && !QuestManager.Instance.HasQuest(quest))
        {
            if (debugLogs) Debug.Log($"[InteractObjective] Jogador NÃO possui a quest '{quest.name}' ativa. requireQuestToBeActive={requireQuestToBeActive}. Abortando progresso.");
            return;
        }

        // Progredir / completar
        if (completeDirectly)
        {
            // CompleteQuest(QuestSO)
            progressed = QuestManager.Instance.CompleteQuest(quest);
            completed = progressed;
            if (debugLogs) Debug.Log($"[InteractObjective] CompleteQuest chamado para '{quest.name}' -> result={progressed}");
        }
        else
        {
            // MarkObjective
            progressed = QuestManager.Instance.MarkObjective(quest, objectiveId, 1);
            if (debugLogs) Debug.Log($"[InteractObjective] MarkObjective chamado para '{quest?.name}' obj='{objectiveId}' -> result={progressed}");

            if (progressed)
            {
                // se a quest foi completada como efeito colateral, HasQuest deve ser false
                completed = QuestManager.Instance.IsQuestCompleted(quest);
                if (debugLogs) Debug.Log($"[InteractObjective] Após MarkObjective: HasQuest('{quest.name}') = {QuestManager.Instance.HasQuest(quest)} -> completed={completed}");
            }
        }

        // Se progrediu/completou, adiciona follow-up conforme configuração
        TryAddFollowupIfNeeded(proceeded: progressed, completed: completed);
    }

    private void TryAddFollowupIfNeeded(bool proceeded, bool completed)
    {
        if (questToAddAfter == null)
        {
            if (debugLogs) Debug.Log("[InteractObjective] Nenhum follow-up configurado.");
            return;
        }

        bool shouldAdd = onlyAddAfterCompletion ? completed : proceeded;
        if (!shouldAdd)
        {
            if (debugLogs) Debug.Log($"[InteractObjective] Decidido NÃO adicionar follow-up '{questToAddAfter.name}' (shouldAdd=false). proceeded={proceeded} completed={completed}");
            return;
        }

        // Se já tem, não tenta adicionar
        if (QuestManager.Instance.HasQuest(questToAddAfter))
        {
            if (debugLogs) Debug.Log($"[InteractObjective] Follow-up já ativa: {questToAddAfter.name}");
            return;
        }

        // Tenta AddQuest normalmente
        if (debugLogs) Debug.Log($"[InteractObjective] Tentando adicionar follow-up quest: {questToAddAfter.name}");
        var added = QuestManager.Instance.AddQuest(questToAddAfter);

        if (added == null)
        {
            if (debugLogs) Debug.LogWarning($"[InteractObjective] AddQuest retornou null para '{questToAddAfter.name}'. Tentando AddQuest_Force() como fallback.");
            // tenta forçar re-add (remove + add)
            var forced = QuestManager.Instance.AddQuest_Force(questToAddAfter);
            if (forced != null)
                Debug.Log($"[InteractObjective] AddQuest_Force bem sucedido para '{questToAddAfter.name}'");
            else
                Debug.LogError($"[InteractObjective] AddQuest_Force falhou para '{questToAddAfter.name}'. Verifique QuestSO e QuestManager.");
        }
        else
        {
            if (debugLogs) Debug.Log($"[InteractObjective] Follow-up adicionada com sucesso: {questToAddAfter.name}");
        }
    }
}
