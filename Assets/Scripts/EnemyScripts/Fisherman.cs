using UnityEngine;

public class Fisherman : LandEnemy
{
    [Header("Fisherman Settings")]
    public GameObject fishingRodVisual;
    public Transform hookPoint;
    public LineRenderer fishingLine;
    public float fishingRange = 5f;

    [Header("Fishing Rod Management")]
    public bool hasRodEquipped = false;
    public bool isLineCast = false;
    public Transform rodAttachPoint;

    [Header("Animation")]
    public Animator animator;
    public bool facingRight = true;

    [Header("Fishing Mechanics")]
    public GameObject currentFish;
    public bool fishOnHook = false;
    public float strikeForce = 10f;
    public float jigStrength = 2f;

    // Components
    private EnemyStateMachine stateMachine;

    protected override void InitializeEnemy()
    {
        // Get state machine component
        stateMachine = GetComponent<EnemyStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<EnemyStateMachine>();
        }

        // Get animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Initialize fishing rod visual
        if (fishingRodVisual != null)
        {
            fishingRodVisual.SetActive(hasRodEquipped);
        }

        // Initialize fishing line
        if (fishingLine == null)
        {
            fishingLine = GetComponent<LineRenderer>();
            if (fishingLine == null)
            {
                fishingLine = gameObject.AddComponent<LineRenderer>();
                SetupFishingLine();
            }
        }

        // Find water check if not assigned
        if (waterCheck == null)
        {
            waterCheck = FindObjectOfType<WaterCheckPlayer>();
        }

        // Start with idle state
        stateMachine.ChangeState<FishermanIdleWithoutRodState>();

