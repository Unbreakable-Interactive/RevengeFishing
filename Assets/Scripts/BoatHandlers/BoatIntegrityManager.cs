using System.Collections.Generic;
using UnityEngine;

public class BoatIntegrityManager : MonoBehaviour
{
    [Header("Integrity System")]
    [SerializeField] private List<Enemy> detectedCrew;
    [SerializeField] private float currentIntegrity;

    [SerializeField] private LayerMask crewLayer = 1 << 10; // BoatEnemy layer

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCrewMember(other))
        {
            Debug.Log("Detecting OnTriggerEnter2D");
          //  RegisterCrewMember(other.GetComponent<Enemy>());
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsCrewMember(other))
        {
            Debug.Log("Detecting OnTriggerStay2D");
        //    RegisterCrewMember(other.GetComponent<Enemy>());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsCrewMember(other))
        {
         //   UnregisterCrewMember(other.GetComponent<Enemy>());
        }
    }


    private bool IsCrewMember(Collider2D collider)
    {
        string colliderLayerName = LayerMask.LayerToName(collider.gameObject.layer);
        string expectedLayerName = LayerMask.LayerToName(GetLayerFromMask(crewLayer));

        return colliderLayerName == expectedLayerName;
        // && collider.GetComponent<Enemy>() != null;
    }

    private int GetLayerFromMask(LayerMask layerMask)
    {
        return Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));
    }


    private void RegisterCrewMember(Enemy crewMember)
    {
        if (crewMember == null || detectedCrew.Contains(crewMember))
            return;

        detectedCrew.Add(crewMember);


        if (enableDebugLogs)
            Debug.Log($"Crew member {crewMember.name} registered. Integrity: {currentIntegrity}");
    }

    private void UnregisterCrewMember(Enemy crewMember)
    {
        if (crewMember == null || !detectedCrew.Contains(crewMember))
            return;

        detectedCrew.Remove(crewMember);


        if (enableDebugLogs)
            Debug.Log($"Crew member {crewMember.name} unregistered. Integrity: {currentIntegrity}");
    }

    public void ResetIntegrity()
    {
        detectedCrew.Clear();
        currentIntegrity = 0f;

        if (enableDebugLogs)
            Debug.Log($"Boat integrity reset");
    }

}
