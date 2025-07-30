using UnityEngine;
using Utils;

public class BoatPlatform : Platform, IBoatComponent
{
    [Header("Boat Identity - ASSIGN IN EDITOR")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Required References - ASSIGN IN EDITOR")]
    [SerializeField] private BoatCrewManager crewManager;
    
    [Header("BOAT SPECIFIC SETTINGS")]
    [SerializeField] private bool autoStartMovementOnRegistration = true;
    [SerializeField] private bool debugBoatTriggers = true;
    [SerializeField] private BoatFloater boatFloater;
    
    public string GetBoatID() => boatID.UniqueID;
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    private void Awake()
    {
        ValidateRequiredReferences();
    }
    
    private void ValidateRequiredReferences()
    {
        if (crewManager == null)
            throw new System.Exception($"BoatPlatform on {gameObject.name}: crewManager must be assigned in editor!");
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (LayerMask.NameToLayer(LayerNames.BOATPLATFORM) != -1)
        {
            gameObject.layer = LayerMask.NameToLayer(LayerNames.BOATPLATFORM);
            if (debugBoatTriggers)
            {
                Debug.Log($"BoatPlatform: Set layer to BoatPlatform (layer {gameObject.layer})");
            }
        }
        else
        {
            Debug.LogWarning($"BoatPlatform: 'BoatPlatform' layer not found! Using Platform layer instead.");
        }
        
        // Find BoatFloater in parent hierarchy if not manually assigned
        if (boatFloater == null)
        {
            boatFloater = GetComponentInParent<BoatFloater>();
            
            if (boatFloater != null && debugBoatTriggers)
            {
                Debug.Log($"BoatPlatform: Auto-found BoatFloater in {boatFloater.name}");
            }
            else if (debugBoatTriggers)
            {
                Debug.LogWarning($"BoatPlatform: No BoatFloater found in parent hierarchy for {gameObject.name}");
            }
        }
        
        if (debugBoatTriggers)
        {
            Debug.Log($"BoatPlatform: Initialized on {gameObject.name} with ID {boatID}");
        }
    }
    
    /// <summary>
    /// FILTRO PRINCIPAL: Solo procesar fishermen que pertenecen a ESTE barco por ID
    /// </summary>
    protected override void RegisterEnemyOnCollision(LandEnemy enemy)
    {
        if (enemy == null || enemy.gameObject == null) return;
        
        // CRÍTICO: Solo procesar fishermen que pertenecen a ESTE barco por ID único
        if (!DoesEnemyBelongToThisBoat(enemy))
        {
            if (debugBoatTriggers)
            {
                string enemyID = enemy is IBoatComponent comp ? comp.GetBoatID() : "NO_ID";
                Debug.Log($"BoatPlatform: Ignoring {enemy.name} (ID: {enemyID}) - doesn't belong to this boat (ID: {boatID})");
            }
            return;
        }
        
        if (assignedEnemies.Contains(enemy)) return;
        
        Platform previousPlatform = enemy.GetAssignedPlatform();
        if (previousPlatform != null && previousPlatform != this)
        {
            previousPlatform.UnregisterEnemy(enemy);
            if (showDebugInfo)
                Debug.Log($"Enemy {enemy.name} MOVED from {previousPlatform.name} to {gameObject.name}");
        }
        
        assignedEnemies.Add(enemy);
        enemy.SetAssignedPlatform(this);
        
        // CRÍTICO: NO MÁS SetParent automático - los fishermen YA están en jerarquía correcta
        // Los BoatFishermanHandler ya son hijos del BoatHandler por diseño
        
        // CRITICAL: Trigger AI activation after platform assignment
        enemy.OnPlatformAssigned(this);
        
        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            Collider2D platformCollider = GetComponent<Collider2D>();
            if (platformCollider != null)
                Physics2D.IgnoreCollision(platformCollider, enemyCollider, false);
        }

        enemy.platformBoundsCalculated = true;

        if (debugBoatTriggers)
            Debug.Log($"BOAT COLLISION ASSIGNMENT: {enemy.name} assigned to boat platform {gameObject.name} (ID: {boatID}). Total enemies: {assignedEnemies.Count}");
        
        TriggerBoatMovement(enemy);
    }
    
