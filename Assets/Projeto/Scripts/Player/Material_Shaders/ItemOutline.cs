using UnityEngine;

public class ItemOutline : MonoBehaviour
{
    private Renderer rend;

    [Header("Configura��o do Contorno")]
    public float velocidade = 2f;       // Velocidade da pulsa��o
    public float magnitude = 1f;        // Amplitude da pulsa��o
    public float intensidadeBase = 1f;  // For�a inicial do contorno

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    private void Update()
    {
        // c�lculo da pulsa��o (respira��o do brilho)
        float intensidade = Mathf.Sin(Time.time * velocidade) * magnitude + 1f;

        // aplica no shader (nome tem que ser igual ao da Blackboard do Shader Graph)
        rend.material.SetFloat("_Intensidade", intensidade * intensidadeBase);
    }
}
