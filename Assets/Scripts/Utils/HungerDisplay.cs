using TMPro;
using UnityEngine;

public class HungerDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private Player player;
    [SerializeField] private bool faceCamera = true; // New option for world space displays

    private TextMeshProUGUI hungerDisplay;
    private Camera mainCamera;

    void Start()
    {
        hungerDisplay = GetComponent<TextMeshProUGUI>();

        // Get camera reference for world space displays
        if (faceCamera)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
        }
    }

    void Update()
    {
        UpdateHungerDisplay();

        // Make text face camera if it's a world space canvas
        if (faceCamera && mainCamera != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }

    void UpdateHungerDisplay()
    {
        if (player == null || hungerDisplay == null) return;

        hungerDisplay.text = $"{player.HungerHandler.GetHunger().ToString()} / {player.HungerHandler.GetMaxHunger().ToString()}";
    }

    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public void SetEntity(Player newEntity)
    {
        player = newEntity;
    }
}
