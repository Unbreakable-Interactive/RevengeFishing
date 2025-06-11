using UnityEngine;

public abstract class LandEnemy : Enemy
{
    [Header("Land Enemy Physics")]
    public float airDrag = 0.5f;
    public float waterDrag = 2f;
    public float buoyancyForce = 5f;
    public float maxUpwardWaterSpeed = 3f;

    [Header("Water detection")]
    public WaterCheckPlayer waterCheck;
    public bool isInWater = false;
    public bool isAboveWater = true;

    protected override void Start()
    {
        base.Start();
    }

    protected override void UpdateEnemyBehavior()
    {
        CheckWaterStatus();
        UpdatePhysics();
        UpdateLandEnemyBehavior();
    }

    // Abstract method for specific land enemy behavior
    protected abstract void UpdateLandEnemyBehavior();

    protected override void HandleDefeatedState()
    {
        // Land enemies hop upward when defeated
        rb.velocity = new Vector2(rb.velocity.x, 3f);
    }

    protected virtual void CheckWaterStatus()
    {
        if (waterCheck != null)
        {
            float waterSurfaceY = waterCheck.transform.position.y;
            float enemyY = transform.position.y;

            bool wasInWater = isInWater;
            isInWater = enemyY < waterSurfaceY;
            isAboveWater = enemyY >= waterSurfaceY;

            if (isInWater && !wasInWater)
            {
                OnEnteredWater();
            }
            else if (!isInWater && wasInWater)
            {
                OnExitedWater();
            }
        }
    }

    protected virtual void OnEnteredWater()
    {
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} entered water");
        }
    }

    protected virtual void OnExitedWater()
    {
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} exited water");
        }
    }

    protected virtual void UpdatePhysics()
    {
        if (isInWater)
        {
            ApplyWaterPhysics();
        }
        else
        {
            ApplyAirPhysics();
        }
    }

    protected virtual void ApplyWaterPhysics()
    {
        rb.drag = waterDrag;
        rb.AddForce(Vector2.up * buoyancyForce * Time.deltaTime, ForceMode2D.Force);

        if (rb.velocity.y > maxUpwardWaterSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxUpwardWaterSpeed);
        }
    }

    protected virtual void ApplyAirPhysics()
    {
        rb.drag = airDrag;
    }

    public override void OnTriggerEnter2D(Collider2D other)
    {
        // Only be eaten if in water and defeated
        if (isDefeated && isInWater && IsPlayer(other.gameObject))
        {
            BeEaten();
        }
    }
}
