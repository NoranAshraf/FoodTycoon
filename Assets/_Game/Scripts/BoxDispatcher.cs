using UnityEngine;

/// <summary>
/// Moves boxes from one stack to another, oldest first, once they have rested for <c>dwellTime</c>. Stops while the
/// destination is full or missing, so boxes simply pile up on the source until there is room again.
/// </summary>
public class BoxDispatcher : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Stack boxes are taken from (the counter).")]
    private BoxStack source;

    [SerializeField, Tooltip("Stack boxes are sent to (the truck bed).")]
    private BoxStack destination;

    [SerializeField, Min(0f), Tooltip("Seconds a box rests on the source before it may be sent.")]
    private float dwellTime = 1f;

    [SerializeField, Min(0f), Tooltip("Minimum seconds between two sends, so a backlog drains one box at a time.")]
    private float sendInterval = 0.25f;

    [SerializeField, Min(0.05f), Tooltip("Seconds a box takes to fly to the destination.")]
    private float sendDuration = 0.6f;

    [SerializeField, Min(0f), Tooltip("Arc height of the flight.")]
    private float sendArcHeight = 0.5f;
    #endregion

    #region Private Fields
    private float nextSendTime;
    #endregion

    #region Public Properties
    public BoxStack Destination
    {
        get => destination;
        set => destination = value;
    }
    #endregion

    #region MonoBehaviour Lifecycle
    private void Update()
    {
        if (source == null || destination == null || destination.IsFull || Time.time < nextSendTime)
            return;

        PackagedBox box = source.Oldest;
        if (box == null || Time.time - box.PlacedTime < dwellTime)
            return;

        int ticket = destination.Reserve();
        source.TakeOldest();
        box.FlyTo(destination, ticket, sendDuration, sendArcHeight);
        nextSendTime = Time.time + sendInterval;
    }
    #endregion
}
