using UnityEngine;
using UnityEngine.Events;

public class MovimentaçãoPlayer : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidade;
    [SerializeField] private float velocidadeAgachado;
    [SerializeField] private float VelocidadeCorrendo;
    [SerializeField] private float velocidadeCansado;
    [SerializeField] private float PuloForca;
    [SerializeField] private float MouseSensibilidade;

    [Header("Câmera")]
    [SerializeField] private Transform cameraReferencia;
    [SerializeField] private float AlturaEmPé;
    [SerializeField] private float AlturaAgachado;
    [SerializeField] private float VelocidadeTransição;

    [Header("Headbob (balanço da câmera)")]
    [SerializeField] private float intensidadeVertical = 0.05f;
    [SerializeField] private float intensidadeHorizontal = 0.03f;
    [SerializeField] private float intensidadeRotacao = 1.5f;
    [SerializeField] private float velocidadeBob = 6f;
    [SerializeField] private float intensidadeTremor = 0.01f;
    private float contadorMovimento;
    private Vector3 posicaoInicialCamera;
    private Quaternion rotacaoInicialCamera;
    private bool passoTocado;

    [Header("Sons de passos")]
    [SerializeField] private AudioSource audioPassos;
    [SerializeField] private AudioClip somPasso;

    [Header("Stamina")]
    [SerializeField] public float MaxEstamina;
    [SerializeField] private float RegeneracaoEstamina;
    [SerializeField] private float EstaminaGasta;

    [Header("Interação")]
    [SerializeField] private float alcanceInteracao = 2f;
    [SerializeField] private KeyCode teclaInteragir = KeyCode.E;

    private float EstaminaAtual;
    private bool podeCorrer = true;
    private bool isCrouching = false;
    private bool isCansado = false;

    private float RotacaoVertical;
    private Rigidbody RB;
    private bool isGrounded = true;

    private void Start()
    {
        RB = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        EstaminaAtual = MaxEstamina;

        if (cameraReferencia != null)
        {
            posicaoInicialCamera = cameraReferencia.localPosition;
            rotacaoInicialCamera = cameraReferencia.localRotation;
        }

        if (audioPassos == null)
            Debug.LogWarning("AudioSource de passos não atribuído!");
    }

    private void Update()
    {
        RotacaoMouse();
        PuloAgachar();
        SistemaStamina();
        AlturaCamera();
        DetectarInteracao();
        //HeadbobAvancado();
    }

    #region Headbob
    private void HeadbobAvancado()
    {
        if (!isGrounded || cameraReferencia == null) return;

        // Velocidade horizontal do jogador
        Vector3 horizontalVel = new Vector3(RB.linearVelocity.x, 0, RB.linearVelocity.z);
        float velocidadeAtual = horizontalVel.magnitude;

        if (velocidadeAtual > 0.1f)
        {
            contadorMovimento += Time.deltaTime * velocidadeBob;

            // Offset vertical e lateral
            float offsetY = Mathf.Sin(contadorMovimento) * intensidadeVertical;
            float offsetX = Mathf.Cos(contadorMovimento * 0.5f) * intensidadeHorizontal;
            float rotZ = Mathf.Sin(contadorMovimento * 0.5f) * intensidadeRotacao;

            // Micro-tremor
            Vector3 tremor = Vector3.zero;
            if ((Input.GetKey(KeyCode.LeftShift) && !isCrouching) || isCansado)
            {
                tremor.x = Random.Range(-intensidadeTremor, intensidadeTremor);
                tremor.y = Random.Range(-intensidadeTremor, intensidadeTremor);
            }

            // Aplica suavemente posição e rotação
            Vector3 targetPos = posicaoInicialCamera + new Vector3(offsetX, offsetY, 0) + tremor;
            cameraReferencia.localPosition = Vector3.Lerp(cameraReferencia.localPosition, targetPos, Time.deltaTime * 10f);

            Quaternion targetRot = rotacaoInicialCamera * Quaternion.Euler(0, 0, rotZ);
            cameraReferencia.localRotation = Quaternion.Slerp(cameraReferencia.localRotation, targetRot, Time.deltaTime * 10f);

            // Som de passo
            if (!passoTocado && offsetY > intensidadeVertical * 0.8f)
            {
                TocarSomPasso();
                passoTocado = true;
            }
            else if (offsetY < 0)
            {
                passoTocado = false;
            }
        }
        else
        {
            cameraReferencia.localPosition = Vector3.Lerp(cameraReferencia.localPosition, posicaoInicialCamera, Time.deltaTime * 5f);
            cameraReferencia.localRotation = Quaternion.Slerp(cameraReferencia.localRotation, rotacaoInicialCamera, Time.deltaTime * 5f);
            contadorMovimento = 0f;
        }
    }

    private void TocarSomPasso()
    {
        if (audioPassos != null && somPasso != null)
        {
            audioPassos.pitch = 0.9f + Random.Range(0f, 0.2f);
            audioPassos.PlayOneShot(somPasso);
        }
    }
    #endregion

    #region Movimento e câmera
    private void RotacaoMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        RotacaoVertical -= mouseY;
        RotacaoVertical = Mathf.Clamp(RotacaoVertical, -80f, 80f);
        cameraReferencia.localRotation = Quaternion.Euler(RotacaoVertical, 0f, cameraReferencia.localRotation.eulerAngles.z);
    }

    private void PuloAgachar()
    {
        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        //{
        //    RB.AddForce(Vector3.up * PuloForca, ForceMode.Impulse);
        //    isGrounded = false;
        //}

        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCrouching = !isCrouching;
    }

    private void AlturaCamera()
    {
        float targetHeight = isCrouching ? AlturaAgachado : AlturaEmPé;
        Vector3 cameraPos = cameraReferencia.localPosition;
        cameraPos.y = Mathf.Lerp(cameraPos.y, targetHeight, Time.deltaTime * VelocidadeTransição);
        cameraReferencia.localPosition = cameraPos;
    }
    #endregion

    #region Stamina
    private void SistemaStamina()
    {
        bool tentandoCorrer = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isCansado;

        if (tentandoCorrer)
        {
            EstaminaAtual -= EstaminaGasta * Time.deltaTime;
            if (EstaminaAtual <= 0f)
            {
                EstaminaAtual = 0f;
                isCansado = true;
                podeCorrer = false;
            }
        }
        else
        {
            EstaminaAtual += RegeneracaoEstamina * Time.deltaTime;
            if (EstaminaAtual >= MaxEstamina)
            {
                EstaminaAtual = MaxEstamina;
                isCansado = false;
                podeCorrer = true;
            }
        }
    }
    #endregion

    #region Movimento físico
    private void FixedUpdate()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed;
        if (isCrouching)
            currentSpeed = velocidadeAgachado;
        else if (Input.GetKey(KeyCode.LeftShift) && !isCansado && EstaminaAtual > 0)
            currentSpeed = VelocidadeCorrendo;
        else if (isCansado)
            currentSpeed = velocidadeCansado;
        else
            currentSpeed = velocidade;

        Vector3 move = transform.right * x + transform.forward * z;
        RB.MovePosition(RB.position + move * currentSpeed * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.7f)
            isGrounded = true;
    }
    #endregion

    #region Interação
    private void DetectarInteracao()
    {
        if (Input.GetKeyDown(teclaInteragir))
        {
            Debug.DrawRay(cameraReferencia.position, cameraReferencia.forward * alcanceInteracao, Color.red, 1f);
            Ray ray = new Ray(cameraReferencia.position, cameraReferencia.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, alcanceInteracao, LayerMask.GetMask("Interagir")))
            {
                ItemInterativo item = hit.collider.GetComponent<ItemInterativo>();
                if (item != null)
                    item.Interagir(this);
            }
        }
    }
    #endregion

    public float GetCurrentStamina() => EstaminaAtual;
    public bool IsCansado() => isCansado;
}
