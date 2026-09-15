using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the UPGRADES overlay (UpgradesPanel.uxml, instanced inside HUD.uxml): builds one <see cref="UpgradeRowView"/>
/// per upgrade the <see cref="UpgradeManager"/> offers, keeps the rows in sync with levels and the wallet, and opens
/// or closes the panel with a small pop.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UpgradesPanelController : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Manager whose upgrades are listed and sold.")]
    private UpgradeManager upgrades;

    [SerializeField, Tooltip("Wallet watched to grey out rows the player can't afford.")]
    private Wallet wallet;

    [SerializeField, Tooltip("UpgradeRow.uxml, instantiated once per upgrade.")]
    private VisualTreeAsset rowTemplate;
    #endregion

    #region Private Fields
    private const string OpenClass = "upgrades-card--open";

    private readonly List<UpgradeRowView> rows = new List<UpgradeRowView>();
    private VisualElement panel;
    private VisualElement overlay;
    private VisualElement card;
    private Button closeButton;
    private Action addOpenClass;
    #endregion

    #region Public Properties
    public bool IsOpen { get; private set; }
    #endregion

    #region MonoBehaviour Lifecycle
    private void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q<VisualElement>("upgrades-panel");
        overlay = panel.Q<VisualElement>("upgrades-overlay");
        card = panel.Q<VisualElement>("upgrades-card");
        closeButton = panel.Q<Button>("upgrades-close");
        ScrollView rowList = panel.Q<ScrollView>("upgrades-rows");
        addOpenClass = () => card.AddToClassList(OpenClass);

        foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
        {
            if (upgrade == null)
                continue;

            UpgradeRowView row = new UpgradeRowView(rowTemplate, upgrade);
            row.OnBuyClicked += HandleBuyClicked;
            rowList.Add(row.Root);
            rows.Add(row);
        }

        closeButton.clicked += Hide;
        overlay.RegisterCallback<ClickEvent>(HandleOverlayClicked);
        upgrades.OnChanged += HandleUpgradesChanged;
        wallet.OnBalanceChanged += HandleBalanceChanged;

        panel.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.clicked -= Hide;

        if (overlay != null)
            overlay.UnregisterCallback<ClickEvent>(HandleOverlayClicked);

        if (upgrades != null)
            upgrades.OnChanged -= HandleUpgradesChanged;

        if (wallet != null)
            wallet.OnBalanceChanged -= HandleBalanceChanged;

        foreach (UpgradeRowView row in rows)
        {
            row.OnBuyClicked -= HandleBuyClicked;
            row.Dispose();
        }

        rows.Clear();
    }
    #endregion

    #region Public Methods
    public void Show()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        Refresh();
        panel.style.display = DisplayStyle.Flex;
        card.RemoveFromClassList(OpenClass);
        // The transition only plays if the class lands a frame after the panel became visible.
        card.schedule.Execute(addOpenClass);
    }

    public void Hide()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        card.RemoveFromClassList(OpenClass);
        panel.style.display = DisplayStyle.None;
    }

    public void Toggle()
    {
        if (IsOpen)
            Hide();
        else
            Show();
    }
    #endregion

    #region Private Methods
    private void HandleBuyClicked(UpgradeDefinition upgrade) => upgrades.TryBuy(upgrade);

    private void HandleUpgradesChanged(UpgradeManager manager) => Refresh();

    private void HandleBalanceChanged(double balance) => Refresh();

    private void HandleOverlayClicked(ClickEvent evt)
    {
        if (evt.target == overlay)
            Hide();
    }

    private void Refresh()
    {
        if (!IsOpen)
            return;

        foreach (UpgradeRowView row in rows)
            row.Refresh(upgrades);
    }
    #endregion
}
