using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Image icon;

    private ItemData itemData;
    private System.Action<ItemData> onItemSelected;

    public void Setup(ItemData item, System.Action<ItemData> callback)
    {
        itemData = item;
        onItemSelected = callback;

        if (nameText != null)
            nameText.text = item.itemName;

        if (costText != null)
            costText.text = $"AP: {item.actionEffect.actionPointCost}";

        if (icon != null && item.icon != null)
            icon.sprite = item.icon;

        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        onItemSelected?.Invoke(itemData);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}
