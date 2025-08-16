using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BoatCrewDebugger : MonoBehaviour
{
    [Header("Debug Configuration")]
    [SerializeField] private bool enableGlobalTracking = true;
    [SerializeField] private bool logAllStateChanges = true;
    [SerializeField] private bool trackPositionChanges = false;
    [SerializeField] private float trackingUpdateInterval = 1f;
    
    [Header("Monitoring")]
    [SerializeField] private List<BoatCrewManager> monitoredCrewManagers = new List<BoatCrewManager>();
    [SerializeField] private Dictionary<string, CrewManagerState> lastKnownStates = new Dictionary<string, CrewManagerState>();
    
    private float lastTrackingUpdate = 0f;
    
    [System.Serializable]
    public class CrewManagerState
    {
        public string boatID;
        public int totalCrew;
        public int activeCrew;
        public List<CrewMemberState> crewStates = new List<CrewMemberState>();
    }
    
    [System.Serializable]
    public class CrewMemberState
    {
        public string name;
        public Enemy.EnemyState state;
        public bool isOnBoat;
        public bool handlerActive;
        public Vector3 position;
    }
    
    private void Start()
    {
        if (enableGlobalTracking)
        {
            FindAllCrewManagers();
            InvokeRepeating(nameof(TrackAllCrewManagers), 1f, trackingUpdateInterval);
        }
    }
    
    private void FindAllCrewManagers()
    {
        BoatCrewManager[] allManagers = FindObjectsOfType<BoatCrewManager>();
        monitoredCrewManagers.Clear();
        monitoredCrewManagers.AddRange(allManagers);
        
        GameLogger.Log($"[CREW DEBUGGER] Found {monitoredCrewManagers.Count} BoatCrewManagers to monitor");
        
        foreach (var manager in monitoredCrewManagers)
        {
            string boatID = manager.GetBoatID();
            lastKnownStates[boatID] = CaptureCrewManagerState(manager);
            GameLogger.Log($"[CREW DEBUGGER] Monitoring boat: {boatID}");
        }
    }
    
    private void TrackAllCrewManagers()
    {
        if (!enableGlobalTracking) return;
        
        foreach (var manager in monitoredCrewManagers)
        {
            if (manager == null) continue;
            
            string boatID = manager.GetBoatID();
            CrewManagerState currentState = CaptureCrewManagerState(manager);
            
            if (lastKnownStates.ContainsKey(boatID))
            {
                CrewManagerState lastState = lastKnownStates[boatID];
                CompareAndLogChanges(lastState, currentState);
            }
            
            lastKnownStates[boatID] = currentState;
        }
    }
    
    private CrewManagerState CaptureCrewManagerState(BoatCrewManager manager)
    {
        CrewManagerState state = new CrewManagerState
        {
            boatID = manager.GetBoatID(),
            totalCrew = manager.GetAllCrewMembers().Count,
            activeCrew = manager.GetActiveCrewCount()
        };
        
        foreach (var crew in manager.GetAllCrewMembers())
        {
            if (crew != null)
            {
                GameObject handler = crew.transform.parent?.gameObject ?? crew.gameObject;
                
                CrewMemberState crewState = new CrewMemberState
                {
                    name = crew.name,
                    state = crew.State,
                    isOnBoat = crew.isOnBoat,
                    handlerActive = handler.activeInHierarchy,
                    position = crew.transform.position
                };
                
                state.crewStates.Add(crewState);
            }
        }
        
        return state;
    }
    
    private void CompareAndLogChanges(CrewManagerState lastState, CrewManagerState currentState)
    {
        // Check crew count changes
        if (lastState.activeCrew != currentState.activeCrew)
        {
            GameLogger.LogError($"🔍 [CREW COUNT CHANGE] Boat {currentState.boatID}: {lastState.activeCrew} → {currentState.activeCrew} active crew");
        }
        
        if (lastState.totalCrew != currentState.totalCrew)
        {
            GameLogger.LogError($"🔍 [TOTAL CREW CHANGE] Boat {currentState.boatID}: {lastState.totalCrew} → {currentState.totalCrew} total crew");
        }
        
        // Check individual crew changes
        for (int i = 0; i < Mathf.Min(lastState.crewStates.Count, currentState.crewStates.Count); i++)
        {
            var lastCrew = lastState.crewStates[i];
            var currentCrew = currentState.crewStates[i];
            
            // State changes
            if (lastCrew.state != currentCrew.state)
            {
                GameLogger.LogError($"🔍 [CREW STATE] {currentCrew.name}: {lastCrew.state} → {currentCrew.state}");
            }
            
            // Handler activation changes
            if (lastCrew.handlerActive != currentCrew.handlerActive)
            {
                GameLogger.LogError($"🔍 [HANDLER STATE] {currentCrew.name}: Handler {(lastCrew.handlerActive ? "Active" : "Inactive")} → {(currentCrew.handlerActive ? "Active" : "Inactive")}");
            }
            
            // OnBoat status changes
            if (lastCrew.isOnBoat != currentCrew.isOnBoat)
            {
                GameLogger.LogError($"🔍 [BOAT STATUS] {currentCrew.name}: OnBoat {lastCrew.isOnBoat} → {currentCrew.isOnBoat}");
            }
            
            // Position changes (if enabled)
            if (trackPositionChanges && Vector3.Distance(lastCrew.position, currentCrew.position) > 0.1f)
            {
                GameLogger.Log($"🔍 [POSITION] {currentCrew.name}: {lastCrew.position} → {currentCrew.position}");
            }
        }
    }
    
    // Debug commands
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            LogAllCrewStates();
        }
        
        if (Input.GetKeyDown(KeyCode.F9))
        {
            FindAllCrewManagers();
        }
        
        if (Input.GetKeyDown(KeyCode.F10))
        {
            AnalyzeCrewDisappearances();
        }
    }
    
    private void LogAllCrewStates()
    {
        GameLogger.Log("=== GLOBAL CREW STATE ANALYSIS ===");
        
        foreach (var manager in monitoredCrewManagers)
        {
            if (manager != null)
            {
                manager.LogCurrentCrewStatus();
            }
        }
        
        GameLogger.Log("=== END GLOBAL ANALYSIS ===");
    }
    
    private void AnalyzeCrewDisappearances()
    {
        GameLogger.Log("=== CREW DISAPPEARANCE ANALYSIS ===");
        
        foreach (var manager in monitoredCrewManagers)
        {
            if (manager == null) continue;
            
            var allCrew = manager.GetAllCrewMembers();
            var activeCrew = manager.GetActiveCrewMembers();
            
            GameLogger.Log($"Boat {manager.GetBoatID()}: {activeCrew.Count}/{allCrew.Count} active");
            
            if (activeCrew.Count < 1)
            {
                GameLogger.LogError($"❌ CRITICAL: Boat {manager.GetBoatID()} has NO active crew members!");
                
                for (int i = 0; i < allCrew.Count; i++)
                {
                    var crew = allCrew[i];
                    if (crew != null)
                    {
                        GameObject handler = crew.transform.parent?.gameObject ?? crew.gameObject;
                        GameLogger.LogError($"  Crew {i}: {crew.name} - State: {crew.State}, Handler Active: {handler.activeInHierarchy}");
                    }
                }
            }
            else if (activeCrew.Count > 2)
            {
                GameLogger.LogWarning($"⚠️ WARNING: Boat {manager.GetBoatID()} has too many active crew ({activeCrew.Count})!");
            }
        }
        
        GameLogger.Log("=== END DISAPPEARANCE ANALYSIS ===");
    }
}
