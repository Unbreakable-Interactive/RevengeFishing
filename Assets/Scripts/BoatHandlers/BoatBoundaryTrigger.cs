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
        Debug.Log($"BoatBoundaryTrigger: {boundType} boundary initialized on {gameObject.name} with ID {boatID} and crewManager: {crewManager.name}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != 6) return;
        
        if (debugTriggers)
            Debug.Log($"BoatBoundaryTrigger: Enemy layer object detected - {other.gameObject.name}");
        
        LandEnemy enemy = FindLandEnemyInHierarchy(other.gameObject);
        
        if (enemy == null)
        {
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: NO LandEnemy found in hierarchy of {other.gameObject.name}");
            return;
        }
        
        if (debugTriggers)
            Debug.Log($"BoatBoundaryTrigger: Found LandEnemy {enemy.name}");
        
        if (ShouldProcess(enemy))
        {
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: PROCESSING {enemy.name} (belongs to this boat)");
            StopEnemyMovement(enemy);
        }
        else
        {
            if (debugTriggers)
            {
                string enemyID = enemy is IBoatComponent comp ? comp.GetBoatID() : "NO_ID";
                Debug.Log($"BoatBoundaryTrigger: IGNORING {enemy.name} (ID: {enemyID}) - doesn't belong to this boat (ID: {boatID})");
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer != 6) return;
        
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
            if (enemy != null)
            {
                if (debugTriggers)
                    Debug.Log($"BoatBoundaryTrigger: Found LandEnemy {enemy.name} in parent {current.name}");
                return enemy;
            }
            current = current.parent;
        }
        
        return null;
    }
    
    private bool ShouldProcess(LandEnemy enemy)
    {
        if (enemy == null)
        {
            if (debugTriggers)
                Debug.Log("BoatBoundaryTrigger: Enemy is NULL in ShouldProcess");
            return false;
        }
        
        if (enemy is IBoatComponent boatComponent)
        {
            bool belongsToThisBoat = boatID.Matches(boatComponent.GetBoatID());
            
            if (debugTriggers)
            {
                Debug.Log($"BoatBoundaryTrigger: {enemy.name} (ID: {boatComponent.GetBoatID()}) belongs to this boat (ID: {boatID})? {belongsToThisBoat}");
            }
            
            return belongsToThisBoat;
        }
        
        if (debugTriggers)
        {
            Debug.Log($"BoatBoundaryTrigger: {enemy.name} doesn't implement IBoatComponent - rejecting");
        }
        
        return false;
    }
    
    private void StopEnemyMovement(LandEnemy enemy)
    {
        var currentState = enemy.MovementStateLand;
        
        if (debugTriggers)
            Debug.Log($"BoatBoundaryTrigger: StopEnemyMovement called for {enemy.name}, current state: {currentState}");
        
        bool shouldStop = false;
        
        if (isLeftBoundary && (currentState == LandEnemy.LandMovementState.WalkLeft || currentState == LandEnemy.LandMovementState.RunLeft))
        {
            shouldStop = true;
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: {enemy.name} is moving LEFT toward LEFT boundary - SHOULD STOP");
        }
        else if (!isLeftBoundary && (currentState == LandEnemy.LandMovementState.WalkRight || currentState == LandEnemy.LandMovementState.RunRight))
        {
            shouldStop = true;
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: {enemy.name} is moving RIGHT toward RIGHT boundary - SHOULD STOP");
        }
        else
        {
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: {enemy.name} current state {currentState} doesn't need stopping at {(isLeftBoundary ? "LEFT" : "RIGHT")} boundary");
        }
        
        if (shouldStop)
        {
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: STOPPING {enemy.name} at {(isLeftBoundary ? "LEFT" : "RIGHT")} boundary");
            
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: {enemy.name} state BEFORE: {enemy.MovementStateLand}");
            enemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            if (debugTriggers)
                Debug.Log($"BoatBoundaryTrigger: {enemy.name} state AFTER: {enemy.MovementStateLand}");
            
            Rigidbody2D fishermanRb = enemy.GetComponent<Rigidbody2D>();
            if (fishermanRb != null)
            {
                Vector2 velocityBefore = fishermanRb.velocity;
                if (debugTriggers)
                    Debug.Log($"BoatBoundaryTrigger: {enemy.name} velocity BEFORE: {velocityBefore}");
                
                Vector2 velocity = fishermanRb.velocity;
                velocity.x = 0f;
                fishermanRb.velocity = velocity;
                
                Vector2 velocityAfter = fishermanRb.velocity;
                if (debugTriggers)
                    Debug.Log($"BoatBoundaryTrigger: {enemy.name} velocity AFTER: {velocityAfter}");
            }
            else
            {
                Debug.LogError($"BoatBoundaryTrigger: {enemy.name} has NO Rigidbody2D!");
            }
            
            LandEnemy.LandMovementState[] excludedStates;
            if (isLeftBoundary)
            {
                excludedStates = new[] { 
                    LandEnemy.LandMovementState.WalkLeft, 
                    LandEnemy.LandMovementState.RunLeft 
                };
                if (debugTriggers)
                    Debug.Log($"BoatBoundaryTrigger: Excluding LEFT movements for {enemy.name}");
            }
            else
            {
                excludedStates = new[] { 
                    LandEnemy.LandMovementState.WalkRight, 
                    LandEnemy.LandMovementState.RunRight 
                };
                if (debugTriggers)
                    Debug.Log($"BoatBoundaryTrigger: Excluding RIGHT movements for {enemy.name}");
            }
            
            try
            {
                enemy.ChooseRandomActionExcluding(excludedStates);
                enemy.ScheduleNextAction();
                if (debugTriggers)
                    Debug.Log($"BoatBoundaryTrigger: Successfully scheduled new action for {enemy.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BoatBoundaryTrigger: Exception in ChooseRandomActionExcluding for {enemy.name}: {e.Message}");
            }
        }
    }
}
