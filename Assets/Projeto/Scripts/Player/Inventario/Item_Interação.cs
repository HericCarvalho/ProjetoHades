using UnityEngine;
using UnityEngine.Events;

// Tipos de interação disponíveis
public enum TipoInteracao
{
    Observavel,  // Ler ou examinar
    Coletavel,   // Pegar e enviar para inventário
    Especial     // Evento custom, ex: portas ou alavancas
}

// Classe base para todos os itens interativos
public abstract class Item_Interação : MonoBehaviour
{
    [Header("Tipo de Interação")]
    public TipoInteracao tipo = TipoInteracao.Observavel;

    [Header("Observável")]
    [TextArea] public string descricao;  // Texto exibido na HUD

    [Header("Coletável")]
    public ItemSO itemColetavel;         // ScriptableObject do item que será coletado

    [Header("Especial")]
    public UnityEvent onInteragir;       // Evento para ações especiais

    // Método chamado pelo jogador ao apertar "E" olhando para este objeto
    public abstract void Interagir(MovimentaçãoPlayer jogador);
}
