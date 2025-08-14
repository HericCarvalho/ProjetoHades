using UnityEngine;

public class MovimentaçãoPlayer : MonoBehaviour
{
    [Header("Configuração de Movimento")]
    [SerializeField] private float velocidade;              // Velocidade normal do jogador
    [SerializeField] private float velocidadeAgachado;      // Velocidade quando está agachado
    [SerializeField] private float VelocidadeCorrendo;      // Velocidade ao correr
    [SerializeField] private float velocidadeCansado;       // Velocidade reduzida quando cansado
    [SerializeField] private float PuloForça;               // Força aplicada ao pular
    [SerializeField] private float MouseSensibilidade;      // Sensibilidade do mouse para rotação

    [Header("Câmera")]
    [SerializeField] private Transform cameraReferencia;    // Transform da câmera
    [SerializeField] private float AlturaEmPé;              // Altura normal da câmera
    [SerializeField] private float AlturaAgachado;          // Altura quando agachado
    [SerializeField] private float VelocidadeTransição;     // Velocidade da interpolação da altura da câmera

    [Header("Stamina")]
    [SerializeField] public float MaxEstamina;              // Quantidade máxima de estamina
    [SerializeField] private float RegeneraçãoEstamina;     // Velocidade de recuperação por segundo
    [SerializeField] private float EstaminaGasta;           // Velocidade de gasto por segundo ao correr

    private float EstaminaAtual;                            // Estamina atual
    private bool podeCorrer = true;                         // Bool para permitir corrida

    private float RotaçãoVertical;                          // Controle da rotação vertical da câmera
    private Rigidbody RB;                                   // Rigidbody para movimento físico
    private bool isGrounded = true;                         // Verifica se jogador está no chão
    private bool isCrouching = false;                       // Estado agachado
    private bool isCansado = false;                         // Estado cansado (estamina zerada)

    void Start()
    {
        RB = GetComponent<Rigidbody>();                     // Obtém o Rigidbody
        Cursor.lockState = CursorLockMode.Locked;           // Trava o cursor no centro da tela
        EstaminaAtual = MaxEstamina;                        // Inicializa com estamina cheia
    }

    void Update()
    {
        // ===== Rotação do jogador com o mouse =====
        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);              // Rotação horizontal do corpo

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        RotaçãoVertical -= mouseY;                          // Rotação vertical da câmera
        RotaçãoVertical = Mathf.Clamp(RotaçãoVertical, -80f, 80f);      // Limita para não virar de cabeça para baixo
        cameraReferencia.localRotation = Quaternion.Euler(RotaçãoVertical, 0f, 0f);

        // ===== Pulo =====
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            RB.AddForce(Vector3.up * PuloForça, ForceMode.Impulse);     // Aplica força de pulo
            isGrounded = false;                                         // Jogador agora está no ar
        }

        // ===== Agachar (toggle com Ctrl) =====
        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCrouching = !isCrouching;                                 // Alterna estado agachado

        // ===== Sistema de Estamina e Estado Cansado =====
        bool tentandoCorrer = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isCansado;

        if (tentandoCorrer)
        {
            EstaminaAtual -= EstaminaGasta * Time.deltaTime;            // Reduz estamina enquanto corre

            if (EstaminaAtual <= 0f)
            {
                EstaminaAtual = 0f;                                     // Garante que não fique negativa
                isCansado = true;                                       // Jogador entra no estado cansado
                podeCorrer = false;                                     // Não pode mais correr
            }
        }
        else
        {
            EstaminaAtual += RegeneraçãoEstamina * Time.deltaTime;      // Recupera estamina
            if (EstaminaAtual >= MaxEstamina)
            {
                EstaminaAtual = MaxEstamina;                            // Garante limite máximo
                isCansado = false;                                      // Sai do estado cansado
                podeCorrer = true;                                      // Pode correr novamente
            }
        }

        // ===== Altura da câmera (suavização) =====
        float targetHeight = isCrouching ? AlturaAgachado : AlturaEmPé;
        Vector3 cameraPos = cameraReferencia.localPosition;
        cameraPos.y = Mathf.Lerp(cameraPos.y, targetHeight, Time.deltaTime * VelocidadeTransição); // Interpola suavemente
        cameraReferencia.localPosition = cameraPos;
    }

    void FixedUpdate()
    {
        float x = Input.GetAxis("Horizontal"); // Movimento lateral
        float z = Input.GetAxis("Vertical");   // Movimento frontal

        // ===== Determina velocidade atual do jogador =====
        float currentSpeed;

        if (isCrouching)
            currentSpeed = velocidadeAgachado;                          // Velocidade reduzida agachado
        else if (Input.GetKey(KeyCode.LeftShift) && !isCansado && EstaminaAtual > 0)
            currentSpeed = VelocidadeCorrendo;                          // Velocidade aumentada correndo
        else if (isCansado)                                             
            currentSpeed = velocidadeCansado;                           // Velocidade reduzida quando cansado
        else                                                            
            currentSpeed = velocidade;                                  // Velocidade normal
                                                                        
        Vector3 move = transform.right * x + transform.forward * z;     // Calcula vetor de movimento
        RB.MovePosition(RB.position + move * currentSpeed * Time.fixedDeltaTime);   // Aplica movimento
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detecta se o jogador tocou o chão
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }

    // ===== Métodos públicos para UI =====

    // Retorna a estamina atual
    public float GetCurrentStamina()
    {
        return EstaminaAtual;
    }

    // Retorna true se o jogador estiver cansado
    public bool IsCansado()
    {
        return isCansado;
    }
}
