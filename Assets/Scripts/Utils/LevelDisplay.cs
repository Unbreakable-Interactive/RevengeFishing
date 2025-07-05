using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private Entity entity;
    [SerializeField] private bool showAbsolutePowerLevel = false; // New option
    [SerializeField] private bool faceCamera = true; // New option for world space displays

    private TextMeshProUGUI levelDisplay;
    private int initPowerLevel;
    private Camera mainCamera;

    void Start()
    {
        levelDisplay = GetComponent<TextMeshProUGUI>();

        if (entity != null)
            initPowerLevel = entity.PowerLevel;

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
        UpdateLevelDisplay();

        // Make text face camera if it's a world space canvas
        if (faceCamera && mainCamera != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }

    void UpdateLevelDisplay()
    {
        if (entity == null || levelDisplay == null) return;

        if (showAbsolutePowerLevel)
        {
            // Show absolute power level (good for enemies)
            levelDisplay.text = entity.PowerLevel.ToString();
        }
        else
        {
            // Show difference from initial (good for player progression)
            levelDisplay.text = (entity.PowerLevel - initPowerLevel).ToString();
        }
    }

    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public void SetEntity(Entity newEntity)
    {
        entity = newEntity;
        if (entity != null)
            initPowerLevel = entity.PowerLevel;
    }
}
