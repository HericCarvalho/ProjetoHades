using UnityEngine;

public class ItemOutline : MonoBehaviour
{
    private Renderer rend;

    [Header("Configuração do Contorno")]
    public float velocidade = 2f;       // Velocidade da pulsação
    public float magnitude = 1f;        // Amplitude da pulsação
    public float intensidadeBase = 1f;  // Força inicial do contorno

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    private void Update()
    {
        // cálculo da pulsação (respiração do brilho)
        float intensidade = Mathf.Sin(Time.time * velocidade) * magnitude + 1f;

        // aplica no shader (nome tem que ser igual ao da Blackboard do Shader Graph)
        rend.material.SetFloat("_Intensidade", intensidade * intensidadeBase);
    }
}
