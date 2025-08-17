using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Renderer))]
public class Interação_Cenario : MonoBehaviour
{
    [Header("Referências")]
    public ItemSO item;
    public GameObject popupPrefab;       // Prefab do popup em mundo 3D
    public float distanciaInteracao = 2f;
    [Range(0, 1)] public float anguloVisao = 0.9f;

    [Header("Contorno")]
    public Material materialContorno;
    public float pulsacaoMagnitude = 0.05f;
    public float pulsacaoVel = 3f;

    [Header("HUD")]
    public Image iconeHUD;               // Ícone que aparece na HUD
    public Sprite iconeInteracaoDefault; // Sprite padrão caso item não tenha icone

    [Header("Player")]
    public Transform jogadorCamera;
    public KeyCode teclaInteragir = KeyCode.E;

    private Renderer rend;
    private Material matOriginal;
    private GameObject popupInstance;
    private TMP_Text textoPopup;
    private bool mostrando = false;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        matOriginal = rend.material;

        if (iconeHUD != null)
            iconeHUD.enabled = false;
    }

    private void Update()
    {
        if (jogadorCamera == null) return;

        Vector3 direcao = (transform.position - jogadorCamera.position).normalized;
        float distancia = Vector3.Distance(transform.position, jogadorCamera.position);
        float olharDot = Vector3.Dot(jogadorCamera.forward, direcao);

        bool podeInteragir = distancia <= distanciaInteracao && olharDot >= anguloVisao;

        if (podeInteragir)
        {
            if (!mostrando)
                MostrarPopup(true);

            PulsarContorno();

            if (Input.GetKeyDown(teclaInteragir))
                Interagir();

            AtualizarPopup();
            AtualizarHUD(true);
        }
        else
        {
            if (mostrando)
                MostrarPopup(false);

            AtualizarHUD(false);
        }
    }

    private void MostrarPopup(bool ativo)
    {
        mostrando = ativo;

        if (ativo)
        {
            if (popupPrefab != null && popupInstance == null)
            {
                popupInstance = Instantiate(popupPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                textoPopup = popupInstance.GetComponentInChildren<TMP_Text>();
                if (textoPopup != null && item != null)
                    textoPopup.text = $"Pressione {teclaInteragir} para pegar {item.nomeItem}";
            }
        }
        else
        {
            if (popupInstance != null)
            {
                Destroy(popupInstance);
                popupInstance = null;
                textoPopup = null;
            }
        }
    }

    private void AtualizarPopup()
    {
        if (popupInstance == null) return;

        popupInstance.transform.position = transform.position + Vector3.up * 1.5f;
        popupInstance.transform.LookAt(jogadorCamera);
        popupInstance.transform.Rotate(0, 180f, 0);
    }

    private void PulsarContorno()
    {
        if (rend == null || materialContorno == null) return;

        float t = Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude + 0.5f;
        rend.material.Lerp(matOriginal, materialContorno, t);
    }

    private void AtualizarHUD(bool ativo)
    {
        if (iconeHUD == null) return;

        iconeHUD.enabled = ativo;
        if (ativo)
        {
            iconeHUD.sprite = (item != null && item.iconeItem != null) ? item.iconeItem : iconeInteracaoDefault;
        }
    }

    private void Interagir()
    {
        if (item != null && Sistema_Inventario.instancia != null)
            Sistema_Inventario.instancia.AdicionarItem(item);

        MostrarPopup(false);
        AtualizarHUD(false);
        Destroy(gameObject);
    }
}
