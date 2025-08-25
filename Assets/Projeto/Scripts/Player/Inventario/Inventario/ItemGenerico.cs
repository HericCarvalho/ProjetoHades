using UnityEngine;

[CreateAssetMenu(fileName = "NovoItemGenerico", menuName = "Inventario/Item Gen�rico")]
public class ItemGenerico : ItemSistema
{
    public override void Usar()
    {
        HUD_Interacao.instancia.MostrarNotificacao("Novo item adquirido!", iconeItem);
    }
}
