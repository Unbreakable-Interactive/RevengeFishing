using UnityEngine;

public class BigBite : AbilityBase
{
    [Header("Big Bite Settings")]
    [SerializeField] private float chargePercentagePerClick = 10f; // 10% of player power per charge level
    
    [Header("Apex Detection")]
    [SerializeField] private float velocityThreshold = 0.1f; // How close to 0 velocity to consider apex
    [SerializeField] private bool enableApexDetection = true;
    
    [Header("Visual Effects")]
    [SerializeField] private bool showChargeEffect = true;
    [SerializeField] private Color chargeColor = Color.yellow;
    
    // Big Bite state tracking
    private bool isCharging = false;
    private bool isWaitingForApex = false; // Ability activated but waiting for apex
    private bool hasReachedApex = false; // Player has reached apex and is falling
    private int chargeLevel = 0;
    private bool hasEatenEnemy = false;
    
    // Apex detection tracking
    private float previousVerticalVelocity = 0f;
    private bool wasGoingUp = false;
    
    // Mouth opening tracking
    private bool isMouthOpen = false;
    
    protected override void OnInitialize()
    {
        abilityName = "Big Bite";
        description = "Airborne eating attack that can consume enemies with sufficient charge";
        requiresAboveWater = true; // Can only be used when airborne
        
        DebugLog("Big Bite ability initialized");
    }
    
    protected override bool CanActivateCustom()
    {
        // Can always activate when airborne (to build charges)
        return true;
    }
    
    protected override void OnActivate()
    {
        if (!isCharging && !isWaitingForApex)
        {
            // First activation - check if we need to wait for apex
            if (enableApexDetection && IsPlayerGoingUp())
            {
                StartWaitingForApex();
            }
            else
            {
                // Start immediately if player is already falling or apex detection is disabled
                StartBigBite();
            }
        }
        else if (isWaitingForApex || isCharging)
        {
            // Additional activations - increase charge level
            IncreaseCharge();
        }
    }
    
    private void StartBigBite()
    {
        isCharging = true;
        isWaitingForApex = false;
        hasReachedApex = true; // Mark as reached apex since we're starting the eating phase
        chargeLevel = Mathf.Max(1, chargeLevel); // Ensure at least 1 charge
        hasEatenEnemy = false;
        isMouthOpen = true;
        
        // Update animation: Now big bite is active, set power based on charge level
        UpdateBigBiteAnimation();
        
        DebugLog($"Started Big Bite - Charge level: {chargeLevel}, Eating power: {GetEatingPower()}");
    }
    
    private void StartWaitingForApex()
    {
        isWaitingForApex = true;
        isCharging = false;
        hasReachedApex = false;
        chargeLevel = 1; // Start with 1 charge
        hasEatenEnemy = false;
        isMouthOpen = false;
        
        // Update animation: Show charging animation immediately
        UpdateBigBiteAnimation();
        
        // Initialize velocity tracking
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            previousVerticalVelocity = rb.velocity.y;
            wasGoingUp = previousVerticalVelocity > 0;
        }
        
