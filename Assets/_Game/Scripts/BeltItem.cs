using UnityEngine;
using UnityEngine.Pool;

/// <summary>A product piece riding a <see cref="ConveyorBelt"/>. Belt-side state is owned by the belt.</summary>
public class BeltItem : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Min(0f), Tooltip("Height of the resting point above the belt path (usually half the item's height).")]
    private float restHeight = 0.04f;
    #endregion

    #region Public Properties
    public float RestHeight => restHeight;

    public float Value { get; set; }

    public float DistanceAlongBelt { get; set; }

    public Vector3 LateralOffset { get; set; }

    public IObjectPool<BeltItem> Pool { get; set; }
    #endregion

    #region Public Methods
    public void ReturnToPool()
    {
        if (Pool != null)
            Pool.Release(this);
        else
            Destroy(gameObject);
    }
    #endregion
}
