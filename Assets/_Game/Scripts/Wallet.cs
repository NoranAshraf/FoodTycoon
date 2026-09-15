using System;
using UnityEngine;

/// <summary>The player's money. Double precision so idle-game numbers can grow without losing cents early on.</summary>
public class Wallet : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Min(0f), Tooltip("Money the player starts a fresh game with.")]
    private float startingBalance;
    #endregion

    #region Public Properties
    public double Balance { get; private set; }
    #endregion

    #region Events
    public event Action<double> OnBalanceChanged;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        Balance = startingBalance;
    }
    #endregion

    #region Public Methods
    public void Add(double amount)
    {
        if (amount <= 0d)
            return;

        Balance += amount;
        OnBalanceChanged?.Invoke(Balance);
    }

    public bool TrySpend(double amount)
    {
        if (amount < 0d || Balance < amount)
            return false;

        Balance -= amount;
        OnBalanceChanged?.Invoke(Balance);
        return true;
    }

    /// <summary>Overwrites the balance, e.g. when restoring a save. Negative or non-finite values become 0.</summary>
    public void SetBalance(double balance)
    {
        bool isValid = !double.IsNaN(balance) && !double.IsInfinity(balance);
        Balance = isValid ? Math.Max(0d, balance) : 0d;
        OnBalanceChanged?.Invoke(Balance);
    }
    #endregion
}
