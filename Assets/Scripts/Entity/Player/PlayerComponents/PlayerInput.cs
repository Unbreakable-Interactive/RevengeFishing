using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool enableInput = true;
    
    private Camera mainCamera;
    private Vector2 mousePosition = Vector2.zero;
    private Vector2 lastMousePosition = Vector2.zero;
    private bool isMousePressed = false;
    private bool wasMousePressed = false;

    public System.Action<Vector2> OnMouseClick;
    public System.Action<Vector2> OnMouseHold;
    public System.Action<Vector2> OnMouseRelease;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public void Initialize(Camera camera)
    {
        mainCamera = camera;
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!enableInput) return;
        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        wasMousePressed = isMousePressed;
        isMousePressed = Input.GetMouseButton(0);
        mousePosition = GetMouseWorldPosition();

        if (isMousePressed && !wasMousePressed)
        {
            OnMouseClick?.Invoke(mousePosition);
            lastMousePosition = mousePosition;
            DebugLog("Mouse clicked at: " + mousePosition);
        }

        if (isMousePressed)
        {
            OnMouseHold?.Invoke(mousePosition);
        }

        if (!isMousePressed && wasMousePressed)
        {
            OnMouseRelease?.Invoke(lastMousePosition);
            DebugLog("Mouse released");
        }
    }

    public Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector2.zero;

        Vector3 mouseScreenPosition = Input.mousePosition;
        
        if (mainCamera.orthographic)
        {
            mouseScreenPosition.z = mainCamera.nearClipPlane;
        }
        else
        {
            float distanceToGamePlane = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            mouseScreenPosition.z = distanceToGamePlane;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    public bool IsMousePressed() => isMousePressed;
    public Vector2 GetCurrentMousePosition() => mousePosition;
    public Vector2 GetLastMousePosition() => lastMousePosition;
    public void SetInputEnabled(bool enabled) => enableInput = enabled;

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[PlayerInput] {message}");
    }
}
