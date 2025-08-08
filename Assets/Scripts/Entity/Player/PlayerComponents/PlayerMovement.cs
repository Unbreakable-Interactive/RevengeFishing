using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Water Movement Settings")]
    [SerializeField] private float forceAmount = 1f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float naturalDrag = 0.5f;
    [SerializeField] private float rotationDrag = 1f;
    [SerializeField] private float constantAccel = 0.2f;
    [SerializeField] private float minForwardVelocity = 1f;
    [SerializeField] private float sidewaysDrag = 1f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float rotationThreshold = 10f;
    [SerializeField] private float boostThreshold = 10f;
    [SerializeField] private float underwaterRotationSpeed = 10f;

    [Header("Steering Settings")]
    [SerializeField] private float steeringForce = 5f;
    [SerializeField] private float steeringDamping = 0.98f;

    [Header("Variable Gravity Settings")]
    [SerializeField] private float airGravityAscending = 1.5f;
    [SerializeField] private float airGravityDescending = 4f;
    [SerializeField] private float gravityTransitionSpeed = 2f;
    [SerializeField] private float velocityThreshold = 0.1f;

    [Header("Air Rotation Settings")]
    [SerializeField] private bool autoRotateInAir = true;
    [SerializeField] private float airRotationSpeed = 8f;
    [SerializeField] private float minSpeedForRotation = 0.3f;

    private Rigidbody2D rb;
    private Entity entity;

    private Quaternion targetRotation;
    private bool isRotatingToTarget = false;
    private bool shouldApplyForceAfterRotation = false;
    private bool hasAppliedBoost = false;
    private float currentGravityScale;

    private float originalMaxSpeed;
    private float currentSpeedMultiplier = 1f;
    private float currentModeMaxSpeed;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        entity = GetComponent<Entity>();
        
        originalMaxSpeed = maxSpeed;
        currentModeMaxSpeed = entity != null ? entity.UnderwaterMaxSpeed : maxSpeed;
        
        targetRotation = transform.rotation;
        rb.drag = naturalDrag;
        currentGravityScale = entity != null ? entity.UnderwaterGravityScale : 0f;
        
        DebugLog("PlayerMovement initialized");
    }

    public void HandleUnderwaterMovement(Vector2 inputPosition, bool isInputPressed, bool isInputHeld)
    {
        if (isInputPressed)
        {
            OnInputPressed(inputPosition);
        }

        if (isInputHeld && !shouldApplyForceAfterRotation && !isRotatingToTarget)
        {
            ApplySteering(inputPosition);
        }

        if (!isInputPressed)
        {
            OnInputReleased();
        }

        UpdateRotation();
        ApplyConstantAccel();
        ReduceSidewaysVelocity();
        ClampVelocity();
    }

    public void HandleAirborneMovement()
    {
        ApplyVariableGravity();
        HandleAirborneRotation();
    }

    public void OnMovementModeChanged(bool aboveWater)
    {
        if (aboveWater)
        {
            currentModeMaxSpeed = entity != null ? entity.AirMaxSpeed : originalMaxSpeed;
            rotationSpeed = airRotationSpeed;
            currentGravityScale = entity != null ? entity.AirGravityScale : 2f;
            DebugLog("Movement mode: AIRBORNE");
        }
        else
        {
            currentModeMaxSpeed = entity != null ? entity.UnderwaterMaxSpeed : originalMaxSpeed;
            rotationSpeed = underwaterRotationSpeed;
            DebugLog("Movement mode: UNDERWATER");
        }
        
        DebugLog($"Max speed for mode: {currentModeMaxSpeed}");
    }

    private void OnInputPressed(Vector2 mousePosition)
    {
        rb.drag = naturalDrag / 20f;
        SetTargetRotation(mousePosition);
        shouldApplyForceAfterRotation = true;
        hasAppliedBoost = false;
        DebugLog("Input pressed - rotating to: " + mousePosition);
    }

    private void OnInputReleased()
    {
        if (rb.drag < naturalDrag) 
            rb.drag += naturalDrag / 60f;
        if (rb.drag > naturalDrag) 
            rb.drag = naturalDrag;
    }

    private void SetTargetRotation(Vector2 mousePosition)
    {
        Vector2 direction = mousePosition - (Vector2)transform.position;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            isRotatingToTarget = true;
        }
    }

    private void UpdateRotation()
    {
        if (isRotatingToTarget)
        {
            ApplyRotationDeceleration();
            ContinueRotationToTarget();
            CheckForBoostTiming();
            CheckRotationCompletion();
        }
    }

    private void ApplyRotationDeceleration()
    {
        rb.velocity *= (1f - rotationDrag * Time.deltaTime);
        DebugLog($"Applying rotation deceleration - Current speed: {rb.velocity.magnitude:F2}");
    }

    private void ContinueRotationToTarget()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void CheckForBoostTiming()
    {
        if (hasAppliedBoost) return;

        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        if (angleDifference <= boostThreshold)
        {
            ApplyForceInDirection();
            hasAppliedBoost = true;
            DebugLog($"Applied boost at {angleDifference:F1} degrees from target!");
        }
    }

    private void CheckRotationCompletion()
    {
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        if (angleDifference < rotationThreshold)
        {
            transform.rotation = targetRotation;
            isRotatingToTarget = false;
            DebugLog("Rotation completed!");
        }
    }

    private void ApplyForceInDirection()
    {
        Vector2 forceDirection = transform.right;
        rb.AddForce(forceDirection * forceAmount, ForceMode2D.Impulse);
        DebugLog("Applied force in direction: " + forceDirection);
    }

    private void ApplySteering(Vector2 targetPosition)
    {
        Vector2 direction = targetPosition - (Vector2)transform.position;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            Vector2 steeringDirection = transform.right;
            rb.AddForce(steeringDirection * steeringForce, ForceMode2D.Force);
            rb.velocity *= steeringDamping;

            DebugLog("Steering toward: " + targetPosition);
        }
    }

    private void ApplyConstantAccel()
    {
        Vector2 forwardDirection = transform.right;
        float forwardVelocity = Vector2.Dot(rb.velocity, forwardDirection);

        if (forwardVelocity < minForwardVelocity)
        {
            rb.AddForce(forwardDirection * constantAccel, ForceMode2D.Force);
            DebugLog($"Applying thrust - Forward velocity: {forwardVelocity:F2}");
        }
    }

    private void ReduceSidewaysVelocity()
    {
        if (rb.velocity.magnitude < 0.1f) return;

        Vector2 forwardDirection = transform.right;
        Vector2 currentVelocity = rb.velocity;

        float forwardVelocity = Vector2.Dot(currentVelocity, forwardDirection);
        Vector2 forwardVelocityVector = forwardDirection * forwardVelocity;
        Vector2 sidewaysVelocityVector = currentVelocity - forwardVelocityVector;

        float sidewaysReduction = sidewaysDrag * Time.deltaTime;
        sidewaysVelocityVector = Vector2.Lerp(sidewaysVelocityVector, Vector2.zero, sidewaysReduction);

        rb.velocity = forwardVelocityVector + sidewaysVelocityVector;
    }

    private void ClampVelocity()
    {
        float finalMaxSpeed = currentModeMaxSpeed * currentSpeedMultiplier;
        if (rb.velocity.magnitude > finalMaxSpeed)
        {
            rb.velocity = rb.velocity.normalized * finalMaxSpeed;
        }
    }

    private void ApplyVariableGravity()
    {
        bool isAscending = rb.velocity.y > velocityThreshold;
        bool isDescending = rb.velocity.y < -velocityThreshold;

        float targetGravityScale;

        if (isAscending)
        {
            targetGravityScale = airGravityAscending;
            DebugLog("Fish ascending - applying lighter gravity");
        }
        else if (isDescending)
        {
            targetGravityScale = airGravityDescending;
            DebugLog("Fish descending - applying stronger gravity");
        }
        else
        {
            targetGravityScale = entity != null ? entity.AirGravityScale : 2f;
            DebugLog("Fish at peak - using default air gravity");
        }

        currentGravityScale = Mathf.Lerp(currentGravityScale, targetGravityScale, gravityTransitionSpeed * Time.deltaTime);
        rb.gravityScale = currentGravityScale;

        DebugLog($"Current gravity scale: {currentGravityScale:F2}, Y velocity: {rb.velocity.y:F2}");
    }

    private void HandleAirborneRotation()
    {
        if (autoRotateInAir && rb.velocity.magnitude > minSpeedForRotation)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.AngleAxis(angle, Vector3.forward), airRotationSpeed * Time.deltaTime);
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = multiplier;
        DebugLog($"Speed multiplier set to {multiplier:F2}");
    }

    public void ResetSpeedMultiplier()
    {
        currentSpeedMultiplier = 1f;
        DebugLog("Speed multiplier reset to normal");
    }

    public float GetCurrentSpeed() => rb.velocity.magnitude;
    public Vector2 GetVelocity() => rb.velocity;
    public bool IsMoving() => rb.velocity.magnitude > 0.1f;
    public float GetMaxSpeed() => currentModeMaxSpeed * currentSpeedMultiplier;
    public Rigidbody2D GetRigidbody() => rb;

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[PlayerMovement] {message}");
    }
}
