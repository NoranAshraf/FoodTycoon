using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Carries <see cref="BeltItem"/>s from Path Start to Path End at <see cref="Speed"/> and scrolls the belt texture at
/// the same rate. Raises <see cref="OnItemReachedEnd"/> when an item arrives; the receiver owns the item from then on.
/// </summary>
[DisallowMultipleComponent]
public class ConveyorBelt : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Renderer whose base texture is scrolled. Defaults to the renderer on this object.")]
    private Renderer beltRenderer;

    [SerializeField, Tooltip("Where items enter the belt (near end).")]
    private Transform pathStart;

    [SerializeField, Tooltip("Where items leave the belt (packing machine entrance).")]
    private Transform pathEnd;

    [SerializeField, Min(0f), Tooltip("Belt surface speed in world units per second.")]
    private float speed = 1f;

    [SerializeField, Tooltip("Flip the travel direction. Off = items and stripes travel from Path Start to Path End.")]
    private bool isReversed;

    [SerializeField, Tooltip("Whether the belt is currently moving.")]
    private bool isRunning = true;

    [SerializeField, Min(0.01f), Tooltip("World distance covered by one repeat of the belt texture. " +
        "Tune so the stripes travel at the same speed as items riding the belt.")]
    private float metersPerTextureRepeat = 1.3f;
    #endregion

    #region Private Fields
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");

    private readonly List<BeltItem> items = new List<BeltItem>();
    private readonly List<BeltItem> arrivedItems = new List<BeltItem>();

    private MaterialPropertyBlock propertyBlock;
    private Vector4 baseScaleOffset;
    private int scaleOffsetId;
    private float scrollOffset;
    #endregion

    #region Public Properties
    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    public bool IsRunning
    {
        get => isRunning;
        set => isRunning = value;
    }

    public Vector3 Origin => isReversed ? pathEnd.position : pathStart.position;

    public Vector3 Destination => isReversed ? pathStart.position : pathEnd.position;

    public Vector3 Direction => (Destination - Origin).normalized;

    public float Length => Vector3.Distance(Origin, Destination);

    public int ItemCount => items.Count;
    #endregion

    #region Events
    public event Action<BeltItem> OnItemReachedEnd;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Reset()
    {
        beltRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        if (beltRenderer == null)
            beltRenderer = GetComponent<Renderer>();

        propertyBlock = new MaterialPropertyBlock();

        Material material = beltRenderer.sharedMaterial;
        scaleOffsetId = material.HasProperty(BaseMapStId) ? BaseMapStId : MainTexStId;
        baseScaleOffset = material.GetVector(scaleOffsetId);
    }

    private void Update()
    {
        if (!isRunning || speed <= 0f)
            return;

        float step = speed * Time.deltaTime;
        ScrollTexture(step);
        MoveItems(step);
    }

    private void OnDisable()
    {
        if (beltRenderer == null || propertyBlock == null)
            return;

        propertyBlock.Clear();
        beltRenderer.SetPropertyBlock(propertyBlock);
    }
    #endregion

    #region Public Methods
    /// <summary>Snaps a world position onto the item path.</summary>
    public Vector3 GetClosestPointOnPath(Vector3 worldPosition)
    {
        Vector3 origin = Origin;
        Vector3 direction = Direction;
        float distance = Mathf.Clamp(Vector3.Dot(worldPosition - origin, direction), 0f, Length);
        return origin + direction * distance;
    }

    /// <summary>
    /// Starts carrying an item from its current world position. Any offset from the path (sideways or vertical)
    /// is preserved while it travels.
    /// </summary>
    public void PlaceItem(BeltItem item)
    {
        Vector3 origin = Origin;
        Vector3 direction = Direction;
        Vector3 fromOrigin = item.transform.position - origin;
        float distance = Mathf.Clamp(Vector3.Dot(fromOrigin, direction), 0f, Length);

        item.DistanceAlongBelt = distance;
        item.LateralOffset = fromOrigin - direction * distance;
        items.Add(item);
        PositionItem(item, origin, direction);
    }

    public void RemoveItem(BeltItem item)
    {
        items.Remove(item);
    }
    #endregion

    #region Private Methods
    private void ScrollTexture(float step)
    {
        float direction = isReversed ? -1f : 1f;
        scrollOffset = Mathf.Repeat(scrollOffset + direction * step / metersPerTextureRepeat, 1f);

        Vector4 scaleOffset = baseScaleOffset;
        scaleOffset.w += scrollOffset;

        beltRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector(scaleOffsetId, scaleOffset);
        beltRenderer.SetPropertyBlock(propertyBlock);
    }

    private void MoveItems(float step)
    {
        if (items.Count == 0)
            return;

        Vector3 origin = Origin;
        Vector3 direction = Direction;
        float length = Length;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            BeltItem item = items[i];
            if (item == null)
            {
                items.RemoveAt(i);
                continue;
            }

            item.DistanceAlongBelt = Mathf.Min(item.DistanceAlongBelt + step, length);
            PositionItem(item, origin, direction);

            if (item.DistanceAlongBelt >= length)
            {
                items.RemoveAt(i);
                arrivedItems.Add(item);
            }
        }

        for (int i = 0; i < arrivedItems.Count; i++)
            OnItemReachedEnd?.Invoke(arrivedItems[i]);

        arrivedItems.Clear();
    }

    private static void PositionItem(BeltItem item, Vector3 origin, Vector3 direction)
    {
        item.transform.position = origin + direction * item.DistanceAlongBelt + item.LateralOffset;
    }
    #endregion
}
