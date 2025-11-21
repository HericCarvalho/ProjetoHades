public enum QuestObjectiveType
{
    Interact,    // interagir com algo (ex: abrir caixa)
    Collect,     // coletar item (coletável)
    EnterArea,   // entrar em trigger
    UseItem,     // usar item em algo (colocar fusivel)
    Sequence,    // sequencia (pressionar alavancas)
    Destroy,     // destruir / quebrar barricada
    Spawn        // spawn de algo
}

[System.Serializable]
public class QuestObjectiveData
{
    public string id;                 // identificador único (ex: "get_cellphone")
    public QuestObjectiveType type;
    public string targetTagOrId;      // ex: "CellphoneItem" ou "FuseBox01"
    public int targetAmount = 1;      // para objetivos de contagem
    public bool autoComplete = false; // se true, completa ao detectar (para spawn auto)
}
