using UnityEngine;
using UnityEngine.UI;

public class UI_Estamina : MonoBehaviour
{
    [Header("Referências ao jogador e componentes da UI")]
    [SerializeField] private MovimentaçãoPlayer player;                 // Script do jogador, para acessar estamina e estado cansado
    [SerializeField] private Image fillImage;                           // A barra interna que se enche/esvazia
    [SerializeField] private Image borderImage;                         // Borda da barra, usada para pulsar
    [SerializeField] private CanvasGroup canvasGroup;                   // CanvasGroup para controlar transparência (fade in/out)

    [Header("Velocidade do Fade")]
    [SerializeField] private float VelocidadeAparecendo = 4f;           // Velocidade do fade-in (quando a barra aparece)
    [SerializeField] private float VelocidadeDesaparecendo = 6f;        // Velocidade do fade-out (quando a barra desaparece)
    [SerializeField] private float Delay = 1.5f;                        // Tempo que a barra permanece visível após último uso

    [Header("Suavização do Preenchimento")]
    [SerializeField] private float VelocidadePreenchimento = 8f;        // Velocidade para suavizar o preenchimento da barra

    [Header("Cores")]
    [SerializeField] private Color corAlta = Color.green;               // Cor quando estamina alta
    [SerializeField] private Color corMedia = Color.yellow;             // Cor quando estamina média
    [SerializeField] private Color corBaixa = Color.red;                // Cor quando estamina baixa
    [SerializeField] private float thresholdBaixo = 0.2f;               // Percentual considerado baixo (20%)

    [Header("Pulso Energético")]
    [SerializeField] private float intensidadePulsoNormal = 0.03f;      // Intensidade de pulso padrão
    [SerializeField] private float intensidadePulsoBaixa = 0.08f;       // Intensidade quando cansado ou crítico
    [SerializeField] private float intensidadePulsoCheia = 0.15f;       // Intensidade quando estamina cheia
    [SerializeField] private float velocidadePulso = 2f;                // Velocidade de oscilação do pulso

    [Header("Pulso da Borda")]
    [SerializeField] private float intensidadeBordaNormal = 0.05f;      // Pulso da borda padrão
    [SerializeField] private float intensidadeBordaCheia = 0.12f;       // Pulso da borda cheia
    [SerializeField] private float intensidadeBordaBaixa = 0.15f;       // Pulso da borda baixa/cansado

    private float UltimavezUsado;                                       // Armazena a hora da última utilização da estamina
    private float fillSuave;                                            // Valor interpolado para suavizar preenchimento da barra

    void Start()
    {
        canvasGroup.alpha = 0f;                                         // Inicialmente a barra está invisível
        fillSuave = 1f;                                                 // Começa preenchida
    }

    void Update()
    {
        // Percentual de estamina atual (0 a 1)
        float staminaPercent = player.GetCurrentStamina() / player.MaxEstamina;
        bool cansado = player.IsCansado();  // Retorna true se o jogador estiver no estado cansado

        // ===== Preenchimento suavizado =====
        fillSuave = Mathf.Lerp(fillSuave, staminaPercent, Time.deltaTime * VelocidadePreenchimento);
        fillImage.fillAmount = fillSuave; // Atualiza visual da barra

        // ===== Fade in/out da barra =====
        if (staminaPercent < 1f)
        {
            UltimavezUsado = Time.time; // Atualiza hora do último uso
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * VelocidadeAparecendo); // Aparece
        }
        else
        {
            // Se passou o tempo de delay, inicia fade-out
            if (Time.time > UltimavezUsado + Delay)
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * VelocidadeDesaparecendo);
        }

        // ===== Determinar cor base da barra =====
        Color corBase;
        if (cansado)
        {
            // Se cansado, a barra fica avermelhada
            corBase = Color.Lerp(corBaixa, corMedia, 0.3f);
        }
        else if (staminaPercent > 0.5f)
            corBase = Color.Lerp(corMedia, corAlta, (staminaPercent - 0.5f) / 0.5f);
        else if (staminaPercent > thresholdBaixo)
            corBase = Color.Lerp(corBaixa, corMedia, (staminaPercent - thresholdBaixo) / (0.5f - thresholdBaixo));
        else
            corBase = corBaixa;

        // ===== Pulso da barra =====
        float intensidadeAtual = cansado ? intensidadePulsoBaixa :
                                 (staminaPercent >= 1f ? intensidadePulsoCheia : intensidadePulsoNormal);

        float pulso = (Mathf.Sin(Time.time * velocidadePulso * Mathf.PI * 2) * 0.5f + 0.5f) * intensidadeAtual;
        fillImage.color = Color.Lerp(corBase, Color.white, pulso); // Interpolação entre cor base e branco

        // ===== Pulso da borda =====
        if (borderImage != null)
        {
            // Intensidade da borda varia de acordo com estado
            float intensidadeBordaAtual = cansado ? intensidadeBordaBaixa :
                                          (staminaPercent >= 1f ? intensidadeBordaCheia : intensidadeBordaNormal);

            // Calcula pulso oscilante
            float pulsoBorda = (Mathf.Sin(Time.time * velocidadePulso * Mathf.PI * 2) * 0.5f + 0.5f) * intensidadeBordaAtual;

            // Cor da borda: vermelho se cansado, branco caso contrário
            Color corBordaBase = cansado ? Color.red : Color.white;

            // Aplica interpolação entre transparente e cor da borda
            borderImage.color = Color.Lerp(Color.clear, corBordaBase, pulsoBorda);
        }
    }
}