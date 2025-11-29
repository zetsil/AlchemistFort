using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIInventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI countText;

    private InventorySlot linkedSlot;

    // Inițializează slotul cu datele lui
    public void Setup(InventorySlot slot)
    {
        linkedSlot = slot;

        if (iconImage != null)
            iconImage.sprite = slot.icon;

        Refresh();
    }

    // Actualizează UI-ul (numărul de iteme etc.)
    public void Refresh()
    {
        if (linkedSlot == null)
        {
            gameObject.SetActive(false);
            return;
        }

        countText.text = linkedSlot.count.ToString();
    }

    // Poți lega asta la un buton
    public void OnClick()
    {
        Debug.Log($"🖱 Click pe slot #{linkedSlot.slotIndex} ({linkedSlot.itemData.itemName})");

        // Exemplu: scade 1 din slotul acesta
        linkedSlot.DecreaseCount(1);
    }
}
