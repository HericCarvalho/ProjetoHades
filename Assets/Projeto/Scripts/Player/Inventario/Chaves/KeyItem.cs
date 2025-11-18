using UnityEngine;

[CreateAssetMenu(menuName = "Itens/Chave", fileName = "NewKey")]
public class KeyItem : ItemSistema
{
    [Tooltip("ID numérico da chave. A porta checa esse ID para destravar.")]
    public int keyID = 1;

    // Implementa o método abstrato obrigatório da sua arquitetura
    public override void Usar()
    {
        // Comportamento padrão: nada. (uso direto de chave raramente faz sentido)
        Debug.Log($"[KeyItem] Usada {nomeItem} (keyID={keyID}) — sem ação padrão.");
    }
}
