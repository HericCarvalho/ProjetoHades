using UnityEngine;

public enum TipoItem
{
    Especial,       // Itens ligados a miss�es, hist�ria ou progress�o
    ColetavelConsumivel,      // Consum�veis ou de uso recorrente (ex: bateria, rem�dio, comida)
    Interativo      // Itens fixos no cen�rio (ex: placas, portas, objetos de lore)
}

[CreateAssetMenu(fileName = "NovoItem", menuName = "Inventario/Item")]
public class ItemSO : ScriptableObject
{

    [Header("Informa��es B�sicas")]
    public string nomeItem;
    public Sprite iconeItem;
    [TextArea] public string descricaoItem;

    [Header("Configura��o")]
    public TipoItem tipoItem;       // Define o tipo de item
    public bool empilhavel = false; // Ex: baterias podem ser v�rias, mas di�rio n�o
    public int quantidadeMax = 1;   // Se empilh�vel, define limite da pilha
    public bool dropavel = false;   // S� faz sentido em colet�veis na sua vis�o

    [Header("Intera��es Especiais")]
    public bool podeUsar = false;   // Se pode ser usado diretamente do invent�rio
    public bool abreJanela = false; // Para itens interativos/lore
    public GameObject prefabMundo;  // Se tiver representa��o f�sica no mundo (ex: colet�vel)

    [Header("Par�metros Personalizados")]
    public int valorInt;            // Exemplo: quantidade de energia de uma bateria
    public string valorString;      // Exemplo: ID de um item especial para miss�es


}
