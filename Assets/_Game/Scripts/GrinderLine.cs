using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The row of machine slots beside the belt. Buys new level-1 grinders into free slots and merges two machines of the
/// same level into one machine a level higher, charging the wallet for both.
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

    [SerializeField, Tooltip("Parent for the pieces the machines produce.")]
    private Transform pieceContainer;

    [SerializeField, Tooltip("Wallet that pays for machines and merges.")]
    private Wallet wallet;

    [SerializeField, Min(0f), Tooltip("Price of the first machine bought (the starting one is free). Prices round to whole dollars.")]
    private float firstMachinePrice = 30f;

    [SerializeField, Min(1f), Tooltip("Machine price multiplier per machine bought so far.")]
    private float machinePriceGrowth = 1.6f;

    [SerializeField, Min(0f), Tooltip("Price of merging two level-1 machines.")]
    private float firstMergePrice = 60f;

    [SerializeField, Min(1f), Tooltip("Merge price multiplier per level of the pair being merged.")]
    private float mergePriceGrowth = 2.2f;

    [SerializeField, Min(0.05f), Tooltip("Seconds the absorbed machine takes to slide into its partner.")]
    private float mergeDuration = 0.3f;
    #endregion

    #region Private Fields
    private Grinder[] machines;
    private int machinesBought;
    #endregion

    #region Public Properties
    public int SlotCount => slots.Length;

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

    private void Start()
    {
        if (initialGrinder != null)
        {
            initialGrinder.transform.SetPositionAndRotation(slots[0].position, slots[0].rotation);
            initialGrinder.Setup(belt, pieceContainer);
            initialGrinder.SetLevel(1);
            machines[0] = initialGrinder;
        }
        else
        {
            machines[0] = Spawn(0);
        }

        OnChanged?.Invoke(this);
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

        kept.SetLevel(level + 1);
        absorbed.IsProducing = false;
        StartCoroutine(AbsorbRoutine(absorbed, kept));
        OnChanged?.Invoke(this);
        return true;
    }
    #endregion

    #region Private Methods
    private Grinder Spawn(int slot)
    {
        Grinder machine = Instantiate(grinderPrefab, slots[slot].position, slots[slot].rotation, transform);
        machine.name = grinderPrefab.name + " " + (slot + 1);
        machine.Setup(belt, pieceContainer);
        machine.SetLevel(1);
        return machine;
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

    /// <summary>Finds the two lowest-level identical machines. Returns their level, or 0 if there is no pair.</summary>
    private int FindMergePair(out int firstSlot, out int secondSlot)
    {
        firstSlot = -1;
        secondSlot = -1;
        int bestLevel = int.MaxValue;

        for (int i = 0; i < machines.Length; i++)
        {
            if (machines[i] == null || machines[i].Level >= bestLevel)
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

    private IEnumerator AbsorbRoutine(Grinder absorbed, Grinder into)
    {
        Transform mover = absorbed.transform;
        Vector3 startPosition = mover.position;
        Vector3 startScale = mover.localScale;
        float elapsed = 0f;

        while (elapsed < mergeDuration && into != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / mergeDuration));
            mover.position = Vector3.Lerp(startPosition, into.transform.position, t);
            mover.localScale = Vector3.Lerp(startScale, startScale * 0.2f, t);
            yield return null;
        }

        absorbed.Dismantle();

        if (into != null)
            into.Pop(0.6f);
    }
    #endregion
}
