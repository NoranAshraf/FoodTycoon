using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stacks <see cref="PackagedBox"/>es in layers above this transform. Arriving boxes reserve a ticket first so several
/// can be in flight at once; boxes are taken oldest-first and the tower settles down to fill the gap.
/// </summary>
public class BoxStack : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Min(0), Tooltip("Maximum boxes, counting ones in flight. 0 = unlimited.")]
    private int capacity = 16;

    [SerializeField, Min(1), Tooltip("Boxes per layer, laid out side by side along local X.")]
    private int slotsPerLayer = 2;

    [SerializeField, Tooltip("Box footprint (x, z) and height (y) in this stack's local space.")]
    private Vector3 boxSize = new Vector3(0.10f, 0.083f, 0.143f);

    [SerializeField, Min(0f), Tooltip("Gap between boxes.")]
    private float gap = 0.004f;

    [SerializeField, Min(0.01f), Tooltip("How fast boxes slide down to fill a gap, in local units per second.")]
    private float settleSpeed = 1.5f;
    #endregion

    #region Private Fields
    private readonly List<PackagedBox> boxes = new List<PackagedBox>();
    private readonly List<int> pendingTickets = new List<int>();
    private int nextTicket;
    private bool isSettling;
    #endregion

    #region Public Properties
    public int Capacity => capacity;

    public int Count => boxes.Count;

    public int PendingCount => pendingTickets.Count;

    public bool IsFull => capacity > 0 && boxes.Count + pendingTickets.Count >= capacity;

    public PackagedBox Oldest => boxes.Count > 0 ? boxes[0] : null;

    public Quaternion SlotRotation => transform.rotation;

    public IReadOnlyList<PackagedBox> Boxes => boxes;

    public float TotalValue
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < boxes.Count; i++)
                total += boxes[i].Value;
            return total;
        }
    }
    #endregion

    #region Events
    public event Action<BoxStack> OnChanged;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Update()
    {
        if (!isSettling)
            return;

        float step = settleSpeed * Time.deltaTime;
        isSettling = false;

        for (int i = 0; i < boxes.Count; i++)
        {
            Transform boxTransform = boxes[i].transform;
            Vector3 target = GetSlotLocalPosition(i);
            if (boxTransform.localPosition == target)
                continue;

            boxTransform.localPosition = Vector3.MoveTowards(boxTransform.localPosition, target, step);
            isSettling = true;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>Reserves room for one incoming box and returns its ticket, or -1 when the stack is full.</summary>
    public int Reserve()
    {
        if (IsFull)
            return -1;

        int ticket = nextTicket++;
        pendingTickets.Add(ticket);
        return ticket;
    }

    /// <summary>Where a reserved box will land right now; moves down as boxes below it are taken.</summary>
    public Vector3 GetReservedWorldPosition(int ticket)
    {
        int pendingIndex = Mathf.Max(0, pendingTickets.IndexOf(ticket));
        return transform.TransformPoint(GetSlotLocalPosition(boxes.Count + pendingIndex));
    }

    public void Place(PackagedBox box, int ticket)
    {
        pendingTickets.Remove(ticket);
        box.transform.SetParent(transform, false);
        box.transform.localPosition = GetSlotLocalPosition(boxes.Count);
        box.transform.localRotation = Quaternion.identity;
        box.PlacedTime = Time.time;
        boxes.Add(box);
        OnChanged?.Invoke(this);
    }

    /// <summary>Removes the oldest box (bottom of the tower) and lets the rest settle down.</summary>
    public PackagedBox TakeOldest()
    {
        if (boxes.Count == 0)
            return null;

        PackagedBox box = boxes[0];
        boxes.RemoveAt(0);
        box.transform.SetParent(null, true);
        isSettling = boxes.Count > 0;
        OnChanged?.Invoke(this);
        return box;
    }

    /// <summary>Returns every stacked box to its pool and empties the stack.</summary>
    public void ReleaseAll()
    {
        for (int i = 0; i < boxes.Count; i++)
            boxes[i].ReturnToPool();

        boxes.Clear();
        isSettling = false;
        OnChanged?.Invoke(this);
    }
    #endregion

    #region Private Methods
    private Vector3 GetSlotLocalPosition(int slot)
    {
        int layer = slot / slotsPerLayer;
        int column = slot % slotsPerLayer;
        float x = (column - (slotsPerLayer - 1) * 0.5f) * (boxSize.x + gap);
        float y = layer * (boxSize.y + gap);
        return new Vector3(x, y, 0f);
    }
    #endregion
}
