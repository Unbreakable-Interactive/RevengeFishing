using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeManagerShop : MonoBehaviour
{
    [Header("Top Points")]
    [SerializeField] private PointsBar pointsBar;
    [SerializeField] private int pointsPerPhase = 5;
    [SerializeField] private int maxPoints = 20;

    [Header("Slots")]
    [SerializeField] private UpgradeSlotView slotSpecial1;
    [SerializeField] private UpgradeSlotView slotSpecial2;
    [SerializeField] private UpgradeSlotView[] normalSlots;

    [Header("Data")]
    [SerializeField] private UpgradeSO special1;
    [SerializeField] private UpgradeSO special2;
    [SerializeField] private List<UpgradeSO> normalPool;
    [SerializeField] private bool uniqueNormals = true;
    [SerializeField] private bool filterByPhase = true;

    [Header("Flow")]
    [SerializeField] private bool applyOnConfirm = false;

    public event System.Action<List<UpgradeSO>> OnConfirmed;

    private int _bank;
    private int _spentPanel;
    private readonly List<UpgradeSO> _pending = new();
    private readonly HashSet<UpgradeSO> _acquired = new();
    private readonly HashSet<UpgradeSO> _pendingSpecials = new();

    public void ShowForPhase(Player.Phase phase)
    {
        _bank = Mathf.Min(_bank + pointsPerPhase, maxPoints);
        _pending.Clear();
        _pendingSpecials.Clear();
        _spentPanel = 0;
        pointsBar?.Init(_bank);
        pointsBar?.SetState(_bank, _spentPanel);
        SetupSpecials();
        SetupNormals(phase);
        EnableAll(true);
        EnforceBudgetLocks();
    }

    public void ResetRound()
    {
        _pending.Clear();
        _pendingSpecials.Clear();
        _spentPanel = 0;
        pointsBar?.SetState(_bank, _spentPanel);

        if (slotSpecial1)
        {
            bool canUse = slotSpecial1.Assigned != null && !_acquired.Contains(slotSpecial1.Assigned);
            slotSpecial1.SetInteractable(canUse);
            slotSpecial1.PaintCost(0);
        }
        if (slotSpecial2)
        {
            bool canUse = slotSpecial2.Assigned != null && !_acquired.Contains(slotSpecial2.Assigned);
            slotSpecial2.SetInteractable(canUse);
            slotSpecial2.PaintCost(0);
        }

        if (normalSlots != null)
        {
            foreach (var s in normalSlots)
            {
                if (!s) continue;
                s.SetInteractable(s.Assigned != null);
                s.PaintCost(0);
            }
        }

        var phase = Player.Instance ? Player.Instance.currentPhase : Player.Phase.Infant;
        SetupNormals(phase);
        EnforceBudgetLocks();
    }

    public void Confirm()
    {
        if (_pending.Count == 0) return;

        foreach (var up in _pending)
            if (up.unique) _acquired.Add(up);

        _bank = Mathf.Max(0, _bank - _spentPanel);
        pointsBar?.SetState(_bank, 0);

        if (applyOnConfirm && Player.Instance)
            foreach (var up in _pending) up.Apply(Player.Instance);

        OnConfirmed?.Invoke(new List<UpgradeSO>(_pending));

        _pending.Clear();
        _pendingSpecials.Clear();
        _spentPanel = 0;

        if (slotSpecial1 && slotSpecial1.Assigned != null && _acquired.Contains(slotSpecial1.Assigned))
            slotSpecial1.SetInteractable(false);
        if (slotSpecial2 && slotSpecial2.Assigned != null && _acquired.Contains(slotSpecial2.Assigned))
            slotSpecial2.SetInteractable(false);
    }

    private void SetupSpecials()
    {
        if (slotSpecial1)
        {
            slotSpecial1.Setup(special1, 3);
            bool canUse = special1 != null && !_acquired.Contains(special1);
            slotSpecial1.SetInteractable(canUse);
            slotSpecial1.AddListener(() => TryBuy(slotSpecial1));
        }
        if (slotSpecial2)
        {
            slotSpecial2.Setup(special2, 3);
            bool canUse = special2 != null && !_acquired.Contains(special2);
            slotSpecial2.SetInteractable(canUse);
            slotSpecial2.AddListener(() => TryBuy(slotSpecial2));
        }
    }

    private void SetupNormals(Player.Phase phase)
    {
        if (normalSlots == null || normalSlots.Length == 0) return;

        var candidates = normalPool
            .Where(u => u != null)
            .Where(u => !uniqueNormals || !_acquired.Contains(u))
            .Where(u => !filterByPhase || phase >= u.minPhase)
            .OrderBy(_ => Random.value)
            .ToList();

        for (int i = 0; i < normalSlots.Length; i++)
        {
            var view = normalSlots[i];
            if (!view) continue;

            var up = (i < candidates.Count) ? candidates[i] : null;
            view.Setup(up, 1);
            view.SetInteractable(up != null);
            view.AddListener(() => TryBuy(view));
        }
    }

    private void TryBuy(UpgradeSlotView slot)
    {
        if (slot == null) return;
        var up = slot.Assigned;
        if (up == null) return;

        int cost = slot.Cost;
        int remaining = _bank - _spentPanel;
        if (remaining < cost) return;

        _pending.Add(up);
        _spentPanel += cost;
        pointsBar?.SetState(_bank, _spentPanel);

        if (slot == slotSpecial1 || slot == slotSpecial2)
        {
            _pendingSpecials.Add(up);
            slot.PaintCost(cost);
            slot.SetInteractable(false);
        }
        else
        {
            slot.PaintCost(cost);
            slot.SetInteractable(true);
        }

        EnforceBudgetLocks();
    }

    private void EnforceBudgetLocks()
    {
        int remaining = _bank - _spentPanel;

        if (slotSpecial1 && slotSpecial1.Assigned != null && !_acquired.Contains(slotSpecial1.Assigned) && !_pendingSpecials.Contains(slotSpecial1.Assigned))
            slotSpecial1.SetInteractable(remaining >= 3);
        if (slotSpecial2 && slotSpecial2.Assigned != null && !_acquired.Contains(slotSpecial2.Assigned) && !_pendingSpecials.Contains(slotSpecial2.Assigned))
            slotSpecial2.SetInteractable(remaining >= 3);

        bool normalsInteract = remaining >= 1;
        if (normalSlots != null)
        {
            foreach (var s in normalSlots)
                if (s && s.Assigned != null) s.SetInteractable(normalsInteract);
        }

        if (remaining <= 0) EnableAll(false);
    }

    private void EnableAll(bool value)
    {
        if (slotSpecial1 && slotSpecial1.Assigned != null)
            slotSpecial1.SetInteractable(value && !_acquired.Contains(slotSpecial1.Assigned) && !_pendingSpecials.Contains(slotSpecial1.Assigned));
        if (slotSpecial2 && slotSpecial2.Assigned != null)
            slotSpecial2.SetInteractable(value && !_acquired.Contains(slotSpecial2.Assigned) && !_pendingSpecials.Contains(slotSpecial2.Assigned));

        if (normalSlots != null)
        {
            foreach (var s in normalSlots)
                if (s && s.Assigned != null) s.SetInteractable(value);
        }
    }
}