    /// <summary>
    /// VERIFICACIÓN POR ID ÚNICO: Verificar si el enemy pertenece a ESTE barco
    /// </summary>
    private bool DoesEnemyBelongToThisBoat(LandEnemy enemy)
    {
        if (enemy is IBoatComponent boatComponent)
        {
            bool matches = boatID.Matches(boatComponent.GetBoatID());
            
            if (debugBoatTriggers)
            {
                Debug.Log($"BoatPlatform ID Check: Enemy {enemy.name} (ID: {boatComponent.GetBoatID()}) vs Platform (ID: {boatID}) = {matches}");
            }
            
            return matches;
        }
        
        if (debugBoatTriggers)
        {
            Debug.Log($"BoatPlatform: Enemy {enemy.name} doesn't implement IBoatComponent - rejecting");
        }
        
        return false;
    }
    
    /// <summary>
    /// Override runtime registration - también con filtro por ID
    /// </summary>
    public override void RegisterEnemyAtRuntime(LandEnemy enemy)
    {
        if (enemy != null && !assignedEnemies.Contains(enemy))
        {
            // FILTRO: Solo procesar si pertenece a este barco por ID
            if (!DoesEnemyBelongToThisBoat(enemy))
            {
                if (debugBoatTriggers)
                {
                    string enemyID = enemy is IBoatComponent comp ? comp.GetBoatID() : "NO_ID";
                    Debug.Log($"BoatPlatform RUNTIME: Ignoring {enemy.name} (ID: {enemyID}) - doesn't belong to this boat (ID: {boatID})");
                }
                return;
            }
            
            assignedEnemies.Add(enemy);
            enemy.SetAssignedPlatform(this);

            // CRÍTICO: NO MÁS SetParent en runtime tampoco

            // Apply collision rules immediately
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
            {
                Collider2D platformCollider = GetComponent<Collider2D>();
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, platformCollider, false);
                }
            }

            if (debugBoatTriggers)
            {
                Debug.Log($"BOAT RUNTIME: Auto-assigned {enemy.name} to boat platform {gameObject.name} (ID: {boatID})");
            }
            
            TriggerBoatMovement(enemy);
        }
    }
    
    /// <summary>
    /// Triggers boat movement when an enemy registers to this platform
    /// </summary>
    private void TriggerBoatMovement(LandEnemy enemy)
    {
        if (!autoStartMovementOnRegistration) return;
        
        if (boatFloater != null)
        {
            boatFloater.RecalculateBuoyancy();
            boatFloater.OnRegisteredToPlatform(this);
            
            if (debugBoatTriggers)
            {
                Debug.Log($"BoatPlatform: Triggered boat movement for {enemy.name} on boat {boatFloater.name}");
            }
        }
        else if (debugBoatTriggers)
        {
            Debug.LogWarning($"BoatPlatform: Cannot trigger boat movement - BoatFloater not found!");
        }
    }
    
    public void SetBoatFloater(BoatFloater floater)
    {
        boatFloater = floater;
        
        if (debugBoatTriggers)
        {
            Debug.Log($"BoatPlatform: BoatFloater manually assigned: {floater.name}");
        }
    }
    
    public BoatFloater GetBoatFloater()
    {
        return boatFloater;
    }
    
    [ContextMenu("🧪 TEST: Trigger Boat Movement")]
    public void ManualTriggerBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.OnRegisteredToPlatform(this);
            Debug.Log("🧪 BoatPlatform: Manual boat movement triggered!");
        }
        else
        {
            Debug.LogWarning("🧪 BoatPlatform: Cannot trigger - BoatFloater not found!");
        }
    }
    
    public void ForceStartBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.ForceStartMovement();
            
            if (debugBoatTriggers)
            {
                Debug.Log("BoatPlatform: Force started boat movement");
            }
        }
    }
    
    public void StopBoatMovement()
    {
        if (boatFloater != null)
        {
            boatFloater.StopMovement();
            
            if (debugBoatTriggers)
            {
                Debug.Log("BoatPlatform: Stopped boat movement");
            }
        }
    }
    
    public bool IsBoatMoving()
    {
        return boatFloater != null && boatFloater.IsMovementActive();
    }
}
