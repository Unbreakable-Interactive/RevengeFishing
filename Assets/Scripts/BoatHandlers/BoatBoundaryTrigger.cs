using UnityEngine;

public class BoatBoundaryTrigger : MonoBehaviour, IBoatComponent
{
    [Header("Boat Identity - ASSIGN IN EDITOR")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Required References - ASSIGN IN EDITOR")]
    [SerializeField] private BoatCrewManager crewManager;
    
    [Header("Boundary Settings")]
    [SerializeField] private bool isLeftBoundary = true;
    [SerializeField] private bool debugTriggers = true;
    
    private const int ENEMY_LAYER = 6;
    
    public string GetBoatID() => boatID.UniqueID;
    public void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    private void Awake()
    {
        ValidateRequiredReferences();
    }
    
    private void ValidateRequiredReferences()
    {
        if (crewManager == null)
            throw new System.Exception($"BoatBoundaryTrigger on {gameObject.name}: crewManager must be assigned in editor!");
    }
    
    private void Start()
    {
        string boundType = isLeftBoundary ? "LEFT" : "RIGHT";
        GameLogger.LogError($"[BOUNDARY TRIGGER] {boundType} boundary initialized - ID: {boatID}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != ENEMY_LAYER) return;
        
        LandEnemy enemy = FindLandEnemyInHierarchy(other.gameObject);
        
        if (enemy == null)
        {
            if (debugTriggers)
                GameLogger.LogVerbose($"BoatBoundaryTrigger: NO LandEnemy found in {other.gameObject.name}");
            return;
        }
        
        if (ShouldProcess(enemy))
        {
            if (debugTriggers)
                GameLogger.LogError($"[BOUNDARY HIT] {enemy.name} hit {(isLeftBoundary ? "LEFT" : "RIGHT")} boundary");
            StopEnemyMovement(enemy);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer != ENEMY_LAYER) return;
        
        LandEnemy enemy = FindLandEnemyInHierarchy(other.gameObject);
        if (enemy != null && ShouldProcess(enemy))
        {
            StopEnemyMovement(enemy);
        }
    }
    
    private LandEnemy FindLandEnemyInHierarchy(GameObject obj)
    {
        LandEnemy enemy = obj.GetComponent<LandEnemy>();
        if (enemy != null) return enemy;
        
        Transform current = obj.transform;
        while (current != null)
        {
            enemy = current.GetComponent<LandEnemy>();
            if (enemy != null) return enemy;
            current = current.parent;
        }
        
        return null;
    }
    
    private bool ShouldProcess(LandEnemy enemy)
    {
        if (enemy == null) return false;
        
        if (enemy is IBoatComponent boatComponent)
        {
            return boatID.Matches(boatComponent.GetBoatID());
        }
        
        return false;
    }
    
    private void StopEnemyMovement(LandEnemy enemy)
    {
        var currentState = enemy.MovementStateLand;
        bool shouldStop = false;
        
        if (isLeftBoundary && (currentState == LandEnemy.LandMovementState.WalkLeft || currentState == LandEnemy.LandMovementState.RunLeft))
        {
            shouldStop = true;
        }
        else if (!isLeftBoundary && (currentState == LandEnemy.LandMovementState.WalkRight || currentState == LandEnemy.LandMovementState.RunRight))
        {
            shouldStop = true;
        }
        
        if (shouldStop)
        {
            enemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 velocity = enemyRb.velocity;
                velocity.x = 0f;
                enemyRb.velocity = velocity;
            }
            
            LandEnemy.LandMovementState[] excludedStates;
            if (isLeftBoundary)
            {
                excludedStates = new[] { 
                    LandEnemy.LandMovementState.WalkLeft, 
                    LandEnemy.LandMovementState.RunLeft 
                };
            }
            else
            {
                excludedStates = new[] { 
                    LandEnemy.LandMovementState.WalkRight, 
                    LandEnemy.LandMovementState.RunRight 
                };
            }
            
            try
            {
                enemy.ChooseRandomActionExcluding(excludedStates);
                enemy.ScheduleNextAction();
                
                if (debugTriggers)
                    GameLogger.LogError($"[BOUNDARY STOP] {enemy.name} stopped and rescheduled");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError($"BoatBoundaryTrigger: Exception for {enemy.name}: {e.Message}");
            }
        }
    }
}
