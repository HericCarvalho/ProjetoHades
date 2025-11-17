using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VisualFusiveis : MonoBehaviour
{
    [HideInInspector] public Fusiveis fuseData;
    [HideInInspector] public int slotIndex = -1; // qual slot da caixa corresponde
    [HideInInspector] public CaixadeFusiveis parentBox;

    // apenas utilitários visuais (ex: highlight)
    private Renderer rend;
    private Color originalColor;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    public void Set(Fusiveis data, int slot, CaixadeFusiveis box)
    {
        fuseData = data;
        slotIndex = slot;
        parentBox = box;

        // opcional: trocar sprite/mesh/material se fuseData tiver assets (não implementado aqui)
        gameObject.name = data != null ? $"Fuse_{data.nomeItem}_S{slot}" : $"Fuse_empty_S{slot}";

        // enable/disable visual
        gameObject.SetActive(data != null);
    }

    public void Highlight(bool on)
    {
        if (rend == null) return;
        if (on) rend.material.color = Color.yellow;
        else rend.material.color = originalColor;
    }
}