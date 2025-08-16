using UnityEngine;

public class BoatCrewPhysics : MonoBehaviour
{
    [Header("Crew Physics Configuration")]
    [SerializeField] private bool debugPhysics = true;
    
    private Rigidbody2D rb;
    private BoatLandEnemy boatEnemy;
    private Transform crewContainer;
    private Transform originalParent;
    private Transform handlerRoot;
    private Vector3 originalLocalPosition;
    
    private bool isInBoatMode = false;
    private bool isGrounded = false;
    private bool isParentedToBoat = false;
    private bool physicsInitialized = false;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Awake, original position: {originalLocalPosition}");
    }

    public void Initialize(Rigidbody2D rigidbody, BoatLandEnemy enemy)
    {
        rb = rigidbody;
        boatEnemy = enemy;
        
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                if (debugPhysics)
                    Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Created new Rigidbody2D");
            }
        }
        
        physicsInitialized = true;
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Initialize complete. RB: {(rb != null ? "YES" : "NO")}, Enemy: {(boatEnemy != null ? "YES" : "NO")}");
    }

    public void SetupAtPosition(Transform container, Vector3 localPosition)
    {
        crewContainer = container;
        isParentedToBoat = true;
        originalParent = transform.parent;
        
        transform.SetParent(crewContainer);
        transform.localPosition = localPosition;
        
        SetBoatMode(true);
        
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Setup at position: {localPosition}, Container: {container.name}");
    }
    
    public void SetupAsChildHandler(Transform container, Transform handlerTransform, Vector3 handlerLocalPosition)
    {
        crewContainer = container;
        isParentedToBoat = true;
        
        if (handlerTransform != null)
        {
            handlerRoot = handlerTransform;
            originalParent = handlerTransform.parent;
            
            handlerTransform.SetParent(crewContainer);
            handlerTransform.localPosition = handlerLocalPosition;
            if (debugPhysics)
                Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Setup as child handler: {handlerLocalPosition}, Handler: {handlerTransform.name}");
        }
        else
        {
            Debug.LogError($"[PHYSICS DEBUG] {gameObject.name} - Handler transform is NULL!");
        }
        
        SetBoatMode(true);
    }
    
    public void SetBoatMode(bool onBoat)
    {
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - SetBoatMode({onBoat}) called. Current mode: {isInBoatMode}");
        
        isInBoatMode = onBoat;
        
        if (isInBoatMode)
        {
            isGrounded = true;
            SetCollidersToSolid(true);
            
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.freezeRotation = true;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.drag = 0f;
                
                if (debugPhysics)
                    Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - ON BOAT PHYSICS: Kinematic mode");
            }
        }
        else
        {
            SetCollidersToSolid(false);
            
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = .1f;
                
                if (debugPhysics)
                    Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - OFF BOAT PHYSICS: Dynamic only, Entity handles the rest");
            }
        }
        
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - SetBoatMode complete");
    }
    
    public void LeaveBoat()
    {
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - LeaveBoat called. IsParentedToBoat: {isParentedToBoat}");
        
        if (isParentedToBoat)
        {
            if (handlerRoot != null)
            {
                if (debugPhysics)
                    Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Unparenting handler: {handlerRoot.name}");
                if (originalParent != null)
                {
                    handlerRoot.SetParent(originalParent);
                    if (debugPhysics)
                        Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Handler reparented to: {originalParent.name}");
                }
                else
                {
                    handlerRoot.SetParent(null);
                    if (debugPhysics)
                        Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Handler set to root");
                }
            }
            else if (originalParent != null)
            {
                transform.SetParent(originalParent);
                if (debugPhysics)
                    Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Reparented to: {originalParent.name}");
            }
            else
            {
                transform.SetParent(null);
                if (debugPhysics)
                    Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Set to root");
            }
            
            SetBoatMode(false);
            isParentedToBoat = false;
            
            if (debugPhysics)
                Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - LeaveBoat complete. Position: {transform.position}");
        }
        else
        {
            if (debugPhysics)
                Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - LeaveBoat called but not parented to boat");
        }
    }
    
    private void SetCollidersToSolid(bool keepSolid)
    {
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - SetCollidersToSolid({keepSolid})");
        
        Collider2D localCollider = GetComponent<Collider2D>();
        if (localCollider != null)
        {
            localCollider.isTrigger = !keepSolid;
        }
        
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in childColliders)
        {
            if (collider.gameObject.name == "Collider" || collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                collider.isTrigger = !keepSolid;
            }
        }
    }
    
    public void SetLocalPosition(Vector3 localPosition)
    {
        if (isParentedToBoat)
        {
            transform.localPosition = localPosition;
        }
    }
    
    public void ResetToOriginalState()
    {
        transform.localPosition = originalLocalPosition;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        
        SetBoatMode(true);
        
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - Reset to original state");
    }
    
    public void ResetPhysics()
    {
        if (rb == null) return;
        
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - ResetPhysics called");
        
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        SetCollidersToSolid(true);
        
        isInBoatMode = false;
        isGrounded = false;
        isParentedToBoat = false;
        physicsInitialized = false;
        handlerRoot = null;
        
        if (debugPhysics)
            Debug.Log($"[PHYSICS DEBUG] {gameObject.name} - ResetPhysics complete");
    }
    
    public bool IsInBoatMode() => isInBoatMode;
    public bool IsGrounded() => isGrounded;
    public bool IsParentedToBoat() => isParentedToBoat;
}
