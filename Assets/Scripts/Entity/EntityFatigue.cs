using UnityEngine;

[System.Serializable]
public class EntityFatigue
{
    [SerializeField] public int fatigue;
    [SerializeField] public int maxFatigue;

    [SerializeField] private float regenPercentPerSecond = 0.02f;
    [SerializeField] private float regenMultiplier = 1f;

    public EntityFatigue(int maxFatigue, int initFatigue = 0)
    {
        this.maxFatigue = maxFatigue;
        fatigue = initFatigue;
    }

    public float GetBaseRegenPercentPerSecond() => regenPercentPerSecond;
    public float GetRegenMultiplier() => regenMultiplier;
    public float GetEffectiveRegenPercentPerSecond() => regenPercentPerSecond * regenMultiplier;

    public void SetFatigue(int value)
    {
        fatigue = Mathf.Clamp(value, 0, maxFatigue);
    }

    public void AddMaxFatigue(int amount)
    {
        maxFatigue = Mathf.Max(1, maxFatigue + amount);
        fatigue = Mathf.Clamp(fatigue, 0, maxFatigue);
    }

    public void MultiplyMaxFatigue(float factor)
    {
        maxFatigue = Mathf.Max(1, Mathf.RoundToInt(maxFatigue * factor));
        fatigue = Mathf.Clamp(fatigue, 0, maxFatigue);
    }

    public void MultiplyRegen(float factor)
    {
        regenMultiplier = Mathf.Max(0f, regenMultiplier * factor);
    }
}