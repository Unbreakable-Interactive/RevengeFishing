using UnityEngine;

public class ApplicationTarget : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Tiempo fijo entre pasos de física (0.02 = 50 Hz)")]
    public float fixedTimestep = 0.02f;

    [Header("Frame Rate Settings")]
    [Tooltip("FPS objetivo para sincronizar mejor con FixedUpdate (múltiplo de 50 recomendado)")]
    public int targetFrameRate = 100;

    void Awake()
    {
        Time.fixedDeltaTime = fixedTimestep;

        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = targetFrameRate;

        Debug.Log($"FrameRateLock: Fixed Timestep = {fixedTimestep} ({1f/fixedTimestep} Hz), Target FPS = {targetFrameRate}");
    }
}
