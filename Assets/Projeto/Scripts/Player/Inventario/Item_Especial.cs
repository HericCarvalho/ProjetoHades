using UnityEngine;

public class Item_Especial : Item_Intera��o
{
    public override void Interagir(Movimenta��oPlayer jogador)
    {
        // Dispara o evento configurado no Inspector
        onInteragir?.Invoke();
    }
}

