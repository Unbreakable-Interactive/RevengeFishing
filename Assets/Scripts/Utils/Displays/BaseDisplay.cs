using TMPro;
using UnityEngine;

public abstract class BaseDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] protected bool faceCamera = true;
    
    [SerializeField] protected TextMeshProUGUI displayText;
    [SerializeField] protected Camera mainCamera;

    protected virtual void Start()
    {
        if (displayText == null)
        {
            GameLogger.LogError($"{gameObject.name}: No TextMeshProUGUI component found!");
            displayText = GetComponent<TextMeshProUGUI>();
            return;
        }

        if (faceCamera)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
    }

    protected virtual void Update()
    {
        if (GameStates.instance.IsGameplayRunning())
        {
            UpdateDisplay();
            HandleCameraFacing();
        }
    }

    /// <summary>
    /// Updates the display text - must be implemented by derived classes
    /// </summary>
    protected abstract void UpdateDisplay();

    /// <summary>
    /// Handles making the text face the camera for world space canvases
    /// </summary>
    protected virtual void HandleCameraFacing()
    {
        if (faceCamera && mainCamera != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// Sets the display text safely
    /// </summary>
    protected virtual void SetDisplayText(string text)
    {
        if (displayText != null)
            displayText.text = text;
    }

    /// <summary>
    /// Validates if display can be updated
    /// </summary>
    protected virtual bool CanUpdateDisplay()
    {
        return displayText != null;
    }
}

