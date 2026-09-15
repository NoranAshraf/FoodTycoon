using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Binds the UI Toolkit HUD (HUD.uxml) to the wallet, the truck depot and the machine line.</summary>
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Wallet whose balance is shown in the top counter.")]
    private Wallet wallet;

    [SerializeField, Tooltip("Depot whose docked truck the SELL button cashes in.")]
    private TruckDepot depot;

    [SerializeField, Tooltip("Machine line the ADD MACHINE and MERGE buttons act on.")]
    private GrinderLine grinderLine;
    #endregion

    #region Private Fields
    private Label moneyLabel;
    private Label sellAmountLabel;
    private Button sellButton;
    private Button buyButton;
    private Label buyPriceLabel;
    private Button mergeButton;
    private Label mergePriceLabel;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        moneyLabel = root.Q<Label>("money-label");
        sellAmountLabel = root.Q<Label>("sell-amount");
        sellButton = root.Q<Button>("sell-button");
        buyButton = root.Q<Button>("buy-button");
        buyPriceLabel = root.Q<Label>("buy-price");
        mergeButton = root.Q<Button>("merge-button");
        mergePriceLabel = root.Q<Label>("merge-price");

        sellButton.clicked += HandleSellClicked;
        buyButton.clicked += HandleBuyClicked;
        mergeButton.clicked += HandleMergeClicked;
        wallet.OnBalanceChanged += HandleBalanceChanged;
        depot.OnLoadValueChanged += HandleLoadValueChanged;
        grinderLine.OnChanged += HandleLineChanged;

        HandleBalanceChanged(wallet.Balance);
        HandleLoadValueChanged(depot.CurrentLoadValue);
    }

    private void OnDestroy()
    {
        if (sellButton != null)
            sellButton.clicked -= HandleSellClicked;

        if (buyButton != null)
            buyButton.clicked -= HandleBuyClicked;

        if (mergeButton != null)
            mergeButton.clicked -= HandleMergeClicked;

        if (wallet != null)
            wallet.OnBalanceChanged -= HandleBalanceChanged;

        if (depot != null)
            depot.OnLoadValueChanged -= HandleLoadValueChanged;

        if (grinderLine != null)
            grinderLine.OnChanged -= HandleLineChanged;
    }
    #endregion

    #region Private Methods
    private void HandleSellClicked() => depot.Sell();

    private void HandleBuyClicked() => grinderLine.TryBuyMachine();

    private void HandleMergeClicked() => grinderLine.TryMerge();

    private void HandleBalanceChanged(double balance)
    {
        moneyLabel.text = MoneyFormatter.Format(balance);
        RefreshUpgradeButtons();
    }

    private void HandleLoadValueChanged(double value) => sellAmountLabel.text = MoneyFormatter.Format(value, true);

    private void HandleLineChanged(GrinderLine line) => RefreshUpgradeButtons();

    private void RefreshUpgradeButtons()
    {
        buyPriceLabel.text = grinderLine.HasFreeSlot ? MoneyFormatter.Format(grinderLine.MachinePrice, true) : "FULL";
        buyButton.SetEnabled(grinderLine.CanBuyMachine);

        mergePriceLabel.text = grinderLine.HasMergePair ? MoneyFormatter.Format(grinderLine.MergePrice, true) : "--";
        mergeButton.SetEnabled(grinderLine.CanMerge);
    }
    #endregion
}
