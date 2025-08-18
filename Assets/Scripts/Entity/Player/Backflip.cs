using UnityEngine;

public class Backflip : AbilityBase
{
    [Header("Backflip Settings")]
    [SerializeField] private float rotationSpeed = 360f; // Reduced speed for better control
    [SerializeField] private float baseDamagePercent = 5f; // 5% of power level per charge
    
    [Header("Visual Effects")]
    [SerializeField] private bool showChargeEffect = true;
    [SerializeField] private Color chargeColor = Color.red;
    
    // Backflip state tracking
    private bool isCharging = false;
    private int chargeLevel = 0;
    private bool hasContactedBoat = false;
    
    // Rotation tracking
    private bool isPerformingInitialFlip = false;
    private bool isPerformingFinalFlip = false;
    private float rotationProgress = 0f;
    private float startRotation = 0f;
    private float totalRotationNeeded = 0f;
    private float targetRotationAngle = 0f; // Track the target angle for current rotation
    
    // Player auto-rotation interference prevention
    private bool originalAutoRotateInAir = false;
    private bool originalSpriteFlipping = false;
    
    protected override void OnInitialize()
    {
        abilityName = "Backflip";
        description = "Airborne spinning attack that charges with multiple button presses";
        requiresAboveWater = true; // Can only be used when airborne
        
        DebugLog("Backflip ability initialized");
    }
    
    protected override bool CanActivateCustom()
    {
        // Can always activate when airborne (to build charges)
        return true;
    }
    
    protected override void OnActivate()
    {
        if (!isCharging)
        {
            // First activation - start the backflip with initial rotation
            StartBackflip();
        }
        else
        {
            // Additional activations - only increase charge (no rotation)
            IncreaseCharge();
        }
    }
    
    private void StartBackflip()
    {
        isCharging = true;
        chargeLevel = 1;
        hasContactedBoat = false;
        
        // Disable player's auto-rotation and sprite flipping to prevent interference
        originalAutoRotateInAir = player.GetAutoRotateInAir();
        originalSpriteFlipping = player.GetSpriteFlipping();
        player.SetAutoRotateInAir(false);
        player.SetSpriteFlipping(false);
        
        // Capture the exact starting rotation
        startRotation = player.transform.eulerAngles.z;
        
        // Set target rotation based on facing direction
        // When facing LEFT: go to 0° (upright/normal)
        // When facing RIGHT: go to 180° (upside down)
        if (player.IsFacingLeft())
        {
            targetRotationAngle = 0f;  // Face left = rotate to 0°
        }
        else
        {
            targetRotationAngle = 180f;  // Face right = rotate to 180°
        }
        
        float rotationDifference = Mathf.DeltaAngle(startRotation, targetRotationAngle);
        
        rotationProgress = 0f; // Reset progress counter
        totalRotationNeeded = Mathf.Abs(rotationDifference);
        isPerformingInitialFlip = true;
        
        DebugLog($"Started backflip - Facing: {(player.IsFacingLeft() ? "LEFT" : "RIGHT")}, Start: {startRotation:F1}°, Target: {targetRotationAngle}°, Rotation needed: {rotationDifference:F1}°");
    }
    
