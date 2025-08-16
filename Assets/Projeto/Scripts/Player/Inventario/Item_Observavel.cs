using UnityEngine;

public class Item_Observavel : Item_Intera��o
{
    // Implementa a fun��o abstrata da base
    public override void Interagir(Movimenta��oPlayer jogador)
    {
        // Mostra mensagem na HUD
        if (HUD_Intera��o.instancia != null)
            HUD_Intera��o.instancia.MostrarMensagem(descricao);
    }
}
