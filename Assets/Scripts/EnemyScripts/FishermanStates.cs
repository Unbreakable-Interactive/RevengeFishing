using UnityEngine;

// Base class for all fisherman states
public abstract class FishermanState : EnemyState
{
    protected Fisherman fisherman;

    public override void Initialize(EnemyStateMachine stateMachine)
    {
        base.Initialize(stateMachine);
        this.fisherman = stateMachine.GetComponent<Fisherman>();
    }
}

// BASIC MOVEMENT STATES
public class FishermanIdleWithoutRodState : FishermanState
{
    private float idleTimer = 0f;
    private float maxIdleTime = 3f;

    public override void EnterState()
    {
        idleTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("IdleWithoutRod");
        }
    }

    public override void UpdateState()
    {
        idleTimer += Time.deltaTime;

        // Example transition logic - you'll customize this
        if (idleTimer >= maxIdleTime)
        {
            // Randomly decide what to do next
            if (Random.value < 0.5f)
            {
                ChangeState<FishermanWalkingState>();
            }
            else
            {
                ChangeState<FishermanEquippingRodState>();
            }
        }
    }

    public override void ExitState()
    {
        // Cleanup if needed
    }
}

public class FishermanIdleWithRodState : FishermanState
{
    private float idleTimer = 0f;
    private float maxIdleTime = 2f;

    public override void EnterState()
    {
        idleTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("IdleWithRod");
        }
    }

    public override void UpdateState()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= maxIdleTime)
        {
            // Start fishing or put rod away
            if (Random.value < 0.7f)
            {
                ChangeState<FishermanCastingState>();
            }
            else
            {
                ChangeState<FishermanPuttingAwayRodState>();
            }
        }
    }

    public override void ExitState()
    {
        // Cleanup
    }
}

public class FishermanWalkingState : FishermanState
{
    private float walkTimer = 0f;
    private float maxWalkTime = 4f;
    private Vector2 walkDirection;
    private float walkSpeed = 2f;

    public override void EnterState()
    {
        walkTimer = 0f;
        walkDirection = Random.value < 0.5f ? Vector2.left : Vector2.right;

        if (fisherman != null)
        {
            fisherman.SetAnimationState("Walking");
            fisherman.SetFacingDirection(walkDirection.x > 0);
        }
    }

    public override void UpdateState()
    {
        walkTimer += Time.deltaTime;

        // Move the fisherman
        rb.velocity = new Vector2(walkDirection.x * walkSpeed, rb.velocity.y);

        if (walkTimer >= maxWalkTime)
        {
            ChangeState<FishermanIdleWithoutRodState>();
        }
    }

    public override void ExitState()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }
}

public class FishermanRunningState : FishermanState
{
    private float runTimer = 0f;
    private float maxRunTime = 2f;
    private Vector2 runDirection;
    private float runSpeed = 5f;

    public override void EnterState()
    {
        runTimer = 0f;
        runDirection = Random.value < 0.5f ? Vector2.left : Vector2.right;

        if (fisherman != null)
        {
            fisherman.SetAnimationState("Running");
            fisherman.SetFacingDirection(runDirection.x > 0);
        }
    }

    public override void UpdateState()
    {
        runTimer += Time.deltaTime;

        // Move the fisherman faster
        rb.velocity = new Vector2(runDirection.x * runSpeed, rb.velocity.y);

        if (runTimer >= maxRunTime)
        {
            ChangeState<FishermanIdleWithoutRodState>();
        }
    }

    public override void ExitState()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }
}

// ROD MANAGEMENT STATES
public class FishermanEquippingRodState : FishermanState
{
    private float equipTimer = 0f;
    private float equipDuration = 1f;

    public override void EnterState()
    {
        equipTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("EquippingRod");
        }
    }

    public override void UpdateState()
    {
        equipTimer += Time.deltaTime;

        if (equipTimer >= equipDuration)
        {
            if (fisherman != null)
            {
                fisherman.SetRodEquipped(true);
            }
            ChangeState<FishermanIdleWithRodState>();
        }
    }

    public override void ExitState()
    {
        // Rod is now equipped
    }
}

public class FishermanPuttingAwayRodState : FishermanState
{
    private float putAwayTimer = 0f;
    private float putAwayDuration = 1f;

    public override void EnterState()
    {
        putAwayTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("PuttingAwayRod");
        }
    }

    public override void UpdateState()
    {
        putAwayTimer += Time.deltaTime;

        if (putAwayTimer >= putAwayDuration)
        {
            if (fisherman != null)
            {
                fisherman.SetRodEquipped(false);
            }
            ChangeState<FishermanIdleWithoutRodState>();
        }
    }

    public override void ExitState()
    {
        // Rod is now put away
    }
}

