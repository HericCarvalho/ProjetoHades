using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Renderer))]
public class Interação_Cenario : MonoBehaviour
{
    [Header("Item")]
    public ItemSO item;

    [Header("Popup (mundo 3D)")]
    public GameObject popupPrefab;   // Canvas (World Space) com TMP_Text e opcional ícone
    public float alturaPopup = 1.5f; // Altura acima do item
    public Sprite iconeInteracaoPopup; // Ícone da mão (fixo no popup)

    [Header("Detecção")]
    public float distanciaInteracao = 2f;
    [Range(0, 1)] public float precisaoOlhar = 0.97f; // precisão do olhar
    public KeyCode teclaInteragir = KeyCode.E;

    [Header("Contorno")]
    public Material materialContorno;
    public float pulsacaoMagnitude = 0.05f;
    public float pulsacaoVel = 3f;

    [Header("Player")]
    public Transform jogadorCamera;

    // Internos
    private Renderer rend;
    private Material matOriginal;
    private GameObject popupInstance;
    private TMP_Text popupTexto;
    private Image popupIcone;
    private bool mostrando = false;
    private bool coletado = false;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        matOriginal = rend.material;
    }

    private void Update()
    {
        if (coletado || jogadorCamera == null) return;

        if (DentroDoAlcanceEOlhando())
        {
            if (!mostrando) CriarPopup();

            AtualizarPopup();
            PulsarContorno();

            if (Input.GetKeyDown(teclaInteragir))
                Coletar();
        }
        else
        {
            if (mostrando) RemoverPopup();
        }
    }

    private bool DentroDoAlcanceEOlhando()
    {
        Vector3 toObj = (transform.position - jogadorCamera.position);
        float dist = toObj.magnitude;
        if (dist > distanciaInteracao) return false;

        toObj.Normalize();
        float dot = Vector3.Dot(jogadorCamera.forward, toObj);
        return dot >= precisaoOlhar;
    }

    private void CriarPopup()
    {
        mostrando = true;

        popupInstance = Instantiate(
            popupPrefab,
            transform.position + Vector3.up * alturaPopup,
            Quaternion.identity
        );

        popupTexto = popupInstance.GetComponentInChildren<TMP_Text>(true);
        popupIcone = popupInstance.GetComponentInChildren<Image>(true);

        if (popupTexto != null)
            popupTexto.text = $"Pressione {teclaInteragir} para pegar {item.nomeItem}";

        if (popupIcone != null)
        {
            popupIcone.sprite = iconeInteracaoPopup;
            popupIcone.enabled = iconeInteracaoPopup != null;
            popupIcone.preserveAspect = true;
        }
    }

    private void RemoverPopup()
    {
        mostrando = false;

        if (popupInstance != null) Destroy(popupInstance);
        popupInstance = null;
        popupTexto = null;
        popupIcone = null;

        if (rend != null && matOriginal != null)
            rend.material = matOriginal;
    }

    private void AtualizarPopup()
    {
        if (popupInstance == null) return;

        popupInstance.transform.position = transform.position + Vector3.up * alturaPopup;
        popupInstance.transform.LookAt(jogadorCamera);
        popupInstance.transform.Rotate(0, 180f, 0); // sempre virado para o player
    }

    private void PulsarContorno()
    {
        if (rend == null || materialContorno == null) return;
        float t = Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude + 0.5f;
        rend.material.Lerp(matOriginal, materialContorno, t);
    }

    private void Coletar()
    {
        if (coletado) return;
        coletado = true;

        if (Sistema_Inventario.instancia != null && item != null)
            Sistema_Inventario.instancia.AdicionarItem(item);

        RemoverPopup();
        Destroy(gameObject);
    }
}
