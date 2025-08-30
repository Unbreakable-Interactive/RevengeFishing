using UnityEngine;

public class HungerDisplay : BaseDisplay
{
    [Header("Hunger Settings")]
    [SerializeField] private Player player;

    protected override void UpdateDisplay()
    {
        if (!CanUpdateDisplay() || player == null || player.HungerHandler == null) return;

        float currentHunger = player.HungerHandler.GetHunger();
        float maxHunger = player.HungerHandler.GetMaxHunger();
        
        string hungerText = $"{currentHunger} / {maxHunger}";
        SetDisplayText(hungerText);
    }
}