using UnityEngine;

public class FatigueDisplay : BaseDisplay
{
    [Header("Fatigue Settings")]
    [SerializeField] private Entity entity;

    protected override void UpdateDisplay()
    {
        if (!CanUpdateDisplay() || entity == null) return;

        string fatigueText = $"{entity.entityFatigue.fatigue} / {entity.entityFatigue.maxFatigue}";
        SetDisplayText(fatigueText);
    }

    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public void SetEntity(Entity newEntity)
    {
        entity = newEntity;
    }

    /// <summary>
    /// Get current entity reference
    /// </summary>
    public Entity GetEntity() => entity;
}