using UnityEngine;
using UnityEngine.UI;

public class EnergyDisplay : BaseDisplay
{
    [Header("Energy Settings")]
    [SerializeField] private Player player;
    [SerializeField] private Image energyFillImage;
    [SerializeField] private bool showEnergyText = false;
    
    [Header("Visual Feedback")]
    [SerializeField] private float lowEnergyThreshold = 0.25f;
    [SerializeField] private float mediumEnergyThreshold = 0.5f;
    
    protected override void Start()
    {
        base.Start();
        
        if (player == null) player = Player.Instance;
        if (energyFillImage == null) energyFillImage = GetComponentInChildren<Image>();
    }
    
    protected override void UpdateDisplay()
    {
        if (player?.entityEnergy == null) return;
        
        float energyPercentage = player.entityEnergy.EnergyPercentage;
        GameLogger.Log($"[Energy Percentage] {energyPercentage}");
        
        if (energyFillImage != null)
            energyFillImage.fillAmount = energyPercentage;
        
        if (showEnergyText && displayText != null)
            SetDisplayText($"Energy: {player.entityEnergy.CurrentEnergy:F0}/{player.entityEnergy.MaxEnergy:F0}");
    }
}