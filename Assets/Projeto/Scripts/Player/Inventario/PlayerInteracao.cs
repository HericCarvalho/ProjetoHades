using UnityEngine;

public class PlayerInteracao : MonoBehaviour
{
    public float alcanceInteracao = 2f;         // apenas uma vez
    public LayerMask camadaInteracao;           // apenas uma vez
    [SerializeField] private MovimentaçãoPlayer jogador; // apenas uma vez

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, alcanceInteracao, camadaInteracao))
            {
                Item_Interativo item = hit.collider.GetComponent<Item_Interativo>();
                if (item != null)
                {
                    item.Interagir(jogador);
                }
            }
        }
    }
}
