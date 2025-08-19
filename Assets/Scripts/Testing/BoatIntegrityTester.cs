using System.Collections;
using UnityEngine;

public class BoatIntegrityTester : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private KeyCode spawnBoatKey = KeyCode.B;
    [SerializeField] private KeyCode damageBoatKey = KeyCode.N;
    [SerializeField] private KeyCode destroyBoatKey = KeyCode.M;
    
    [Header("Test Settings")]
    [SerializeField] private float testDamageAmount = 25f;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private BoatController lastSpawnedBoat;
    private Player player;
    
    void Start()
    {
        player = FindObjectOfType<Player>();
        DebugLog("BoatIntegrityTester initialized. Controls:");
        DebugLog($"- Press {spawnBoatKey} to spawn a boat");
        DebugLog($"- Press {damageBoatKey} to damage the last spawned boat");
        DebugLog($"- Press {destroyBoatKey} to instantly destroy the last spawned boat");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(spawnBoatKey))
        {
            SpawnTestBoat();
        }
        
        if (Input.GetKeyDown(damageBoatKey))
        {
            DamageTestBoat();
        }
        
        if (Input.GetKeyDown(destroyBoatKey))
        {
            DestroyTestBoat();
        }
    }
    
    void SpawnTestBoat()
    {
        if (SimpleObjectPool.Instance == null)
        {
            DebugLog("ERROR: SimpleObjectPool.Instance is null!");
            return;
        }
        
        Vector3 spawnPosition = transform.position + spawnOffset;
        if (player != null)
        {
            spawnPosition = player.transform.position + spawnOffset;
        }
        
        GameObject boatObj = SimpleObjectPool.Instance.Spawn("Boat", spawnPosition);
        
        if (boatObj != null)
        {
            lastSpawnedBoat = boatObj.GetComponent<BoatController>();
            
            if (lastSpawnedBoat != null)
            {
                // Initialize the boat with some basic boundaries
                Transform leftBoundary = new GameObject("TempLeftBoundary").transform;
                Transform rightBoundary = new GameObject("TempRightBoundary").transform;
                
                leftBoundary.position = spawnPosition + Vector3.left * 10f;
                rightBoundary.position = spawnPosition + Vector3.right * 10f;
                
                // Check integrity before initialization
                DebugLog($"Before initialization: Integrity {lastSpawnedBoat.GetCurrentIntegrity()}/{lastSpawnedBoat.GetMaxIntegrity()}");
                
                lastSpawnedBoat.Initialize(leftBoundary, rightBoundary);
                
                // Wait a frame for crew initialization
                StartCoroutine(CheckIntegrityAfterDelay());
                
                DebugLog($"Spawned test boat at {spawnPosition}");
            }
            else
            {
                DebugLog("❌ Spawned boat doesn't have BoatController component!");
            }
        }
        else
        {
            DebugLog("❌ Failed to spawn boat from object pool!");
        }
    }
    
    private IEnumerator CheckIntegrityAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Wait for crew initialization
        
        if (lastSpawnedBoat != null)
        {
            float currentIntegrity = lastSpawnedBoat.GetCurrentIntegrity();
            float maxIntegrity = lastSpawnedBoat.GetMaxIntegrity();
            
            DebugLog($"After initialization: Integrity {currentIntegrity}/{maxIntegrity}");
            
            if (currentIntegrity <= 0)
            {
                DebugLog("WARNING: Boat still has 0 integrity after initialization!");
                
                // Force set integrity for testing
                lastSpawnedBoat.SetInitialIntegrity(100f, 100f);
                DebugLog($"Manually set integrity: {lastSpawnedBoat.GetCurrentIntegrity()}/{lastSpawnedBoat.GetMaxIntegrity()}");
            }
        }
    }
    
    void DamageTestBoat()
    {
        if (lastSpawnedBoat == null)
        {
            DebugLog("❌ No boat to damage! Spawn one first.");
            return;
        }
        
        float integrityBefore = lastSpawnedBoat.GetCurrentIntegrity();
        lastSpawnedBoat.TakeDamageFromPlayer(testDamageAmount, "Manual Test");
        float integrityAfter = lastSpawnedBoat.GetCurrentIntegrity();
        
        DebugLog($"🔥 Damaged boat: {integrityBefore} → {integrityAfter} (damage: {testDamageAmount})");
        
        if (integrityAfter <= 0)
        {
            DebugLog("💥 Boat was destroyed!");
            lastSpawnedBoat = null;
        }
    }
    
    void DestroyTestBoat()
    {
        if (lastSpawnedBoat == null)
        {
            DebugLog("❌ No boat to destroy! Spawn one first.");
            return;
        }
        
        float currentIntegrity = lastSpawnedBoat.GetCurrentIntegrity();
        lastSpawnedBoat.TakeDamageFromPlayer(currentIntegrity + 10f, "Instant Destroy Test");
        
        DebugLog($"💥 Instantly destroyed boat (dealt {currentIntegrity + 10f} damage)");
        lastSpawnedBoat = null;
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.Log($"[BoatIntegrityTester] {message}");
    }
}