using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Periodically grinds out product pieces and scatters them onto the belt in front of it. Higher levels (reached by
/// merging two machines) produce more pieces per cycle and are colour coded. Pooled pieces live at the scene root.
/// </summary>
public class Grinder : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Belt the pieces land on.")]
    private ConveyorBelt belt;

    [SerializeField, Tooltip("Piece spawned each cycle.")]
    private BeltItem piecePrefab;

    [SerializeField, Tooltip("Where pieces leave the grinder (the extrusion plate).")]
    private Transform outputPoint;

    [SerializeField, Min(0.1f), Tooltip("Seconds between grind cycles.")]
    private float cycleTime = 2f;

    [SerializeField, Min(0f), Tooltip("Delay before the first cycle. Stagger grinders so they don't fire in unison.")]
    private float startDelay;

    [SerializeField, Min(1), Tooltip("Pieces produced per cycle at level 1.")]
    private int piecesPerCycle = 2;

    [SerializeField, Min(1), Tooltip("Pieces per cycle are multiplied by this for every level above 1.")]
    private int levelOutputMultiplier = 2;

    [SerializeField, Min(0f), Tooltip("Seconds over which the pieces of one cycle leave the grinder, at random moments.")]
    private float burstDuration = 0.4f;

    [SerializeField, Min(0f), Tooltip("Random landing spread across the belt, either side of its centre line.")]
    private float landingSpreadAcross = 0.14f;

    [SerializeField, Min(0f), Tooltip("Random landing spread along the belt, either side of the output point.")]
    private float landingSpreadAlong = 0.15f;

    [SerializeField, Min(0f), Tooltip("Money value carried by each piece.")]
    private float pieceValue = 1f;

    [SerializeField, Min(0.05f), Tooltip("Seconds a piece takes to fly from the output point to the belt.")]
    private float tossDuration = 0.35f;

    [SerializeField, Range(0f, 1f), Tooltip("Random variation of the toss duration, as a fraction.")]
    private float tossDurationJitter = 0.25f;

    [SerializeField, Min(0f), Tooltip("Extra arc height of the toss.")]
    private float tossArcHeight = 0.15f;

    [SerializeField, Min(1), Tooltip("Upper bound on pooled pieces.")]
    private int maxPooledPieces = 64;

    [SerializeField, Tooltip("World-space label showing the machine level. Faces the camera.")]
    private TextMesh levelLabel;

    [SerializeField, Tooltip("Renderers tinted with the level colour (URP/Lit, _BaseColor).")]
    private Renderer[] accentRenderers;

    [SerializeField, Tooltip("Tint per level, starting at level 2. Level 1 keeps the original materials.")]
    private Color[] levelColors =
    {
        new Color(0.22f, 0.72f, 0.36f),
        new Color(0.96f, 0.56f, 0.12f),
        new Color(0.89f, 0.22f, 0.27f),
        new Color(0.98f, 0.80f, 0.16f),
        new Color(0.16f, 0.66f, 0.78f),
        new Color(0.93f, 0.36f, 0.64f),
    };

    [SerializeField, Min(0.05f), Tooltip("Duration of the scale pop played on spawn and level up.")]
    private float popDuration = 0.35f;
    #endregion

    #region Private Fields
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private ObjectPool<BeltItem> piecePool;
    private readonly List<BeltItem> livePieces = new List<BeltItem>();
    private readonly List<BeltItem> tossingPieces = new List<BeltItem>();
    private MaterialPropertyBlock propertyBlock;
    private Camera mainCamera;
    private Vector3 baseScale;
    private Coroutine popRoutine;
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

    /// <summary>Pieces produced per cycle at the current level; doubles with every merge by default.</summary>
    public int CurrentPiecesPerCycle => piecesPerCycle * (int)Mathf.Pow(levelOutputMultiplier, Level - 1);

    public int Level { get; private set; } = 1;

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

        propertyBlock = new MaterialPropertyBlock();
        mainCamera = Camera.main;
        baseScale = transform.localScale;
        cycleTimer = -startDelay;
        ApplyLevelVisuals();
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

    private void LateUpdate()
    {
        if (levelLabel != null && mainCamera != null)
            levelLabel.transform.rotation = mainCamera.transform.rotation;
    }
    #endregion

    #region Public Methods
    /// <summary>Points a freshly spawned machine at the belt it feeds.</summary>
    public void Setup(ConveyorBelt targetBelt)
    {
        belt = targetBelt;
    }

    public void SetLevel(int level)
    {
        Level = Mathf.Max(1, level);
        ApplyLevelVisuals();
    }

    /// <summary>Scale punch from <paramref name="fromScale"/> (fraction of the resting scale) back to normal.</summary>
    public void Pop(float fromScale = 0f)
    {
        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine(fromScale));
    }

    /// <summary>
    /// Removes the machine. Pieces still flying are recalled; pieces already on the belt keep riding it and are
    /// destroyed instead of pooled once the packer consumes them.
    /// </summary>
    public void Dismantle()
    {
        IsProducing = false;
        StopAllCoroutines();

        for (int i = tossingPieces.Count - 1; i >= 0; i--)
            tossingPieces[i].ReturnToPool();

        tossingPieces.Clear();

        foreach (BeltItem piece in livePieces)
            piece.Pool = null;

        livePieces.Clear();
        piecePool.Clear();
        Destroy(gameObject);
    }
    #endregion

    #region Private Methods
    private void Grind()
    {
        int count = CurrentPiecesPerCycle;
        for (int i = 0; i < count; i++)
            StartCoroutine(TossRoutine(Random.Range(0f, burstDuration)));
    }

    /// <summary>Waits <paramref name="delay"/>, then lobs one piece onto a random spot on the belt ahead.</summary>
    private IEnumerator TossRoutine(float delay)
    {
        while (delay > 0f)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        BeltItem piece = piecePool.Get();
        piece.Value = pieceValue;
        piece.transform.SetPositionAndRotation(outputPoint.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        tossingPieces.Add(piece);

        Vector3 across = Vector3.Cross(Vector3.up, belt.Direction).normalized;
        Vector3 target = belt.GetClosestPointOnPath(outputPoint.position)
            + across * Random.Range(-landingSpreadAcross, landingSpreadAcross)
            + belt.Direction * Random.Range(-landingSpreadAlong, landingSpreadAlong)
            + Vector3.up * piece.RestHeight;

        Vector3 start = piece.transform.position;
        float duration = tossDuration * (1f + Random.Range(-tossDurationJitter, tossDurationJitter));
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, target, t);
            position.y += tossArcHeight * 4f * t * (1f - t);
            piece.transform.position = position;
            yield return null;
        }

        piece.transform.position = target;
        tossingPieces.Remove(piece);
        belt.PlaceItem(piece);
    }

    private IEnumerator PopRoutine(float fromScale)
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            transform.localScale = baseScale * Mathf.LerpUnclamped(fromScale, 1f, EaseOutBack(t));
            yield return null;
        }

        transform.localScale = baseScale;
        popRoutine = null;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    private void ApplyLevelVisuals()
    {
        if (levelLabel != null)
            levelLabel.text = "LV " + Level;

        if (accentRenderers == null)
            return;

        int colorIndex = Mathf.Min(Level - 2, levelColors.Length - 1);

        foreach (Renderer accent in accentRenderers)
        {
            if (accent == null)
                continue;

            propertyBlock.Clear();
            if (colorIndex >= 0)
                propertyBlock.SetColor(BaseColorId, levelColors[colorIndex]);

            accent.SetPropertyBlock(propertyBlock);
        }
    }

    private BeltItem CreatePiece()
    {
        BeltItem piece = Instantiate(piecePrefab);
        piece.Pool = piecePool;
        return piece;
    }

    private void OnGetPiece(BeltItem piece)
    {
        livePieces.Add(piece);
        piece.gameObject.SetActive(true);
    }

    private void OnReleasePiece(BeltItem piece)
    {
        livePieces.Remove(piece);
        piece.gameObject.SetActive(false);
    }

    private static void OnDestroyPiece(BeltItem piece)
    {
        // Fix: pools are cleared on Play-mode exit after scene objects are already gone.
        if (piece != null)
            Destroy(piece.gameObject);
    }
    #endregion
}
