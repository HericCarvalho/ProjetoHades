using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Renderer))]
public class InteracaoCenario : MonoBehaviour
{
    public ItemSO item;
    public GameObject popupPrefab;
    public float alturaPopup = 1.5f;
    public Sprite iconeInteracaoPopup;
    public float distanciaInteracao = 2f;
    [Range(0, 1)] public float precisaoOlhar = 0.97f;
    public KeyCode teclaInteragir = KeyCode.E;
    public Transform jogadorCamera;
    public Material materialContorno;
    public float pulsacaoMagnitude = 0.05f;
    public float pulsacaoVel = 3f;

    private Renderer rend;
    private Material matOriginal;
    private GameObject popupInstance;
    private TMP_Text popupTexto;
    private Image popupIcone;
    private bool mostrando = false;
    private bool coletado = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        matOriginal = rend.material;
    }

    void Update()
    {
        if (coletado || jogadorCamera == null) return;

        if (DentroDoAlcanceEOlhando())
        {
            if (!mostrando) CriarPopup();
            AtualizarPopup();
            PulsarContorno();
            if (Input.GetKeyDown(teclaInteragir)) Coletar();
        }
        else if (mostrando) RemoverPopup();
    }

    bool DentroDoAlcanceEOlhando()
    {
        Vector3 toObj = (transform.position - jogadorCamera.position);
        if (toObj.magnitude > distanciaInteracao) return false;
        return Vector3.Dot(jogadorCamera.forward, toObj.normalized) >= precisaoOlhar;
    }

    void CriarPopup()
    {
        mostrando = true;
        popupInstance = Instantiate(popupPrefab, transform.position + Vector3.up * alturaPopup, Quaternion.identity);
        popupTexto = popupInstance.GetComponentInChildren<TMP_Text>(true);
        popupIcone = popupInstance.GetComponentInChildren<Image>(true);
        if (popupTexto != null) popupTexto.text = $"Pressione {teclaInteragir} para pegar {item.nomeItem}";
        if (popupIcone != null)
        {
            popupIcone.sprite = iconeInteracaoPopup;
            popupIcone.enabled = iconeInteracaoPopup != null;
            popupIcone.preserveAspect = true;
        }
    }

    void RemoverPopup()
    {
        mostrando = false;
        if (popupInstance != null) Destroy(popupInstance);
        popupInstance = null;
        popupTexto = null;
        popupIcone = null;
        if (rend != null && matOriginal != null) rend.material = matOriginal;
    }

    void AtualizarPopup()
    {
        if (popupInstance == null) return;
        popupInstance.transform.position = transform.position + Vector3.up * alturaPopup;
        popupInstance.transform.LookAt(jogadorCamera);
        popupInstance.transform.Rotate(0, 180f, 0);
    }

    void PulsarContorno()
    {
        if (rend == null || materialContorno == null) return;
        float t = Mathf.Sin(Time.time * pulsacaoVel) * pulsacaoMagnitude + 0.5f;
        rend.material.Lerp(matOriginal, materialContorno, t);
    }

    void Coletar()
    {
        if (coletado) return;
        coletado = true;
        if (Sistema_Inventario.instancia != null && item != null)
            Sistema_Inventario.instancia.AdicionarItem(item);
        RemoverPopup();
        Destroy(gameObject);
    }
}
