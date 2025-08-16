using UnityEngine;

public class Item_Coletavel : Item_Interação
{
    public override void Interagir(MovimentaçãoPlayer jogador)
    {
        // Adiciona ao inventário do jogador
        if (jogador != null && itemColetavel != null)
        {
            jogador.AdicionarAoInventario(itemColetavel);

            // Mostra notificação na HUD
            if (HUD_Interação.instancia != null)
                HUD_Interação.instancia?.MostrarNotificacao($"Pegou {itemColetavel.nomeItem}", itemColetavel.iconeItem);

            // Remove o objeto do cenário
            Destroy(gameObject);
        }
    }
}

