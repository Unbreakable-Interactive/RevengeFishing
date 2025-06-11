using UnityEngine;

public abstract class EnemyState
{
    protected EnemyStateMachine stateMachine;
    protected Enemy enemy;
    protected Transform transform;
    protected Rigidbody2D rb;

    public virtual void Initialize(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        this.enemy = stateMachine.GetComponent<Enemy>();
        this.transform = stateMachine.transform;
        this.rb = stateMachine.GetComponent<Rigidbody2D>();
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();

    // Helper method for state transitions
    protected void ChangeState<T>() where T : EnemyState, new()
    {
        stateMachine.ChangeState<T>();
    }

    protected void ChangeState(System.Type stateType)
    {
        stateMachine.ChangeState(stateType);
    }
}
