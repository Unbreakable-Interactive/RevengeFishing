using UnityEngine;
using Cinemachine;

// ! Revisar y optimizar los llamados de FindObjectOfType

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
            GameLogger.LogError("No PlayerScaler component found!");
            // playerScaler = FindObjectOfType<PlayerScaler>();

        if (virtualCamera == null)
            GameLogger.LogError("No CinemachineVirtualCamera component found!");
            // virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera != null)
        {
            InitializeCameraBody();
        }
    }

    private void InitializeCameraBody()
    {
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            baseOffset = transposer.m_FollowOffset;
            baseDistance = baseOffset.z;
            GameLogger.LogVerbose($"CameraScaler: Found Transposer, Base Follow Offset: {baseOffset}, Distance: {baseDistance}");
            return;
        }

        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer != null)
        {
            baseDistance = framingTransposer.m_CameraDistance;
            GameLogger.LogVerbose($"CameraScaler: Found Framing Transposer, Base Camera Distance: {baseDistance}");
            return;
        }

        GameLogger.LogError("CameraScaler: Virtual Camera doesn't have Transposer or Framing Transposer body!");
    }

    private void Start()
    {
        if (playerScaler == null)
        {
            GameLogger.LogError("CameraScaler: No PlayerScaler found!");
            return;
        }

        if (virtualCamera == null)
        {
            GameLogger.LogError("CameraScaler: No Virtual Camera found!");
            return;
        }

        currentDistance = Mathf.Abs(baseDistance);
        targetDistance = currentDistance;

        GameLogger.LogVerbose($"CameraScaler initialized: Base distance {baseDistance}");
    }

    private void Update()
    {
        if (playerScaler == null || virtualCamera == null) return;

        float playerScale = playerScaler.GetCurrentScaleMultiplier();
        float newTargetDistance = Mathf.Abs(baseDistance) * playerScale;

        targetDistance = newTargetDistance;

        if (smoothMovement)
        {
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, moveSpeed * Time.deltaTime);
        }
        else
        {
            currentDistance = targetDistance;
        }

        UpdateCameraDistance();
    }

    private void UpdateCameraDistance()
    {
        if (transposer != null)
        {
            Vector3 newOffset = baseOffset;
            newOffset.z = -currentDistance;
            transposer.m_FollowOffset = newOffset;

            GameLogger.LogVerbose($"Updated Transposer Follow Offset Z to: {newOffset.z:F1}");
        }
        else if (framingTransposer != null)
        {
            framingTransposer.m_CameraDistance = currentDistance;
        }
    }

    public float GetCurrentDistance() => currentDistance;
    public float GetTargetDistance() => targetDistance;
    public string GetBodyType()
    {
        if (transposer != null) return "Transposer";
        if (framingTransposer != null) return "Framing Transposer";
        return "Unknown";
    }
}
