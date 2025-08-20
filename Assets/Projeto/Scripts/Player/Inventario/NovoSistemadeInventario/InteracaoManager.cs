using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteracaoManager : MonoBehaviour
{
    [Header("Player")]
    public Transform jogadorCamera;
    public float distanciaInteracao = 2f;
    public LayerMask camadaInteracao;
    public KeyCode teclaInteragir = KeyCode.E;
    [Range(0, 1)] public float precisaoOlhar = 0.97f;

    [Header("Popup Mundo 3D")]
    public GameObject prefabPopup;
    public float alturaPopup = 1.5f;
    public Sprite iconeInteracao;

    [Header("Contorno")]
    public Material materialContorno;
    public float pulsacaoMagnitude = 0.05f;
    public float pulsacaoVel = 3f;

    private GameObject popupInstance;
    private TMP_Text popupTexto;
    private Image popupIcone;
    private Renderer objetoRend;
    private Material matOriginal;
    private ItemInterativo objetoInterativo;
    private bool mostrandoPopup = false;

    void Update()
    {
        DetectarInteracao();
        if (Input.GetKeyDown(teclaInteragir)) Interagir();
    }

    void DetectarInteracao()
    {
        Ray ray = new Ray(jogadorCamera.position, jogadorCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, camadaInteracao))
        {
            ItemInterativo item = hit.collider.GetComponent<ItemInterativo>();
            if (item != null)
            {
                objetoInterativo = item;
                if (!mostrandoPopup) CriarPopup(item, hit.collider.GetComponent<Renderer>());
                AtualizarPopup();
                PulsarContorno();
                return;
            }
        }

        if (mostrandoPopup) RemoverPopup();
    }

    void Interagir()
    {
        if (objetoInterativo != null)
        {
            objetoInterativo.Interagir(jogadorCamera.GetComponent<Movimenta��oPlayer>());
            RemoverPopup();
        }
    }

    void CriarPopup(ItemInterativo item, Renderer rend)
    {
        mostrandoPopup = true;
        objetoRend = rend;
        matOriginal = rend != null ? rend.material : null;

        popupInstance = Instantiate(prefabPopup, item.transform.position + Vector3.up * alturaPopup, Quaternion.identity);
        popupTexto = popupInstance.GetComponentInChildren<TMP_Text>();
        popupIcone = popupInstance.GetComponentInChildren<Image>();

        if (popupTexto != null) popupTexto.text = $"Pressione {teclaInteragir} para {item.tipo}";
        if (popupIcone != null)
        {
            popupIcone.sprite = iconeInteracao;
            popupIcone.enabled = iconeInteracao != null;
            popupIcone.preserveAspect = true;
        }
    }

    void RemoverPopup()
    {
        mostrandoPopup = false;
        objetoInterativo = null;

        if (popupInstance != null) Destroy(popupInstance);
        popupInstance = null;
        popupTexto = null;
        popupIcone = null;

        if (objetoRend != null && matOriginal != null) objetoRend.material = matOriginal;
        objetoRend = null;
    }

    void AtualizarPopup()
    {
        if (popupInstance == null || objetoInterativo == null) return;
        popupInstance.transform.position = objetoInterativo.transform.position + Vector3.up * alturaPopup;
        popupInstance.transform.LookAt(jogadorCamera);
        popupInstance.transform.Rotate(0, 180f, 0);
    }

    void PulsarContorno()
    {
        if (objetoRend == null || materialContorno == null) return;
        float t = Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude + 0.5f;
        objetoRend.material.Lerp(matOriginal, materialContorno, t);
    }
}
