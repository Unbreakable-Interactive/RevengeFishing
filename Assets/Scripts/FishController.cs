using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishController : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody2D rb;

    [Header("Fish Movement")]
    public float swimForce = 8f;
    public float maxSpeed = 5f;
    public float rotationSpeed = 10f;
    public float drag = 2f;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.drag = drag;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SwimTowardMouse();
        }

        ClampVelocity();
    }

    void SwimTowardMouse()
    {
        Vector2 mousePosition = GetMouseWorldPosition();
        Vector2 fishPosition = transform.position;
        Vector2 directionToMouse = (mousePosition - fishPosition).normalized;

        // Apply force directly toward mouse
        rb.AddForce(directionToMouse * swimForce, ForceMode2D.Impulse);

        // Rotate fish to face the direction it's swimming
        if (directionToMouse != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Debug.Log("Swimming toward: " + mousePosition);
    }

    void ClampVelocity()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x, mouseScreenPosition.y, mainCamera.nearClipPlane));
        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
    }
}
