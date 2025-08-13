using UnityEngine;  

public class MovimentaçãoPlayer : MonoBehaviour
{
    [Header("Configuração de Movimento")]
    [SerializeField] private float velocidade;                  // Velocidade normal
    [SerializeField] private float velocidadeAgachado;          // Velocidade agachado
    [SerializeField] private float VelocidadeCorrendo;          // Velocidade correndo
    [SerializeField] private float PuloForça;                   // Força do pulo
    [SerializeField] private float MouseSensibilidade;          // Sensibilidade do mouse

    [Header("Câmera")]
    [SerializeField] private Transform cameraReferencia;        // Referência à câmera
    [SerializeField] private float AlturaEmPé;                  // Altura em pé
    [SerializeField] private float AlturaAgachado;              // Altura agachado
    [SerializeField] private float VelocidadeTransição;         // Velocidade da transição da altura

    [Header("Stamina")]
    [SerializeField] public float MaxEstamina;                 // Quantos segundos correndo até acabar
    [SerializeField] private float RegeneraçãoEstamina;         // Velocidade de recuperação por segundo
    [SerializeField] private float EstaminaGasta;               // Velocidade de gasto por segundo

    private float EstaminaAtual;                                // Stamina atual
    private bool podeCorrer = true;                             // Pode correr?

    private float RotaçãoVertical;
    private Rigidbody RB;
    private bool isGrounded = true;
    private bool isCrouching = false;

    void Start()
    {
        RB = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        EstaminaAtual = MaxEstamina;                           // Começa com stamina cheia
    }

    void Update()
    {
        // ===== Rotação com mouse =====
        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        RotaçãoVertical -= mouseY;
        RotaçãoVertical = Mathf.Clamp(RotaçãoVertical, -80f, 80f);
        cameraReferencia.localRotation = Quaternion.Euler(RotaçãoVertical, 0f, 0f);

        // ===== Pulo =====
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            RB.AddForce(Vector3.up * PuloForça, ForceMode.Impulse);
            isGrounded = false;
        }

        // ===== Agachar (Toggle com Ctrl) =====
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }

        // ===== Sistema de Stamina =====
        bool tryingToRun = Input.GetKey(KeyCode.LeftShift) && !isCrouching && EstaminaAtual > 0;
        if (tryingToRun)
        {
            EstaminaAtual -= EstaminaGasta * Time.deltaTime;
            if (EstaminaAtual <= 0)
            {
                EstaminaAtual = 0;
                podeCorrer = false;
            }
        }
        else
        {
            EstaminaAtual += RegeneraçãoEstamina * Time.deltaTime;
            if (EstaminaAtual >= MaxEstamina)
            {
                EstaminaAtual = MaxEstamina;
                podeCorrer = true;
            }
        }

        // ===== Altura da câmera =====
        float targetHeight = isCrouching ? AlturaAgachado : AlturaEmPé;
        Vector3 cameraPos = cameraReferencia.localPosition;
        cameraPos.y = Mathf.Lerp(cameraPos.y, targetHeight, Time.deltaTime * VelocidadeTransição);
        cameraReferencia.localPosition = cameraPos;
    }

    void FixedUpdate()
    {
        // ===== Movimento =====
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = velocidade;
        if (isCrouching)
        {
            currentSpeed = velocidadeAgachado;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && podeCorrer && EstaminaAtual > 0)
        {
            currentSpeed = VelocidadeCorrendo;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        RB.MovePosition(RB.position + move * currentSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }

    // Método para pegar a stamina atual (para UI futuramente)
    public float GetCurrentStamina()
    {
        return EstaminaAtual;
    }
}