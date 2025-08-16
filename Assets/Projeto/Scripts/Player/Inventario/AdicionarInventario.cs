using UnityEngine;

public class PlayerInteracao : MonoBehaviour
{
    public float alcanceInteracao = 2f;
    public LayerMask camadaInteracao;
    [SerializeField] private Movimenta��oPlayer jogador;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, alcanceInteracao, camadaInteracao))
            {
                Item_Intera��o item = hit.collider.GetComponent<Item_Intera��o>();
                if (item != null)
                {
                    item.Interagir(jogador);
                }
            }
        }
    }
}

