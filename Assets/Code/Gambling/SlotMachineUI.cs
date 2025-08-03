
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public enum PrizeType
{
    None,
    Trash,
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Exotic,
    Ultimate,
    SuperUltimate,
    Godlike,
    Unknown
}
public class SlotMachineUI : MonoBehaviour
{
    [Header("Slot Machine References")]
    public SlotMachine slotMachine;




    [Header("Auto-Generated UI Elements")]
    private GameObject slotMachinePanel;
    private Image[] reelImages = new Image[3];
    private Button spinButton;
    private TextMeshProUGUI resultText;
    private GameObject prizeDisplayPanel;
    private Image prizeIcon;
    private TextMeshProUGUI prizeNameText;
    private TextMeshProUGUI prizeDescriptionText;
    private Button claimPrizeButton;
    private Button closeButton;

    [SerializeField]private Canvas mainCanvas;

    private void Start()
    {
        FindOrCreateCanvas();
        CreateSlotMachineUI();
        SetupSlotMachine();
    }


    private void FindOrCreateCanvas()
    {
       
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            Debug.LogError("No Canvas found! Make sure UIManager is properly set up.");
        }
    }

    private void CreateSlotMachineUI()
    {
        if (mainCanvas == null) return;

        // Create main slot machine panel
        slotMachinePanel = CreatePanel("Slot Machine Panel", mainCanvas.transform);
        SetRectTransform(slotMachinePanel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 600));
        slotMachinePanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        slotMachinePanel.SetActive(false);

        // Create title
        CreateSlotMachineTitle();

        // Create slot reels
        CreateSlotReels();

        // Create control buttons
        CreateControlButtons();

        // Create result display
        CreateResultDisplay();

        // Create prize display panel
        CreatePrizeDisplay();
    }

    private void CreateSlotMachineTitle()
    {
        GameObject titleObj = CreateText("Slot Machine Title", slotMachinePanel.transform, "🎰 SLOT MACHINE 🎰");
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(titleObj.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(0, 40));
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.yellow;
    }

    private void CreateSlotReels()
    {
        // Create reel container
        GameObject reelContainer = CreatePanel("Reel Container", slotMachinePanel.transform);
        SetRectTransform(reelContainer.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);
        reelContainer.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Create individual reels
        for (int i = 0; i < 3; i++)
        {
            GameObject reel = CreatePanel($"Reel {i + 1}", reelContainer.transform);
            float xPos = 0.16f + (i * 0.34f);
            SetRectTransform(reel.GetComponent<RectTransform>(),
                new Vector2(xPos, 0.1f), new Vector2(xPos + 0.2f, 0.9f), Vector2.zero, Vector2.zero);
            reel.GetComponent<Image>().color = Color.white;

            // Create reel image
            GameObject reelImageObj = CreateImage($"Reel Image {i + 1}", reel.transform);
            reelImages[i] = reelImageObj.GetComponent<Image>();
            SetRectTransform(reelImageObj.GetComponent<RectTransform>(),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            reelImages[i].color = Color.gray;

            // Add reel number text
            GameObject reelNumObj = CreateText($"Reel Number {i + 1}", reel.transform, "?");
            SetRectTransform(reelNumObj.GetComponent<RectTransform>(),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI reelNumText = reelNumObj.GetComponent<TextMeshProUGUI>();
            reelNumText.alignment = TextAlignmentOptions.Center;
            reelNumText.fontSize = 48;
            reelNumText.fontStyle = FontStyles.Bold;
        }
    }

    private void CreateControlButtons()
    {
        // Create spin button
        GameObject spinButtonObj = CreateButton("Spin Button", slotMachinePanel.transform, "🎲 SPIN 🎲");
        spinButton = spinButtonObj.GetComponent<Button>();
        SetRectTransform(spinButtonObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(200, 50));
        StyleButton(spinButton, new Color(0.2f, 0.8f, 0.2f), Color.white);

        TextMeshProUGUI spinButtonText = spinButton.GetComponentInChildren<TextMeshProUGUI>();
        if (spinButtonText != null)
        {
            spinButtonText.fontSize = 20;
            spinButtonText.fontStyle = FontStyles.Bold;
        }




        // Create close button
        GameObject closeButtonObj = CreateButton("Close Button", slotMachinePanel.transform, "✖");
        closeButton = closeButtonObj.GetComponent<Button>();
        SetRectTransform(closeButtonObj.GetComponent<RectTransform>(),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(30, 30));
        StyleButton(closeButton, new Color(0.8f, 0.2f, 0.2f), Color.white);
    }

    private void CreateResultDisplay()
    {
        GameObject resultObj = CreateText("Result Text", slotMachinePanel.transform, "Press SPIN to play!");
        resultText = resultObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(resultObj.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.28f), Vector2.zero, Vector2.zero);
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 18;
        resultText.fontStyle = FontStyles.Bold;
        resultText.color = Color.cyan;
    }

    private void CreatePrizeDisplay()
    {
        // Create prize display panel (hidden by default)
        prizeDisplayPanel = CreatePanel("Prize Display Panel", mainCanvas.transform);
        SetRectTransform(prizeDisplayPanel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 300));
        prizeDisplayPanel.GetComponent<Image>().color = new Color(0.9f, 0.8f, 0.2f, 0.95f);
        prizeDisplayPanel.SetActive(false);

        // Prize title
        CreateText("Prize Title", prizeDisplayPanel.transform, "🎉 CONGRATULATIONS! 🎉", 20, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRectTransform(prizeDisplayPanel.transform.GetChild(prizeDisplayPanel.transform.childCount - 1).GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(0, 40));

        // Prize icon
        GameObject prizeIconObj = CreateImage("Prize Icon", prizeDisplayPanel.transform);
        prizeIcon = prizeIconObj.GetComponent<Image>();
        SetRectTransform(prizeIconObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(80, 80));

        // Prize name
        GameObject prizeNameObj = CreateText("Prize Name", prizeDisplayPanel.transform, "Amazing Prize!");
        prizeNameText = prizeNameObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(prizeNameObj.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f), Vector2.zero, Vector2.zero);
        prizeNameText.alignment = TextAlignmentOptions.Center;
        prizeNameText.fontSize = 18;
        prizeNameText.fontStyle = FontStyles.Bold;
        prizeNameText.color = Color.black;

        // Prize description
        GameObject prizeDescObj = CreateText("Prize Description", prizeDisplayPanel.transform, "You won an incredible prize!");
        prizeDescriptionText = prizeDescObj.GetComponent<TextMeshProUGUI>();
        SetRectTransform(prizeDescObj.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.4f), Vector2.zero, Vector2.zero);
        prizeDescriptionText.alignment = TextAlignmentOptions.Center;
        prizeDescriptionText.fontSize = 14;
        prizeDescriptionText.color = Color.black;

        // Claim prize button
        GameObject claimButtonObj = CreateButton("Claim Prize Button", prizeDisplayPanel.transform, "CLAIM PRIZE");
        claimPrizeButton = claimButtonObj.GetComponent<Button>();
        SetRectTransform(claimButtonObj.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(150, 40));
        StyleButton(claimPrizeButton, new Color(0.2f, 0.8f, 0.2f), Color.white);
    }

    private void SetupSlotMachine()
    {
        if (slotMachine == null)
        {
            Debug.LogError("SlotMachine reference not set!");
            return;
        }

        // Assign UI references to slot machine
        slotMachine.slotMachinePanel = slotMachinePanel;
        slotMachine.reelImages = reelImages;
        slotMachine.spinButton = spinButton;
        slotMachine.resultText = resultText;
        slotMachine.prizeDisplayPanel = prizeDisplayPanel;
        slotMachine.prizeIcon = prizeIcon;
        slotMachine.prizeNameText = prizeNameText;
        slotMachine.prizeDescriptionText = prizeDescriptionText;
        slotMachine.claimPrizeButton = claimPrizeButton;
        slotMachine.closeButton = closeButton;
    }

    // Utility methods for creating UI elements
    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        return panel;
    }

    private GameObject CreateText(string name, Transform parent, string text, float fontSize = 16, FontStyles fontStyle = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();

        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.fontStyle = fontStyle;
        textComp.alignment = alignment;
        textComp.color = Color.white;

        return textObj;
    }

    private GameObject CreateButton(string name, Transform parent, string buttonText)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        Image image = buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();

        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        button.targetGraphic = image;

        // Create button text
        GameObject textObj = CreateText("Text", buttonObj.transform, buttonText, 16, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRectTransform(textObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        textObj.GetComponent<TextMeshProUGUI>().color = Color.white;

        return buttonObj;
    }

    private GameObject CreateImage(string name, Transform parent)
    {
        GameObject imageObj = new GameObject(name);
        imageObj.transform.SetParent(parent, false);

        RectTransform rect = imageObj.AddComponent<RectTransform>();
        Image image = imageObj.AddComponent<Image>();

        return imageObj;
    }

    private void SetRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void StyleButton(Button button, Color normalColor, Color textColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = normalColor * 1.2f;
        colors.pressedColor = normalColor * 0.8f;
        colors.selectedColor = normalColor;
        button.colors = colors;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = textColor;
        }
    }


}
