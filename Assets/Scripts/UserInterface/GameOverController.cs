using UnityEngine;
using TMPro; 

public class GameOverController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Default Messages")]
    [SerializeField] private string defaultGameOverMessage = "You've died!";

    private void Start()
    {
        SetupGameOverMessage();
    }

    private void SetupGameOverMessage()
    {
        if (gameOverText == null)
        {
            GameLogger.LogError("GameOverController: gameOverText is not assigned in inspector! Please assign the TextMeshProUGUI component in the inspector.");
            return;
        }

        string message = DeathManager.GetDeathMessage(DeathManager.LastDeathType);
        gameOverText.text = message;

        GameLogger.LogVerbose($"Game Over message set to: {message}");
    }
}