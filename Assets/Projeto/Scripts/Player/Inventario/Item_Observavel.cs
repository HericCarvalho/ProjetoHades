using UnityEngine;

public class Item_Observavel : Item_Interação
{
    // Implementa a função abstrata da base
    public override void Interagir(MovimentaçãoPlayer jogador)
    {
        // Mostra mensagem na HUD
        if (HUD_Interação.instancia != null)
            HUD_Interação.instancia.MostrarMensagem(descricao);
    }
}
