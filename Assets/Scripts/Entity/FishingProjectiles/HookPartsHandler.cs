using UnityEngine;

public class HookPartsHandler: MonoBehaviour
{
    [SerializeField] private WaterCheck waterCheck;
    [SerializeField] private FishingProjectile fishingProjectile;
    
    public WaterCheck WaterCheck => waterCheck;
    public FishingProjectile FishingProjectile => fishingProjectile;
}
