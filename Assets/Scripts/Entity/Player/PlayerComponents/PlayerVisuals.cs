using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public void Initialize()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void UpdateRotationFlip(Quaternion rotation)
    {
        if (playerSpriteRenderer == null) return;

        if (rotation.z > 0.7f || rotation.z < -0.7f)
        {
            transform.localScale = new Vector3(transform.localScale.x, -Mathf.Abs(transform.localScale.y), transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Abs(transform.localScale.y), transform.localScale.z);
        }
    }

    public void SetPlayerSpriteRenderer(SpriteRenderer renderer)
    {
        playerSpriteRenderer = renderer;
    }

    public SpriteRenderer GetPlayerSpriteRenderer()
    {
        return playerSpriteRenderer;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[PlayerVisuals] {message}");
    }
}