using UnityEngine;

public class DeathManager : MonoBehaviour
{
    public static Player.Status LastDeathType { get; private set; } = Player.Status.Alive;

    public static void SetDeathType(Player.Status deathType)
    {
        LastDeathType = deathType;
        Debug.Log($"Death type set to: {deathType}");
    }

    public static string GetDeathMessage(Player.Status deathType)
    {
        switch (deathType)
        {
            case Player.Status.Fished:
                return "You've been fished!";
            case Player.Status.Starved:
                return "You've starved!";
            case Player.Status.Slain:
                return "You've been slain!";
            default:
                return "You've died!";
        }
    }
}
