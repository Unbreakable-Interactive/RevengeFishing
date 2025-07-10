using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3];

    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 6f;
    [SerializeField] private float waterDrag = 0.85f;
    [SerializeField] private float angularDrag = 0.7f;

    [Header("Wave Rolling Control")]
    [SerializeField] private float waveRollStrength = 2.5f;
    [SerializeField] private float rollResponseSpeed = 1.5f;
    [SerializeField] private float maxRollAngle = 12f;
    [SerializeField] private bool enableWaveRolling = true;

    [Header("Stability")]
    [SerializeField] private float stabilityForce = 0.3f;

    [Header("VERTICAL MOVEMENT CONTROL")]
    [SerializeField] private float maxVerticalSpeed = 3f;
    [SerializeField] private float verticalDamping = 0.8f;
    [SerializeField] private bool enableSpeedLimit = true;
    [SerializeField] private float smoothBuoyancy = 0.5f;

    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private float currentRollAngle = 0f;
    private float rollVelocity = 0f;
    private Vector2 previousVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;

        if (floatPoints[0] == null || floatPoints[1] == null || floatPoints[2] == null)
        {
            Debug.LogError("BoatFloater: Float Points not assigned in inspector.");
        }
    }

    void FixedUpdate()
    {
        if (waterPhysics == null) return;


        previousVelocity = rb.velocity;

        ApplyBuoyancy();
        ApplyWaterResistance();

        if (enableWaveRolling)
        {
            ApplyWaveRolling();
        }

        ApplyStability();


        if (enableSpeedLimit)
        {
            LimitVerticalMovement();
        }
    }

    void ApplyBuoyancy()
    {
        int submergedPoints = 0;
        Vector2 totalForce = Vector2.zero;

        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;

            Vector2 worldPos = point.position;
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
            float submersion = waterHeight - worldPos.y;

            if (submersion > 0)
            {
                submergedPoints++;


                float speedFactor = 1f;
                if (enableSpeedLimit && rb.velocity.y > 0)
                {
                    speedFactor = Mathf.Lerp(1f, smoothBuoyancy, rb.velocity.y / maxVerticalSpeed);
                }

                float force = submersion * buoyancyForce * speedFactor;
                totalForce += Vector2.up * force;
            }
        }

        if (submergedPoints > 0)
        {
            rb.AddForce(totalForce);
        }
    }

    void LimitVerticalMovement()
    {
        Vector2 velocity = rb.velocity;


        if (Mathf.Abs(velocity.y) > maxVerticalSpeed)
        {
            velocity.y = Mathf.Sign(velocity.y) * maxVerticalSpeed;
        }

        if (Mathf.Abs(velocity.y) > maxVerticalSpeed * 0.7f)
        {
            velocity.y *= verticalDamping;
        }


        float velocityChange = Mathf.Abs(velocity.y - previousVelocity.y);
        if (velocityChange > maxVerticalSpeed * 0.5f)
        {

            velocity.y = Mathf.Lerp(previousVelocity.y, velocity.y, 0.7f);
        }


        rb.velocity = velocity;


        if (enableSpeedLimit && Application.isEditor)
        {
            if (Mathf.Abs(velocity.y) > maxVerticalSpeed * 0.8f)
            {
                Debug.Log($"Boat speed limited: {velocity.y:F2} -> clamped to {maxVerticalSpeed}");
            }
        }
    }

    void ApplyWaveRolling()
    {
        if (floatPoints.Length < 3) return;

        float bowHeight = waterPhysics.GetWaterHeightAt(floatPoints[0].position);
        float sternHeight = waterPhysics.GetWaterHeightAt(floatPoints[2].position);

        float heightDifference = bowHeight - sternHeight;

        float targetRollAngle = heightDifference * waveRollStrength;
        targetRollAngle = Mathf.Clamp(targetRollAngle, -maxRollAngle, maxRollAngle);

        currentRollAngle = Mathf.SmoothDamp(
            currentRollAngle,
            targetRollAngle,
            ref rollVelocity,
            1f / rollResponseSpeed,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        float currentBoatAngle = transform.eulerAngles.z;
        if (currentBoatAngle > 180f) currentBoatAngle -= 360f;

        float angleDifference = Mathf.DeltaAngle(currentBoatAngle, currentRollAngle);
        float rollTorque = angleDifference * rollResponseSpeed;

        rb.AddTorque(rollTorque, ForceMode2D.Force);
    }

    void ApplyWaterResistance()
    {
        if (IsInWater())
        {
            rb.velocity *= waterDrag;
            rb.angularVelocity *= angularDrag;
        }
    }

    void ApplyStability()
    {
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        float stabilityTorque = -currentAngle * stabilityForce * Time.fixedDeltaTime;
        rb.AddTorque(stabilityTorque);
    }

    bool IsInWater()
    {
        foreach (Transform point in floatPoints)
        {
            if (point != null)
            {
                Vector2 worldPos = point.position;
                float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
                if (waterHeight > worldPos.y) return true;
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (floatPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in floatPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.1f);
                }
            }

            if (enableWaveRolling)
            {
                Gizmos.color = Color.yellow;
                Vector3 rollIndicator = transform.position + Vector3.up * currentRollAngle * 0.1f;
                Gizmos.DrawLine(transform.position, rollIndicator);
            }


            if (enableSpeedLimit && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Vector3 speedIndicator = transform.position + Vector3.up * rb.velocity.y;
                Gizmos.DrawLine(transform.position, speedIndicator);


                Gizmos.color = Color.green;
                Vector3 maxSpeedIndicator = transform.position + Vector3.up * maxVerticalSpeed;
                Gizmos.DrawWireSphere(maxSpeedIndicator, 0.2f);
            }
        }
    }
}
