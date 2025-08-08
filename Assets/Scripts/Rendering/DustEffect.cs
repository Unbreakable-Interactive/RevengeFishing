using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DustEffect : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private float waterSurface;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
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
