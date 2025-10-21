using UnityEngine;

public class MenuCameraMovement : MonoBehaviour
{
    [Header("Caminho da câmera")]
    public Transform[] pontos; // Adicione aqui os pontos no inspetor
    public float velocidade = 1.5f;
    public float tempoEspera = 2f;

    private int indiceAtual = 0;
    private float tempoParado = 0f;

    void Update()
    {
        if (pontos.Length == 0) return;

        Transform alvo = pontos[indiceAtual];
        transform.position = Vector3.MoveTowards(transform.position, alvo.position, velocidade * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, alvo.rotation, Time.deltaTime * velocidade);

        if (Vector3.Distance(transform.position, alvo.position) < 0.1f)
        {
            tempoParado += Time.deltaTime;
            if (tempoParado >= tempoEspera)
            {
                indiceAtual = (indiceAtual + 1) % pontos.Length;
                tempoParado = 0f;
            }
        }
    }
}
