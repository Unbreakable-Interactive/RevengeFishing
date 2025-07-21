using UnityEngine;

public class BoatFisherman : Fisherman
{
    [Header("Boat Platform Settings")]
    [SerializeField] private bool useManualBounds = false;
    [SerializeField] private float manualLeftEdge = -5f;
    [SerializeField] private float manualRightEdge = 5f;
    [SerializeField] private float boatEdgeReduction = 1f;

    private Transform boatParent;

    protected override void Start()
    {
        base.Start();
        FindBoatParent();
    }

    private void FindBoatParent()
    {
        // Look for parent with BoatPlatform layer
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.gameObject.layer == LayerMask.NameToLayer("BoatPlatform"))
            {
                boatParent = current;
                break;
            }
            current = current.parent;
        }

        // If not found by layer, look for BoatIntegrityManager component
        if (boatParent == null)
        {
            current = transform.parent;
            while (current != null)
            {
                if (current.GetComponent<BoatIntegrityManager>() != null)
                {
                    boatParent = current;
                    break;
                }
                current = current.parent;
            }
        }
    }

    protected override void CalculatePlatformBounds()
    {
        if (useManualBounds && boatParent != null)
        {
            // Use boat parent position for manual bounds
            platformLeftEdge = boatParent.position.x + manualLeftEdge;
            platformRightEdge = boatParent.position.x + manualRightEdge;
            platformBoundsCalculated = true;
            return;
        }

        if (assignedPlatform == null) return;

        if (useManualBounds)
        {
            platformLeftEdge = assignedPlatform.transform.position.x + manualLeftEdge;
            platformRightEdge = assignedPlatform.transform.position.x + manualRightEdge;
        }
        else
        {
            Collider2D platformCol = assignedPlatform.GetComponent<Collider2D>();
            if (platformCol != null)
            {
                Bounds bounds = platformCol.bounds;
                float adjustedBuffer = edgeBuffer + boatEdgeReduction;
                platformLeftEdge = bounds.min.x + adjustedBuffer;
                platformRightEdge = bounds.max.x - adjustedBuffer;
            }
        }

        platformBoundsCalculated = true;
    }

    protected override void CheckPlatformBounds()
    {
        if (!platformBoundsCalculated) return;

        float currentX = transform.position.x;

        if (currentX <= platformLeftEdge && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
        }
        else if (currentX >= platformRightEdge && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
        }
    }
}
