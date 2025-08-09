using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class SceneSwitchButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "";

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

    public void SwitchScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            if (enableDebugLogs)
                GameLogger.LogWarning($"SceneSwitchButton: Target scene name is empty on {gameObject.name}");
            return;
        }

        if (enableDebugLogs)
            GameLogger.LogVerbose($"SceneSwitchButton: Switching to scene '{targetSceneName}'");

        SceneManager.LoadScene(targetSceneName);
    }

    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }
}