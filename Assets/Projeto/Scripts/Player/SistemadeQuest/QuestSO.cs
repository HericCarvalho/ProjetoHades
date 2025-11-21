using System.Collections.Generic;
using UnityEngine;

public enum QuestType { Principal, Ramificacao, Secundaria }

[CreateAssetMenu(menuName = "Quest/QuestSO")]
public class QuestSO : ScriptableObject
{
    [TextArea(3, 6)]
    public string descricao;

    public QuestType tipo;

    public List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();
}
