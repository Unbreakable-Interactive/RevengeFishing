using UnityEngine;

public class LevelDisplay : BaseDisplay
{
    [Header("Level Settings")]
    [SerializeField] private Entity entity;
    [SerializeField] private bool showAbsolutePowerLevel = false;
    
    private int initPowerLevel;

    protected override void Start()
    {
        base.Start();
        
        if (entity != null)
            initPowerLevel = entity.PowerLevel;
    }

    protected override void UpdateDisplay()
    {
        if (!CanUpdateDisplay() || entity == null) return;

        string levelText;
        
        if (showAbsolutePowerLevel)
        {
            // Show absolute power level (good for enemies)
            levelText = entity.PowerLevel.ToString("N0");
        }
        else
        {
            // Show difference from initial (good for player progression)
            int progression = entity.PowerLevel - initPowerLevel;
            levelText = progression >= 0 ? $"{progression:N0}" : progression.ToString("N0");
        }
        
        SetDisplayText(levelText);
    }

    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public void SetEntity(Entity newEntity)
    {
        entity = newEntity;
        if (entity != null)
            initPowerLevel = entity.PowerLevel;
    }

    /// <summary>
    /// Get current entity reference
    /// </summary>
    public Entity GetEntity() => entity;

    /// <summary>
    /// Toggle between absolute and relative power level display
    /// </summary>
    public void ToggleDisplayMode()
    {
        showAbsolutePowerLevel = !showAbsolutePowerLevel;
    }
}