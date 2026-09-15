using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A HUD label pinned over a point in the 3D scene. Owns its <see cref="Label"/> element; the
/// <see cref="WorldLabelLayer"/> handed in by whoever spawns the object draws it in the UI Toolkit HUD at this
/// transform's screen position, so it gets the same outlined font as the rest of the UI. Replaces world-space
/// TextMesh labels: put it on the anchor object and set <see cref="Text"/> from the owning script.
/// </summary>
public class WorldLabel : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Text shown until a script sets Text.")]
    private string text = "";

    [SerializeField, Tooltip("Point of the label placed on the anchor: (0.5, 0.5) centres it, (1, 0.5) puts its " +
        "right edge there.")]
    private Vector2 pivot = new Vector2(0.5f, 0.5f);

    [SerializeField, Tooltip("Extra USS class on the label, e.g. for a per-object size. Optional.")]
    private string styleClass;
    #endregion

    #region Private Fields
    private const string BaseClass = "world-label";

    private WorldLabelLayer layer;
    private Label element;
    private float baseScale;
    #endregion

    #region Public Properties
    public string Text
    {
        get => text;
        set
        {
            text = value;
            if (element != null)
                element.text = value;
        }
    }

    /// <summary>UI element the layer draws. Created on first use so it exists whatever the Awake order.</summary>
    public Label Element
    {
        get
        {
            EnsureElement();
            return element;
        }
    }

    /// <summary>Layer that draws this label. Setting it re-registers the label while it is active.</summary>
    public WorldLabelLayer Layer
    {
        get => layer;
        set
        {
            if (layer == value)
                return;

            if (layer != null && isActiveAndEnabled)
                layer.Remove(this);

            layer = value;

            if (layer != null && isActiveAndEnabled)
                layer.Add(this);
        }
    }

    /// <summary>How much the object has grown or shrunk since Awake, so the label follows pops and merges.</summary>
    public float ScaleFactor => baseScale > 0f ? transform.lossyScale.y / baseScale : 1f;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        baseScale = transform.lossyScale.y;
        EnsureElement();
    }

    private void OnEnable()
    {
        if (layer != null)
            layer.Add(this);
    }

    private void OnDisable()
    {
        if (layer != null)
            layer.Remove(this);
    }
    #endregion

    #region Private Methods
    private void EnsureElement()
    {
        if (element != null)
            return;

        element = new Label(text) { pickingMode = PickingMode.Ignore };
        element.AddToClassList(BaseClass);
        if (!string.IsNullOrEmpty(styleClass))
            element.AddToClassList(styleClass);

        element.style.translate = new Translate(Length.Percent(-pivot.x * 100f), Length.Percent(-pivot.y * 100f));
    }
    #endregion
}
