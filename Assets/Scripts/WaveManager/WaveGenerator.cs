using UnityEngine;

public class WaveGenerator : MonoBehaviour
{
    [Header("Automatic Generation")]
    public bool generateWaves = true;
    public float waveInterval = 2f;
    public float waveIntensity = 1f;
    public Vector2 waveArea = new Vector2(10f, 10f);
    
    private float nextWaveTime;
    
    void Update()
    {
        if (!generateWaves || WaterPhysics.Instance == null) return;
        
        if (Time.time >= nextWaveTime)
        {
            GenerateRandomWave();
            nextWaveTime = Time.time + waveInterval + Random.Range(-0.5f, 0.5f);
        }
    }
    
    void GenerateRandomWave()
    {
        Vector2 randomPosition = new Vector2(
            Random.Range(-waveArea.x, waveArea.x),
            Random.Range(-waveArea.y, waveArea.y)
        );
        
        WaterPhysics.Instance.CreateWave(randomPosition, waveIntensity);
    }
}
