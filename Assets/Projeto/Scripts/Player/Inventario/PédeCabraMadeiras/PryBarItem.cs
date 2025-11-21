using UnityEngine;

[CreateAssetMenu(fileName = "Novo_PeDeCabra", menuName = "Inventario/PeDeCabra")]
public class PryBarItem : ItemSistema
{
    // se quiser comportamento ao "usar" diretamente, implemente aqui.
    public override void Usar()
    {
        Debug.Log("[PryBarItem] Usado (comportamento padrão).");
        // normalmente o pé de cabra só fica no inventário e é verificado por interatores (não usado diretamente).
    }
}
