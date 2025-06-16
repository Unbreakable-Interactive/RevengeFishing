using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public enum Tier
    {
        Tier1=0,
        Tier2=1,
        Tier3=2,
        Tier4=3,
        Tier5=4,
        Tier6=5
    }

    public enum EnemyState
    {
        Alive=0,
        Defeated=1,
        Eaten=2,
        Dead=3
    }

    public enum EnemyType
    {
        Land,
        Water
    }

    // Movement states for land enemies
    public enum LandMovementState
    {
        Idle,
        WalkLeft,
        WalkRight,
        RunLeft,
        RunRight
    }

    protected Rigidbody2D _rb;
    protected float _powerLevel;
    protected float _fatigue;
    protected float _maxFatigue;
    protected Tier _tier;
    protected EnemyState _state;
    protected EnemyType _type;
    protected LandMovementState _landMovementState;

    #region Platform Assignment
    [Header("Platform Assignment")]
    public Platform assignedPlatform; // For land enemies only

    // Method called by Platform when assigning this enemy
    public virtual void SetAssignedPlatform(Platform platform)
    {
        assignedPlatform = platform;
    }

    public Platform GetAssignedPlatform()
    {
        return assignedPlatform;
    }


    #endregion

    #region Land Variables

    protected float walkingSpeed;
    protected float runningSpeed;
    protected float edgeBuffer; // Distance from platform edge to change direction
    // assigned platform was set previously in Platform Assignment region

    // AI state for land movement
    protected float minActionTime; //Minimum seconds enemy will do an action, like walk, idle, or run
    protected float maxActionTime; //Maximum seconds enemy will do an action, like walk, idle, or run

    protected LandMovementState currentMovementState;
    protected float nextActionTime;
    protected bool isGrounded;

    // Platform bounds
    protected float platformLeftEdge;
    protected float platformRightEdge;
    protected bool platformBoundsCalculated;


    protected float floatingForce;
    protected float maxUpwardVelocity; // For when they swim upward

    protected float weight; // How much the enemy sinks in water; varies between 60 and 100 kg
    
    #endregion

    #region Water Variables

    protected float swimForce;
    protected float minSwimSpeed;
    protected float maxSwimSpeed;
    
    #endregion

    #region Base Behaviours

    // How enemy behaves when interacts with player
    public abstract void ReverseFishingBehaviour();

    public virtual void LandMovement()
    {
        LandWalk();
    }

    public abstract void WaterMovement();
    

    public virtual void Initialize(float powerLevel)
    {
        _powerLevel = powerLevel;
        
        _fatigue = 0;
        _maxFatigue = _powerLevel;

        _state = EnemyState.Alive;

        // weight must be a random value between x and y

        // CalculateTier();

        walkingSpeed = 2f;
        runningSpeed = 4f;
        edgeBuffer = .5f; // Distance from platform edge to change direction
        // assigned platform was set previously in Platform Assignment region

        // AI state for land movement
        minActionTime = 1f; //Minimum seconds enemy will do an action, like walk, idle, or run
        maxActionTime = 4f; //Maximum seconds enemy will do an action, like walk, idle, or run

        LandMovementState currentMovementState;
        isGrounded = false;

        platformBoundsCalculated = false;

        weight = 6f; // How much the enemy sinks in water; varies between 60 and 100 kg

}

