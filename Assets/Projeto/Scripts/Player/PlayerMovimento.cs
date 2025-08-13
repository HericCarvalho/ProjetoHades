using UnityEngine;

public class PlayerMovimento : MonoBehaviour
{
    [Header("Configura��o de Movimento")]
    [SerializeField] private float velocidade = 5f;                  // Velocidade normal
    [SerializeField] private float velocidadeAgachado = 2.5f;          // Velocidade agachado
    [SerializeField] private float PuloFor�a = 5f;              // For�a do pulo
    [SerializeField] private float MouseSensibilidade = 2f;       // Sensibilidade do mouse

    [Header("C�mera")]
    [SerializeField] private Transform cameraReferencia;         // Refer�ncia � c�mera
    [SerializeField] private float AlturaEmP� = 0.8f;          // Altura em p�
    [SerializeField] private float AlturaAgachado = 1.0f;         // Altura agachado
    [SerializeField] private float VelocidadeTransi��o = 5f;  // Velocidade da transi��o da altura

    private float Rota�a�Vertical = 0f;      // Armazena rota��o vertical
    
    private bool isGrounded = true;           // Verifica se est� no ch�o
    private bool isCrouching = false;         // Estado do agachamento

    private Rigidbody RB;

    void Start()
    {
        RB = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // Trava o mouse no centro
    }

    void Update()
    {
        // ===== Rota��o com mouse =====
        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        Rota�a�Vertical -= mouseY;
        Rota�a�Vertical = Mathf.Clamp(Rota�a�Vertical, -80f, 80f);
        cameraReferencia.localRotation = Quaternion.Euler(Rota�a�Vertical, 0f, 0f);

        // ===== Pulo =====
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            RB.AddForce(Vector3.up * PuloFor�a, ForceMode.Impulse);
            isGrounded = false;
        }

        // ===== Agachar (Toggle com Shift) =====
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isCrouching = !isCrouching; // Alterna estado
        }

        // Transi��o suave de altura da c�mera
        float targetHeight = isCrouching ? AlturaAgachado : AlturaEmP�;
        Vector3 cameraPos = cameraReferencia.localPosition;
        cameraPos.y = Mathf.Lerp(cameraPos.y, targetHeight, Time.deltaTime * VelocidadeTransi��o);
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
        // Detecta se tocou o ch�o
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }
}