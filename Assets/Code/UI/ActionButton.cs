using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    [Header("References")]
    public Button button;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI costText;

    // Support both actions and items
    private ActionData actionData;
    private ItemData itemData;
    private System.Action<ActionData> onActionSelected;
    private System.Action<ItemData> onItemSelected;

    // Setup for ActionData
    public void Setup(ActionData action, System.Action<ActionData> callback)
    {
        actionData = action;
        itemData = null;
        onActionSelected = callback;
        onItemSelected = null;

        if (nameText != null)
            nameText.text = action.actionName;

        if (costText != null)
            costText.text = $"AP: {action.actionPointCost}";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    // Setup for ItemData (keep existing functionality)
    public void Setup(ItemData item, System.Action<ItemData> callback)
    {
        itemData = item;
        actionData = null;
        onItemSelected = callback;
        onActionSelected = null;

        if (nameText != null)
            nameText.text = item.itemName;

        if (costText != null)
            costText.text = $"AP: {item.actionEffect.actionPointCost}";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (actionData != null && onActionSelected != null)
        {
            onActionSelected.Invoke(actionData);
        }
        else if (itemData != null && onItemSelected != null)
        {
            onItemSelected.Invoke(itemData);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}
