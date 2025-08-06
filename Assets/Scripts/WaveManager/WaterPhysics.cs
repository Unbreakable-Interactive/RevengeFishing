using UnityEngine;
using System.Collections.Generic;

public class WaterPhysics : MonoBehaviour
{
    public static WaterPhysics Instance { get; private set; }
    
    [Header("Water Settings")]
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] private float waveHeight = 0.8f;
    [SerializeField] private float waveSpeed = 4f;
    [SerializeField] private float waveLength = 3f;
    
    [Header("Interactive Waves")]
    [SerializeField] private int maxInteractiveWaves = 8;
    [SerializeField] private float waveDecayTime = 3f;
    
    [Header("Automatic Wave Generation")]
    [SerializeField] private bool generateWaves = true;
    [SerializeField] private float waveInterval = 1.2f;
    [SerializeField] private float waveIntensity = 3f;
    [SerializeField] private Vector2 waveArea = new Vector2(30f, 10f);
    [SerializeField] private float waveIntervalVariation = 0.5f;
    
    [Header("Boat Wave Isolation")]
    [SerializeField] private bool preventBoatInterference = true;
    [SerializeField] private float boatIsolationDistance = 5f;
    [SerializeField] private bool disableBoatWaves = true;
    [SerializeField] private bool debugWaveIsolation = false;
    
    private List<InteractiveWave> interactiveWaves = new List<InteractiveWave>();
    private float nextWaveTime;
    
    [System.Serializable]
    private class InteractiveWave
    {
        public Vector2 position;
        public float intensity;
        public float timeCreated;
        public Transform sourceBoat;
        
        public InteractiveWave(Vector2 pos, float power, Transform source = null)
        {
            position = pos;
            intensity = power;
            timeCreated = Time.time;
            sourceBoat = source;
        }
    }
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        nextWaveTime = Time.time + waveInterval;
        
        if (debugWaveIsolation)
        {
            GameLogger.LogVerbose($"WaterPhysics: Boat wave isolation = {preventBoatInterference}, Disable boat waves = {disableBoatWaves}");
        }
    }
    
    void Update()
    {
        interactiveWaves.RemoveAll(wave => Time.time - wave.timeCreated > waveDecayTime);
        
        if (generateWaves && Time.time >= nextWaveTime)
        {
            GenerateRandomWave();
            nextWaveTime = Time.time + waveInterval + Random.Range(-waveIntervalVariation, waveIntervalVariation);
        }
    }
    
    public float GetWaterHeightAt(Vector2 position, Transform requestingBoat = null)
    {
        float height = waterLevel;
        
        float time = Time.time * waveSpeed;
        height += Mathf.Sin((position.x / waveLength) + time) * waveHeight * 0.4f;
        height += Mathf.Sin((position.x / waveLength * 0.7f) + time * 1.3f) * waveHeight * 0.3f;
        height += Mathf.Sin((position.x / waveLength * 1.2f) + time * 0.8f) * waveHeight * 0.2f;
        height += Mathf.Sin((position.y / waveLength * 0.8f) + time * 0.9f) * waveHeight * 0.1f;
        
        foreach (var wave in interactiveWaves)
        {
            if (preventBoatInterference && requestingBoat != null)
            {
                if (IsWaveFromNearbyBoat(wave, requestingBoat))
                {
                    if (debugWaveIsolation)
                    {
                        GameLogger.LogVerbose($"WaveIsolation: Ignoring wave from nearby boat for {requestingBoat.name}");
                    }
                    continue;
                }
            }
            
            float distance = Vector2.Distance(position, wave.position);
            float waveRadius = (Time.time - wave.timeCreated) * 5f;
            
            if (Mathf.Abs(distance - waveRadius) < 1f)
            {
                float ageMultiplier = 1f - ((Time.time - wave.timeCreated) / waveDecayTime);
                float waveValue = Mathf.Sin((distance - waveRadius) * 3f) * wave.intensity * ageMultiplier;
                height += waveValue;
            }
        }
        
        return height;
    }
    
    public float GetWaterHeightAt(Vector2 position)
    {
        return GetWaterHeightAt(position, null);
    }
    
    private bool IsWaveFromNearbyBoat(InteractiveWave wave, Transform requestingBoat)
    {
        if (wave.sourceBoat != null)
        {
            float distanceToRequestingBoat = Vector3.Distance(wave.sourceBoat.position, requestingBoat.position);
            return distanceToRequestingBoat < boatIsolationDistance;
        }
        
        BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
        
        foreach (BoatFloater boat in allBoats)
        {
            if (boat.transform == requestingBoat) continue;
            
            float distanceToWaveSource = Vector2.Distance(wave.position, boat.transform.position);
            float distanceToRequestingBoat = Vector3.Distance(boat.transform.position, requestingBoat.position);
            
            if (distanceToWaveSource < 3f && distanceToRequestingBoat < boatIsolationDistance)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void CreateWave(Vector2 position, float intensity = 1f, Transform sourceBoat = null)
    {
        interactiveWaves.Add(new InteractiveWave(position, intensity, sourceBoat));
        
        if (interactiveWaves.Count > maxInteractiveWaves)
        {
            interactiveWaves.RemoveAt(0);
        }
        
        if (debugWaveIsolation && sourceBoat != null)
        {
            GameLogger.LogVerbose($"WaveCreated: Position {position}, Intensity {intensity:F1}, Source: {sourceBoat.name}");
        }
    }
    
    public void CreateWave(Vector2 position, float intensity = 1f)
    {
        CreateWave(position, intensity, null);
    }
    
    private void GenerateRandomWave()
    {
        Vector2 centerPosition = transform.position;
        Vector2 randomPosition = centerPosition + new Vector2(
            Random.Range(-waveArea.x / 2f, waveArea.x / 2f),
            Random.Range(-waveArea.y / 2f, waveArea.y / 2f)
        );
        
        float randomIntensity = waveIntensity * Random.Range(0.7f, 1.3f);
        CreateWave(randomPosition, randomIntensity, null);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>())
        {
            if (disableBoatWaves && other.GetComponent<BoatFloater>() != null)
            {
                if (debugWaveIsolation)
                {
                    GameLogger.LogVerbose($"WaveBlocked: Boat {other.name} wave creation disabled");
                }
                return;
            }
            
            if (preventBoatInterference && other.GetComponent<BoatFloater>() != null)
            {
                if (AreOtherBoatsNearby(other.transform.position, other.transform))
                {
                    if (debugWaveIsolation)
                    {
                        GameLogger.LogVerbose($"WaveBlocked: Boat {other.name} near other boats, wave creation skipped");
                    }
                    return;
                }
            }
            
            float intensity = other.GetComponent<Rigidbody2D>().mass * 0.5f;
            Transform sourceBoat = other.GetComponent<BoatFloater>() != null ? other.transform : null;
            
            CreateWave(other.transform.position, intensity, sourceBoat);
            
            if (debugWaveIsolation)
            {
                GameLogger.LogVerbose($"WaveCreated: {other.name} created wave with intensity {intensity:F1}");
            }
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
    
    [ContextMenu("🧹 Clear All Interactive Waves")]
    public void ClearAllWaves()
    {
        interactiveWaves.Clear();
        GameLogger.Log("WaterPhysics: All interactive waves cleared");
    }
    
    [ContextMenu("📊 Debug Wave Info")]
    public void DebugWaveInfo()
    {
        GameLogger.LogVerbose($"WaterPhysics: {interactiveWaves.Count} active waves");
        
        for (int i = 0; i < interactiveWaves.Count; i++)
        {
            var wave = interactiveWaves[i];
            string source = wave.sourceBoat != null ? wave.sourceBoat.name : "Random";
            float age = Time.time - wave.timeCreated;
            GameLogger.LogVerbose($"Wave {i}: Source={source}, Age={age:F1}s, Intensity={wave.intensity:F1}");
        }
    }
    
    public void SetBoatIsolation(bool enabled, float distance = 5f)
    {
        preventBoatInterference = enabled;
        boatIsolationDistance = distance;
        
        if (debugWaveIsolation)
        {
            GameLogger.LogVerbose($"WaterPhysics: Boat isolation set to {enabled}, distance {distance}");
        }
    }
    
    public void SetDisableBoatWaves(bool disabled)
    {
        disableBoatWaves = disabled;
        
        if (debugWaveIsolation)
        {
            GameLogger.LogVerbose($"WaterPhysics: Boat waves disabled = {disabled}");
        }
    }
}
