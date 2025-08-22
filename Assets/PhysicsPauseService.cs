using UnityEngine;

public class PhysicsPauseService : MonoBehaviour
{
    [Tooltip("Si quieres clamp de dt cuando haya spikes")]
    public float maxStep = 0.033f; // 30 FPS cap del step acumulado (opcional)

    float acc; // acumulador para step fijo

    void Awake()
    {
        // Modo manual
        Physics.autoSimulation = false;                  // 3D
        Physics2D.simulationMode = SimulationMode2D.Script; // 2D
    }

    void OnEnable()
    {
        if (GameStates.instance != null)
            GameStates.instance.OnStateChanged += HandleState;
    }

    void OnDisable()
    {
        if (GameStates.instance != null)
            GameStates.instance.OnStateChanged -= HandleState;
    }

    void HandleState(GameState oldState, GameState newState)
    {
        // Al pausar, no hay que hacer nada especial: simplemente dejamos de simular
        // Al reanudar, el acumulador sigue; si prefieres, resetea acc = 0;
        if (newState == GameState.Pause || newState == GameState.Upgrade) acc = 0f;
    }

    void Update()
    {
        if (GameStates.instance == null || !GameStates.instance.IsGameplayRunning()) return;

        // Acumula tiempo real por frame; mantenemos determinismo por steps fijos
        float dt = Mathf.Min(Time.deltaTime, maxStep);
        acc += dt;
        float step = Time.fixedDeltaTime;

        // Itera steps fijos (pueden ser 0, 1 o varios si hubo lag)
        while (acc >= step)
        {
            Physics.Simulate(step);    // 3D
            Physics2D.Simulate(step);  // 2D
            acc -= step;
        }
    }
}