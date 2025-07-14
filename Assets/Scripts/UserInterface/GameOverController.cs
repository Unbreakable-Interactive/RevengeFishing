using UnityEngine;
using UnityEngine.UI;
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
        // Validate that gameOverText is assigned in inspector
        if (gameOverText == null)
        {
            Debug.LogError("GameOverController: gameOverText is not assigned in inspector! Please assign the TextMeshProUGUI component in the inspector.");
            return;
        }

        // Get the appropriate message based on death type
        string message = DeathManager.GetDeathMessage(DeathManager.LastDeathType);
        gameOverText.text = message;

        Debug.Log($"Game Over message set to: {message}");
    }
}
