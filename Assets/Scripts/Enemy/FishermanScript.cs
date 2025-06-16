using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    private int timer;
    // Start is called before the first frame update
    void Start()
    {
        timer = 0;
        _type = EnemyType.Land;

        //enable fishing tool for fisherman
        hasFishingTool = true;

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
        timer++;
        // Call the base class land movement AI
        if (_type == EnemyType.Land)
        {
            LandMovement();
        }

        if (timer >= 600)
        {
            ReverseFishingBehaviour();
            timer = 0; // Reset timer after 10 seconds (600 frames at 60 FPS)
        }
    }

    public override void ReverseFishingBehaviour()
    {
        if (!fishingToolEquipped) return;

        // WEIGHTED SELECTION
        float randomValue = UnityEngine.Random.value; // 0.0 to 1.0

        if (randomValue < 0.1f) // 10% chance to put away
        {
            TryUnequipFishingTool();
        }
    }

    //public override void LandMovement()
    //{
    //    //throw new System.NotImplementedException();
    //}

    public override void WaterMovement()
    {
        //throw new System.NotImplementedException();
    }

}