    private void IncreaseCharge()
    {
        if (!isCharging || isPerformingInitialFlip) return;
        
        chargeLevel++;
        
        DebugLog($"Increased charge level to: {chargeLevel} (Total damage: {GetTotalDamage()})");
        
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
    
    private int GetTotalDamage()
    {
        int damage = Mathf.RoundToInt(player.PowerLevel * (baseDamagePercent / 100f) * chargeLevel);
        DebugLog($"Damage calculation: PowerLevel({player.PowerLevel}) * DamagePercent({baseDamagePercent}%) * Charge({chargeLevel}) = {damage}");
        return damage;
    }
    
    void Update()
    {
        if (!isCharging && !isPerformingFinalFlip) return;
        
        // Handle initial rotation (first half-flip)
        if (isPerformingInitialFlip)
        {
            UpdateInitialRotation();
        }
        
        // Handle final rotation (return to upright)
        if (isPerformingFinalFlip)
        {
            UpdateFinalRotation();
        }
        
        // Check for boat collisions while charging (alternative to OnTriggerEnter2D)
        if (isCharging && !isPerformingInitialFlip)
        {
            CheckForBoatCollisions();
        }
        
        // Check if player has returned to water (only if we're charging and not mid-rotation)
        if (isCharging && !isPerformingInitialFlip && !player.IsAboveWater)
        {
            EndBackflip();
        }
    }
    
    private void UpdateInitialRotation()
    {
        rotationProgress += rotationSpeed * Time.deltaTime;
        
        // Debug the rotation progress
        DebugLog($"Initial rotation progress: {rotationProgress:F1}° / {totalRotationNeeded:F1}°");
        
        // Check if we've completed the rotation to target angle
        if (rotationProgress >= totalRotationNeeded)
        {
            // Snap to exact target position
            player.transform.rotation = Quaternion.Euler(0, 0, targetRotationAngle);
            isPerformingInitialFlip = false;
            
            DebugLog($"Initial flip completed! Start: {startRotation:F1}°, Final: {targetRotationAngle:F1}°");
            return;
        }
        
        // Use interpolation to ensure smooth rotation toward target
        float progress = rotationProgress / totalRotationNeeded;
        float currentAngle = Mathf.LerpAngle(startRotation, targetRotationAngle, progress);
        player.transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }
    
    private void UpdateFinalRotation()
    {
        rotationProgress += rotationSpeed * Time.deltaTime;
        
        // Debug the final rotation progress
        DebugLog($"Final rotation progress: {rotationProgress:F1}° / {totalRotationNeeded:F1}°");
        
        // Check if we've completed the rotation to target angle
        if (rotationProgress >= totalRotationNeeded)
        {
            // Snap to exact target position
            player.transform.rotation = Quaternion.Euler(0, 0, targetRotationAngle);
            
            // Complete state reset
            CompleteBackflip();
            return;
        }
        
        // Use interpolation to ensure smooth rotation to target
        float progress = rotationProgress / totalRotationNeeded;
        float currentAngle = Mathf.LerpAngle(startRotation, targetRotationAngle, progress);
        player.transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }
    
    private void CompleteBackflip()
    {
        // Reset all rotation state
        isPerformingFinalFlip = false;
        isPerformingInitialFlip = false;
        rotationProgress = 0f;
        startRotation = 0f;
        totalRotationNeeded = 0f;
        targetRotationAngle = 0f;
        
        // Restore player's original settings
        player.SetAutoRotateInAir(originalAutoRotateInAir);
        player.SetSpriteFlipping(originalSpriteFlipping);
        
        DebugLog($"Backflip completed! Player at final rotation. All state reset.");
    }
    
    private void EndBackflip()
    {
        if (!isCharging) return;
        
        DebugLog($"Ending backflip - Final charge level: {chargeLevel}, contacted boat: {hasContactedBoat}");
        
        // Perform final 180-degree rotation to return upright
        StartFinalRotation();
        
        // Reset charging state
        isCharging = false;
        chargeLevel = 0;
        hasContactedBoat = false;
        
        // Safety: If we're not doing final flip, restore settings immediately
        if (!isPerformingFinalFlip)
        {
            player.SetAutoRotateInAir(originalAutoRotateInAir);
            player.SetSpriteFlipping(originalSpriteFlipping);
            DebugLog("Settings restored (safety check)");
        }
    }
    
    // Safety method to restore settings if something goes wrong
    private void OnDisable()
    {
        if (player != null && (isCharging || isPerformingFinalFlip || isPerformingInitialFlip))
        {
            player.SetAutoRotateInAir(originalAutoRotateInAir);
            player.SetSpriteFlipping(originalSpriteFlipping);
            DebugLog("Settings restored (OnDisable safety)");
        }
    }
    
    private void StartFinalRotation()
    {
        // Capture the exact starting rotation for final flip
        startRotation = player.transform.eulerAngles.z;
        
        // Set the final target based on which direction player was facing
        // When facing LEFT: initial was 0°, so final should be 180°
        // When facing RIGHT: initial was 180°, so final should be 0°
        if (player.IsFacingLeft())
        {
            targetRotationAngle = 180f;  // Face left = final rotation to 180°
        }
        else
        {
            targetRotationAngle = 0f;    // Face right = final rotation to 0°
        }
        
        float rotationDifference = Mathf.DeltaAngle(startRotation, targetRotationAngle);
        
        rotationProgress = 0f; // Reset progress counter
        totalRotationNeeded = Mathf.Abs(rotationDifference);
        isPerformingFinalFlip = true;
        
        DebugLog($"Starting final rotation - Facing: {(player.IsFacingLeft() ? "LEFT" : "RIGHT")}, from {startRotation:F1}° to {targetRotationAngle}°, Rotation needed: {rotationDifference:F1}°");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCharging || hasContactedBoat || isPerformingInitialFlip) return;
        
        // Check if we hit a boat
        BoatController boat = other.GetComponentInParent<BoatController>();
        if (boat != null)
        {
            DealDamageToBoat(boat);
        }
    }
    
    // Alternative collision detection using physics overlap
    private void CheckForBoatCollisions()
    {
        if (!isCharging || hasContactedBoat || isPerformingInitialFlip) return;
        
        // Use physics to detect nearby boats
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            // Fallback: check for boats in a radius around player
            CheckBoatsInRadius();
            return;
        }
        
        // Check for overlapping colliders
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = false;
        
        Collider2D[] results = new Collider2D[10];
        int hitCount = playerCollider.OverlapCollider(filter, results);
        
        for (int i = 0; i < hitCount; i++)
        {
            if (results[i] != null)
            {
                BoatController boat = results[i].GetComponentInParent<BoatController>();
                if (boat != null)
                {
                    DealDamageToBoat(boat);
                    break; // Only hit one boat at a time
                }
            }
        }
    }
    
    private void CheckBoatsInRadius()
    {
        // Find all boats in the scene and check distance
        BoatController[] boats = FindObjectsOfType<BoatController>();
        float attackRadius = 2f; // Attack range in units
        
        foreach (BoatController boat in boats)
        {
            if (boat != null && boat.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(player.transform.position, boat.transform.position);
                
                if (distance <= attackRadius)
                {
                    DebugLog($"Found boat within attack radius: {distance:F1} units");
                    DealDamageToBoat(boat);
                    break; // Only hit one boat at a time
                }
            }
        }
    }
    
    private void DealDamageToBoat(BoatController boat)
    {
        hasContactedBoat = true;
        int damage = GetTotalDamage();
        
        DebugLog($"Backflip hit boat! Dealing {damage} damage (Charge level: {chargeLevel})");
        
        // Deal damage to the boat using the proper damage system
        boat.TakeDamageFromPlayer(damage, "Backflip Attack");
        
        // You could add visual/audio effects here
        // PlayHitEffect();
        // PlayHitSound();
    }
    
    // Public method that can be called from Player.cs SpecialAbilityOne()
    public void TriggerBackflip()
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
    public int ChargeLevel => chargeLevel;
    public int PotentialDamage => GetTotalDamage();
    public bool IsFlipping => isPerformingInitialFlip || isPerformingFinalFlip;
}
