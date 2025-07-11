using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FatigueDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private Entity entity;
    [SerializeField] private bool faceCamera = true; // New option for world space displays

    private TextMeshProUGUI fatigueDisplay;
    private Camera mainCamera;

    void Start()
    {
        fatigueDisplay = GetComponent<TextMeshProUGUI>();

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
        UpdateFatigueDisplay();

        // Make text face camera if it's a world space canvas
        if (faceCamera && mainCamera != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }

    void UpdateFatigueDisplay()
    {
        if (entity == null || fatigueDisplay == null) return;

        fatigueDisplay.text = $"{entity.entityFatigue.fatigue.ToString()} / {entity.entityFatigue.maxFatigue.ToString()}";
    }

    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public void SetEntity(Entity newEntity)
    {
        entity = newEntity;
    }
}