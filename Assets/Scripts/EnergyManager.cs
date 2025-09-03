using UnityEngine;
using System.Collections;

public class EnergyManager : MonoBehaviour
{
    [Header("Energy Configuration")]
    [SerializeField] private float energyUpdateInterval = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    [SerializeField] private Player player;
    private Coroutine energyDecayCoroutine;
    
    public float CurrentEnergyRate => CalculateCurrentEnergyDecayRate();
    public float MaxEnergy => player?.entityEnergy.MaxEnergy ?? 0f;
    
    public System.Action<float> OnEnergyChanged;
    public System.Action<float> OnEnergyRateChanged;
    public System.Action OnEnergyDepleted;
    
    private void Start()
    {
        Initialize();
    }
    
    public void Initialize()
    {
        if (player == null) player = GetComponent<Player>();
        if (player == null) return;
        
        StartEnergyDecaySystem();
    }
    
    public void StartEnergyDecaySystem()
    {
        if (player == null) return;
        energyDecayCoroutine = StartCoroutine(EnergyDecayCoroutine());
    }
    
    public void StopEnergyDecaySystem()
    {
        if (energyDecayCoroutine != null) 
        { 
            StopCoroutine(energyDecayCoroutine); 
            energyDecayCoroutine = null; 
        }
    }
    
    private IEnumerator EnergyDecayCoroutine()
    {
        while (player != null)
        {
            ProcessEnergyDecay();
            yield return new WaitForSeconds(energyUpdateInterval);
        }
    }
    
    private void ProcessEnergyDecay()
    {
        if (player == null || player.entityEnergy == null) return;
        
        // Decay según David: 1% por segundo constante
        player.entityEnergy.DecayEnergy(energyUpdateInterval);
        
        float energyPercentage = player.entityEnergy.EnergyPercentage;
        OnEnergyChanged?.Invoke(energyPercentage);
        OnEnergyRateChanged?.Invoke(CalculateCurrentEnergyDecayRate());
        
        // Verificar si se agotó la energía
        if (player.entityEnergy.IsEnergyDepleted)
        {
            HandleEnergyDepletion();
        }
        
        player.PlayerStats?.RefreshNow();
    }
    
    private float CalculateCurrentEnergyDecayRate()
    {
        if (player?.entityEnergy == null) return 0f;
        return 1f; // Constante según David: 1% por segundo
    }
    
    private void HandleEnergyDepletion()
    {
        OnEnergyDepleted?.Invoke();
        player.PlayerDie(Player.Status.Starved);
        StopEnergyDecaySystem();
    }
    
    public float GetEnergyPercentage()
    {
        if (player?.entityEnergy == null) return 0f;
        return player.entityEnergy.EnergyPercentage;
    }
    
    public void HandleEnemyDamage(float damageAmount)
    {
        if (player?.entityEnergy != null)
        {
            player.entityEnergy.ConsumeEnergyFromDamage(damageAmount);
            OnEnergyChanged?.Invoke(player.entityEnergy.EnergyPercentage);
            
            if (enableDebugLogs)
            {
                GameLogger.Log($"Player took {damageAmount} energy damage. Current energy: {player.entityEnergy.CurrentEnergy:F1}%");
            }
        }
    }
    
    public void HandlePlayerEating(float recoveryAmount)
    {
        if (player?.entityEnergy != null)
        {
            player.entityEnergy.RecoverEnergyFromEating(recoveryAmount);
            OnEnergyChanged?.Invoke(player.entityEnergy.EnergyPercentage);
            
            if (enableDebugLogs)
            {
                GameLogger.Log($"Player recovered {recoveryAmount} energy from eating. Current energy: {player.entityEnergy.CurrentEnergy:F1}%");
            }
        }
    }
    
    private void OnDestroy()
    {
        StopEnergyDecaySystem();
    }
}
