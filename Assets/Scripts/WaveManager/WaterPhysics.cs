using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering; // GraphicsDeviceType

[System.Serializable]
public class WaveParticle
{
    public Vector2 position;
    public float amplitude;
    public float wavelength;
    public float speed;
    public float lifetime;
    public float decay;
    public float timeCreated;

    public WaveParticle(Vector3 pos, float force)
    {
        position = new Vector2(pos.x, pos.z);
        amplitude = force;
        wavelength = force * 2f;
        timeCreated = Time.time;
    }

    public bool Update(float deltaTime)
    {
        float age = Time.time - timeCreated;
        if (age >= lifetime) return false;
        amplitude *= (1f - decay * deltaTime);
        return amplitude > 0.01f;
    }

    public float GetHeight(Vector2 samplePos)
    {
        float distance = Vector2.Distance(samplePos, position);
        float waveRadius = (Time.time - timeCreated) * speed;

        if (Mathf.Abs(distance - waveRadius) < 1f)
        {
            float ageMultiplier = 1f - ((Time.time - timeCreated) / lifetime);
            return Mathf.Sin((distance - waveRadius) * 3f) * amplitude * ageMultiplier;
        }
        return 0f;
    }
}

public class WaterPhysics : MonoBehaviour
{
    public static WaterPhysics Instance { get; private set; }

    [Header("Water Properties")]
    public Material waterMaterial;
    public float baseWaterHeight = 0f;

    [Header("Wave Parameters")]
    [Range(0, 1)] public float waveAmplitude = 0.2f;
    [Range(0, 10)] public float waveFrequency = 2f;
    [Range(0, 2)] public float waveSpeed = 0.5f;

    [Header("Interactive Waves")]
    public int maxWaveParticles = 64; // debe coincidir con MAX_WAVE_PARTICLES del shader
    public float interactiveWaveStrength = 1f;
    public float interactiveWaveDecay = 0.5f;
    public float interactiveWaveSpeed = 3f;
    public float interactiveWaveLifetime = 3f;

    [Header("Height Sampling")]
    public bool useApproximation = false;
    public int approximationGridSize = 32;
    public float approximationUpdateInterval = 0.2f;
    public float gridSize = 100f;

    [Header("Boat Wave Isolation")]
    [SerializeField] private bool preventBoatInterference = true;
    [SerializeField] private float boatIsolationDistance = 5f;
    [SerializeField] private bool disableBoatWaves = true;

    [SerializeField] private List<WaveParticle> waveParticles = new List<WaveParticle>();

    // === Compat: StructuredBuffer sólo en Metal/Vulkan. En GLES3 usamos VectorArray.
    private ComputeBuffer waveParticleBuffer;         // ruta Metal/Vulkan
    private Vector4[] waveArrayCache;                 // ruta GLES3 (y también podemos rellenarlo siempre)
    private bool useStructuredBuffer;

    // cache grid
    private float[,] heightGrid;
    private Vector3 gridOrigin;
    private float lastUpdateTime;

    // Shader property IDs
    private static readonly int WaveParticleCountProp = Shader.PropertyToID("_WaveParticleCount");
    private static readonly int WaveParticlesProp     = Shader.PropertyToID("_WaveParticles");      // StructuredBuffer
    private static readonly int WaveParticlesArrProp  = Shader.PropertyToID("_WaveParticlesArr");   // uniform float4[]
    private static readonly int WaveAmplitudeProp     = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int WaveFrequencyProp     = Shader.PropertyToID("_WaveFrequency");
    private static readonly int WaveSpeedProp         = Shader.PropertyToID("_WaveSpeed");
    private static readonly int InteractiveWaveSpeedProp = Shader.PropertyToID("_InteractiveWaveSpeed");
    private static readonly int ObjectScaleProp       = Shader.PropertyToID("_ObjectScale");

    const int MaxWaveParticlesShader = 64; // debe igualar MAX_WAVE_PARTICLES en el shader

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (waterMaterial == null) return;

