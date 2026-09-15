using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// One row of the upgrades menu: which <see cref="UpgradeStat"/> it scales, by how much per level and what each level
/// costs. Pure configuration — the current level lives in the <see cref="UpgradeManager"/>. To add an upgrade, create
/// one of these (Create → FoodTycoon → Upgrade) and add it to the manager's list.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade", menuName = "FoodTycoon/Upgrade")]
public class UpgradeDefinition : ScriptableObject
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Title shown on the menu row, e.g. \"+CONVEYOR SPEED\".")]
    private string displayName = "+UPGRADE";

    [SerializeField, Tooltip("Icon for the row, drawn with styled elements (see Assets/_Game/UI/UpgradeIcons). " +
        "Optional.")]
    private VisualTreeAsset icon;

    [SerializeField, Tooltip("Gameplay value this upgrade scales.")]
    private UpgradeStat stat;

    [SerializeField, Min(1), Tooltip("Number of levels that can be bought.")]
    private int maxLevel = 10;

    [SerializeField, Min(0f), Tooltip("Added to the multiplier per level: 0.2 = +20% per level, so level 5 = ×2.")]
    private float bonusPerLevel = 0.2f;

    [SerializeField, Min(0f), Tooltip("Price of the first level. Prices round to whole dollars.")]
    private float basePrice = 20f;

    [SerializeField, Min(1f), Tooltip("Price multiplier per level already bought.")]
    private float priceGrowth = 1.5f;
    #endregion

    #region Public Properties
    public string DisplayName => displayName;

    public VisualTreeAsset Icon => icon;

    public UpgradeStat Stat => stat;

    public int MaxLevel => maxLevel;

    public float BonusPerLevel => bonusPerLevel;
    #endregion

    #region Public Methods
    /// <summary>Multiplier applied to the stat at <paramref name="level"/>; 1 at level 0.</summary>
    public float GetMultiplier(int level) => 1f + bonusPerLevel * Mathf.Clamp(level, 0, maxLevel);

    /// <summary>Price of going from <paramref name="level"/> to the next one, in whole dollars.</summary>
    public double GetPrice(int level) => Math.Round(basePrice * Math.Pow(priceGrowth, Mathf.Max(0, level)));
    #endregion
}