        if (showDebugInfo)
        {
            Debug.Log($"Fisherman {gameObject.name} initialized with power level {powerLevel}");
        }
    }

    protected override void UpdateLandEnemyBehavior()
    {
        // The state machine handles all behavior updates
        // Individual states call specific methods on this class

        UpdateFishingLine();
        CheckForFishBites();
    }

    void SetupFishingLine()
    {
        if (fishingLine != null)
        {
            fishingLine.enabled = false;
            fishingLine.startWidth = 0.02f;
            fishingLine.endWidth = 0.02f;
            fishingLine.positionCount = 2;
            fishingLine.useWorldSpace = true;
            fishingLine.material = new Material(Shader.Find("Sprites/Default"));
            //fishingLine.color = Color.brown;
        }
    }

    void UpdateFishingLine()
    {
        if (fishingLine != null && isLineCast && hookPoint != null)
        {
            fishingLine.enabled = true;

            // Set line from rod tip to hook point
            if (rodAttachPoint != null)
            {
                fishingLine.SetPosition(0, rodAttachPoint.position);
            }
            else
            {
                fishingLine.SetPosition(0, transform.position + Vector3.up * 0.5f);
            }

            fishingLine.SetPosition(1, hookPoint.position);
        }
        else
        {
            if (fishingLine != null)
            {
                fishingLine.enabled = false;
            }
        }
    }

    void CheckForFishBites()
    {
        if (isLineCast && hookPoint != null && !fishOnHook)
        {
            // Check for fish in fishing range
            Collider2D[] nearbyFish = Physics2D.OverlapCircleAll(hookPoint.position, 1f);

            foreach (Collider2D fishCollider in nearbyFish)
            {
                if (fishCollider.CompareTag("Player") && Random.value < 0.005f) // 0.5% chance per frame
                {
                    // Fish bit the hook!
                    currentFish = fishCollider.gameObject;
                    fishOnHook = true;

                    if (showDebugInfo)
                    {
                        Debug.Log($"{gameObject.name} got a fish on the hook!");
                    }
                    break;
                }
            }
        }
    }

    // Methods called by states
    public void SetAnimationState(string stateName)
    {
        if (animator != null)
        {
            // You'll set up animation triggers/parameters based on your animation setup
            animator.SetTrigger(stateName);

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} animation: {stateName}");
            }
        }
    }

    public void SetFacingDirection(bool faceRight)
    {
        if (facingRight != faceRight)
        {
            facingRight = faceRight;

            // Flip the sprite
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facingRight ? 1 : -1);
            transform.localScale = scale;
        }
    }

    public void SetRodEquipped(bool equipped)
    {
        hasRodEquipped = equipped;

        if (fishingRodVisual != null)
        {
            fishingRodVisual.SetActive(equipped);
        }

        // Can't have line cast if rod is not equipped
        if (!equipped)
        {
            SetLineCast(false);
        }

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} rod equipped: {equipped}");
        }
    }

    public void SetLineCast(bool cast)
    {
        if (!hasRodEquipped && cast)
        {
            Debug.LogWarning($"{gameObject.name} tried to cast line without rod equipped!");
            return;
        }

        isLineCast = cast;

        if (cast)
        {
            // Position hook point in water
            if (waterCheck != null)
            {
                Vector3 hookPosition = transform.position + (facingRight ? Vector3.right : Vector3.left) * fishingRange;
                hookPosition.y = waterCheck.transform.position.y - 1f; // Below water surface

                if (hookPoint == null)
                {
                    // Create hook point if it doesn't exist
                    GameObject hookObj = new GameObject("HookPoint");
                    hookPoint = hookObj.transform;
                    hookPoint.SetParent(transform);
                }

                hookPoint.position = hookPosition;
            }
        }
        else
        {
            // Reset fishing state
            fishOnHook = false;
            currentFish = null;
        }

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} line cast: {cast}");
        }
    }

    public void JigLine()
    {
        if (isLineCast && hookPoint != null)
        {
            // Move hook point up and down quickly
            Vector3 jigPosition = hookPoint.position;
            jigPosition.y += Random.Range(-jigStrength, jigStrength) * Time.deltaTime;
            hookPoint.position = jigPosition;

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} jigged the line");
            }
        }
    }

    public void StrikeLine()
    {
        if (fishOnHook && currentFish != null)
        {
            // Apply force to pull fish toward fisherman
            Rigidbody2D fishRb = currentFish.GetComponent<Rigidbody2D>();
            if (fishRb != null)
            {
                Vector2 pullDirection = (transform.position - currentFish.transform.position).normalized;
                fishRb.AddForce(pullDirection * strikeForce, ForceMode2D.Impulse);

                // Deal fatigue damage to fish (player)
                PlayerMovement player = currentFish.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    // You'll implement this when you add fatigue to player
                    // player.TakeFatigueDamage(10f);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} struck the line!");
            }
        }
    }

    public void CatchFish()
    {
        if (currentFish != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} caught a fish!");
            }

            // Reset fishing state
            fishOnHook = false;
            currentFish = null;
        }
    }

    public bool HasFishOnHook()
    {
        return fishOnHook && currentFish != null;
    }

    public bool IsFishDefeated()
    {
        if (currentFish != null)
        {
            // Check if player is defeated (you'll implement this with player fatigue system)
            // PlayerMovement player = currentFish.GetComponent<PlayerMovement>();
            // return player != null && player.IsDefeated();

            // For now, randomly determine if fish is defeated
            return Random.value < 0.1f; // 10% chance per check
        }
        return false;
    }

    // Override land enemy defeated behavior
    protected override void HandleDefeatedState()
    {
        // Put away rod if equipped before falling
        if (hasRodEquipped)
        {
            SetRodEquipped(false);
        }

        // Standard land enemy defeat behavior (hop then fall)
        base.HandleDefeatedState();
    }

    void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            // Draw fishing range
            Gizmos.color = Color.blue;
            Vector3 fishingDirection = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawWireSphere(transform.position + fishingDirection * fishingRange, 0.5f);

            // Draw hook point if line is cast
            if (isLineCast && hookPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hookPoint.position, 0.2f);

                // Draw line
                //Gizmos.color = Color.brown;
                Vector3 rodTip = rodAttachPoint != null ? rodAttachPoint.position : transform.position + Vector3.up * 0.5f;
                Gizmos.DrawLine(rodTip, hookPoint.position);
            }

            // Draw fish detection radius around hook
            if (isLineCast && hookPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hookPoint.position, 1f);
            }
        }
    }
}
