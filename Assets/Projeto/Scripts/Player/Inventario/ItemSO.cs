using UnityEngine;

public enum TipoItem
{
    Especial,       // Itens ligados a missões, história ou progressão
    ColetavelConsumivel,      // Consumíveis ou de uso recorrente (ex: bateria, remédio, comida)
    Interativo      // Itens fixos no cenário (ex: placas, portas, objetos de lore)
}

[CreateAssetMenu(fileName = "NovoItem", menuName = "Inventario/Item")]
public class ItemSO : ScriptableObject
{

    [Header("Informações Básicas")]
    public string nomeItem;
    public Sprite iconeItem;
    [TextArea] public string descricaoItem;

    [Header("Configuração")]
    public TipoItem tipoItem;       // Define o tipo de item
    public bool empilhavel = false; // Ex: baterias podem ser várias, mas diário não
    public int quantidadeMax = 1;   // Se empilhável, define limite da pilha
    public bool dropavel = false;   // Só faz sentido em coletáveis na sua visão

    [Header("Interações Especiais")]
    public bool podeUsar = false;   // Se pode ser usado diretamente do inventário
    public bool abreJanela = false; // Para itens interativos/lore
    public GameObject prefabMundo;  // Se tiver representação física no mundo (ex: coletável)

    [Header("Parâmetros Personalizados")]
    public int valorInt;            // Exemplo: quantidade de energia de uma bateria
    public string valorString;      // Exemplo: ID de um item especial para missões


}