        // Detecta backend: StructuredBuffer ok en Metal/Vulkan; en GLES3 no.
        useStructuredBuffer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal
                            || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan);

        // Ajusta max a lo que admite el shader
        maxWaveParticles = Mathf.Clamp(maxWaveParticles, 0, MaxWaveParticlesShader);

        // Prepara recursos según la ruta
        waveArrayCache = new Vector4[MaxWaveParticlesShader];

        if (useStructuredBuffer)
        {
            waveParticleBuffer = new ComputeBuffer(MaxWaveParticlesShader, sizeof(float) * 4);
            waterMaterial.SetBuffer(WaveParticlesProp, waveParticleBuffer);
        }

        // Sincroniza propiedades del shader (opcional)
        GetShaderProperties();
        UpdateShaderInteractiveProperties();
        SyncObjectScaleToShader();

        if (useApproximation) InitializeApproximationGrid();

        GameLogger.LogVerbose($"WaterPhysics: Using {(useStructuredBuffer ? "StructuredBuffer" : "VectorArray")} path; Boat isolation={preventBoatInterference}, Disable boat waves={disableBoatWaves}");
    }

    void Update()
    {
        // Actualiza partículas (y elimina muertas)
        for (int i = waveParticles.Count - 1; i >= 0; i--)
            if (!waveParticles[i].Update(Time.deltaTime)) waveParticles.RemoveAt(i);

        if (waterMaterial != null) UpdateShaderData();

        if (useApproximation && Time.time > lastUpdateTime + approximationUpdateInterval)
        {
            UpdateApproximationGrid();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateShaderInteractiveProperties()
    {
        if (waterMaterial == null) return;
        waterMaterial.SetFloat(InteractiveWaveSpeedProp, interactiveWaveSpeed);
    }

    private void GetShaderProperties()
    {
        if (waterMaterial == null) return;
        waveAmplitude = waterMaterial.GetFloat(WaveAmplitudeProp);
        waveFrequency = waterMaterial.GetFloat(WaveFrequencyProp);
        waveSpeed     = waterMaterial.GetFloat(WaveSpeedProp);
    }

    private void SyncObjectScaleToShader()
    {
        if (waterMaterial == null) return;
        // El shader divide worldPos.xz por _ObjectScale.xz. Envío la escala del objeto de agua.
        var ls = transform.lossyScale;
        waterMaterial.SetVector(ObjectScaleProp, new Vector4(ls.x, 1f, ls.z, 1f));
    }

    private void UpdateShaderData()
    {
        if (waterMaterial == null) return;

        int count = Mathf.Min(waveParticles.Count, MaxWaveParticlesShader);

        // Rellena array CPU (lo usamos siempre; en Metal/Vulkan además subimos al ComputeBuffer)
        for (int i = 0; i < count; i++)
        {
            var wp = waveParticles[i];
            waveArrayCache[i] = new Vector4(wp.position.x, wp.position.y, wp.amplitude, wp.wavelength);
        }
        for (int i = count; i < MaxWaveParticlesShader; i++)
            waveArrayCache[i] = Vector4.zero;

        // Ruta GLES3 (y también sirve de respaldo siempre)
        waterMaterial.SetVectorArray(WaveParticlesArrProp, waveArrayCache);
        waterMaterial.SetInt(WaveParticleCountProp, count);

        // Ruta Metal/Vulkan
        if (useStructuredBuffer && waveParticleBuffer != null)
        {
            waveParticleBuffer.SetData(waveArrayCache); // sube los mismos datos
            // SetBuffer ya se hizo en Start()
        }
    }

    private void InitializeApproximationGrid()
    {
        heightGrid = new float[approximationGridSize, approximationGridSize];
        gridOrigin = transform.position - new Vector3(gridSize / 2f, 0, gridSize / 2f);
    }

    private void UpdateApproximationGrid()
    {
        float cellSize = gridSize / (approximationGridSize - 1);
        for (int x = 0; x < approximationGridSize; x++)
        {
            for (int z = 0; z < approximationGridSize; z++)
            {
                Vector3 worldPos = gridOrigin + new Vector3(x * cellSize, 0, z * cellSize);
                heightGrid[x, z] = CalculateWaterHeight(worldPos);
            }
        }
    }

    public void AddWaveParticle(Vector3 position, float force)
    {
        if (waveParticles.Count >= MaxWaveParticlesShader) return;

        position.y = transform.position.y;

        WaveParticle newParticle = new WaveParticle(position, force * interactiveWaveStrength)
        {
            speed = interactiveWaveSpeed,
            lifetime = interactiveWaveLifetime,
            decay = interactiveWaveDecay
        };

        waveParticles.Add(newParticle);
    }

    public float GetWaterHeightAt(Vector2 position, Transform requestingBoat = null)
        => GetWaterHeightAtPosition(new Vector3(position.x, 0, position.y), requestingBoat);

    public float GetWaterHeightAt(Vector2 position)
        => GetWaterHeightAt(position, null);

    public float GetWaterHeightAtPosition(Vector3 worldPosition, Transform requestingBoat = null)
        => useApproximation ? GetApproximatedHeight(worldPosition) : CalculateWaterHeight(worldPosition, requestingBoat);

    public float GetWaterHeightAtPosition(Vector3 worldPosition)
        => GetWaterHeightAtPosition(worldPosition, null);

    private float GetApproximatedHeight(Vector3 worldPosition)
    {
        float cellSize = gridSize / (approximationGridSize - 1);
        float gridX = (worldPosition.x - gridOrigin.x) / cellSize;
        float gridZ = (worldPosition.z - gridOrigin.z) / cellSize;

        gridX = Mathf.Clamp(gridX, 0, approximationGridSize - 1);
        gridZ = Mathf.Clamp(gridZ, 0, approximationGridSize - 1);

        int x0 = Mathf.FloorToInt(gridX);
        int z0 = Mathf.FloorToInt(gridZ);
        int x1 = Mathf.Min(x0 + 1, approximationGridSize - 1);
        int z1 = Mathf.Min(z0 + 1, approximationGridSize - 1);

        float tx = gridX - x0;
        float tz = gridZ - z0;

        float h00 = heightGrid[x0, z0];
        float h10 = heightGrid[x1, z0];
        float h01 = heightGrid[x0, z1];
        float h11 = heightGrid[x1, z1];

        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(h0, h1, tz);
    }

    private float CalculateWaterHeight(Vector3 worldPosition, Transform requestingBoat = null)
    {
        float height = baseWaterHeight + transform.position.y;

        // Gerstner (coincide con shader)
        Vector3 waveOffset = CalculateGerstnerWaves(worldPosition);
        height += waveOffset.y;

        // Interactivas
        float particleHeight = 0f;
        Vector2 samplePos = new Vector2(worldPosition.x, worldPosition.z);

        foreach (var particle in waveParticles)
        {
            if (preventBoatInterference && requestingBoat != null)
            {
                if (IsWaveFromNearbyBoat(particle, requestingBoat))
                {
                    GameLogger.LogVerbose($"WaveIsolation: Ignoring wave from nearby boat for {requestingBoat.name}");
                    continue;
                }
            }
            particleHeight += particle.GetHeight(samplePos);
        }
        return height + particleHeight;
    }

    private Vector3 CalculateGerstnerWaves(Vector3 worldPosition)
    {
        Vector3 waveOffset = Vector3.zero;
        waveOffset += GerstnerWave(worldPosition, waveAmplitude * 0.5f, 2 / waveFrequency, waveSpeed * 0.8f, 0);
        waveOffset += GerstnerWave(worldPosition, waveAmplitude * 0.25f, 4 / waveFrequency, waveSpeed, 30);
        waveOffset += GerstnerWave(worldPosition, waveAmplitude * 0.125f, 8 / waveFrequency, waveSpeed * 1.2f, 60);
        return waveOffset;
    }

    private Vector3 GerstnerWave(Vector3 worldPosition, float steepness, float wavelength, float speed, float direction)
    {
        direction = direction * Mathf.PI / 180f;
        Vector2 d = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
        float k = 2 * Mathf.PI / wavelength;
        float f = k * (Vector2.Dot(d, new Vector2(worldPosition.x, worldPosition.z)) - speed * Time.time);
        float a = steepness / k;
        return new Vector3(d.x * a * Mathf.Cos(f), a * Mathf.Sin(f), d.y * a * Mathf.Cos(f));
    }

    private bool IsWaveFromNearbyBoat(WaveParticle wave, Transform requestingBoat)
    {
        BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
        foreach (BoatFloater boat in allBoats)
        {
            if (boat.transform == requestingBoat) continue;
            float distanceToWaveSource = Vector2.Distance(wave.position, new Vector2(boat.transform.position.x, boat.transform.position.z));
            float distanceToRequestingBoat = Vector3.Distance(boat.transform.position, requestingBoat.position);
            if (distanceToWaveSource < 3f && distanceToRequestingBoat < boatIsolationDistance) return true;
        }
        return false;
    }

    public void CreateWave(Vector2 position, float intensity = 1f, Transform sourceBoat = null)
    {
        Vector3 pos3D = new Vector3(position.x, transform.position.y, position.y);
        AddWaveParticle(pos3D, intensity);
        if (sourceBoat != null)
            GameLogger.LogVerbose($"WaveCreated: Position {position}, Intensity {intensity:F1}, Source: {sourceBoat.name}");
    }

    public void CreateWave(Vector2 position, float intensity = 1f)
        => CreateWave(position, intensity, null);

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (!rb) return;

        if (disableBoatWaves && other.GetComponent<BoatFloater>() != null)
        {
            GameLogger.LogVerbose($"WaveBlocked: Boat {other.name} wave creation disabled");
            return;
        }
        if (preventBoatInterference && other.GetComponent<BoatFloater>() != null)
        {
            if (AreOtherBoatsNearby(other.transform.position, other.transform))
            {
                GameLogger.LogVerbose($"WaveBlocked: Boat {other.name} near other boats, wave creation skipped");
                return;
            }
        }

        float intensity = rb.mass * 0.5f;
        AddWaveParticle(other.transform.position, intensity);
        GameLogger.LogVerbose($"WaveCreated: {other.name} created wave with intensity {intensity:F1}");
    }

    private bool AreOtherBoatsNearby(Vector3 position, Transform excludeBoat = null)
    {
        BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
        int boatsNearby = 0;
        foreach (BoatFloater boat in allBoats)
        {
            if (excludeBoat != null && boat.transform == excludeBoat) continue;
            float distance = Vector3.Distance(position, boat.transform.position);
            if (distance < boatIsolationDistance) boatsNearby++;
        }
        return boatsNearby > 0;
    }

    public void ClearAllWaves()
    {
        waveParticles.Clear();
        GameLogger.Log("WaterPhysics: All interactive waves cleared");
    }

    public void SetBoatIsolation(bool enabled, float distance = 5f)
    {
        preventBoatInterference = enabled;
        boatIsolationDistance = distance;
        GameLogger.LogVerbose($"WaterPhysics: Boat isolation set to {enabled}, distance {distance}");
    }

    public void SetDisableBoatWaves(bool disabled)
    {
        disableBoatWaves = disabled;
        GameLogger.LogVerbose($"WaterPhysics: Boat waves disabled = {disabled}");
    }

    private void OnDisable() => ReleaseBuffers();
    private void OnDestroy()
    {
        ReleaseBuffers();
        if (Instance == this) Instance = null;
    }

    private void ReleaseBuffers()
    {
        if (waveParticleBuffer != null)
        {
            waveParticleBuffer.Release();
            waveParticleBuffer = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (useApproximation && heightGrid != null)
        {
            float cellSize = gridSize / (approximationGridSize - 1);
            for (int x = 0; x < approximationGridSize; x += 4)
            for (int z = 0; z < approximationGridSize; z += 4)
            {
                Vector3 worldPos = gridOrigin + new Vector3(x * cellSize, heightGrid[x, z], z * cellSize);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(worldPos, 0.1f);
            }
        }
    }
}