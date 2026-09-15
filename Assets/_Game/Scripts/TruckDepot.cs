using System;
using UnityEngine;

/// <summary>
/// Keeps one truck parked at the dock. <see cref="Sell"/> cashes in the docked truck's load, sends it off to the
/// right and immediately drives a fresh truck in from the left. Boxes wait on the counter while no truck is docked.
/// </summary>
public class TruckDepot : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Truck spawned for every delivery after the first.")]
    private Truck truckPrefab;

    [SerializeField, Tooltip("Truck already sitting at the dock when the scene starts. Spawned if empty.")]
    private Truck initialTruck;

    [SerializeField, Tooltip("Where a docked truck parks and gets loaded.")]
    private Transform dockPoint;

    [SerializeField, Tooltip("Off-screen point new trucks drive in from.")]
    private Transform entryPoint;

    [SerializeField, Tooltip("Off-screen point sold trucks drive out to.")]
    private Transform exitPoint;

    [SerializeField, Tooltip("Dispatcher that loads the docked truck; its destination follows the current truck.")]
    private BoxDispatcher dispatcher;

    [SerializeField, Tooltip("Wallet that receives the money from each sale.")]
    private Wallet wallet;

    [SerializeField, Tooltip("HUD layer that draws every truck's capacity label.")]
    private WorldLabelLayer labelLayer;

    [SerializeField, Min(0.1f), Tooltip("Truck driving speed in world units per second.")]
    private float truckSpeed = 3f;
    #endregion

    #region Public Properties
    public Truck CurrentTruck { get; private set; }

    /// <summary>Value of the load on the docked truck, or 0 while none is docked.</summary>
    public double CurrentLoadValue => CurrentTruck != null ? CurrentTruck.LoadValue : 0d;
    #endregion

    #region Events
    public event Action<double> OnLoadValueChanged;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Start()
    {
        if (initialTruck != null)
        {
            initialTruck.transform.SetPositionAndRotation(dockPoint.position, dockPoint.rotation);
            initialTruck.Setup(labelLayer);
            Dock(initialTruck);
        }
        else
        {
            Dock(SpawnTruck(dockPoint));
        }
    }

    private void OnDisable()
    {
        if (CurrentTruck != null)
            CurrentTruck.BedStack.OnChanged -= HandleLoadChanged;
    }
    #endregion

    #region Public Methods
    /// <summary>Sells whatever is on the docked truck (even nothing), sends it away and calls in the next one.</summary>
    public void Sell()
    {
        Truck truck = CurrentTruck;
        if (truck == null || truck.IsDriving)
            return;

        Undock(truck);
        wallet.Add(truck.LoadValue);
        truck.Depart(exitPoint.position, truckSpeed);

        Truck next = SpawnTruck(entryPoint);
        next.DriveTo(dockPoint.position, truckSpeed, () => Dock(next));
    }
    #endregion

    #region Private Methods
    private Truck SpawnTruck(Transform at)
    {
        Truck truck = Instantiate(truckPrefab, at.position, at.rotation, transform);
        truck.Setup(labelLayer);
        return truck;
    }

    private void Dock(Truck truck)
    {
        CurrentTruck = truck;
        truck.BedStack.OnChanged += HandleLoadChanged;
        dispatcher.Destination = truck.BedStack;
        OnLoadValueChanged?.Invoke(truck.LoadValue);
    }

    private void Undock(Truck truck)
    {
        truck.BedStack.OnChanged -= HandleLoadChanged;
        dispatcher.Destination = null;
        CurrentTruck = null;
        OnLoadValueChanged?.Invoke(0d);
    }

    private void HandleLoadChanged(BoxStack stack) => OnLoadValueChanged?.Invoke(stack.TotalValue);
    #endregion
}
