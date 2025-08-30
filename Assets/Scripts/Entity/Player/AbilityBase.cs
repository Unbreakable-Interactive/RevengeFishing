using UnityEngine;

public abstract class AbilityBase : MonoBehaviour
{
    [Header("Ability Info")]
    [SerializeField] protected string abilityName;
    [TextArea] [SerializeField] protected string description;

    [Header("Requirements")]
    [SerializeField] protected Player.Phase minimumPhase = Player.Phase.Infant;
    [SerializeField] protected bool requiresUnderwater = false;
    [SerializeField] protected bool requiresAboveWater = false;

    [Header("Lock")]
    [SerializeField] private bool unlocked = false;

    protected Player player;
    protected AbilitySystem abilitySystem;

    protected string lastBlockReason = "";

    public bool IsUnlocked => unlocked;
    public void Unlock() => unlocked = true;

    protected void Bind(Player p, AbilitySystem sys) { player = p; abilitySystem = sys; }

    public string GetBlockReason() => lastBlockReason;

    public bool CanActivate()
    {
        lastBlockReason = "";

        if (!unlocked) { lastBlockReason = "Locked"; return false; }

        if (player == null)
        {
            lastBlockReason = "No player";
            return false;
        }

        if (player.currentPhase < minimumPhase)
        {
            lastBlockReason = $"Requires phase {minimumPhase}";
            return false;
        }
        if (requiresUnderwater && player.IsAboveWater)
        {
            lastBlockReason = "Requires underwater";
            return false;
        }
        if (requiresAboveWater && !player.IsAboveWater)
        {
            lastBlockReason = "Requires above water";
            return false;
        }

        return CanActivateCustom();
    }

    protected abstract bool CanActivateCustom();
    public void Activate()
    {
        if (!CanActivate()) return;
        OnActivate();
    }
    protected abstract void OnActivate();

    public virtual void Initialize(AbilitySystem sys, Player p)
    {
        Bind(p, sys);
        OnInitialize();
    }
    protected virtual void OnInitialize() {}
    
    protected void DebugLog(string message)
    {
        GameLogger.Log($"[{abilityName}] {message}");
    }
}