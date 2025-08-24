using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UpgradeSlotView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;

    [Header("Costo (pips en el botón)")]
    [SerializeField] private Image[] costPips;
    [SerializeField] private Color costAvailable = Color.white;
    [SerializeField] private Color costSpent = Color.gray;

    [Header("Bloqueo visual opcional")]
    [SerializeField] private CanvasGroup cg;

    public UpgradeSO Assigned { get; private set; }
    public int Cost { get; private set; } = 0;

    private UnityAction cachedClick; 

    public void Setup(UpgradeSO upgrade, int cost)
    {
        Assigned = upgrade;
        Cost = cost;

        if (button != null && cachedClick != null)
            button.onClick.RemoveListener(cachedClick);
        cachedClick = null;

        if (icon) icon.sprite = upgrade ? upgrade.icon : null;
        if (titleText) titleText.text = upgrade ? upgrade.title : "-";
        if (descText) descText.text = upgrade ? upgrade.description : "";

        PaintCost(0);

        SetInteractable(upgrade != null);
    }

    public void PaintCost(int spent)
    {
        if (costPips == null) return;

        bool isSpent = (spent >= Cost) && Cost > 0;
        Color c = isSpent ? costSpent : costAvailable;

        for (int i = 0; i < costPips.Length; i++)
        {
            if (!costPips[i]) continue;
            costPips[i].gameObject.SetActive(i < Cost);
            if (i < Cost) costPips[i].color = c;
        }
    }

    public void SetInteractable(bool value)
    {
        if (button) button.interactable = value;

        if (cg)
        {
            cg.alpha = value ? 1f : 0.5f;
            cg.interactable = value;
            cg.blocksRaycasts = value;
        }
    }

    public void AddListener(UnityAction onClick)
    {
        if (!button) return;

        if (cachedClick != null)
            button.onClick.RemoveListener(cachedClick);

        cachedClick = onClick;
        button.onClick.AddListener(cachedClick);
    }
}