using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FishingLineRenderer : MonoBehaviour
{
    [Header("Line Visual Settings")]
    private Vector3 startPoint;  // Will be set to spawn point
    private Transform endPoint;  // Hook/player

    [Header("Normal Line Appearance")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float normalWidth = 0.05f;
    [SerializeField] private Material normalMaterial;

    [Header("Stretched Line Appearance")]
    [SerializeField] private Color stretchedColor = Color.red;
    [SerializeField] private float stretchedWidth = 0.1f;
    [SerializeField] private Material stretchedMaterial;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve stretchColorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve stretchWidthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool enableWobble = true;
    [SerializeField] private float wobbleIntensity = 0.1f;
    [SerializeField] private float wobbleSpeed = 10f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem stretchParticles;
    [SerializeField] private ParticleSystem snapParticles;
    [SerializeField] private AudioClip stretchSound;
    [SerializeField] private AudioClip snapSound;

    private LineRenderer lineRenderer;
    private AudioSource audioSource;
    private FishingProjectile fishingProjectile;

    // State tracking
    private bool isStretching = false;
    private float currentStretchAmount = 0f;
    private float stretchTimer = 0f;
    private float maxStretchTime = 1f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();
        fishingProjectile = GetComponentInParent<FishingProjectile>();

        SetupLineRenderer();
    }

    void Start()
    {
        if (fishingProjectile != null)
        {
            // Get spawn point from FishingProjectile
            startPoint = fishingProjectile.spawnPoint;
            endPoint = fishingProjectile.transform;

            // Subscribe to events
            fishingProjectile.OnStretchStarted += OnStretchStarted;
            fishingProjectile.OnStretchChanged += OnStretchChanged;
            fishingProjectile.OnStretchEnded += OnStretchEnded;
            fishingProjectile.OnSnapBack += OnSnapBack;

            Debug.Log($"Line Renderer: Using spawn point {startPoint} as start point");
        }
        else
        {
            Debug.LogWarning("FishingLineRenderer: No FishingProjectile found in parent!");
        }
    }

    private void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;

        // Create default material if none assigned
        if (normalMaterial == null)
        {
            normalMaterial = new Material(Shader.Find("Sprites/Default"));
            normalMaterial.color = normalColor;
        }

        SetupNormalAppearance();
    }

    void Update()
    {
        UpdateLinePositions();
    }

    private void UpdateLinePositions()
    {
        startPoint = fishingProjectile.spawnPoint;

        if (endPoint == null)
        {
            Debug.LogWarning("FishingLineRenderer: Missing end point!");
            return;
        }

        if (isStretching && enableWobble && currentStretchAmount > 0.3f)
        {
            UpdateWobbleLine();
        }
        else
        {
            // Simple straight line from spawn point to hook
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint.position);
        }
    }

    private void UpdateWobbleLine()
    {
        int segments = 5;
        lineRenderer.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 basePosition = Vector3.Lerp(startPoint, endPoint.position, t);

            if (i > 0 && i < segments) // Don't wobble the endpoints
            {
                Vector3 direction = (endPoint.position - startPoint).normalized;
                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);

                float wobbleAmount = Mathf.Sin((Time.time * wobbleSpeed) + (i * 2f)) * wobbleIntensity * currentStretchAmount;
                basePosition += perpendicular * wobbleAmount;
            }

            lineRenderer.SetPosition(i, basePosition);
        }
    }

    #region Event Handlers

    private void OnStretchStarted()
    {
        isStretching = true;
        Debug.Log("Line Renderer: Stretch started");

        if (stretchParticles != null)
            stretchParticles.Play();

        PlaySound(stretchSound);
    }

    private void OnStretchChanged(float stretchAmount, float timer, float maxTime)
    {
        currentStretchAmount = stretchAmount;
        stretchTimer = timer;
        maxStretchTime = maxTime;

        UpdateLineAppearance();
    }

    private void OnStretchEnded()
    {
        isStretching = false;
        currentStretchAmount = 0f;
        stretchTimer = 0f;

        SetupNormalAppearance();
        Debug.Log("Line Renderer: Stretch ended");

        if (stretchParticles != null)
            stretchParticles.Stop();
    }

    private void OnSnapBack(float snapStrength)
    {
        Debug.Log($"Line Renderer: Snap back with strength {snapStrength}");

        if (snapParticles != null)
        {
            snapParticles.transform.position = endPoint.position;
            snapParticles.Play();
        }

        PlaySound(snapSound);
    }

    #endregion

    #region Visual Updates

    private void UpdateLineAppearance()
    {
        if (!isStretching) return;

        float colorT = stretchColorCurve.Evaluate(currentStretchAmount);
        Color currentColor = Color.Lerp(normalColor, stretchedColor, colorT);

        float widthT = stretchWidthCurve.Evaluate(currentStretchAmount);
        float currentWidth = Mathf.Lerp(normalWidth, stretchedWidth, widthT);

        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;

        if (stretchedMaterial != null)
        {
            lineRenderer.material = stretchedMaterial;
        }
    }

    private void SetupNormalAppearance()
    {
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        lineRenderer.startWidth = normalWidth;
        lineRenderer.endWidth = normalWidth;

        if (normalMaterial != null)
        {
            lineRenderer.material = normalMaterial;
        }
    }

    #endregion

    #region Utility Methods

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    void OnDestroy()
    {
        if (fishingProjectile != null)
        {
            fishingProjectile.OnStretchStarted -= OnStretchStarted;
            fishingProjectile.OnStretchChanged -= OnStretchChanged;
            fishingProjectile.OnStretchEnded -= OnStretchEnded;
            fishingProjectile.OnSnapBack -= OnSnapBack;
        }
    }
}
