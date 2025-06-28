using UnityEngine;

public class DroppedTool : Entity
{
    private Vector2 dropForce;
    private bool hasAntiRotated = false;

    protected void Start()
    {
        dropForce = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(0.2f, 1f)
        );

        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();

        rb.AddForce(dropForce * 20, ForceMode2D.Impulse);
        rb.AddTorque(-dropForce.x * 8, ForceMode2D.Impulse);
        Debug.Log($"Tool dropped in direction {dropForce}");
    }

    protected override void AirborneBehavior() {}

    protected override void UnderwaterBehavior()
    {
        if (!hasAntiRotated)
        {
            rb.AddTorque(dropForce.x * 8, ForceMode2D.Impulse);
            rb.drag = 2f;
            hasAntiRotated = true; // Prevents continuous anti-rotation
        }

        if (rb.velocity.magnitude < 0.1f)
        {
            GameObject parent = transform.parent?.gameObject;
            Destroy(parent); // Destroy if nearly stationary
        }
    }
}