using UnityEngine;

public class BoatCrewAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool allowMovementOnPlatform = true;
    [SerializeField] private float platformMovementChance = 0.6f;
    [SerializeField] private bool enableIndependentMovement = true;
    
    [Header("Fishing Behavior")]
    [SerializeField] private float fishingAttemptInterval = 8f; 
    [SerializeField] private float minIdleTimeForFishing = 2f;
    
    private BoatLandEnemy boatEnemy;
    private BoatCrewPlatformTracker platformTracker;
    private BoatCrewPhysics crewPhysics;
    private BoatCrewFisherman crewFisherman;
    
    private float lastFishingAttempt = 0f;
    private float idleStartTime = 0f;
    
    public bool IsIndependentMovementEnabled => enableIndependentMovement;
    
    public void Initialize(BoatLandEnemy enemy, BoatCrewPlatformTracker tracker, BoatCrewPhysics physics)
    {
        boatEnemy = enemy;
        platformTracker = tracker;
        crewPhysics = physics;
        crewFisherman = enemy.GetComponent<BoatCrewFisherman>();
        
        GameLogger.LogError($"[CREW AI] {gameObject.name} - AI system initialized");
    }
    
    public bool ShouldMakeAIDecisions()
    {
        return !(platformTracker != null && platformTracker.IsTrackingMovement && !allowMovementOnPlatform);
    }
    
    public bool IsTimeForNextAction()
    {
        return Time.time >= boatEnemy.NextActionTime;
    }
    
    public void MakeAIDecision()
    {
        if (boatEnemy.State == Enemy.EnemyState.Defeated) return;

        if (boatEnemy.HasThrownHook)
        {
            boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            boatEnemy.ScheduleNextAction();
            return;
        }

        if (platformTracker != null && platformTracker.IsTrackingMovement && !allowMovementOnPlatform)
        {
            boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            boatEnemy.ScheduleNextAction();
            return;
        }

        // SI NO HAY CONFIG DE PESCA, SOLO MOVIMIENTO BÁSICO
        if (boatEnemy.fishermanConfig == null)
        {
            if (platformTracker != null && platformTracker.IsTrackingMovement)
            {
                MakeCrewSpecificAIDecision();
                return;
            }
            
            MakeBasicMovementDecision();
            return;
        }

        bool canAttemptFishing = CanAttemptFishing();
        
        if (canAttemptFishing)
        {
            if (!boatEnemy.fishingToolEquipped)
            {
                if (Random.value < boatEnemy.fishermanConfig.equipToolChance)
                {
                    boatEnemy.TryEquipFishingTool();
                    boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
                    GameLogger.LogError($"[CREW AI EQUIP] {gameObject.name}: Equipped fishing tool for fishing attempt");
                    boatEnemy.ScheduleNextAction();
                    return;
                }
            }
            else
            {
                if (Random.value < boatEnemy.fishermanConfig.hookThrowChance)
                {
                    if (crewFisherman != null && crewFisherman.CanThrowHook())
                    {
                        crewFisherman.ThrowHook();
                        boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
                        lastFishingAttempt = Time.time;
                        
                        GameLogger.LogError($"[CREW AI HOOK] {gameObject.name}: Threw fishing hook - CREW member idle (BOAT KEEPS MOVING)");
                        boatEnemy.ScheduleNextAction();
                        return;
                    }
                }
                else if (Random.value < boatEnemy.fishermanConfig.unequipToolChance)
                {
                    boatEnemy.TryUnequipFishingTool();
                    GameLogger.LogVerbose($"BoatCrewAI {gameObject.name}: Unequipped fishing tool");
                }
            }
        }

        if (platformTracker != null && platformTracker.IsTrackingMovement)
        {
            MakeCrewMovementDecision();
        }
        else
        {
            MakeBasicMovementDecision();
        }
        
        boatEnemy.ScheduleNextAction();
    }
    
    private bool CanAttemptFishing()
    {
        if (boatEnemy.fishermanConfig == null) return false;
        
        if (Time.time - lastFishingAttempt < fishingAttemptInterval) return false;
        
        if (!boatEnemy.trackingPlatformMovement) return false;
        
        if (boatEnemy.HasThrownHook) return false;
        
        return true;
    }
    
    private void MakeCrewSpecificAIDecision()
    {
        float random = Random.value;
        
        if (random < 0.6f) 
        {
            boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            TrackIdleTime();
        }
        else
        {
            MakeCrewMovementDecision();
        }
        boatEnemy.ScheduleNextAction();
    }

    private void MakeCrewMovementDecision()
    {
        float random = Random.value;
        
        if (random < 0.5f)
        {
            boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            TrackIdleTime();
        }
        else if (random < 0.75f)
        {
            boatEnemy.MovementStateLand = Random.value < 0.5f ? LandEnemy.LandMovementState.WalkLeft : LandEnemy.LandMovementState.WalkRight;
            ResetIdleTime();
        }
        else
        {
            boatEnemy.MovementStateLand = Random.value < 0.5f ? LandEnemy.LandMovementState.RunLeft : LandEnemy.LandMovementState.RunRight;
            ResetIdleTime();
        }
        
        if (boatEnemy.MovementStateLand != LandEnemy.LandMovementState.Idle)
        {
            GameLogger.LogVerbose($"BoatCrewAI {gameObject.name}: Crew member movement decision: {boatEnemy.MovementStateLand}");
        }
    }
    
    private void MakeBasicMovementDecision()
    {
        float random = Random.value;
        
        if (random < 0.4f) 
        {
            boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            TrackIdleTime();
        }
        else if (random < 0.7f)
        {
            boatEnemy.MovementStateLand = Random.value < 0.5f ? LandEnemy.LandMovementState.WalkLeft : LandEnemy.LandMovementState.WalkRight;
            ResetIdleTime();
        }
        else
        {
            boatEnemy.MovementStateLand = Random.value < 0.5f ? LandEnemy.LandMovementState.RunLeft : LandEnemy.LandMovementState.RunRight;
            ResetIdleTime();
        }
        
        boatEnemy.ScheduleNextAction();
    }
    
    private void TrackIdleTime()
    {
        if (idleStartTime == 0f)
        {
            idleStartTime = Time.time;
        }
    }
    
    private void ResetIdleTime()
    {
        idleStartTime = 0f;
    }
    
    public Vector2 GetTargetHorizontalVelocity()
    {
        if (boatEnemy == null) return Vector2.zero;
        
        if (boatEnemy.HasThrownHook)
        {
            return Vector2.zero; 
        }
        
        switch (boatEnemy.MovementStateLand)
        {
            case LandEnemy.LandMovementState.WalkLeft:
                return Vector2.left * boatEnemy.WalkingSpeed;
            case LandEnemy.LandMovementState.WalkRight:
                return Vector2.right * boatEnemy.WalkingSpeed;
            case LandEnemy.LandMovementState.RunLeft:
                return Vector2.left * boatEnemy.RunningSpeed;
            case LandEnemy.LandMovementState.RunRight:
                return Vector2.right * boatEnemy.RunningSpeed;
            case LandEnemy.LandMovementState.Idle:
            default:
                return Vector2.zero;
        }
    }
    
    public void SetIndependentMovement(bool independent)
    {
        enableIndependentMovement = independent;
        
        GameLogger.LogVerbose($"BoatCrewAI {gameObject.name}: Independent movement set to {independent}");
    }
}
