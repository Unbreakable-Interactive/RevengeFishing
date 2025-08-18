using System.Collections.Generic;
using UnityEngine;

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
    [Range(0, 1)]
    public float waveAmplitude = 0.2f;
    [Range(0, 10)]
    public float waveFrequency = 2f;
    [Range(0, 2)]
    public float waveSpeed = 0.5f;
    
    [Header("Interactive Waves")]
    public int maxWaveParticles = 64;
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
    private ComputeBuffer waveParticleBuffer;
    
    private float[,] heightGrid;
    private Vector3 gridOrigin;
    private float lastUpdateTime;
    
    // Shader property IDs
    private static readonly int WaveParticleCountProp = Shader.PropertyToID("_WaveParticleCount");
    private static readonly int WaveParticlesProp = Shader.PropertyToID("_WaveParticles");
    private static readonly int WaveAmplitudeProp = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int WaveFrequencyProp = Shader.PropertyToID("_WaveFrequency");
    private static readonly int WaveSpeedProp = Shader.PropertyToID("_WaveSpeed");
    private static readonly int InteractiveWaveSpeedProp = Shader.PropertyToID("_InteractiveWaveSpeed");
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (waterMaterial != null)
        {
            waveParticleBuffer = new ComputeBuffer(maxWaveParticles, sizeof(float) * 4);
            GetShaderProperties();
            UpdateShaderInteractiveProperties();
        }
        
        if (useApproximation)
        {
            InitializeApproximationGrid();
        }
       
        GameLogger.LogVerbose($"WaterPhysics: Boat wave isolation = {preventBoatInterference}, Disable boat waves = {disableBoatWaves}");
    }
    
    void Update()
    {
        for (int i = waveParticles.Count - 1; i >= 0; i--)
        {
            if (!waveParticles[i].Update(Time.deltaTime))
            {
                waveParticles.RemoveAt(i);
            }
        }
        
        if (waterMaterial != null)
        {
            UpdateShaderData();
        }
        
        if (useApproximation && Time.time > lastUpdateTime + approximationUpdateInterval)
        {
            UpdateApproximationGrid();
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateShaderInteractiveProperties()
    {
        if (waterMaterial != null)
        {
            waterMaterial.SetFloat(InteractiveWaveSpeedProp, interactiveWaveSpeed);
        }
    }
    
    private void GetShaderProperties()
    {
        if (waterMaterial != null)
        {
            waveAmplitude = waterMaterial.GetFloat(WaveAmplitudeProp);
            waveFrequency = waterMaterial.GetFloat(WaveFrequencyProp);
            waveSpeed = waterMaterial.GetFloat(WaveSpeedProp);
        }
    }
    
    private void UpdateShaderData()
    {
        if (waterMaterial == null || waveParticleBuffer == null) return;
        
        Vector4[] waveData = new Vector4[maxWaveParticles];
        for (int i = 0; i < waveParticles.Count; i++)
        {
            var wp = waveParticles[i];
            waveData[i] = new Vector4(
                wp.position.x, wp.position.y,
                wp.amplitude, wp.wavelength
            );
        }
        
        for (int i = waveParticles.Count; i < maxWaveParticles; i++)
        {
            waveData[i] = new Vector4(0, 0, 0, 0);
        }
        
        waveParticleBuffer.SetData(waveData);
        
        waterMaterial.SetBuffer(WaveParticlesProp, waveParticleBuffer);
        waterMaterial.SetInt(WaveParticleCountProp, waveParticles.Count);
    }
    
    private void InitializeApproximationGrid()
    {
        heightGrid = new float[approximationGridSize, approximationGridSize];
        gridOrigin = transform.position - new Vector3(gridSize/2, 0, gridSize/2);
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
        if (waveParticles.Count >= maxWaveParticles) return;
        
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
    {
        Vector3 worldPos = new Vector3(position.x, 0, position.y);
        return GetWaterHeightAtPosition(worldPos, requestingBoat);
    }
    
    public float GetWaterHeightAt(Vector2 position)
    {
        return GetWaterHeightAt(position, null);
    }
    
    public float GetWaterHeightAtPosition(Vector3 worldPosition, Transform requestingBoat = null)
    {
        if (useApproximation)
        {
            return GetApproximatedHeight(worldPosition);
        }
        else
        {
            return CalculateWaterHeight(worldPosition, requestingBoat);
        }
    }
    
    public float GetWaterHeightAtPosition(Vector3 worldPosition)
    {
        return GetWaterHeightAtPosition(worldPosition, null);
    }
    
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
        
        // Gerstner waves for more realistic water
        Vector3 waveOffset = CalculateGerstnerWaves(worldPosition);
        height += waveOffset.y;
        
        // Interactive wave particles
        float particleHeight = 0f;
        Vector2 samplePos = new Vector2(worldPosition.x, worldPosition.z);
        
        foreach (var particle in waveParticles)
        {
            // Apply boat isolation logic
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
        
        waveOffset += GerstnerWave(worldPosition, waveAmplitude * 0.5f, 2/waveFrequency, waveSpeed * 0.8f, 0);
        waveOffset += GerstnerWave(worldPosition, waveAmplitude * 0.25f, 4/waveFrequency, waveSpeed, 30);
        waveOffset += GerstnerWave(worldPosition, waveAmplitude * 0.125f, 8/waveFrequency, waveSpeed * 1.2f, 60);
        
        return waveOffset;
    }
    
    private Vector3 GerstnerWave(Vector3 worldPosition, float steepness, float wavelength, float speed, float direction)
    {
        direction = direction * Mathf.PI / 180f;
        Vector2 d = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
        float k = 2 * Mathf.PI / wavelength;
        float f = k * (Vector2.Dot(d, new Vector2(worldPosition.x, worldPosition.z)) - speed * Time.time);
        float a = steepness / k;
        
        return new Vector3(
            d.x * a * Mathf.Cos(f),
            a * Mathf.Sin(f),
            d.y * a * Mathf.Cos(f)
        );
    }
    
    private bool IsWaveFromNearbyBoat(WaveParticle wave, Transform requestingBoat)
    {
        BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
        
        foreach (BoatFloater boat in allBoats)
        {
            if (boat.transform == requestingBoat) continue;
            
            float distanceToWaveSource = Vector2.Distance(wave.position, new Vector2(boat.transform.position.x, boat.transform.position.z));
            float distanceToRequestingBoat = Vector3.Distance(boat.transform.position, requestingBoat.position);
            
            if (distanceToWaveSource < 3f && distanceToRequestingBoat < boatIsolationDistance)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // COMPATIBILITY: Keep old CreateWave methods
    public void CreateWave(Vector2 position, float intensity = 1f, Transform sourceBoat = null)
    {
        Vector3 pos3D = new Vector3(position.x, transform.position.y, position.y);
        AddWaveParticle(pos3D, intensity);
        
        if (sourceBoat != null)
        {
            GameLogger.LogVerbose($"WaveCreated: Position {position}, Intensity {intensity:F1}, Source: {sourceBoat.name}");
        }
    }
    
    public void CreateWave(Vector2 position, float intensity = 1f)
    {
        CreateWave(position, intensity, null);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>())
        {
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
            
            float intensity = other.GetComponent<Rigidbody2D>().mass * 0.5f;
            AddWaveParticle(other.transform.position, intensity);
            
            GameLogger.LogVerbose($"WaveCreated: {other.name} created wave with intensity {intensity:F1}");
        }
    }
    
    private bool AreOtherBoatsNearby(Vector3 position, Transform excludeBoat = null)
    {
        BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
        int boatsNearby = 0;
        
        foreach (BoatFloater boat in allBoats)
        {
            if (excludeBoat != null && boat.transform == excludeBoat) continue;
            
            float distance = Vector3.Distance(position, boat.transform.position);
            if (distance < boatIsolationDistance)
            {
                boatsNearby++;
            }
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
    
    private void OnDestroy()
    {
        if (waveParticleBuffer != null)
        {
            waveParticleBuffer.Release();
            waveParticleBuffer = null;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (useApproximation && heightGrid != null)
        {
            float cellSize = gridSize / (approximationGridSize - 1);
            for (int x = 0; x < approximationGridSize; x += 4)
            {
                for (int z = 0; z < approximationGridSize; z += 4)
                {
                    Vector3 worldPos = gridOrigin + new Vector3(x * cellSize, heightGrid[x, z], z * cellSize);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(worldPos, 0.1f);
                }
            }
        }
    }
}