// FISHING STATES
public class FishermanCastingState : FishermanState
{
    private float castTimer = 0f;
    private float castDuration = 1.5f;

    public override void EnterState()
    {
        castTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("Casting");
        }
    }

    public override void UpdateState()
    {
        castTimer += Time.deltaTime;

        if (castTimer >= castDuration)
        {
            if (fisherman != null)
            {
                fisherman.SetLineCast(true);
            }
            // Transition to waiting for fish or line management
            ChangeState<FishermanWaitingForBiteState>();
        }
    }

    public override void ExitState()
    {
        // Line is now cast
    }
}

public class FishermanWaitingForBiteState : FishermanState
{
    private float waitTimer = 0f;
    private float maxWaitTime = 8f;

    public override void EnterState()
    {
        waitTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("WaitingForBite");
        }
    }

    public override void UpdateState()
    {
        waitTimer += Time.deltaTime;

        // Check if a fish bit the hook
        if (fisherman != null && fisherman.HasFishOnHook())
        {
            ChangeState<FishermanStrikingState>();
            return;
        }

        // Occasionally jig the line
        if (Random.value < 0.01f) // 1% chance per frame to jig
        {
            ChangeState<FishermanJiggingState>();
            return;
        }

        // Give up after max wait time
        if (waitTimer >= maxWaitTime)
        {
            ChangeState<FishermanRetrievingState>();
        }
    }

    public override void ExitState()
    {
        // Continue with line management
    }
}

public class FishermanJiggingState : FishermanState
{
    private float jigTimer = 0f;
    private float jigDuration = 0.5f;

    public override void EnterState()
    {
        jigTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("Jigging");
            fisherman.JigLine();
        }
    }

    public override void UpdateState()
    {
        jigTimer += Time.deltaTime;

        if (jigTimer >= jigDuration)
        {
            ChangeState<FishermanWaitingForBiteState>();
        }
    }

    public override void ExitState()
    {
        // Return to waiting
    }
}

public class FishermanStrikingState : FishermanState
{
    private float strikeTimer = 0f;
    private float strikeDuration = 0.3f;
    private int strikeCount = 0;
    private int maxStrikes = 5;
    private float strikeInterval = 0.5f;

    public override void EnterState()
    {
        strikeTimer = 0f;
        strikeCount = 0;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("Striking");
        }
    }

    public override void UpdateState()
    {
        strikeTimer += Time.deltaTime;

        if (strikeTimer >= strikeInterval && strikeCount < maxStrikes)
        {
            strikeCount++;
            strikeTimer = 0f;

            if (fisherman != null)
            {
                fisherman.StrikeLine();
                // Deal fatigue damage to the fish
                // You'll implement this when you add fish combat
            }
        }

        // Check if fish is defeated or escaped
        if (fisherman != null)
        {
            if (fisherman.IsFishDefeated())
            {
                ChangeState<FishermanCatchingState>();
                return;
            }
            else if (!fisherman.HasFishOnHook())
            {
                ChangeState<FishermanRetrievingState>();
                return;
            }
        }

        // Continue striking if fish is still fighting
        if (strikeCount >= maxStrikes)
        {
            strikeCount = 0; // Reset for continuous striking
        }
    }

    public override void ExitState()
    {
        // Combat is over
    }
}

public class FishermanRetrievingState : FishermanState
{
    private float retrieveTimer = 0f;
    private float retrieveDuration = 2f;

    public override void EnterState()
    {
        retrieveTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("Retrieving");
        }
    }

    public override void UpdateState()
    {
        retrieveTimer += Time.deltaTime;

        if (retrieveTimer >= retrieveDuration)
        {
            if (fisherman != null)
            {
                fisherman.SetLineCast(false);
            }
            ChangeState<FishermanIdleWithRodState>();
        }
    }

    public override void ExitState()
    {
        // Line is now retrieved
    }
}

public class FishermanCatchingState : FishermanState
{
    private float catchTimer = 0f;
    private float catchDuration = 1.5f;

    public override void EnterState()
    {
        catchTimer = 0f;
        if (fisherman != null)
        {
            fisherman.SetAnimationState("Catching");
        }
    }

    public override void UpdateState()
    {
        catchTimer += Time.deltaTime;

        if (catchTimer >= catchDuration)
        {
            if (fisherman != null)
            {
                fisherman.CatchFish();
                fisherman.SetLineCast(false);
            }
            ChangeState<FishermanIdleWithRodState>();
        }
    }

    public override void ExitState()
    {
        // Fish is caught, line is retrieved
    }
}
