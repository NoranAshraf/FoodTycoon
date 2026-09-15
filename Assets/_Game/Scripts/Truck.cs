using System;
using System.Collections;
using UnityEngine;

/// <summary>Delivery truck. Owns the box stack in its bed, shows how full it is, and can drive between points.</summary>
public class Truck : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Stack of boxes in the truck bed.")]
    private BoxStack bedStack;

    [SerializeField, Tooltip("World-space label showing loaded/capacity. Faces the camera.")]
    private TextMesh capacityLabel;
    #endregion

    #region Private Fields
    private Camera mainCamera;
    #endregion

    #region Public Properties
    public BoxStack BedStack => bedStack;

    public bool IsFull => bedStack != null && bedStack.IsFull;

    public bool IsDriving { get; private set; }

    /// <summary>Money value of everything currently in the bed.</summary>
    public double LoadValue => bedStack != null ? bedStack.TotalValue : 0d;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        mainCamera = Camera.main;
    }

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

    private void LateUpdate()
    {
        if (capacityLabel != null && mainCamera != null)
            capacityLabel.transform.rotation = mainCamera.transform.rotation;
    }
    #endregion

    #region Public Methods
    /// <summary>Drives in a straight line to <paramref name="target"/>, easing in and out.</summary>
    public void DriveTo(Vector3 target, float speed, Action onArrived = null)
    {
        StartCoroutine(DriveRoutine(target, speed, onArrived));
    }

    /// <summary>Drives to <paramref name="exit"/> and despawns there, taking its load with it.</summary>
    public void Depart(Vector3 exit, float speed)
    {
        DriveTo(exit, speed, () => Destroy(gameObject));
    }
    #endregion

    #region Private Methods
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

        capacityLabel.text = bedStack.Count + "/" + bedStack.Capacity;
    }
    #endregion
}
