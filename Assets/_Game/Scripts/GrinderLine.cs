using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The row of machine slots beside the belt. Buys new level-1 grinders into free slots and merges two machines of the
/// same level into one machine a level higher (up to <see cref="MaxMachineLevel"/>), charging the wallet for both.
/// </summary>
public class GrinderLine : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Machine spawned for every purchase.")]
    private Grinder grinderPrefab;

    [SerializeField, Tooltip("Slot anchors along the belt, in fill order.")]
    private Transform[] slots;

    [SerializeField, Tooltip("Machine already standing in the first slot when the scene starts. Spawned if empty.")]
    private Grinder initialGrinder;

    [SerializeField, Tooltip("Belt every machine feeds.")]
    private ConveyorBelt belt;

    [SerializeField, Tooltip("HUD layer that draws every machine's level label.")]
    private WorldLabelLayer labelLayer;

    [SerializeField, Tooltip("Wallet that pays for machines and merges.")]
    private Wallet wallet;

    [SerializeField, Tooltip("Upgrades whose Grinding Speed and Grinder Money multipliers apply to every machine. " +
        "Optional.")]
    private UpgradeManager upgrades;

    [SerializeField, Min(0f), Tooltip("Price of the first machine bought (the starting one is free). Prices round to whole dollars.")]
    private float firstMachinePrice = 30f;

    [SerializeField, Min(1f), Tooltip("Machine price multiplier per machine bought so far.")]
    private float machinePriceGrowth = 1.6f;

    [SerializeField, Min(0f), Tooltip("Price of merging two level-1 machines.")]
    private float firstMergePrice = 60f;

    [SerializeField, Min(1f), Tooltip("Merge price multiplier per level of the pair being merged.")]
    private float mergePriceGrowth = 2.2f;

    [SerializeField, Min(0.05f), Tooltip("Seconds the absorbed machine takes to hop into its partner.")]
    private float mergeDuration = 0.45f;

    [SerializeField, Min(0f), Tooltip("Peak height of the absorbed machine's hop, in world units.")]
    private float mergeHopHeight = 0.5f;

    [SerializeField, Min(1f), Tooltip("Scale the surviving machine bulges to when the other lands in it.")]
    private float mergeImpactScale = 1.3f;

    [SerializeField, Min(1), Tooltip("Highest level a machine can reach; two machines at this level can't be merged.")]
    private int maxMachineLevel = 2;
    #endregion

    #region Private Fields
    private Grinder[] machines;
    private int machinesBought;
    #endregion

    #region Public Properties
    public int SlotCount => slots.Length;

    public int MaxMachineLevel => maxMachineLevel;

    /// <summary>Machines bought so far (the starting one doesn't count); drives <see cref="MachinePrice"/>.</summary>
    public int MachinesBought => machinesBought;

    public int MachineCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < machines.Length; i++)
            {
                if (machines[i] != null)
                    count++;
            }

            return count;
        }
    }

    public bool HasFreeSlot => FindFreeSlot() >= 0;

    public double MachinePrice => Math.Round(firstMachinePrice * Math.Pow(machinePriceGrowth, machinesBought));

    public bool CanBuyMachine => HasFreeSlot && wallet.Balance >= MachinePrice;

    /// <summary>Level of the lowest pair of identical machines, or 0 when nothing can be merged.</summary>
    public int MergeLevel => FindMergePair(out _, out _);

    public bool HasMergePair => MergeLevel > 0;

    public double MergePrice
    {
        get
        {
            int level = MergeLevel;
            return level > 0 ? Math.Round(firstMergePrice * Math.Pow(mergePriceGrowth, level - 1)) : 0d;
        }
    }

    public bool CanMerge => HasMergePair && wallet.Balance >= MergePrice;
    #endregion

    #region Events
    /// <summary>Raised whenever a machine is bought, merged or the prices change.</summary>
    public event Action<GrinderLine> OnChanged;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        machines = new Grinder[slots.Length];
    }

    private void OnEnable()
    {
        if (upgrades != null)
            upgrades.OnChanged += HandleUpgradesChanged;
    }

    private void Start()
    {
        if (initialGrinder != null)
        {
            initialGrinder.transform.SetPositionAndRotation(slots[0].position, slots[0].rotation);
            initialGrinder.Setup(belt, labelLayer);
            initialGrinder.SetLevel(1);
            ApplyUpgrades(initialGrinder);
            machines[0] = initialGrinder;
        }
        else
        {
            machines[0] = Spawn(0);
        }

        OnChanged?.Invoke(this);
    }

    private void OnDisable()
    {
        if (upgrades != null)
            upgrades.OnChanged -= HandleUpgradesChanged;
    }
    #endregion

    #region Public Methods
    /// <summary>Buys a level-1 machine into the first free slot. Returns false when full or unaffordable.</summary>
    public bool TryBuyMachine()
    {
        int slot = FindFreeSlot();
        if (slot < 0 || !wallet.TrySpend(MachinePrice))
            return false;

        Grinder machine = Spawn(slot);
        machine.Pop();
        machine.PlaySpawnEffect();
        machines[slot] = machine;
        machinesBought++;
        OnChanged?.Invoke(this);
        return true;
    }

    /// <summary>Merges the lowest pair of identical machines into one a level higher, in the earlier slot.</summary>
    public bool TryMerge()
    {
        int level = FindMergePair(out int keepSlot, out int absorbSlot);
        if (level <= 0 || !wallet.TrySpend(MergePrice))
            return false;

        Grinder kept = machines[keepSlot];
        Grinder absorbed = machines[absorbSlot];
        machines[absorbSlot] = null;

        // The level counts at once (so a second merge can't reuse this machine); the visuals wait for the impact.
        kept.SetLevel(level + 1, false);
        absorbed.IsProducing = false;
        StartCoroutine(AbsorbRoutine(absorbed, kept));
        OnChanged?.Invoke(this);
        return true;
    }

    /// <summary>Level of the machine in each slot, 0 for an empty slot, for saving.</summary>
    public int[] GetMachineLevels()
    {
        int[] levels = new int[machines.Length];
        for (int i = 0; i < machines.Length; i++)
            levels[i] = machines[i] != null ? machines[i].Level : 0;

        return levels;
    }

    /// <summary>
    /// Rebuilds the line from a save: spawns, re-levels or dismantles machines until every slot holds the level in
    /// <paramref name="levels"/> (0 or missing = empty), without the purchase animations. Levels are clamped to
    /// <see cref="MaxMachineLevel"/>.
    /// </summary>
    public void RestoreMachines(IReadOnlyList<int> levels, int bought)
    {
        int count = levels != null ? levels.Count : 0;

        for (int i = 0; i < machines.Length; i++)
        {
            int level = i < count ? Mathf.Min(levels[i], maxMachineLevel) : 0;

            if (level <= 0)
            {
                if (machines[i] != null)
                {
                    machines[i].Dismantle();
                    machines[i] = null;
                }

                continue;
            }

            if (machines[i] == null)
                machines[i] = Spawn(i);

            machines[i].SetLevel(level);
        }

        machinesBought = Mathf.Max(0, bought);
        OnChanged?.Invoke(this);
    }
    #endregion

    #region Private Methods
    private Grinder Spawn(int slot)
    {
        Grinder machine = Instantiate(grinderPrefab, slots[slot].position, slots[slot].rotation, transform);
        machine.name = grinderPrefab.name + " " + (slot + 1);
        machine.Setup(belt, labelLayer);
        machine.SetLevel(1);
        ApplyUpgrades(machine);
        return machine;
    }

    private void ApplyUpgrades(Grinder machine)
    {
        if (upgrades == null)
            return;

        machine.SpeedMultiplier = upgrades.GetMultiplier(UpgradeStat.GrindingSpeed);
        machine.ValueMultiplier = upgrades.GetMultiplier(UpgradeStat.GrinderMoney);
    }

    private void HandleUpgradesChanged(UpgradeManager manager)
    {
        for (int i = 0; i < machines.Length; i++)
        {
            if (machines[i] != null)
                ApplyUpgrades(machines[i]);
        }
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < machines.Length; i++)
        {
            if (machines[i] == null)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the two lowest-level identical machines below the level cap. Returns their level, or 0 if there is no
    /// mergeable pair.
    /// </summary>
    private int FindMergePair(out int firstSlot, out int secondSlot)
    {
        firstSlot = -1;
        secondSlot = -1;
        int bestLevel = int.MaxValue;

        for (int i = 0; i < machines.Length; i++)
        {
            if (machines[i] == null || machines[i].Level >= bestLevel || machines[i].Level >= maxMachineLevel)
                continue;

            for (int j = i + 1; j < machines.Length; j++)
            {
                if (machines[j] == null || machines[j].Level != machines[i].Level)
                    continue;

                bestLevel = machines[i].Level;
                firstSlot = i;
                secondSlot = j;
                break;
            }
        }

        return firstSlot >= 0 ? bestLevel : 0;
    }

    /// <summary>
    /// The absorbed machine lifts off at once, arcs over and accelerates into its partner while shrinking; the partner
    /// then reveals its new level with a bulge.
    /// </summary>
    private IEnumerator AbsorbRoutine(Grinder absorbed, Grinder into)
    {
        Transform mover = absorbed.transform;
        Vector3 startPosition = mover.position;
        Vector3 startScale = absorbed.RestingScale;
        float elapsed = 0f;

        // Fix: a machine bought a moment ago is still popping in; take the scale over from the rest size.
        absorbed.CancelPop();

        while (elapsed < mergeDuration && into != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / mergeDuration);
            Vector3 position = Vector3.Lerp(startPosition, into.transform.position, t * t);
            position.y += mergeHopHeight * Mathf.Sin(t * Mathf.PI);
            mover.position = position;
            mover.localScale = startScale * Mathf.Lerp(1f, 0.15f, t * t * t);
            yield return null;
        }

        absorbed.Dismantle();

        if (into != null)
        {
            into.ShowLevel();
            into.Pop(mergeImpactScale);
            into.PlayMergeEffect();
        }
    }
    #endregion
}
