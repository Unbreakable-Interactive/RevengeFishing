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
    
    protected float _powerLevel;
    protected float _fatigue;
    protected float _maxFatigue;
    protected Tier _tier;
    protected EnemyState _state;
    protected EnemyType _type;

    #region Land Variables

    protected float walkingSpeed;
    protected float runningSpeed;
    // protected BoatScript parentBoat;
    protected float floatingForce;
    protected float maxUpwardVelocity; // For when they swim upward

    protected float weight;
    
    #endregion

    #region Water Variables

    protected float swimForce;
    protected float minSwimSpeed;
    protected float maxSwimSpeed;
    
    #endregion

    #region Base Behaviours

    // How enemy behaves when interacts with player
    public abstract void ReverseFishingBehaviour();

    public abstract void LandMovement();

    public abstract void WaterMovement();
    

    public virtual void Initialize(float powerLevel)
    {
        _powerLevel = powerLevel;
        
        _fatigue = 0;
        _maxFatigue = _powerLevel;

        _state = EnemyState.Alive;
        
        // weight must be a random value between x and y

        // CalculateTier();
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

}
