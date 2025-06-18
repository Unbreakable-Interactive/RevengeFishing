using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishermanScript : EnemyBase
{
    [Header("Hook Throwing")]
    private HookSpawner hookSpawner;
    public float hookThrowChance = 0.4f; // 40% chance per frame when idle

    private int timer;

    // Start is called before the first frame update
    protected override void Start()
    {
        timer = 0;
        _type = EnemyType.Land;

        //enable fishing tool for fisherman
        hasFishingTool = true;

        // Get or add hook spawner component
        hookSpawner = GetComponent<HookSpawner>();
        if (hookSpawner == null)
        {
            hookSpawner = gameObject.AddComponent<HookSpawner>();
        }

        base.Start(); // Call the base class Start method

    }

    // Update is called once per frame
    protected override void Update()
    {
        timer++;

        // Call base Update for movement handling
        base.Update();

        // Check for hook throwing when idle and equipped
        CheckHookThrowing();

        if (timer >= 600)
        {
            ReverseFishingBehaviour();
            timer = 0; // Reset timer after 10 seconds (600 frames at 60 FPS)
        }
    }

    private void CheckHookThrowing()
    {
        // Only throw hook if:
        // 1. Fisherman is in idle state
        // 2. Has fishing tool equipped
        // 3. Hook spawner can throw (cooldown passed)
        // 4. Random chance passes
        if (currentMovementState == LandMovementState.Idle &&
            fishingToolEquipped &&
            hookSpawner != null &&
            hookSpawner.CanThrowHook() &&
            Random.value < hookThrowChance * Time.deltaTime)
        {
            hookSpawner.ThrowHook();
        }
    }

    public override void ReverseFishingBehaviour()
    {
        if (!fishingToolEquipped) return;

        // Check if there's an active hook - cannot unequip while hook is out!
        if (hookSpawner != null && hookSpawner.HasActiveHook())
        {
            Debug.Log($"{gameObject.name} cannot put away fishing rod - hook is still out!");
            return; // Cannot unequip while hook is active
        }

        // WEIGHTED SELECTION
        float randomValue = UnityEngine.Random.value; // 0.0 to 1.0

        if (randomValue < 0.1f) // 10% chance to put away
        {
            TryUnequipFishingTool();
        }
    }

    //public override void LandMovement()
    //{
    //    //throw new System.NotImplementedException();
    //}

    public override void WaterMovement()
    {
        //throw new System.NotImplementedException();
    }

}
