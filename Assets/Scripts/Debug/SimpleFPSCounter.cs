using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleFPSCounter : MonoBehaviour
{
    [Header("FPS Settings")]
    public bool showFPS = true;
    public KeyCode toggleKey = KeyCode.F1;
    
    // [Header("Display")]
    // public int fontSize = 18;
    // public Vector2 position = new Vector2(10, 10);
    
    [Header("Colors")]
    public Color goodColor = Color.green;      // >= 60 FPS
    public Color okayColor = Color.yellow;     // >= 30 FPS
    public Color badColor = Color.red;         // < 30 FPS
    
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private Canvas fpsCanvas;
    [SerializeField] private float deltaTime = 0.0f;
    
    private void Start()
    {
        // CreateSimpleFPSUI();
    }
    
    // private void CreateSimpleFPSUI()
    // {
    //     // Create canvas for FPS display
    //     GameObject canvasObj = new GameObject("SimpleFPSCanvas");
    //     canvasObj.transform.SetParent(transform);
    //     
    //     fpsCanvas = canvasObj.AddComponent<Canvas>();
    //     fpsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    //     fpsCanvas.sortingOrder = 1000; // Top priority
    //     
    //     // Add CanvasScaler for resolution independence
    //     CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
    //     scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    //     scaler.referenceResolution = new Vector2(1920, 1080);
    //     scaler.matchWidthOrHeight = 0.5f;
    //     
    //     // Create FPS text object
    //     GameObject textObj = new GameObject("FPSText");
    //     textObj.transform.SetParent(fpsCanvas.transform, false);
    //     
    //     RectTransform rectTransform = textObj.AddComponent<RectTransform>();
    //     rectTransform.anchorMin = new Vector2(0, 1);
    //     rectTransform.anchorMax = new Vector2(0, 1);
    //     rectTransform.pivot = new Vector2(0, 1);
    //     rectTransform.anchoredPosition = position;
    //     rectTransform.sizeDelta = new Vector2(150, 50);
    //     
    //     fpsText = textObj.AddComponent<Text>();
    //     fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    //     fpsText.fontSize = fontSize;
    //     fpsText.color = Color.white;
    //     fpsText.text = "FPS: --";
    //     
    //     // Add background for better readability
    //     GameObject bgObj = new GameObject("FPSBackground");
    //     bgObj.transform.SetParent(textObj.transform, false);
    //     bgObj.transform.SetSiblingIndex(0);
    //     
    //     RectTransform bgRect = bgObj.AddComponent<RectTransform>();
    //     bgRect.anchorMin = Vector2.zero;
    //     bgRect.anchorMax = Vector2.one;
    //     bgRect.offsetMin = new Vector2(-5, -5);
    //     bgRect.offsetMax = new Vector2(5, 5);
    //     
    //     Image bgImage = bgObj.AddComponent<Image>();
    //     bgImage.color = new Color(0, 0, 0, 0.5f);
    //     
    //     Debug.Log("Simple FPS Counter created successfully!");
    // }
    
    private void Update()
    {
        // Toggle FPS display
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFPS();
        }
        
        if (!showFPS || fpsText == null) return;
        
        // Calculate FPS with smoothing
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        // Update text
        fpsText.text = $"FPS: {fps:F0}";
        
        // Color coding based on performance
        if (fps >= 60f)
            fpsText.color = goodColor;
        else if (fps >= 30f)
            fpsText.color = okayColor;
        else
            fpsText.color = badColor;
    }
    
    public void ToggleFPS()
    {
        showFPS = !showFPS;
        if (fpsCanvas != null)
        {
            fpsCanvas.gameObject.SetActive(showFPS);
        }
        Debug.Log($"FPS Counter {(showFPS ? "enabled" : "disabled")}");
    }
    
    public void SetVisible(bool visible)
    {
        showFPS = visible;
        if (fpsCanvas != null)
        {
            fpsCanvas.gameObject.SetActive(visible);
        }
    }
}