private void CalculateTier()
    {
        // if (_powerLevel is > 100 and < 500)
        // {
        //     _tier = Tier.Tier1;
        // }

        _tier = Tier.Tier1;
    }

    protected float TakeFatigue(float playerPowerLevel)
    {
        // 5% more fatigue
        _fatigue += playerPowerLevel * .05f;
        return Mathf.Clamp(_fatigue, 0, _maxFatigue);
    }

    #endregion

    #region State Management
    
    // Make sure your GetState() method is public
    public EnemyState GetState()
    {
        return _state;
    }

    public virtual void ChangeState_Defeated() => _state = EnemyState.Defeated;
    public virtual void ChangeState_Eaten() => _state = EnemyState.Eaten;
    public virtual void ChangeState_Dead() => _state = EnemyState.Dead;

    #endregion


    #region Actions

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == EnemyState.Defeated)
        {
            ChangeState_Eaten();
        }
    }

    public virtual void EnemyDie() { }

    #endregion

    #region Land Movement Logic
    public virtual void LandWalk()
    {
        if (_type != EnemyType.Land) return;

        // Initialize components if needed
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody2D>();
                _rb.freezeRotation = true;
            }
        }

        // Check if we're on our assigned platform
        CheckGroundedStatus();

        // Calculate platform bounds once we have an assigned platform
        if (assignedPlatform != null && !platformBoundsCalculated)
        {
            CalculatePlatformBounds();
        }

        // Execute current movement behavior
        ExecuteLandMovementBehavior();

        // Check if it's time to change behavior
        if (Time.time >= nextActionTime)
        {
            ChooseRandomAction();
            ScheduleNextAction();
        }

        // Safety check - don't fall off platform
        if (platformBoundsCalculated)
        {
            CheckPlatformBounds();
        }

    }

    protected virtual void CalculatePlatformBounds()
    {
        if (assignedPlatform == null) return;

        Collider2D platformCollider = assignedPlatform.GetComponent<Collider2D>();
        if (platformCollider != null)
        {
            Bounds bounds = platformCollider.bounds;
            platformLeftEdge = bounds.min.x + edgeBuffer;
            platformRightEdge = bounds.max.x - edgeBuffer;
            platformBoundsCalculated = true;

            if (assignedPlatform.showDebugInfo)
            {
                Debug.Log($"Platform bounds calculated for {gameObject.name}: Left={platformLeftEdge}, Right={platformRightEdge}");
            }
        }
    }

    protected virtual void CheckGroundedStatus()
    {
        // Simple ground check - raycast down
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        isGrounded = hit.collider != null && hit.collider.gameObject == assignedPlatform?.gameObject;
    }

    protected virtual void ExecuteLandMovementBehavior()
    {
        if (!isGrounded || assignedPlatform == null) return;

        Vector2 movement = Vector2.zero;

        switch (currentMovementState)
        {
            case LandMovementState.Idle:
                // Do nothing, just stand still
                break;

            case LandMovementState.WalkLeft:
                movement = Vector2.left * walkingSpeed;
                break;

            case LandMovementState.WalkRight:
                movement = Vector2.right * walkingSpeed;
                break;

            case LandMovementState.RunLeft:
                movement = Vector2.left * runningSpeed;
                break;

            case LandMovementState.RunRight:
                movement = Vector2.right * runningSpeed;
                break;
        }

        // Apply movement while preserving Y velocity (gravity)
        if (_rb != null)
        {
            _rb.velocity = new Vector2(movement.x, _rb.velocity.y);
        }
    }

    protected virtual void CheckPlatformBounds()
    {
        float currentX = transform.position.x;

        // If we're near the left edge and moving left, stop or turn around
        if (currentX <= platformLeftEdge && (currentMovementState == LandMovementState.WalkLeft || currentMovementState == LandMovementState.RunLeft))
        {
            // Choose a new action that doesn't involve going left
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
        }
        // If we're near the right edge and moving right, stop or turn around
        else if (currentX >= platformRightEdge && (currentMovementState == LandMovementState.WalkRight || currentMovementState == LandMovementState.RunRight))
        {
            // Choose a new action that doesn't involve going right
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
        }
    }

    protected virtual void ChooseRandomAction()
    {
        // WEIGHTED SELECTION - Bias toward idle
        float randomValue = UnityEngine.Random.value; // 0.0 to 1.0

        if (randomValue < 0.5f) // 50% chance for idle
        {
            currentMovementState = LandMovementState.Idle;
        }
        else if (randomValue < 0.7f) // 20% chance for walk left
        {
            currentMovementState = LandMovementState.WalkLeft;
        }
        else if (randomValue < 0.9f) // 20% chance for walk right
        {
            currentMovementState = LandMovementState.WalkRight;
        }
        else if (randomValue < 0.95f) // 5% chance for run left
        {
            currentMovementState = LandMovementState.RunLeft;
        }
        else // 5% chance for run right
        {
            currentMovementState = LandMovementState.RunRight;
        }

        //LandMovementState[] possibleStates = {
        //    LandMovementState.Idle,
        //    LandMovementState.WalkLeft,
        //    LandMovementState.WalkRight,
        //    LandMovementState.RunLeft,
        //    LandMovementState.RunRight
        //};

        //currentMovementState = possibleStates[UnityEngine.Random.Range(0, possibleStates.Length)];

        //if (assignedPlatform != null && assignedPlatform.showDebugInfo)
        //{
        //    Debug.Log($"{gameObject.name} chose action: {currentMovementState}");
        //}
    }

    protected virtual void ChooseRandomActionExcluding(params LandMovementState[] excludedStates)
    {
        LandMovementState[] allStates = {
            LandMovementState.Idle,
            LandMovementState.WalkLeft,
            LandMovementState.WalkRight,
            LandMovementState.RunLeft,
            LandMovementState.RunRight
        };

        List<LandMovementState> validStates = new List<LandMovementState>();

        foreach (LandMovementState state in allStates)
        {
            bool isExcluded = false;
            foreach (LandMovementState excluded in excludedStates)
            {
                if (state == excluded)
                {
                    isExcluded = true;
                    break;
                }
            }

            if (!isExcluded)
            {
                validStates.Add(state);
            }
        }

        if (validStates.Count > 0)
        {
            currentMovementState = validStates[UnityEngine.Random.Range(0, validStates.Count)];
        }
        else
        {
            currentMovementState = LandMovementState.Idle; // Fallback
        }
    }

    protected virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
    }

    #endregion
}
