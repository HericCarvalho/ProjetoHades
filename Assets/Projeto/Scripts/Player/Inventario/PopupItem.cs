using UnityEngine;
using UnityEngine.UI;

public class PopupItem : MonoBehaviour
{
    public Button botaoUsar;
    public Button botaoCancelar;

    private SlotInventario slotAtual;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Abrir(SlotInventario slot, Vector3 posMouse)
    {
        slotAtual = slot;
        transform.position = posMouse;
        gameObject.SetActive(true);

        botaoUsar.onClick.RemoveAllListeners();
        botaoCancelar.onClick.RemoveAllListeners();

        botaoUsar.onClick.AddListener(() => { slotAtual.UsarItem(); Fechar(); });
        botaoCancelar.onClick.AddListener(Fechar);
    }

    public void Fechar()
    {
        gameObject.SetActive(false);
        slotAtual = null;
    }
}
