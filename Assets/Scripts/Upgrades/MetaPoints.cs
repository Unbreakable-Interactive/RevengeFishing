using UnityEngine;

public static class MetaPoints
{
    const string KeyTotal = "META_TOTAL";
    const string KeyPhasePrefix = "META_PHASE_";
    const string KeyWins = "META_WINS";

    public static int GetTotal() => PlayerPrefs.GetInt(KeyTotal, 0);

    public static void AddPhasePoints(Player.Phase phase)
    {
        var total = PhasePoints(phase);
        PlayerPrefs.SetInt(KeyTotal, total);
        PlayerPrefs.Save();
    }

    public static int PhasePoints(Player.Phase phase)
    {
        int points = 0;
        switch (phase)
        {
            default:
            case Player.Phase.Juvenile:
                points = GetTotal() + 10;
                break;
            
            case Player.Phase.Adult:
                points = GetTotal() + 20;
                break;
            
            case Player.Phase.Beast:
                points = GetTotal() + 30;
                break;
            
            case Player.Phase.Monster:
                points = GetTotal() + 50;
                break;
        }

        return points;
    }

    public static bool Spend(int amount)
    {
        if (amount <= 0) return true;
        var total = GetTotal();
        if (total < amount) return false;
        PlayerPrefs.SetInt(KeyTotal, total - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyTotal);
        PlayerPrefs.DeleteKey(KeyWins);
        foreach (Player.Phase p in System.Enum.GetValues(typeof(Player.Phase)))
            PlayerPrefs.DeleteKey(KeyPhasePrefix + (int)p);
        PlayerPrefs.Save();
    }
}