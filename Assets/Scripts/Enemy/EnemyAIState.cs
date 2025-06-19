using UnityEngine;

public abstract class EnemyAIState
{
    protected EnemyBase enemy;

    public EnemyAIState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
    public abstract bool CanTransitionTo(System.Type stateType);
}

public class IdleState : EnemyAIState
{
    public IdleState(EnemyBase enemy) : base(enemy) { }

    public override void EnterState()
    {
        enemy.currentMovementState = EnemyBase.LandMovementState.Idle;
    }

    public override void UpdateState()
    {
        // Handle idle behavior 
    }

    public override void ExitState() { }

    public override bool CanTransitionTo(System.Type stateType) => true;
}

public class WalkingState : EnemyAIState
{
    public WalkingState(EnemyBase enemy) : base(enemy) { }

    public override void EnterState()
    {
        // Choose random walking direction
        enemy.currentMovementState = (UnityEngine.Random.value < 0.5f) ?
            EnemyBase.LandMovementState.WalkLeft : EnemyBase.LandMovementState.WalkRight;
    }

    public override void UpdateState()
    {
        // Handle walking behavior 
    }

    public override void ExitState() { }

    public override bool CanTransitionTo(System.Type stateType) => true;
}

public class FishingState : EnemyAIState
{
    public FishingState(EnemyBase enemy) : base(enemy) { }

    public override void EnterState()
    {
        enemy.currentMovementState = EnemyBase.LandMovementState.Idle;
        enemy.TryEquipFishingTool();
    }

    public override void UpdateState()
    {
        // Handle fishing decisions 
    }

    public override void ExitState()
    {
        enemy.TryUnequipFishingTool();
    }

    public override bool CanTransitionTo(System.Type stateType)
    {
        return !enemy.fishingToolEquipped;
    }
}
