using UnityEngine;
using System.Collections.Generic;

public class EnemyStateMachine : MonoBehaviour
{
    public EnemyState currentState;
    public EnemyState previousState;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Dictionary<System.Type, EnemyState> stateCache = new Dictionary<System.Type, EnemyState>();

    void Update()
    {
        if (currentState != null)
        {
            currentState.UpdateState();
        }
    }

    public void ChangeState<T>() where T : EnemyState, new()
    {
        ChangeState(typeof(T));
    }

    public void ChangeState(System.Type stateType)
    {
        if (currentState != null && currentState.GetType() == stateType)
            return; // Already in this state

        // Get or create state
        if (!stateCache.ContainsKey(stateType))
        {
            EnemyState newState = (EnemyState)System.Activator.CreateInstance(stateType);
            newState.Initialize(this);
            stateCache[stateType] = newState;
        }

        // Exit current state
        if (currentState != null)
        {
            currentState.ExitState();
            previousState = currentState;
        }

        // Enter new state
        currentState = stateCache[stateType];
        currentState.EnterState();

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} changed state to {stateType.Name}");
        }
    }

    public T GetState<T>() where T : EnemyState
    {
        System.Type stateType = typeof(T);
        if (stateCache.ContainsKey(stateType))
        {
            return (T)stateCache[stateType];
        }
        return null;
    }

    public bool IsInState<T>() where T : EnemyState
    {
        return currentState != null && currentState is T;
    }

    public bool WasInState<T>() where T : EnemyState
    {
        return previousState != null && previousState is T;
    }
}
