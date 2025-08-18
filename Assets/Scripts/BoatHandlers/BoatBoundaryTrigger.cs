using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BoatBoundaryTrigger : MonoBehaviour
{
    [SerializeField] private bool isLeftBoundary = true;
    [SerializeField] private string boatID = "";
    
    private const int ENEMY_LAYER = 6;
    
    public bool IsLeftBoundary => isLeftBoundary;
    
    public void SetBoatID(BoatID newBoatID)
    {
        if (newBoatID != null)
        {
            boatID = newBoatID.UniqueID;
        }
    }
    
    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        BoatPlatform platform = GetComponentInParent<BoatPlatform>();
        if (platform != null)
        {
            boatID = platform.GetBoatID();
        }
        
        GameLogger.LogVerbose($"[BOUNDARY TRIGGER] {(isLeftBoundary ? "LEFT" : "RIGHT")} boundary initialized - ID: {boatID}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != ENEMY_LAYER) return;
        
        BoatLandEnemy enemy = FindBoatLandEnemyInHierarchy(other.gameObject);
        
        if (enemy == null || !ShouldProcess(enemy)) return;
        
        GameLogger.LogVerbose($"[BOUNDARY HIT] {enemy.name} hit {(isLeftBoundary ? "LEFT" : "RIGHT")} boundary - handled by BoatLandEnemy");
    }
    
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (other.gameObject.layer != ENEMY_LAYER) return;
    //
    //     BoatLandEnemy enemy = FindBoatLandEnemyInHierarchy(other.gameObject);
    //
    //     if (enemy == null || !ShouldProcess(enemy)) return;
    //
    //     if (debugTriggers)
    //         GameLogger.LogVerbose($"[BOUNDARY HIT] {enemy.name} hit {(isLeftBoundary ? "LEFT" : "RIGHT")} boundary - calling OnBoundaryHit!");
    //
    //     enemy.OnBoundaryHit(isLeftBoundary);
    // }
    
    private BoatLandEnemy FindBoatLandEnemyInHierarchy(GameObject go)
    {
        BoatLandEnemy enemy = go.GetComponent<BoatLandEnemy>();
        if (enemy != null) return enemy;
        
        enemy = go.GetComponentInParent<BoatLandEnemy>();
        if (enemy != null) return enemy;
        
        enemy = go.GetComponentInChildren<BoatLandEnemy>();
        return enemy;
    }
    
    private bool ShouldProcess(BoatLandEnemy enemy)
    {
        if (enemy == null) return false;
        
        string enemyBoatID = enemy.GetBoatID();
        
        if (string.IsNullOrEmpty(enemyBoatID) || string.IsNullOrEmpty(boatID))
        {
            GameLogger.LogWarning($"[BOUNDARY] Missing BoatID - Enemy: '{enemyBoatID}', Boundary: '{boatID}'");
            return false;
        }
        
        bool belongs = enemyBoatID == boatID;
        
        if (!belongs)
            GameLogger.LogVerbose($"[BOUNDARY] Enemy {enemy.name} (ID: {enemyBoatID}) doesn't belong to this boat (ID: {boatID})");
            
        return belongs;
    }
    
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;
        
        Gizmos.color = isLeftBoundary ? Color.red : Color.blue;
        
        if (col is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(circle.offset, circle.radius);
        }
    }
}
