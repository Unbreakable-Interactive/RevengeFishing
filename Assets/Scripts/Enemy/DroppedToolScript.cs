using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedToolScript : EntityMovement
{
    private Vector2 dropForce;
    private bool hasAntiRotated = false;

    // Start is called before the first frame update
    protected override void Start()
    {
        dropForce = new Vector2(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(0.2f, 1f)
        );

        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    protected override void Initialize(int powerLevel)
    {
        //rb.AddForce(new Vector2 (0, 1) * 20, ForceMode2D.Impulse);
        rb.AddForce(dropForce * 20, ForceMode2D.Impulse);
        rb.AddTorque(-dropForce.x * 8, ForceMode2D.Impulse);
        Debug.Log($"Tool dropped in direction {dropForce}");
    }

    protected override void AirborneBehavior()
    {

    }

    protected override void UnderwaterBehavior()
    {
        if (!hasAntiRotated)
        {
            rb.AddTorque(dropForce.x * 8, ForceMode2D.Impulse);
            hasAntiRotated = true; // Prevents continuous anti-rotation
        }
    }

}
