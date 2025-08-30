using UnityEngine;
using UnityEngine.UI;

public class FatigueDisplay : BaseDisplay
{
    [Header("Fatigue Settings")]
    [SerializeField] private Entity entity;

    [SerializeField] private Image image;
    
    protected override void UpdateDisplay()
    {
        if (entity == null) return;

        image.fillAmount = 1 - ((float)entity.entityFatigue.fatigue / (float)entity.entityFatigue.maxFatigue);
    }
}