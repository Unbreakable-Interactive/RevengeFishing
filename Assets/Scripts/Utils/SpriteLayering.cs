using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteLayering : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private float waterSurface;
    SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

        if (mainCamera?.transform.position.y > waterSurface && transform.position.y < waterSurface && spriteRenderer.sortingOrder != -1)
        {
            spriteRenderer.sortingOrder = -1;
        }
        else if (mainCamera?.transform.position.y < waterSurface && transform.position.y < waterSurface && spriteRenderer.sortingOrder != 1)
        {
            spriteRenderer.sortingOrder = 1;
        }
        else if (mainCamera?.transform.position.y > waterSurface && transform.position.y > waterSurface && spriteRenderer.sortingOrder != 1)
        {
            spriteRenderer.sortingOrder = 1;
        }
        else if (mainCamera?.transform.position.y < waterSurface && transform.position.y > waterSurface && spriteRenderer.sortingOrder != 1)
        {
            spriteRenderer.sortingOrder = -1;
        }
    }
}
