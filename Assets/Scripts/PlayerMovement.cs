using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Vector2 lastMousePosition = Vector2.zero;
    private Quaternion targetRotation;
    private bool isRotatingToTarget = false;
    private bool shouldApplyForceAfterRotation = false;
    private bool hasAppliedBoost = false; // Track if boost was already applied
    private bool isMoving = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;
    public float rotationThreshold = 10f; // Degrees - How close to target before considering "complete"
    public float boostThreshold = 10f; // Degrees - When to apply boost (higher = earlier boost)


    [Header("Movement Settings")]
    public float forceAmount = 1f;
    public ForceMode2D forceMode = ForceMode2D.Impulse;
    public float maxSpeed = 5f;
    public float naturalDrag = 0.5f;
    public float rotationDrag = 1f; // Extra drag applied during rotation
    public float constantAccel = 0.2f; 
    public float minForwardVelocity = 1f; 
    public float sidewaysDrag = 1f; // higher = less sideways drift

    [Header("Steering Settings")]
    public float steeringForce = 5f;
    public float minVelocityForSteering = 0.5f; // Minimum speed needed to steer
    public float steeringDamping = 0.98f; // Reduces velocity over time when steering

    [Header("Debug")]
    public bool enableDebugLogs = false;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main ?? FindObjectOfType<Camera>();

        rb = GetComponent<Rigidbody2D>();

        // Add Rigidbody2D if it doesn't exist
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            DebugLog("Added Rigidbody2D component to Player");
        }
        rb.drag = naturalDrag;

        targetRotation = transform.rotation; //sets the target rotation to the player's current rotation
    }

    // Update is called once per frame
    void Update()
    {
        HandleMouseInput();
        UpdateMovementState();
        UpdateRotation();
        HandleSteering();
        ApplyConstantAccel();
        ReduceSidewaysVelocity();
        ClampVelocity();
    }

    void HandleMouseInput()
    {

        if (Input.GetMouseButtonDown(0))
        {
            OnMouseClick();
        }

        if (Input.GetMouseButton(0))
        {
            OnMouseHold();
            lastMousePosition = GetMouseWorldPosition();
        }

        if (!Input.GetMouseButton(0))
        {
            WhileMouseUnheld(lastMousePosition);
        }
    }

    void UpdateMovementState()
    {
        // Consider the object moving if it has significant velocity
        isMoving = rb.velocity.magnitude > minVelocityForSteering;
    }

    void UpdateRotation()
    {
        if (isRotatingToTarget)
        {
            ApplyRotationDeceleration(); // Start decelerating immediately
            ContinueRotationToTarget();
            CheckForBoostTiming(); // Check if it's time to boost
            CheckRotationCompletion();
        }
    }

    void HandleSteering()
    {
        // Only allow steering if:
        // 1. Object is moving
        // 2. Mouse is being held (for steering input)
        // 3. Not currently doing initial rotation
        if (isMoving && Input.GetMouseButton(0) && !shouldApplyForceAfterRotation)
        {
            ApplySteering(GetMouseWorldPosition());
        }
    }

    //Triggers frame mouse is clicked
    void OnMouseClick()
    {
        Vector2 mousePosition = GetMouseWorldPosition();
        SetTargetRotation(mousePosition);
        shouldApplyForceAfterRotation = true; // Flag to apply force when rotation completes

        DebugLog("Mouse clicked - rotating to point at: " + mousePosition);
        hasAppliedBoost = false;
    }

    //Triggers as long as mouse is being clicked
    void OnMouseHold()
    {
        // If we're not currently doing initial rotation and we're moving, this will trigger steering
        if (!shouldApplyForceAfterRotation && !isRotatingToTarget)
        {
            DebugLog("Mouse held - steering");
        }
        else
        {
            // Still doing initial rotation/launch
            Vector2 mousePosition = GetMouseWorldPosition();
            SetTargetRotation(mousePosition);
            DebugLog("Mouse held - continuously rotating to: " + mousePosition);
        }

    }

    //Triggers as long as the mouse remains unclicked
    void WhileMouseUnheld(Vector2 lastMousePosition)
    {
        //Let object coast if mouse is released
    }

    void SetTargetRotation(Vector2 mousePosition)
    {
        Vector2 direction = mousePosition - (Vector2)transform.position;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            isRotatingToTarget = true;
        }
    }

    void ApplyRotationDeceleration()
    {
        // Apply extra drag during rotation to simulate fish slowing down to turn
        rb.velocity *= (1f - rotationDrag * Time.deltaTime);
        DebugLog($"Applying rotation deceleration - Current speed: {rb.velocity.magnitude:F2}");
    }

    void ContinueRotationToTarget()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void CheckForBoostTiming()
    {
        if (hasAppliedBoost) return; // Only boost once per rotation

        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        // Apply boost when close to target (but not complete)
        if (angleDifference <= boostThreshold)
        {
            ApplyForceInDirection();
            hasAppliedBoost = true;
            DebugLog($"Applied boost at {angleDifference:F1} degrees from target!");
        }
    }

    void CheckRotationCompletion()
    {
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        if (angleDifference < rotationThreshold)
        {
            // Rotation is complete
            transform.rotation = targetRotation; // Snap to exact rotation
            isRotatingToTarget = false;

            // Apply force
            //ApplyForceInDirection();
            //shouldApplyForceAfterRotation = false;

            DebugLog("Rotation completed!");
        }
    }

    void ApplyForceInDirection()
    {
        // Get the direction the object is facing (right direction in local space)
        Vector2 forceDirection = transform.right;

        // Apply force in that direction
        rb.AddForce(forceDirection * forceAmount, forceMode);

        DebugLog("Applied force in direction: " + forceDirection);
    }

    void ApplySteering(Vector2 targetPosition)
    {
        Vector2 direction = targetPosition - (Vector2)transform.position;

        if (direction != Vector2.zero)
        {
            // Rotate toward the target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Apply steering force in the direction we're facing
            Vector2 steeringDirection = transform.right;
            rb.AddForce(steeringDirection * steeringForce, ForceMode2D.Force);

            // Apply damping to prevent infinite acceleration
            rb.velocity *= steeringDamping;

            DebugLog("Steering toward: " + targetPosition);
        }
    }

    void ApplyConstantAccel()
    {
        // Apply small constant force in the direction the object is facing
        Vector2 forwardDirection = transform.right;
        // Calculate how fast we're moving in the forward direction
        float forwardVelocity = Vector2.Dot(rb.velocity, forwardDirection);

        // Only apply constant force if we're not moving forward fast enough
        if (forwardVelocity < minForwardVelocity)
        {
            rb.AddForce(forwardDirection * constantAccel, ForceMode2D.Force);
            DebugLog($"Applying thrust - Forward velocity: {forwardVelocity:F2}");
        }
    }

    void ReduceSidewaysVelocity()
    {
        if (rb.velocity.magnitude < 0.1f) return; // Skip if barely moving

        Vector2 forwardDirection = transform.right;
        Vector2 currentVelocity = rb.velocity;

        // Calculate forward and sideways components of velocity
        float forwardVelocity = Vector2.Dot(currentVelocity, forwardDirection);
        Vector2 forwardVelocityVector = forwardDirection * forwardVelocity;
        Vector2 sidewaysVelocityVector = currentVelocity - forwardVelocityVector;

        // Reduce sideways velocity over time
        float sidewaysReduction = sidewaysDrag * Time.deltaTime;
        sidewaysVelocityVector = Vector2.Lerp(sidewaysVelocityVector, Vector2.zero, sidewaysReduction);

        // Apply the corrected velocity
        rb.velocity = forwardVelocityVector + sidewaysVelocityVector;
    }

    void ClampVelocity()
    {
        // Hard clamp for absolute maximum speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    //Determines the current position of the mouse in the context of the game world
    Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, mainCamera.nearClipPlane));
        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
    }

    //Passes Debugger messages through enabled check
    void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log(message);
    }
}
