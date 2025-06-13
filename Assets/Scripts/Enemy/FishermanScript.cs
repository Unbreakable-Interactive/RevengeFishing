using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{

    [Header("Fisherman Settings")]
    public float walkSpd;
    public float runSpd;
    public float edgeBfr; // Distance before edge to stop walking
    public float minActTime;
    public float maxActTime;

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
        // Call the base class land movement AI
        if (_type == EnemyType.Land)
        {
            LandMovement();
        }
    }

    public override void ReverseFishingBehaviour()
    {
        //throw new System.NotImplementedException();
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