        DebugLog($"Waiting for apex - Current velocity: {previousVerticalVelocity:F2}, Going up: {wasGoingUp}");
    }
    
    private bool IsPlayerGoingUp()
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return false;
        
        return rb.velocity.y > velocityThreshold;
    }
    
    private bool CheckForApex()
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return false;
        
        float currentVelocity = rb.velocity.y;
        
        // Apex reached when:
        // 1. We were going up and now velocity is near zero or negative
        // 2. Or velocity crosses from positive to negative
        bool apexReached = false;
        
        if (wasGoingUp && currentVelocity <= velocityThreshold)
        {
            apexReached = true;
            DebugLog($"Apex detected: velocity changed from {previousVerticalVelocity:F2} to {currentVelocity:F2}");
        }
        
        // Update tracking
        previousVerticalVelocity = currentVelocity;
        wasGoingUp = currentVelocity > velocityThreshold;
        
        return apexReached;
    }
    
    private void IncreaseCharge()
    {
        // Allow charging during waiting phase or active charging phase
        if (!isWaitingForApex && !isCharging) return;
        
        chargeLevel++;
        
        // Update animation power whenever charge changes
        UpdateBigBiteAnimation();
        
        DebugLog($"Increased charge level to: {chargeLevel} (Eating power: {GetEatingPower()}) - State: {(isWaitingForApex ? "Waiting for apex" : "Active charging")}");
        
        // Visual/audio feedback for charging
        if (showChargeEffect)
        {
            ShowChargeEffect();
        }
    }
    
    private void ShowChargeEffect()
    {
        // Simple visual feedback - could be enhanced with particles
        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Flash the sprite briefly
            StartCoroutine(FlashSprite(spriteRenderer));
        }
    }
    
    private System.Collections.IEnumerator FlashSprite(SpriteRenderer spriteRenderer)
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.Lerp(originalColor, chargeColor, 0.7f);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    private void UpdateBigBiteAnimation()
    {
        if (player == null) return;
        
        // Show big bite animation whenever the ability is active and has charges
        bool shouldShowBigBite = (isCharging || isWaitingForApex) && chargeLevel > 0;
        
        // Set animation parameters
        player.SetBigBiteAnimation(shouldShowBigBite, chargeLevel);
        
        DebugLog($"Animation Updated - isBiting: {shouldShowBigBite}, power: {chargeLevel}");
    }
    
    private int GetEatingPower()
    {
        if (player == null) return 0;
        
        // Calculate eating power: (charge level * percentage per click * player's power level) / 100
        float playerPowerLevel = player.PowerLevel;
        float totalPercentage = chargeLevel * chargePercentagePerClick;
        float eatingPower = (totalPercentage * playerPowerLevel) / 100f;
        
        int finalEatingPower = Mathf.RoundToInt(eatingPower);
        
        DebugLog($"Eating power calculation: {chargeLevel} charges × {chargePercentagePerClick}% × player power({playerPowerLevel}) / 100 = {finalEatingPower}");
        
        return finalEatingPower;
    }
    
    void Update()
    {
        // Handle waiting for apex phase
        if (isWaitingForApex)
        {
            if (CheckForApex())
            {
                DebugLog("Apex reached! Starting Big Bite eating phase.");
                StartBigBite();
            }
            
            // Safety: Cancel if player returns to water while waiting
            if (!player.IsAboveWater)
            {
                DebugLog("Player returned to water while waiting for apex. Cancelling.");
                CancelBigBite();
            }
            
            return;
        }
        
        if (!isCharging) return;
        
        // Check if player has returned to water
        if (!player.IsAboveWater)
        {
            EndBigBite();
        }
    }
    
    private void EndBigBite()
    {
        if (!isCharging) return;
        
        DebugLog($"Ending Big Bite - Final charge level: {chargeLevel}, eaten enemy: {hasEatenEnemy}");
        
        // Turn off big bite animation when ending
        player.SetBigBiteAnimation(false, 0);
        
        // Reset charging state
        isCharging = false;
        chargeLevel = 0;
        hasEatenEnemy = false;
        hasReachedApex = false;
        isMouthOpen = false;
    }
    
    private void CancelBigBite()
    {
        DebugLog("Cancelling Big Bite");
        
        // Turn off big bite animation
        if (player != null)
        {
            player.SetBigBiteAnimation(false, 0);
        }
        
        // Reset all states
        isWaitingForApex = false;
        isCharging = false;
        chargeLevel = 0;
        hasEatenEnemy = false;
        hasReachedApex = false;
        isMouthOpen = false;
        
        DebugLog("Big Bite cancelled");
    }
    
    // Safety method to restore settings if something goes wrong
    private void OnDisable()
    {
        if (player != null)
        {
            // Turn off big bite animation on disable
            player.SetBigBiteAnimation(false, 0);
        }
        
        // Reset all states on disable
        if (isWaitingForApex)
        {
            CancelBigBite();
        }
    }
    
    // Public method for collision forwarder to call
    public void HandleEnemyCollision(Enemy enemy)
    {
        // Only eat if we're in the charging phase (after apex) and mouth is open
        if (!isCharging || hasEatenEnemy || !isMouthOpen) return;
        
        // Additional check: only eat if we've reached apex (on the way down)
        if (!hasReachedApex) return;
        
        // Check if we can eat this enemy
        if (CanEatEnemy(enemy))
        {
            EatEnemy(enemy);
        }
        else
        {
            DebugLog($"Cannot eat enemy {enemy.name} - insufficient eating power. Need: {enemy.PowerLevel}, Have: {GetEatingPower()}");
        }
    }
    
    private bool CanEatEnemy(Enemy enemy)
    {
        if (enemy == null) return false;
        
        // Check if enemy is alive (not defeated, eaten, or dead)
        if (enemy.State != Enemy.EnemyState.Alive)
        {
            DebugLog($"Cannot eat enemy in state: {enemy.State}");
            return false;
        }
        
        // Check if our eating power is greater than enemy's power level
        int eatingPower = GetEatingPower();
        bool canEat = eatingPower > enemy.PowerLevel;
        
        DebugLog($"Eat check - Enemy: {enemy.name}, Enemy Power: {enemy.PowerLevel}, Our Eating Power: {eatingPower}, Can Eat: {canEat}");
        
        return canEat;
    }
    
    private void EatEnemy(Enemy enemy)
    {
        hasEatenEnemy = true;
        int eatingPower = GetEatingPower();
        
        DebugLog($"Big Bite ate enemy! {enemy.name} with power {enemy.PowerLevel} (Our eating power: {eatingPower})");
        
        // Mark enemy as eaten using the existing enemy state system
        enemy.ChangeState_Eaten();
        
        // Trigger the eaten behavior (this should handle cleanup, pool return, etc.)
        if (enemy.GetComponent<Enemy>() != null)
        {
            // The enemy's existing eaten logic should handle cleanup
            enemy.TriggerEaten();
        }
        
        // Give player some benefit for eating (could be health, power, points, etc.)
        // Using PowerLevel instead of health since health system might not exist yet
        int powerGain = Mathf.RoundToInt(enemy.PowerLevel * 0.1f); // Gain 10% of enemy's power
        
        // For now, just log the power gain - can be implemented later when player stats system is ready
        DebugLog($"Player gained {powerGain} power from eating {enemy.name}");
        
        // You could add visual/audio effects here
        // PlayEatingEffect();
        // PlayEatingSound();
        
        // End the ability after successful eating
        EndBigBite();
    }
    
    // Public method that can be called from Player.cs SpecialAbilityTwo()
    public void TriggerBigBite()
    {
        if (abilitySystem != null)
        {
            abilitySystem.TryActivateAbility(this);
        }
        else
        {
            // Fallback: activate directly if no ability system
            Activate();
        }
    }
    
    // Public properties for debugging/UI
    public bool IsCharging => isCharging;
    public bool IsWaitingForApex => isWaitingForApex;
    public bool HasReachedApex => hasReachedApex;
    public int ChargeLevel => chargeLevel;
    public int EatingPower => GetEatingPower();
    public bool IsMouthOpen => isMouthOpen;
    public bool IsActive => isCharging || isWaitingForApex;
}