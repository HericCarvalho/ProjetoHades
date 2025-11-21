using UnityEngine;

public class RoomEventTrigger : MonoBehaviour
{
    public AudioClip loudNoise;
    public GameObject invertedCrossPrefab;
    public Transform spawnPoint;

    private bool triggered = false;

    // chame quando o jogador sair da sala (p.ex., OnTriggerExit)
    private void OnTriggerExit(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        // toca som
        if (loudNoise != null) AudioSource.PlayClipAtPoint(loudNoise, transform.position);
        // spawn cross (ao retornar você verá)
        if (invertedCrossPrefab != null && spawnPoint != null)
            Instantiate(invertedCrossPrefab, spawnPoint.position, spawnPoint.rotation);

        triggered = true;
    }
}
