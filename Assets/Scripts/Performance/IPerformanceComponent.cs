using UnityEngine;

public interface IPerformanceComponent
{
    string ComponentName { get; }
    bool IsEnabled { get; set; }
    bool ShowDebugInfo { get; set; }
    void Initialize();
    void UpdateComponent();
    float RenderDebugGUI(float startY);
    string GetPerformanceMetrics();
    void SetEnabled(bool enabled);
    void Reset();
}

public abstract class PerformanceComponentBase : MonoBehaviour, IPerformanceComponent
{
    [Header("Performance Component Settings")]
    [SerializeField] protected bool isEnabled = true;
    [SerializeField] protected bool showDebugInfoField = false;
    
    public abstract string ComponentName { get; }
    
    public bool IsEnabled 
    { 
        get => isEnabled; 
        set => SetEnabled(value); 
    }
    
    public bool ShowDebugInfo 
    { 
        get => showDebugInfoField; 
        set => showDebugInfoField = value; 
    }
    
    protected virtual void Awake()
    {
        PerformanceManager manager = FindObjectOfType<PerformanceManager>();
        if (manager != null)
        {
            manager.RegisterComponent(this);
        }
    }
    
    protected virtual void Start()
    {
        if (isEnabled)
        {
            Initialize();
        }
    }
    
    public abstract void Initialize();
    public abstract void UpdateComponent();
    public abstract float RenderDebugGUI(float startY);
    public abstract string GetPerformanceMetrics();
    
    public virtual void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        this.enabled = enabled;
        
        if (enabled)
        {
            Initialize();
        }
    }
    
    public virtual void Reset()
    {
        isEnabled = true;
        showDebugInfoField = false;
    }
    
    protected GUIStyle GetDebugGUIStyle(int fontSize = 14, Color? textColor = null)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = textColor ?? Color.white;
        return style;
    }
}