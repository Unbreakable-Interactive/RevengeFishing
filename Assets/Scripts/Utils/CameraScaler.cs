using UnityEngine;
using Cinemachine;

public class CameraScaler : MonoBehaviour
{
    [Header("Camera Distance Settings")]
    [SerializeField] private PlayerScaler playerScaler;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float baseDistance = -10f;

    [Header("Smooth Movement")]
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Runtime Info")]
    [SerializeField] private float currentDistance = -10f;
    [SerializeField] private float targetDistance = -10f;

    private CinemachineTransposer transposer;
    private CinemachineFramingTransposer framingTransposer;
    private Vector3 baseOffset;

    private void Awake()
    {
        if (playerScaler == null)
            playerScaler = FindObjectOfType<PlayerScaler>();

        if (virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera != null)
        {
            InitializeCameraBody();
        }
    }

    private void InitializeCameraBody()
    {
        // Try to get Transposer component
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            baseOffset = transposer.m_FollowOffset;
            baseDistance = baseOffset.z;
            Debug.Log($"CameraScaler: Found Transposer, Base Follow Offset: {baseOffset}, Distance: {baseDistance}");
            return;
        }

        // Try to get Framing Transposer component
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer != null)
        {
            // For Framing Transposer, use Camera Distance
            baseDistance = framingTransposer.m_CameraDistance;
            Debug.Log($"CameraScaler: Found Framing Transposer, Base Camera Distance: {baseDistance}");
            return;
        }

        Debug.LogError("CameraScaler: Virtual Camera doesn't have Transposer or Framing Transposer body!");
    }

    private void Start()
    {
        if (playerScaler == null)
        {
            Debug.LogError("CameraScaler: No PlayerScaler found!");
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogError("CameraScaler: No Virtual Camera found!");
            return;
        }

        currentDistance = Mathf.Abs(baseDistance); // Use absolute value for distance
        targetDistance = currentDistance;

        Debug.Log($"CameraScaler initialized: Base distance {baseDistance}");
    }

    private void Update()
    {
        if (playerScaler == null || virtualCamera == null) return;

        // Get player scale
        float playerScale = playerScaler.GetCurrentScaleMultiplier();

        // Calculate target distance - scale proportionally
        float newTargetDistance = Mathf.Abs(baseDistance) * playerScale;

        // Debug logging
        if (Time.frameCount % 60 == 0) // Once per second
        {
            Debug.Log($"Player Scale: {playerScale:F2}, Target Distance: {newTargetDistance:F1}, Current: {currentDistance:F1}");
        }

        // Update target if changed
        targetDistance = newTargetDistance;
        Debug.Log($"CAMERA DISTANCE CHANGE: Player {playerScale:F2}x to Distance {targetDistance:F1}");

        // Apply smooth or instant movement
        if (smoothMovement)
        {
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, moveSpeed * Time.deltaTime);
        }
        else
        {
            currentDistance = targetDistance;
        }

        // Update the CORRECT Cinemachine property
        UpdateCameraDistance();
    }

    private void UpdateCameraDistance()
    {
        if (transposer != null)
        {
            // Update Transposer Follow Offset Z
            Vector3 newOffset = baseOffset;
            newOffset.z = -currentDistance; // Negative for camera behind
            transposer.m_FollowOffset = newOffset;

            Debug.Log($"Updated Transposer Follow Offset Z to: {newOffset.z:F1}");
        }
        else if (framingTransposer != null)
        {
            // Update Framing Transposer Camera Distance
            framingTransposer.m_CameraDistance = currentDistance;

            Debug.Log($"Updated Framing Transposer Camera Distance to: {currentDistance:F1}");
        }
    }

    // Public methods for debugging
    public float GetCurrentDistance() => currentDistance;
    public float GetTargetDistance() => targetDistance;
    public string GetBodyType()
    {
        if (transposer != null) return "Transposer";
        if (framingTransposer != null) return "Framing Transposer";
        return "Unknown";
    }
}
