using UnityEngine;

public class Item_Especial : Item_Interação
{
    public override void Interagir(MovimentaçãoPlayer jogador)
    {
        // Dispara o evento configurado no Inspector
        onInteragir?.Invoke();
    }
}

