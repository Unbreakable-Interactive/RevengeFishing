using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    // Start is called before the first frame update
    void Start()
    {
        _type = EnemyType.Land;

        // Initialize with default power level
        Initialize(100f);

        // Ensure we have a Rigidbody2D for gravity
        if (GetComponent<Rigidbody2D>() == null)
        {
            gameObject.AddComponent<Rigidbody2D>();
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void ReverseFishingBehaviour()
    {
        throw new System.NotImplementedException();
    }

    public override void LandMovement()
    {
        throw new System.NotImplementedException();
    }

    public override void WaterMovement()
    {
        throw new System.NotImplementedException();
    }

}
