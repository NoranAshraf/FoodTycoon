using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Periodically grinds out product pieces and tosses them onto the belt in front of it.</summary>
public class Grinder : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Belt the pieces land on.")]
    private ConveyorBelt belt;

    [SerializeField, Tooltip("Piece spawned each cycle.")]
    private BeltItem piecePrefab;

    [SerializeField, Tooltip("Where pieces leave the grinder (the extrusion plate).")]
    private Transform outputPoint;

    [SerializeField, Tooltip("Optional parent for spawned pieces, to keep the hierarchy tidy.")]
    private Transform pieceContainer;

    [SerializeField, Min(0.1f), Tooltip("Seconds between grind cycles.")]
    private float cycleTime = 2f;

    [SerializeField, Min(0f), Tooltip("Delay before the first cycle. Stagger grinders so they don't fire in unison.")]
    private float startDelay;

    [SerializeField, Min(1), Tooltip("Pieces produced per cycle.")]
    private int piecesPerCycle = 2;

    [SerializeField, Min(0f), Tooltip("Sideways spacing between the pieces of one cycle, across the belt.")]
    private float pieceSpacing = 0.09f;

    [SerializeField, Min(0f), Tooltip("Money value carried by each piece.")]
    private float pieceValue = 1f;

    [SerializeField, Min(0.05f), Tooltip("Seconds a piece takes to fly from the output point to the belt.")]
    private float tossDuration = 0.35f;

    [SerializeField, Min(0f), Tooltip("Extra arc height of the toss.")]
    private float tossArcHeight = 0.15f;

    [SerializeField, Min(1), Tooltip("Upper bound on pooled pieces.")]
    private int maxPooledPieces = 64;
    #endregion

    #region Private Fields
    private ObjectPool<BeltItem> piecePool;
    private float cycleTimer;
    #endregion

    #region Public Properties
    public float CycleTime
    {
        get => cycleTime;
        set => cycleTime = Mathf.Max(0.1f, value);
    }

    public float PieceValue
    {
        get => pieceValue;
        set => pieceValue = Mathf.Max(0f, value);
    }

    public bool IsProducing { get; set; } = true;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        piecePool = new ObjectPool<BeltItem>(
            createFunc: CreatePiece,
            actionOnGet: OnGetPiece,
            actionOnRelease: OnReleasePiece,
            actionOnDestroy: OnDestroyPiece,
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: maxPooledPieces
        );

        cycleTimer = -startDelay;
    }

    private void Update()
    {
        if (!IsProducing || belt == null || piecePrefab == null)
            return;

        cycleTimer += Time.deltaTime;
        if (cycleTimer < cycleTime)
            return;

        cycleTimer -= cycleTime;
        Grind();
    }
    #endregion

    #region Private Methods
    private void Grind()
    {
        Vector3 landing = belt.GetClosestPointOnPath(outputPoint.position);
        Vector3 across = Vector3.Cross(Vector3.up, belt.Direction).normalized;
        float firstOffset = -(piecesPerCycle - 1) * 0.5f * pieceSpacing;

        for (int i = 0; i < piecesPerCycle; i++)
        {
            BeltItem piece = piecePool.Get();
            piece.Value = pieceValue;
            piece.transform.SetPositionAndRotation(outputPoint.position, Quaternion.identity);

            Vector3 target = landing + across * (firstOffset + i * pieceSpacing) + Vector3.up * piece.RestHeight;
            StartCoroutine(TossRoutine(piece, target));
        }
    }

    private IEnumerator TossRoutine(BeltItem piece, Vector3 target)
    {
        Vector3 start = piece.transform.position;
        float elapsed = 0f;

        while (elapsed < tossDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tossDuration);
            Vector3 position = Vector3.Lerp(start, target, t);
            position.y += tossArcHeight * 4f * t * (1f - t);
            piece.transform.position = position;
            yield return null;
        }

        piece.transform.position = target;
        belt.PlaceItem(piece);
    }

    private BeltItem CreatePiece()
    {
        BeltItem piece = Instantiate(piecePrefab, pieceContainer);
        piece.Pool = piecePool;
        return piece;
    }

    private static void OnGetPiece(BeltItem piece) => piece.gameObject.SetActive(true);

    private static void OnReleasePiece(BeltItem piece) => piece.gameObject.SetActive(false);

    private static void OnDestroyPiece(BeltItem piece)
    {
        // Fix: pools are cleared on Play-mode exit after scene objects are already gone.
        if (piece != null)
            Destroy(piece.gameObject);
    }
    #endregion
}
