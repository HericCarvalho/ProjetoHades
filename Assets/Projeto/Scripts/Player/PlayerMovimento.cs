using UnityEngine;

public class PlayerMovimento : MonoBehaviour
{
    [Header("Configuração de Movimento")]
    [SerializeField] private float velocidade = 5f;                  // Velocidade normal
    [SerializeField] private float velocidadeAgachado = 2.5f;          // Velocidade agachado
    [SerializeField] private float PuloForça = 5f;              // Força do pulo
    [SerializeField] private float MouseSensibilidade = 2f;       // Sensibilidade do mouse

    [Header("Câmera")]
    [SerializeField] private Transform cameraReferencia;         // Referência à câmera
    [SerializeField] private float AlturaEmPé = 0.8f;          // Altura em pé
    [SerializeField] private float AlturaAgachado = 1.0f;         // Altura agachado
    [SerializeField] private float VelocidadeTransição = 5f;  // Velocidade da transição da altura

    private float RotaçaõVertical = 0f;      // Armazena rotação vertical
    
    private bool isGrounded = true;           // Verifica se está no chão
    private bool isCrouching = false;         // Estado do agachamento

    private Rigidbody RB;

    void Start()
    {
        RB = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // Trava o mouse no centro
    }

    void Update()
    {
        // ===== Rotação com mouse =====
        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        RotaçaõVertical -= mouseY;
        RotaçaõVertical = Mathf.Clamp(RotaçaõVertical, -80f, 80f);
        cameraReferencia.localRotation = Quaternion.Euler(RotaçaõVertical, 0f, 0f);

        // ===== Pulo =====
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            RB.AddForce(Vector3.up * PuloForça, ForceMode.Impulse);
            isGrounded = false;
        }

        // ===== Agachar (Toggle com Shift) =====
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isCrouching = !isCrouching; // Alterna estado
        }

        // Transição suave de altura da câmera
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

        float currentSpeed = isCrouching ? velocidadeAgachado : velocidade;

        Vector3 move = transform.right * x + transform.forward * z;
        RB.MovePosition(RB.position + move * currentSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detecta se tocou o chão
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }
}