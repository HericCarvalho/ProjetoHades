using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlotVisual : MonoBehaviour
{
    public int slotIndex = 0;
    public CaixadeFusiveis box;
    public SpriteRenderer spriteRenderer;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Refresh()
    {
        if (box == null || spriteRenderer == null) return;
        if (slotIndex < 0 || slotIndex >= box.slots.Length) return;

        var item = box.slots[slotIndex];
        if (item == null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
        }
        else
        {
            spriteRenderer.sprite = item.icone;
            spriteRenderer.enabled = item.icone != null;
        }
    }
}
