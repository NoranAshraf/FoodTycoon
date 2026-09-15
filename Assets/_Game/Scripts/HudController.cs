using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Binds the UI Toolkit HUD (HUD.uxml) to the wallet and the truck depot.</summary>
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Wallet whose balance is shown in the top counter.")]
    private Wallet wallet;

    [SerializeField, Tooltip("Depot whose docked truck the SELL button cashes in.")]
    private TruckDepot depot;
    #endregion

    #region Private Fields
    private Label moneyLabel;
    private Label sellAmountLabel;
    private Button sellButton;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        moneyLabel = root.Q<Label>("money-label");
        sellAmountLabel = root.Q<Label>("sell-amount");
        sellButton = root.Q<Button>("sell-button");

        sellButton.clicked += HandleSellClicked;
        wallet.OnBalanceChanged += HandleBalanceChanged;
        depot.OnLoadValueChanged += HandleLoadValueChanged;

        HandleBalanceChanged(wallet.Balance);
        HandleLoadValueChanged(depot.CurrentLoadValue);
    }

    private void OnDestroy()
    {
        if (sellButton != null)
            sellButton.clicked -= HandleSellClicked;

        if (wallet != null)
            wallet.OnBalanceChanged -= HandleBalanceChanged;

        if (depot != null)
            depot.OnLoadValueChanged -= HandleLoadValueChanged;
    }
    #endregion

    #region Private Methods
    private void HandleSellClicked() => depot.Sell();

    private void HandleBalanceChanged(double balance) => moneyLabel.text = MoneyFormatter.Format(balance);

    private void HandleLoadValueChanged(double value) => sellAmountLabel.text = MoneyFormatter.Format(value, true);
    #endregion
}
