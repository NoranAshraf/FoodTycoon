using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Consumes pieces arriving at the end of the belt, packs them into boxes and drops each box onto the counter stack
/// beside the machine. A <see cref="BoxDispatcher"/> moves them on from there. Owns the box pool: boxes come back
/// here (via <see cref="PackagedBox.ReturnToPool"/>) once a truck has driven off with them. Pooled boxes live at the
/// scene root.
/// </summary>
public class PackingMachine : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Belt whose end feeds this machine.")]
    private ConveyorBelt inputBelt;

    [SerializeField, Tooltip("Box produced for every Pieces Per Box pieces.")]
    private PackagedBox boxPrefab;

    [SerializeField, Tooltip("Where finished boxes pop out.")]
    private Transform boxOutput;

    [SerializeField, Min(1), Tooltip("Pieces packed into one box.")]
    private int piecesPerBox = 2;

    [SerializeField, Tooltip("Stack that receives finished boxes (the counter beside the machine).")]
    private BoxStack outputStack;

    [SerializeField, Min(0.05f), Tooltip("Seconds a box takes to hop from the output to the counter.")]
    private float sendDuration = 0.4f;

    [SerializeField, Min(0f), Tooltip("Arc height of the hop.")]
    private float sendArcHeight = 0.3f;

    [SerializeField, Min(1), Tooltip("Upper bound on idle boxes kept in the pool (a full truck returns 16 at once).")]
    private int maxPooledBoxes = 32;
    #endregion

    #region Private Fields
    private ObjectPool<PackagedBox> boxPool;
    private int bufferedPieces;
    private float bufferedValue;
    #endregion

    #region Public Properties
    public int PiecesPerBox
    {
        get => piecesPerBox;
        set => piecesPerBox = Mathf.Max(1, value);
    }
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        boxPool = new ObjectPool<PackagedBox>(
            createFunc: CreateBox,
            actionOnGet: OnGetBox,
            actionOnRelease: OnReleaseBox,
            actionOnDestroy: OnDestroyBox,
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: maxPooledBoxes
        );
    }

    private void OnEnable()
    {
        if (inputBelt != null)
            inputBelt.OnItemReachedEnd += HandlePieceArrived;
    }

    private void OnDisable()
    {
        if (inputBelt != null)
            inputBelt.OnItemReachedEnd -= HandlePieceArrived;
    }
    #endregion

    #region Private Methods
    private void HandlePieceArrived(BeltItem piece)
    {
        bufferedPieces++;
        bufferedValue += piece.Value;
        piece.ReturnToPool();

        if (bufferedPieces < piecesPerBox)
            return;

        bufferedPieces -= piecesPerBox;
        float boxValue = bufferedValue;
        bufferedValue = 0f;
        PackBox(boxValue);
    }

    private void PackBox(float value)
    {
        if (outputStack == null || outputStack.IsFull)
            return;

        int ticket = outputStack.Reserve();
        PackagedBox box = boxPool.Get();
        box.Value = value;
        box.transform.SetPositionAndRotation(boxOutput.position, boxOutput.rotation);
        box.FlyTo(outputStack, ticket, sendDuration, sendArcHeight);
    }

    private PackagedBox CreateBox()
    {
        PackagedBox box = Instantiate(boxPrefab);
        box.Pool = boxPool;
        return box;
    }

    private static void OnGetBox(PackagedBox box)
    {
        box.gameObject.SetActive(true);
    }

    private void OnReleaseBox(PackagedBox box)
    {
        box.transform.SetParent(null, false);
        box.gameObject.SetActive(false);
    }

    private static void OnDestroyBox(PackagedBox box)
    {
        // Fix: pools are cleared on Play-mode exit after scene objects are already gone.
        if (box != null)
            Destroy(box.gameObject);
    }
    #endregion
}
