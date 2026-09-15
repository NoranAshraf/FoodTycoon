using System;
using UnityEngine;

/// <summary>One saved upgrade level, keyed by the <see cref="UpgradeDefinition"/> asset name.</summary>
[Serializable]
public struct UpgradeLevelEntry
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Asset name of the upgrade definition.")]
    private string id;

    [SerializeField, Tooltip("Levels bought.")]
    private int level;
    #endregion

    #region Public Properties
    public string Id => id;

    public int Level => level;
    #endregion

    #region Public Methods
    public UpgradeLevelEntry(string id, int level)
    {
        this.id = id;
        this.level = level;
    }
    #endregion
}
