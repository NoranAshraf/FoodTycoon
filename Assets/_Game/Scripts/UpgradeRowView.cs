using System;
using UnityEngine.UIElements;

/// <summary>
/// One row of the upgrades menu (an instance of UpgradeRow.uxml) bound to an <see cref="UpgradeDefinition"/>. Shows
/// the icon, title, level progress and the price button; the owning panel decides when to refresh it.
/// </summary>
public class UpgradeRowView
{
    #region Private Fields
    private const string MaxedClass = "upgrade-row--maxed";

    private readonly VisualElement row;
    private readonly Label levelLabel;
    private readonly VisualElement progressFill;
    private readonly Button buyButton;
    private readonly Label priceLabel;
    #endregion

    #region Public Properties
    public UpgradeDefinition Upgrade { get; }

    /// <summary>Element to add to the menu (the template container wrapping the row).</summary>
    public VisualElement Root { get; }
    #endregion

    #region Events
    public event Action<UpgradeDefinition> OnBuyClicked;
    #endregion

    #region Constructor
    public UpgradeRowView(VisualTreeAsset template, UpgradeDefinition upgrade)
    {
        Upgrade = upgrade;
        Root = template.Instantiate();
        Root.style.flexShrink = 0;

        row = Root.Q<VisualElement>("upgrade-row");
        levelLabel = Root.Q<Label>("row-level");
        progressFill = Root.Q<VisualElement>("row-progress-fill");
        buyButton = Root.Q<Button>("row-buy");
        priceLabel = Root.Q<Label>("row-price");

        Root.Q<Label>("row-title").text = upgrade.DisplayName;

        if (upgrade.Icon != null)
            Root.Q<VisualElement>("row-icon").Add(upgrade.Icon.Instantiate());

        buyButton.clicked += HandleBuyClicked;
    }
    #endregion

    #region Public Methods
    public void Refresh(UpgradeManager upgrades)
    {
        int level = upgrades.GetLevel(Upgrade);
        bool isMaxed = upgrades.IsMaxed(Upgrade);

        levelLabel.text = "LV " + level + "/" + Upgrade.MaxLevel;
        progressFill.style.width = Length.Percent(100f * level / Upgrade.MaxLevel);
        priceLabel.text = isMaxed ? "MAX" : MoneyFormatter.Format(upgrades.GetPrice(Upgrade), true);
        buyButton.SetEnabled(upgrades.CanBuy(Upgrade));
        row.EnableInClassList(MaxedClass, isMaxed);
    }

    public void Dispose()
    {
        buyButton.clicked -= HandleBuyClicked;
    }
    #endregion

    #region Private Methods
    private void HandleBuyClicked() => OnBuyClicked?.Invoke(Upgrade);
    #endregion
}
