using UnityEngine;
using UnityEngine.Events;

public class MovimentaçãoPlayer : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidade;                      // Velocidade normal
    [SerializeField] private float velocidadeAgachado;              // Velocidade ao agachar
    [SerializeField] private float VelocidadeCorrendo;              // Velocidade ao correr
    [SerializeField] private float velocidadeCansado;               // Velocidade reduzida por cansaço
    [SerializeField] private float PuloForca;                       // Força do pulo
    [SerializeField] private float MouseSensibilidade;              // Sensibilidade do mouse

    [Header("Câmera")]
    [SerializeField] private Transform cameraReferencia;            // Referência da câmera
    [SerializeField] private float AlturaEmPé;                      // Altura normal
    [SerializeField] private float AlturaAgachado;                  // Altura agachado
    [SerializeField] private float VelocidadeTransição;             // Velocidade da transição da altura

    [Header("Stamina")]
    [SerializeField] public float MaxEstamina;                      // Stamina máxima
    [SerializeField] private float RegeneracaoEstamina;             // Quanto a stamina recupera por segundo
    [SerializeField] private float EstaminaGasta;                   // Quanto a stamina consome ao correr

    [Header("Interação")]
    [SerializeField] private float alcanceInteracao = 2f;           // Distância máxima para interagir
    [SerializeField] private KeyCode teclaInteragir = KeyCode.E;    // Tecla para interação
    

    // Estados internos
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
        Cursor.lockState = CursorLockMode.Locked;   // Trava cursor na tela
        EstaminaAtual = MaxEstamina;               // Começa com stamina cheia
    }

    private void Update()
    {
        RotacaoMouse();       // Rotaciona o jogador e a câmera
        PuloAgachar();        // Controla pulo e agachar
        SistemaStamina();     // Atualiza a stamina e estado cansado
        AlturaCamera();       // Ajusta altura da câmera suavemente
        DetectarInteracao();  // Detecta se o jogador quer interagir
    }

    #region Movimento e câmera
    private void RotacaoMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * MouseSensibilidade;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * MouseSensibilidade;
        RotacaoVertical -= mouseY;
        RotacaoVertical = Mathf.Clamp(RotacaoVertical, -80f, 80f);
        cameraReferencia.localRotation = Quaternion.Euler(RotacaoVertical, 0f, 0f);
    }

    private void PuloAgachar()
    {
        // Pulo
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            RB.AddForce(Vector3.up * PuloForca, ForceMode.Impulse);
            isGrounded = false;
        }

        // Toggle agachar
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

    #region Stamina e estados
    private void SistemaStamina()
    {
        bool tentandoCorrer = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isCansado;

        if (tentandoCorrer)
        {
            EstaminaAtual -= EstaminaGasta * Time.deltaTime;

            // Se acabar a stamina, jogador fica cansado
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

            // Se recuperar totalmente, volta ao normal
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

        // Determina velocidade atual
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
        // Tecla pressionada para interagir
        if (Input.GetKeyDown(teclaInteragir))
        {
            Debug.DrawRay(cameraReferencia.position, cameraReferencia.forward * alcanceInteracao, Color.red, 1f);
            Ray ray = new Ray(cameraReferencia.position, cameraReferencia.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, alcanceInteracao, LayerMask.GetMask("Interagir")))
            {
                ItemInterativo item = hit.collider.GetComponent<ItemInterativo>();
                if (item != null)
                    item.Interagir(this); // Passa o jogador para o item
            }
        }
    }
    #endregion

    // Métodos para HUD e inventário
    public float GetCurrentStamina() => EstaminaAtual;              // Para a UI
    public bool IsCansado() => isCansado;                           // Estado cansado

    public void MostrarHUD(string mensagem)
    {
        HUD_Interacao.instancia.MostrarMensagem(mensagem);
    }

    // Envia notificação para a HUD (agora aceita ícone opcional)
    public void NotificacaoInventario(string mensagem, Sprite icone = null)
    {
        if (HUD_Interacao.instancia != null)
            HUD_Interacao.instancia.MostrarNotificacao(mensagem, icone);
    }

    // Quando adicionar item, passe nome + ícone do ItemSO
    public void AdicionarAoInventario(ItemSistema item)
    {
        // Use o seu gerenciador atual (Inventario.instancia OU Sistema_Inventario.instancia)
        SistemaInventario.instancia.AdicionarItem(item);

        // Envia notificação com imagem do item
        NotificacaoInventario($"Pegou {item.nomeItem}", item.iconeItem);
    }
}