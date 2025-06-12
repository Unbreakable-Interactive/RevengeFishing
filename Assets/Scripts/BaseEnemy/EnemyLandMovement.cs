using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLandMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Water/Air Movement Modes")]
    public bool isAboveWater = false;
    public float airGravityScale = 2f;
    public float underwaterGravityScale = 0f;
    public float airDrag = 1.5f;
    public float underwaterDrag = 0.5f;
    public float airMaxSpeed = 3f;
    public float underwaterMaxSpeed = 5f;
    public float underwaterRotationSpeed = 10f;

    private float currentGravityScale;          // Current gravity being applied
    public float maxSpeed = 5f;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.drag = airDrag;

    }

    // Update is called once per frame
    void Update()
    {
        if (isAboveWater)
        {
            AirborneBehavior();
        }
        else
        {
            UnderwaterBehavior();
        }

    }

    public void UnderwaterBehavior()
    {

    }

    public void AirborneBehavior()
    {

    }

    public void SetMovementMode(bool aboveWater)
    {
        isAboveWater = aboveWater;

        if (isAboveWater)
        {
            // Airborne mode - fish is out of water
            currentGravityScale = airGravityScale; // Initialize current gravity
            rb.gravityScale = currentGravityScale;
            rb.drag = airDrag;
            maxSpeed = airMaxSpeed;
        }
        else
        {
            // Underwater mode - fish is in water
            rb.gravityScale = underwaterGravityScale;
            rb.drag = underwaterDrag;
            maxSpeed = underwaterMaxSpeed;
        }
    }

}
