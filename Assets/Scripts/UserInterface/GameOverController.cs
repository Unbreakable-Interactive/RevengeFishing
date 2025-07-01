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
        // Find the game over text if not assigned
        if (gameOverText == null)
        {
            gameOverText = GameObject.Find("DeathMessage")?.GetComponent<TextMeshProUGUI>();
        }

        if (gameOverText != null)
        {
            // Get the appropriate message based on death type
            string message = DeathManager.GetDeathMessage(DeathManager.LastDeathType);
            gameOverText.text = message;

            Debug.Log($"Game Over message set to: {message}");
        }
        else
        {
            Debug.LogError("GameOverController: Could not find TextMeshProUGUI component for game over message!");
        }
    }
}
