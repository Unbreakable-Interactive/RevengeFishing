using UnityEngine;

public abstract class WaterEnemy : Enemy
{
    [Header("Water Enemy Physics")]
    public float waterDrag = 1f;
    public float swimSpeed = 5f;

    protected override void UpdateEnemyBehavior()
    {
        ApplyWaterPhysics();
        UpdateWaterEnemyBehavior();
    }

    // Abstract method for specific water enemy behavior
    protected abstract void UpdateWaterEnemyBehavior();

    protected override void HandleDefeatedState()
    {
        // Water enemies just slow down when defeated
        rb.drag = waterDrag * 2f;
    }

    protected virtual void ApplyWaterPhysics()
    {
        rb.drag = waterDrag;
        // Water enemies are always in water, no buoyancy needed
    }

    public override void OnTriggerEnter2D(Collider2D other)
    {
        // Water enemies can always be eaten when defeated
        if (isDefeated && IsPlayer(other.gameObject))
        {
            BeEaten();
        }
    }
}
