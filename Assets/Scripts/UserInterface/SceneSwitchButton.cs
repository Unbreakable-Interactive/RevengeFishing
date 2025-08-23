using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneSwitchButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private GameScene gameScene;
    
    
    [Header("Optional Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(SwitchScene);
        }
        else
        {
            if (enableDebugLogs)
                GameLogger.LogError($"SceneSwitchButton: No Button component found on {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(SwitchScene);
        }
    }

    public void SwitchScene() => GameSceneManager.LoadScene(gameScene);
}