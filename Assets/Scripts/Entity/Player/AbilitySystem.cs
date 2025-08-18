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
    
    private Dictionary<KeyCode, AbilityBase> keyToAbilityMap = new Dictionary<KeyCode, AbilityBase>();

    void Start()
    {
        InitializeAbilitySystem();
    }

    void Update()
    {
        HandleAbilityInput();
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
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.Log($"[AbilitySystem] {message}");
    }
}
