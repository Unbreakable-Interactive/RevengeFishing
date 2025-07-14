using UnityEngine;

public class WaterCheckFollow : MonoBehaviour
{
    [Header("Settings")]
    public float waterSurfaceY = 5.92f;

    public GameObject target;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

    }

    void Update()
    {
        // Follow player horizontally, stay at fixed Y
        transform.position = new Vector3(target.transform.position.x, waterSurfaceY, transform.position.z);
    }
}
