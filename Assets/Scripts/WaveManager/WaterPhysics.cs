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
    
    private List<InteractiveWave> interactiveWaves = new List<InteractiveWave>();
    private float nextWaveTime;
    
    [System.Serializable]
    private class InteractiveWave
    {
        public Vector2 position;
        public float intensity;
        public float timeCreated;
        
        public InteractiveWave(Vector2 pos, float power)
        {
            position = pos;
            intensity = power;
            timeCreated = Time.time;
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
    
    public float GetWaterHeightAt(Vector2 position)
    {
        float height = waterLevel;
        
        float time = Time.time * waveSpeed;
        height += Mathf.Sin((position.x / waveLength) + time) * waveHeight * 0.4f;
        height += Mathf.Sin((position.x / waveLength * 0.7f) + time * 1.3f) * waveHeight * 0.3f;
        height += Mathf.Sin((position.x / waveLength * 1.2f) + time * 0.8f) * waveHeight * 0.2f;
        height += Mathf.Sin((position.y / waveLength * 0.8f) + time * 0.9f) * waveHeight * 0.1f;
        
        foreach (var wave in interactiveWaves)
        {
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
    
    public void CreateWave(Vector2 position, float intensity = 1f)
    {
        interactiveWaves.Add(new InteractiveWave(position, intensity));
        
        if (interactiveWaves.Count > maxInteractiveWaves)
        {
            interactiveWaves.RemoveAt(0);
        }
    }
    
    private void GenerateRandomWave()
    {
        Vector2 centerPosition = transform.position;
        Vector2 randomPosition = centerPosition + new Vector2(
            Random.Range(-waveArea.x / 2f, waveArea.x / 2f),
            Random.Range(-waveArea.y / 2f, waveArea.y / 2f)
        );
        
        float randomIntensity = waveIntensity * Random.Range(0.7f, 1.3f);
        CreateWave(randomPosition, randomIntensity);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>())
        {
            float intensity = other.GetComponent<Rigidbody2D>().mass * 0.5f;
            CreateWave(other.transform.position, intensity);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(waveArea.x, waveArea.y, 0));
        
        Gizmos.color = Color.blue;
        foreach (var wave in interactiveWaves)
        {
            float waveRadius = (Time.time - wave.timeCreated) * 5f;
            Gizmos.DrawWireSphere(wave.position, waveRadius);
        }
        
        Gizmos.color = Color.green;
        Vector3 waterLine = new Vector3(transform.position.x, waterLevel, transform.position.z);
        Gizmos.DrawLine(waterLine + Vector3.left * waveArea.x, waterLine + Vector3.right * waveArea.x);
    }
}
