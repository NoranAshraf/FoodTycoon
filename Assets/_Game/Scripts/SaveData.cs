using System;
using UnityEngine;

/// <summary>
/// Everything the <see cref="SaveManager"/> persists, laid out for <see cref="JsonUtility"/>. Only progress is kept:
/// product on the belt, boxes on the counter and the truck load are transient and restart empty.
/// </summary>
[Serializable]
public class SaveData
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Layout version of this file, for future migrations.")]
    private int version = CurrentVersion;

    [SerializeField, Tooltip("UTC time of the save, ISO 8601.")]
    private string savedAtUtc;

    [SerializeField, Tooltip("Wallet balance.")]
    private double balance;

    [SerializeField, Tooltip("Machines bought so far; drives the next machine's price.")]
    private int machinesBought;

    [SerializeField, Tooltip("Machine level per slot, 0 for an empty slot.")]
    private int[] machineLevels = Array.Empty<int>();

    [SerializeField, Tooltip("Level bought of every upgrade.")]
    private UpgradeLevelEntry[] upgradeLevels = Array.Empty<UpgradeLevelEntry>();
    #endregion

    #region Public Properties
    public const int CurrentVersion = 1;

    public int Version => version;

    public string SavedAtUtc
    {
        get => savedAtUtc;
        set => savedAtUtc = value;
    }

    public double Balance
    {
        get => balance;
        set => balance = value;
    }

    public int MachinesBought
    {
        get => machinesBought;
        set => machinesBought = value;
    }

    public int[] MachineLevels
    {
        get => machineLevels;
        set => machineLevels = value ?? Array.Empty<int>();
    }

    public UpgradeLevelEntry[] UpgradeLevels
    {
        get => upgradeLevels;
        set => upgradeLevels = value ?? Array.Empty<UpgradeLevelEntry>();
    }
    #endregion
}
