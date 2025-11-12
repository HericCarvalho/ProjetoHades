using UnityEngine;

public class ColetarCelular : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            HUD_Interacao hud = FindObjectOfType<HUD_Interacao>();
            if (hud != null)
            {
                hud.PegarCelular();
                Debug.Log("Celular coletado pelo jogador.");
            }

            gameObject.SetActive(false);
        }
    }
}
