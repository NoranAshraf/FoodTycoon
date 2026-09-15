using System;
using System.Collections;
using UnityEngine;

/// <summary>Delivery truck. Owns the box stack in its bed, shows how full it is, and can drive between points.</summary>
public class Truck : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Stack of boxes in the truck bed.")]
    private BoxStack bedStack;

    [SerializeField, Tooltip("HUD label pinned over the bed showing loaded/capacity. Optional.")]
    private WorldLabel capacityLabel;
    #endregion

    #region Public Properties
    public BoxStack BedStack => bedStack;

    public bool IsFull => bedStack != null && bedStack.IsFull;

    public bool IsDriving { get; private set; }

    /// <summary>Money value of everything currently in the bed.</summary>
    public double LoadValue => bedStack != null ? bedStack.TotalValue : 0d;
    #endregion

    #region MonoBehaviour Lifecycle
    private void OnEnable()
    {
        if (bedStack != null)
            bedStack.OnChanged += HandleStackChanged;

        RefreshLabel();
    }

    private void OnDisable()
    {
        if (bedStack != null)
            bedStack.OnChanged -= HandleStackChanged;
    }
    #endregion

    #region Public Methods
    /// <summary>Points a freshly spawned truck at the HUD layer that draws its capacity label.</summary>
    public void Setup(WorldLabelLayer labelLayer)
    {
        if (capacityLabel != null)
            capacityLabel.Layer = labelLayer;
    }

    /// <summary>Drives in a straight line to <paramref name="target"/>, easing in and out.</summary>
    public void DriveTo(Vector3 target, float speed, Action onArrived = null)
    {
        StartCoroutine(DriveRoutine(target, speed, onArrived));
    }

    /// <summary>Drives to <paramref name="exit"/> and despawns there, returning its load to the box pool.</summary>
    public void Depart(Vector3 exit, float speed)
    {
        DriveTo(exit, speed, Despawn);
    }
    #endregion

    #region Private Methods
    private void Despawn()
    {
        if (bedStack != null)
            bedStack.ReleaseAll();

        Destroy(gameObject);
    }

    private IEnumerator DriveRoutine(Vector3 target, float speed, Action onArrived)
    {
        IsDriving = true;
        Vector3 start = transform.position;
        float duration = Vector3.Distance(start, target) / Mathf.Max(0.01f, speed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        IsDriving = false;
        onArrived?.Invoke();
    }

    private void HandleStackChanged(BoxStack stack) => RefreshLabel();

    private void RefreshLabel()
    {
        if (capacityLabel == null || bedStack == null)
            return;

        capacityLabel.Text = bedStack.Count + "/" + bedStack.Capacity;
    }
    #endregion
}
