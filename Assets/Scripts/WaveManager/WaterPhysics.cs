using UnityEngine;
using System.Collections.Generic;

public class WaterPhysics : MonoBehaviour
{
    public static WaterPhysics Instance { get; private set; }
    
    [Header("Water Settings")]
    public float waterLevel = 0f;
    public float waveHeight = 0.3f;
    public float waveSpeed = 2f;
    public float waveLength = 4f;
    
    [Header("Interactive Waves")]
    public int maxInteractiveWaves = 8;
    public float waveDecayTime = 3f;
    
    private List<InteractiveWave> interactiveWaves = new List<InteractiveWave>();
    
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
    
    void Update()
    {
        interactiveWaves.RemoveAll(wave => Time.time - wave.timeCreated > waveDecayTime);
    }
    
    public float GetWaterHeightAt(Vector2 position)
    {
        float height = waterLevel;
        
        float time = Time.time * waveSpeed;
        height += Mathf.Sin((position.x / waveLength) + time) * waveHeight * 0.4f;
        height += Mathf.Sin((position.x / waveLength * 0.7f) + time * 1.3f) * waveHeight * 0.3f;
        height += Mathf.Sin((position.x / waveLength * 1.2f) + time * 0.8f) * waveHeight * 0.2f;
        
        foreach (var wave in interactiveWaves)
        {
            float distance = Vector2.Distance(position, wave.position);
            float waveRadius = (Time.time - wave.timeCreated) * 5f; // Velocidad propagación
            
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
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>())
        {
            float intensity = other.GetComponent<Rigidbody2D>().mass * 0.5f;
            CreateWave(other.transform.position, intensity);
        }
    }
}
