using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DustEffect : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private float waterSurface;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (GameStates.instance.IsGameplayRunning())
        {
            if (mainCamera.transform.position.y > waterSurface)
            {
                this.transform.position = new Vector3(mainCamera.transform.position.x, waterSurface, transform.position.z);
            }
            else
            {
                this.transform.position = mainCamera.transform.position;
            }
        }
    }
}
