using UnityEngine;

public class BoatFisherman : Fisherman
{
    [Header("Boat Fisherman Specific")]
    [SerializeField] private BoatController boatController;
    
    public System.Action<BoatFisherman> OnCrewMemberDefeated;
    public System.Action<BoatFisherman> OnCrewMemberEaten;
    
    public void SetBoatController(BoatController controller)
    {
        boatController = controller;
    }
    
    protected override void TriggerDefeat()
    {
        base.TriggerDefeat();
        
        OnCrewMemberDefeated?.Invoke(this);
    }
    
    protected override void TriggerEaten()
    {
        base.TriggerEaten();
        
        OnCrewMemberEaten?.Invoke(this);
    }
    
    public override void LandMovement()
    {
        if (Time.time >= nextActionTime)
        {
            MakeAIDecision();
        }
        
        if (platformBoundsCalculated)
        {
            CheckPlatformBounds();
        }
    }
    
    protected override void CalculatePlatformBounds()
    {
        if (assignedPlatform == null) 
        {
            Debug.LogWarning($"{gameObject.name} - No assigned platform for BoatFisherman bounds calculation");
            return;
        }

        BoatPlatform boatPlatform = assignedPlatform as BoatPlatform;
        if (boatPlatform == null)
        {
            Debug.LogError($"{gameObject.name} - Assigned platform is not a BoatPlatform!");
            return;
        }

        Collider2D platformCol = boatPlatform.GetComponent<Collider2D>();
        
        if (platformCol != null)
        {
            Bounds bounds = platformCol.bounds;
            
            float boatEdgeBuffer = edgeBuffer * 2f;
            
            platformLeftEdge = bounds.min.x + boatEdgeBuffer;
            platformRightEdge = bounds.max.x - boatEdgeBuffer;
            platformBoundsCalculated = true;

            if (enableDebugMessages)
            {
                Debug.Log($"BOAT BOUNDS: {gameObject.name} calculated boat platform bounds: Left={platformLeftEdge:F2}, Right={platformRightEdge:F2}, Buffer={boatEdgeBuffer:F2}");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name} - BoatPlatform {boatPlatform.name} has no Collider2D for bounds calculation!");
        }
    }
    
    protected override void CheckPlatformBounds()
    {
        if (!platformBoundsCalculated) return;
        
        float currentX = transform.position.x;
        
        if (currentX <= platformLeftEdge || currentX >= platformRightEdge)
        {
            if (rb != null)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
            
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp(currentX, platformLeftEdge, platformRightEdge);
            transform.position = clampedPosition;
            
            _landMovementState = LandMovementState.Idle;
            
            if (enableDebugMessages)
            {
                Debug.Log($"BOAT BOUNDS: {gameObject.name} clamped to boat bounds at X={clampedPosition.x:F2}");
            }
        }
    }
    
    protected override void CleanupBeforePoolReturn()
    {
        base.CleanupBeforePoolReturn();
        
        if (boatController != null)
        {
            boatController = null;
        }
        
        OnCrewMemberDefeated = null;
        OnCrewMemberEaten = null;
    }
    
    protected override void TriggerEscape()
    {
      
        Debug.Log($"{gameObject.name} - BoatFisherman can't escape individually, boat integrity will handle this");
    }
}
