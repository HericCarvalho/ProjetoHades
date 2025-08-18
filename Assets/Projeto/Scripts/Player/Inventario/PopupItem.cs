using UnityEngine;
using UnityEngine.UI;

public class PopupItem : MonoBehaviour
{
    public static PopupItem instancia;

    [Header("Botões")]
    public Button botaoUsar;
    public Button botaoExcluir;
    public Button botaoCancelar;

    private SlotInventario slotAtual;

    void Awake()
    {
        instancia = this;
        gameObject.SetActive(false);
    }

    public void Abrir(SlotInventario slot, Vector3 posMouse)
    {
        slotAtual = slot;

        // Se o popup estiver em Canvas Screen Space - Overlay:
        transform.position = posMouse;

        // Se for Screen Space - Camera / World Space, ajuste com RectTransformUtility.ScreenPointToLocalPointInRectangle.

        gameObject.SetActive(true);

        // Limpa e adiciona listeners
        if (botaoUsar != null)
        {
            botaoUsar.onClick.RemoveAllListeners();
            botaoUsar.onClick.AddListener(() =>
            {
                slotAtual?.UsarItem();
                Fechar();
            });
        }

        if (botaoExcluir != null)
        {
            botaoExcluir.onClick.RemoveAllListeners();
            botaoExcluir.onClick.AddListener(ExcluirItem);
        }

        if (botaoCancelar != null)
        {
            botaoCancelar.onClick.RemoveAllListeners();
            botaoCancelar.onClick.AddListener(Fechar);
        }
    }

    private void ExcluirItem()
    {
        if (slotAtual != null)
        {
            Debug.Log("Excluiu o item: " + slotAtual.GetNomeItem());
            slotAtual.RemoverItem();   // remove toda a pilha deste slot
        }
        Fechar();
    }

    public void Fechar()
    {
        gameObject.SetActive(false);
        slotAtual = null;
    }
}
