using UnityEngine;

/// <summary>
/// Scrolls the belt surface texture so the conveyor reads as running. Purely visual: gameplay drives
/// <see cref="Speed"/> and <see cref="IsRunning"/> (belt upgrades, stalling when the packer is full).
/// </summary>
[DisallowMultipleComponent]
public class ConveyorBelt : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Renderer whose base texture is scrolled. Defaults to the renderer on this object.")]
    private Renderer beltRenderer;

    [SerializeField, Min(0f), Tooltip("Belt surface speed in world units per second.")]
    private float speed = 1f;

    [SerializeField, Tooltip("Flip the scroll direction. Off = stripes travel toward the packing machine.")]
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

        float direction = isReversed ? -1f : 1f;
        float offsetPerSecond = speed / metersPerTextureRepeat;
        scrollOffset = Mathf.Repeat(scrollOffset + direction * offsetPerSecond * Time.deltaTime, 1f);
        ApplyScrollOffset();
    }

    private void OnDisable()
    {
        if (beltRenderer == null || propertyBlock == null)
            return;

        propertyBlock.Clear();
        beltRenderer.SetPropertyBlock(propertyBlock);
    }
    #endregion

    #region Private Methods
    private void ApplyScrollOffset()
    {
        Vector4 scaleOffset = baseScaleOffset;
        scaleOffset.w += scrollOffset;

        beltRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector(scaleOffsetId, scaleOffset);
        beltRenderer.SetPropertyBlock(propertyBlock);
    }
    #endregion
}
