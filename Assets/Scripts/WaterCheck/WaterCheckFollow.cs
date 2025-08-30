using System;
using UnityEngine;

public class WaterCheckFollow : MonoBehaviour
{
    [Header("Settings")]
    public float waterSurfaceY = 5.92f;

    public GameObject target;

    public bool isOnBoat = false;
    
    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (GameStates.instance.IsGameplayRunning())
        {
            // Follow player horizontally, stay at fixed Y
            if(!isOnBoat)
                transform.position = new Vector3(target.transform.position.x, waterSurfaceY, transform.position.z);
        }
    }

    private void FixedUpdate()
    {
        if(isOnBoat)
            transform.position = new Vector3(target.transform.position.x, waterSurfaceY, transform.position.z);
    }
}
