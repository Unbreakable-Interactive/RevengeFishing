using UnityEngine;

[System.Serializable]
public class BoatID
{
    [SerializeField] private string _uniqueID;
    
    public string UniqueID => _uniqueID;
    
    public BoatID()
    {
        GenerateNewID();
    }
    
    public void GenerateNewID()
    {
        _uniqueID = $"Boat_{System.Guid.NewGuid().ToString("N")[0..8]}";
        
        GameLogger.LogVerbose($"[BOAT ID] Generated new unique ID: {_uniqueID}");
    }
    
    public bool Matches(BoatID other)
    {
        return other != null && _uniqueID == other._uniqueID;
    }
    
    public bool Matches(string otherID)
    {
        return _uniqueID == otherID;
    }
    
    public override string ToString()
    {
        return _uniqueID ?? "NULL_ID";
    }
}

public interface IBoatComponent
{
    string GetBoatID();
    void SetBoatID(BoatID boatID);
}