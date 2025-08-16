using UnityEngine;

public class Item_Coletavel : Item_Intera��o
{
    public override void Interagir(Movimenta��oPlayer jogador)
    {
        // Adiciona ao invent�rio do jogador
        if (jogador != null && itemColetavel != null)
        {
            jogador.AdicionarAoInventario(itemColetavel);

            // Mostra notifica��o na HUD
            if (HUD_Intera��o.instancia != null)
                HUD_Intera��o.instancia?.MostrarNotificacao($"Pegou {itemColetavel.nomeItem}", itemColetavel.iconeItem);

            // Remove o objeto do cen�rio
            Destroy(gameObject);
        }
    }
}

