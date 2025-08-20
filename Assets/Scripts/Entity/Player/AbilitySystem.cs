using System.Collections.Generic;
using UnityEngine;

public class AbilitySystem : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Player player;
    
    [Header("Ability Settings")]
    [SerializeField] private List<AbilityBase> abilities = new List<AbilityBase>();
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode ability1Key = KeyCode.Q;
    [SerializeField] private KeyCode ability2Key = KeyCode.E;
    [SerializeField] private KeyCode ability3Key = KeyCode.R;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    [Header("Mutual Exclusivity System")]
    [SerializeField] private bool enableMutualExclusivity = true; // Toggle for mutual exclusivity
    
    private Dictionary<KeyCode, AbilityBase> keyToAbilityMap = new Dictionary<KeyCode, AbilityBase>();
    
    // Mutual exclusivity tracking
    private AbilityBase lastActivatedAirborneAbility = null;
    private float lastAirborneActivationTime = 0f;

    void Start()
    {
        InitializeAbilitySystem();
    }

    void Update()
    {
        HandleAbilityInput();
        HandleMutualExclusivityReset();
    }
        
    private void InitializeAbilitySystem()
    {
        // Get Player component if not assigned
        if (player == null)
        {
            player = GetComponent<Player>();
            if (player == null)
            {
                player = GetComponentInParent<Player>();
            }
        }
        
        if (player == null)
        {
            GameLogger.LogError("AbilitySystem: No Player component found!");
            return;
        }
        
        // Find all abilities on this GameObject and its children
        AbilityBase[] foundAbilities = GetComponentsInChildren<AbilityBase>();
        abilities.AddRange(foundAbilities);
        
        // Initialize each ability
        foreach (var ability in abilities)
        {
            ability.Initialize(this, player);
        }
        
        // Set up key mappings for the first few abilities
        SetupKeyMappings();
        
        DebugLog($"AbilitySystem initialized with {abilities.Count} abilities");
    }
    
    private void SetupKeyMappings()
    {
        keyToAbilityMap.Clear();
        
        // Map abilities to keys based on their order
        for (int i = 0; i < abilities.Count && i < 3; i++)
        {
            KeyCode key = i switch
            {
                0 => ability1Key,
                1 => ability2Key,
                2 => ability3Key,
                _ => KeyCode.None
            };
            
            if (key != KeyCode.None)
            {
                keyToAbilityMap[key] = abilities[i];
                DebugLog($"Mapped {abilities[i].GetType().Name} to {key}");
            }
        }
    }
    
    private void HandleAbilityInput()
    {
        foreach (var kvp in keyToAbilityMap)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                TryActivateAbility(kvp.Value);
            }
        }
    }
    
    public bool TryActivateAbility(AbilityBase ability)
    {
        if (ability == null) return false;
        
        if (ability.CanActivate())
        {
            ability.Activate();
            
            // Track airborne abilities for mutual exclusivity
            if (enableMutualExclusivity && player.IsAboveWater)
            {
                lastActivatedAirborneAbility = ability;
                lastAirborneActivationTime = Time.time;
                DebugLog($"Tracked airborne ability activation: {ability.GetType().Name} at {lastAirborneActivationTime}");
            }
            
            DebugLog($"Activated ability: {ability.GetType().Name}");
            return true;
        }
        else
        {
            DebugLog($"Cannot activate {ability.GetType().Name} - {ability.GetBlockReason()}");
            return false;
        }
    }
    
    public bool TryActivateAbility<T>() where T : AbilityBase
    {
        foreach (var ability in abilities)
        {
            if (ability is T targetAbility)
            {
                return TryActivateAbility(targetAbility);
            }
        }
        
        DebugLog($"Ability of type {typeof(T).Name} not found");
        return false;
    }
    
    public T GetAbility<T>() where T : AbilityBase
    {
        foreach (var ability in abilities)
        {
            if (ability is T targetAbility)
            {
                return targetAbility;
            }
        }
        return null;
    }
    
    public void RegisterAbility(AbilityBase ability)
    {
        if (!abilities.Contains(ability))
        {
            abilities.Add(ability);
            ability.Initialize(this, player);
            DebugLog($"Registered new ability: {ability.GetType().Name}");
        }
    }
    
    public void UnregisterAbility(AbilityBase ability)
    {
        if (abilities.Contains(ability))
        {
            abilities.Remove(ability);
            DebugLog($"Unregistered ability: {ability.GetType().Name}");
        }
    }
    
    #region Mutual Exclusivity System
    
    /// <summary>
    /// Checks if this ability is the most recently activated airborne ability
    /// </summary>
    public bool IsLastActivatedAirborneAbility(AbilityBase ability)
    {
        if (!enableMutualExclusivity) return true; // No mutual exclusivity
        
        return lastActivatedAirborneAbility == ability;
    }
    
    /// <summary>
    /// Called when abilities reach apex to determine which one should continue
    /// </summary>
    public AbilityBase GetActiveAirborneAbility()
    {
        return lastActivatedAirborneAbility;
    }
    
    /// <summary>
    /// Cancels all airborne abilities except the specified one
    /// </summary>
    public void CancelOtherAirborneAbilities(AbilityBase activeAbility)
    {
        if (!enableMutualExclusivity) return;
        
        foreach (var ability in abilities)
        {
            if (ability != activeAbility)
            {
                // Check if ability has a cancel method (we'll add this to abilities)
                if (ability is Backflip backflip && backflip.IsWaitingForApex)
                {
                    DebugLog($"Cancelling Backflip due to mutual exclusivity with {activeAbility.GetType().Name}");
                    backflip.CancelBackflip();
                }
                else if (ability is BigBite bigBite && bigBite.IsWaitingForApex)
                {
                    DebugLog($"Cancelling Big Bite due to mutual exclusivity with {activeAbility.GetType().Name}");
                    bigBite.CancelBigBite();
                }
            }
        }
    }
    
    /// <summary>
    /// Resets mutual exclusivity tracking when player returns to water
    /// </summary>
    public void ResetMutualExclusivity()
    {
        if (lastActivatedAirborneAbility != null)
        {
            DebugLog($"Resetting mutual exclusivity - was tracking {lastActivatedAirborneAbility.GetType().Name}");
        }
        
        lastActivatedAirborneAbility = null;
        lastAirborneActivationTime = 0f;
    }
    
    #endregion
        
    private void HandleMutualExclusivityReset()
    {
        // Reset mutual exclusivity when player returns to water
        if (enableMutualExclusivity && lastActivatedAirborneAbility != null && !player.IsAboveWater)
        {
            ResetMutualExclusivity();
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.Log($"[AbilitySystem] {message}");
    }
}
