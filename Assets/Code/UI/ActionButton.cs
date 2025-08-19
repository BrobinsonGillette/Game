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
    private System.Action<ActionData> onActionSelected;

    // Setup for ActionData
    public void Setup(ActionData action, System.Action<ActionData> callback)
    {
        actionData = action;
        onActionSelected = callback;


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



    private void OnButtonClicked()
    {
        if (actionData != null && onActionSelected != null)
        {
            onActionSelected.Invoke(actionData);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}
