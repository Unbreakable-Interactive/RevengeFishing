using UnityEngine;

public static class BoatSpawnHelper
{
    public static float MINIMUM_BOAT_SEPARATION = 6f;
    public static LayerMask BOAT_LAYER_MASK = (1 << 6);
    public static int MAX_SPAWN_ATTEMPTS = 15;
    
    public static bool IsPositionFreeForBoat(Vector3 position, bool showDebug = false)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, MINIMUM_BOAT_SEPARATION, BOAT_LAYER_MASK);
        
        foreach (var collider in nearbyColliders)
        {
            BoatController nearbyBoat = collider.GetComponent<BoatController>();
            if (nearbyBoat != null)
            {
                float distance = Vector3.Distance(position, collider.transform.position);
                
                if (showDebug)
                {
                    GameLogger.LogVerbose($"Boat collision detected! Distance: {distance:F2}m (minimum: {MINIMUM_BOAT_SEPARATION}m)");
                }
                
                return false;
            }
        }
        
        if (showDebug)
        {
            GameLogger.LogVerbose($"Position {position} is free for boat spawn");
        }
        
        return true; 
    }
    
    public static Vector3 FindValidBoatSpawnPosition(Vector3 point1, Vector3 point2, SpawnHandlerConfig config)
    {
        for (int attempt = 0; attempt < MAX_SPAWN_ATTEMPTS; attempt++)
        {
            float xPosition = Random.Range(Mathf.Min(point1.x, point2.x), Mathf.Max(point1.x, point2.x));
            float yPosition = Random.Range(Mathf.Min(point1.y, point2.y), Mathf.Max(point1.y, point2.y));
            Vector3 candidatePosition = new Vector3(xPosition, yPosition, 0);
            
            if (!config.IsValidDistance(candidatePosition))
            {
                continue;
            }
            
            if (IsPositionFreeForBoat(candidatePosition, config.showLogs))
            {
                if (config.showLogs)
                {
                    GameLogger.LogVerbose($"🎯 Found valid boat spawn position after {attempt + 1} attempts: {candidatePosition}");
                }
                return candidatePosition;
            }
        }
        
        GameLogger.LogWarning($"⚠Could not find free boat spawn position after {MAX_SPAWN_ATTEMPTS} attempts! Boats might overlap.");
        return Vector3.zero;
    }
}
