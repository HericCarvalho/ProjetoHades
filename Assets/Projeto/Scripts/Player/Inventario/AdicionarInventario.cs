using UnityEngine;

public class PlayerInteracao : MonoBehaviour
{
    public float alcanceInteracao = 2f;
    public LayerMask camadaInteracao;
    [SerializeField] private MovimentaçãoPlayer jogador;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, alcanceInteracao, camadaInteracao))
            {
                Item_Interação item = hit.collider.GetComponent<Item_Interação>();
                if (item != null)
                {
                    item.Interagir(jogador);
                }
            }
        }
    }
}

