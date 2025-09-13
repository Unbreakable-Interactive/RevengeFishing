using UnityEngine;

[System.Serializable]
public class Timer
{
    [SerializeField] private float duration;
    [SerializeField] private float currentTime;
    [SerializeField] private bool isRunning;
    [SerializeField] private bool hasCompleted;

    public Timer(float duration)
    {
        this.duration = duration;
        Reset();
    }

    public void Start()
    {
        isRunning = true;
        hasCompleted = false;
    }

    public void Stop()
    {
        isRunning = false;
    }

    public void Reset()
    {
        currentTime = 0f;
        isRunning = false;
        hasCompleted = false;
    }

    public void Restart(float newDuration = -1f)
    {
        if (newDuration > 0f)
            duration = newDuration;
        Reset();
        Start();
    }

    public void Update(float deltaTime)
    {
        if (!isRunning) return;

        currentTime += deltaTime;
        if (currentTime >= duration && !hasCompleted)
        {
            hasCompleted = true;
            isRunning = false;
        }
    }

    public bool IsFinished => hasCompleted;
    public bool IsRunning => isRunning;
    public float Progress => duration > 0 ? Mathf.Clamp01(currentTime / duration) : 1f;
    public float RemainingTime => Mathf.Max(0f, duration - currentTime);
    public float CurrentTime => currentTime;
    public float Duration => duration;

    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }
}