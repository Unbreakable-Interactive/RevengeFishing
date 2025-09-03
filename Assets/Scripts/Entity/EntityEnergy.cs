using UnityEngine;

[System.Serializable]
public class EntityEnergy
{
    [Header("Energy System")]
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float maxEnergy = 100f;
    
    [Header("Energy Decay")]
    [SerializeField] private float energyDecayPerSecond = 1f; // 1% por segundo según David
    
    [Header("Energy Recovery")]
    [SerializeField] private float energyRecoveryMultiplier = 1f;

    public float EnergyRecoveryMultiplier => energyRecoveryMultiplier;

    // Properties públicas
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyPercentage => maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;
    public bool IsEnergyDepleted => currentEnergy <= 0f;
    
    // Constructor - David especifica que energy empieza en 100%
    public EntityEnergy(float maxEnergy = 100f, float startingEnergy = 100f)
    {
        this.maxEnergy = maxEnergy;
        this.currentEnergy = startingEnergy;
    }
    
    // Energy decay según requerimientos de David: 1% por segundo
    public void DecayEnergy(float deltaTime)
    {
        float decay = energyDecayPerSecond * deltaTime;
        ModifyEnergy(-decay);
    }
    
    // Modificar energía (positivo para recuperar, negativo para consumir)
    public void ModifyEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
    }
    
    // Consumir energía por daño
    public void ConsumeEnergyFromDamage(float damageAmount)
    {
        ModifyEnergy(-damageAmount);
    }
    
    // Recuperar energía al comer
    public void RecoverEnergyFromEating(float recoveryAmount)
    {
        float actualRecovery = recoveryAmount * energyRecoveryMultiplier;
        ModifyEnergy(actualRecovery);
    }
    
    // Setters para upgrades
    public void SetMaxEnergy(float newMaxEnergy)
    {
        maxEnergy = Mathf.Max(1f, newMaxEnergy);
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }
    
    public void MultiplyMaxEnergy(float factor)
    {
        SetMaxEnergy(maxEnergy * factor);
    }
    
    public void MultiplyRecoveryRate(float factor)
    {
        energyRecoveryMultiplier = Mathf.Max(0f, energyRecoveryMultiplier * factor);
    }
    
    public void AddMaxEnergy(float amount)
    {
        SetMaxEnergy(maxEnergy + amount);
    }
    
    public void SetEnergyDecayRate(float newDecayRate)
    {
        energyDecayPerSecond = Mathf.Max(0f, newDecayRate);
    }
    
    public void MultiplyEnergyDecayRate(float factor)
    {
        energyDecayPerSecond = Mathf.Max(0f, energyDecayPerSecond * factor);
    }
}
