using UnityEngine;

/// <summary>
/// CollectibleItem: adiciona item ao inventário, pode ativar uma quest ao coletar
/// e/ou avançar um objetivo. Não destrói antes de garantir que foi adicionado.
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    [Header("Inventário")]
    public ItemSistema itemToAdd;          // optional: o item que será colocado no inventário
    public int amountToAdd = 1;

    [Header("Progredir Objetivo")]
    public QuestSO questToAdvance;
    public string objectiveId;

    [Header("Ativar Missão ao Coletar")]
    public QuestSO questToGiveOnCollect;

    [Header("Comportamento")]
    public bool removeOnCollect = true;

    [Header("Debug")]
    public bool debugLogs = true;

    public void Collect()
    {
        if (debugLogs) Debug.Log($"[CollectibleItem] Collect() chamado em '{gameObject.name}'. questToGive={(questToGiveOnCollect != null ? questToGiveOnCollect.name : "NULL")} questToAdvance={(questToAdvance != null ? questToAdvance.name : "NULL")} objectiveId={objectiveId} itemToAdd={(itemToAdd != null ? itemToAdd.nomeItem : "NULL")}");

        // 0) adicionar ao inventário primeiro (se houver item configurado)
        if (itemToAdd != null && SistemaInventario.instancia != null)
        {
            SistemaInventario.instancia.AdicionarItem(itemToAdd, amountToAdd);
            if (debugLogs) Debug.Log($"[CollectibleItem] Item '{itemToAdd.nomeItem}' adicionado ao inventário x{amountToAdd}.");
        }

        // 1) ativar quest ao coletar (se configurado) - faz AddQuest
        if (questToGiveOnCollect != null && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.HasQuest(questToGiveOnCollect))
            {
                QuestManager.Instance.AddQuest(questToGiveOnCollect);
                if (debugLogs) Debug.Log($"[CollectibleItem] Missão ativada ao coletar: " + questToGiveOnCollect.name);
            }
            else
            {
                if (debugLogs) Debug.Log($"[CollectibleItem] Missão já ativa: " + questToGiveOnCollect.name);
            }
        }

        // 2) marcar objetivo (se configurado)
        if (questToAdvance != null && !string.IsNullOrEmpty(objectiveId) && QuestManager.Instance != null)
        {
            bool res = QuestManager.Instance.MarkObjective(questToAdvance, objectiveId, 1);
            if (debugLogs) Debug.Log($"[CollectibleItem] MarkObjective chamado: quest={questToAdvance.name} obj={objectiveId} -> result={res}");
        }

        // 3) Som e destruição (após tudo)
        if (removeOnCollect)
        {
            // efeito sonoro no local (se houver)
            var somColeta = GetComponent<ItemInterativo>() != null ? GetComponent<ItemInterativo>().somColeta : null;
            if (somColeta != null)
                AudioSource.PlayClipAtPoint(somColeta, transform.position);

            if (debugLogs) Debug.Log($"[CollectibleItem] Removendo GameObject '{gameObject.name}' do mundo.");
            Destroy(gameObject);
        }
    }
}
