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

    /// <summary>
    /// Set the player reference at runtime
    /// </summary>
    public void SetPlayer(Player newPlayer)
    {
        player = newPlayer;
    }

    /// <summary>
    /// Get current player reference
    /// </summary>
    public Player GetPlayer() => player;

    /// <summary>
    /// Get hunger percentage (0-1)
    /// </summary>
    public float GetHungerPercentage()
    {
        if (player?.HungerHandler == null) return 0f;
        
        float current = player.HungerHandler.GetHunger();
        float max = player.HungerHandler.GetMaxHunger();
        
        return max > 0 ? current / max : 0f;
    }
}