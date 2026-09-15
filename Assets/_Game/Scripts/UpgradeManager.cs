using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the level of every <see cref="UpgradeDefinition"/> on offer, sells the next level for wallet money and folds
/// the levels into one multiplier per <see cref="UpgradeStat"/>. Systems read <see cref="GetMultiplier"/> and
/// re-read it on <see cref="OnChanged"/>; the menu reads the per-upgrade level, price and affordability.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Wallet that pays for every upgrade.")]
    private Wallet wallet;

    [SerializeField, Tooltip("Upgrades on offer, in menu order. Several may scale the same stat; their bonuses stack.")]
    private UpgradeDefinition[] upgrades;
    #endregion

    #region Private Fields
    private static readonly int StatCount = Enum.GetValues(typeof(UpgradeStat)).Length;

    private int[] levels;
    private float[] multipliers;
    #endregion

    #region Public Properties
    public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;
    #endregion

    #region Events
    /// <summary>Raised after a level is bought, once the multipliers have been recalculated.</summary>
    public event Action<UpgradeManager> OnChanged;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        EnsureInitialized();
    }
    #endregion

    #region Public Methods
    public int GetLevel(UpgradeDefinition upgrade)
    {
        EnsureInitialized();
        int index = IndexOf(upgrade);
        return index >= 0 ? levels[index] : 0;
    }

    public bool IsMaxed(UpgradeDefinition upgrade) => GetLevel(upgrade) >= upgrade.MaxLevel;

    /// <summary>Price of the next level, or 0 once every level is bought.</summary>
    public double GetPrice(UpgradeDefinition upgrade) => IsMaxed(upgrade) ? 0d : upgrade.GetPrice(GetLevel(upgrade));

    public bool CanBuy(UpgradeDefinition upgrade) => !IsMaxed(upgrade) && wallet.Balance >= GetPrice(upgrade);

    /// <summary>Buys the next level. Returns false when maxed, unknown or unaffordable.</summary>
    public bool TryBuy(UpgradeDefinition upgrade)
    {
        EnsureInitialized();
        int index = IndexOf(upgrade);
        if (index < 0 || levels[index] >= upgrade.MaxLevel || !wallet.TrySpend(upgrade.GetPrice(levels[index])))
            return false;

        levels[index]++;
        RecalculateMultipliers();
        OnChanged?.Invoke(this);
        return true;
    }

    /// <summary>Combined multiplier of every bought level that scales <paramref name="stat"/>; 1 when none.</summary>
    public float GetMultiplier(UpgradeStat stat)
    {
        EnsureInitialized();
        return multipliers[(int)stat];
    }

    /// <summary>Level of every upgrade keyed by its asset name, for saving.</summary>
    public UpgradeLevelEntry[] GetLevels()
    {
        EnsureInitialized();
        UpgradeLevelEntry[] entries = new UpgradeLevelEntry[upgrades.Length];
        for (int i = 0; i < upgrades.Length; i++)
            entries[i] = new UpgradeLevelEntry(upgrades[i] != null ? upgrades[i].name : string.Empty, levels[i]);

        return entries;
    }

    /// <summary>
    /// Replaces every level with the saved ones, matched by asset name. Upgrades missing from the save reset to 0,
    /// unknown names are ignored and levels are clamped to each upgrade's maximum.
    /// </summary>
    public void RestoreLevels(IReadOnlyList<UpgradeLevelEntry> entries)
    {
        EnsureInitialized();
        Array.Clear(levels, 0, levels.Length);

        int count = entries != null ? entries.Count : 0;
        for (int i = 0; i < count; i++)
        {
            int index = IndexOf(entries[i].Id);
            if (index >= 0)
                levels[index] = Mathf.Clamp(entries[i].Level, 0, upgrades[index].MaxLevel);
        }

        RecalculateMultipliers();
        OnChanged?.Invoke(this);
    }
    #endregion

    #region Private Methods
    /// <summary>Lazy so readers subscribing in their OnEnable get correct values whatever the script order.</summary>
    private void EnsureInitialized()
    {
        if (upgrades == null)
            upgrades = Array.Empty<UpgradeDefinition>();

        // Fix: the Editor's domain-reload backup restores null private arrays as empty ones, so test sizes, not null.
        bool hasLevels = levels != null && levels.Length == upgrades.Length;
        bool hasMultipliers = multipliers != null && multipliers.Length == StatCount;
        if (hasLevels && hasMultipliers)
            return;

        levels = new int[upgrades.Length];
        multipliers = new float[StatCount];
        RecalculateMultipliers();
    }

    private int IndexOf(UpgradeDefinition upgrade) => upgrade != null ? Array.IndexOf(upgrades, upgrade) : -1;

    private int IndexOf(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
            return -1;

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] != null && upgrades[i].name == assetName)
                return i;
        }

        return -1;
    }

    private void RecalculateMultipliers()
    {
        for (int i = 0; i < multipliers.Length; i++)
            multipliers[i] = 1f;

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] != null)
                multipliers[(int)upgrades[i].Stat] *= upgrades[i].GetMultiplier(levels[i]);
        }
    }
    #endregion
}
