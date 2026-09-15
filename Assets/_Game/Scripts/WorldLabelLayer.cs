using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draws every registered <see cref="WorldLabel"/> inside the HUD. Each frame it projects the label's transform
/// through the camera into panel space and moves the label's element there, scaled with the object. Lives on the HUD
/// UIDocument; the elements go into the <c>world-labels</c> container, which sits under the rest of the HUD so
/// buttons and overlays cover them.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class WorldLabelLayer : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Camera the labels are projected through. The main camera if empty.")]
    private Camera worldCamera;

    [SerializeField, Tooltip("Name of the container element in the HUD UXML that holds the labels.")]
    private string containerName = "world-labels";
    #endregion

    #region Private Fields
    private readonly List<WorldLabel> labels = new List<WorldLabel>();
    private UIDocument document;
    private VisualElement container;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        document = GetComponent<UIDocument>();
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (worldCamera == null || !TryResolveContainer())
            return;

        IPanel panel = container.panel;

        for (int i = 0; i < labels.Count; i++)
        {
            WorldLabel label = labels[i];
            Label element = label.Element;
            Vector3 worldPosition = label.transform.position;
            bool isInFront = worldCamera.WorldToViewportPoint(worldPosition).z > 0f;

            element.style.display = isInFront ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isInFront)
                continue;

            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPosition, worldCamera);
            element.style.left = panelPosition.x;
            element.style.top = panelPosition.y;

            float scale = label.ScaleFactor;
            element.style.scale = new Scale(new Vector2(scale, scale));
        }
    }
    #endregion

    #region Public Methods
    public void Add(WorldLabel label)
    {
        if (label == null || labels.Contains(label))
            return;

        labels.Add(label);
        if (container != null)
            container.Add(label.Element);
    }

    public void Remove(WorldLabel label)
    {
        if (label == null || !labels.Remove(label))
            return;

        label.Element.RemoveFromHierarchy();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Finds the container once the UIDocument has built its tree, and again if the tree was rebuilt (live reload),
    /// re-parenting every registered element into it.
    /// </summary>
    private bool TryResolveContainer()
    {
        if (container != null && container.panel != null)
            return true;

        VisualElement root = document.rootVisualElement;
        if (root == null)
            return false;

        container = root.Q<VisualElement>(containerName);
        if (container == null)
        {
            Debug.LogWarning($"HUD has no '{containerName}' element; world labels will not be drawn.", this);
            enabled = false;
            return false;
        }

        for (int i = 0; i < labels.Count; i++)
            container.Add(labels[i].Element);

        return true;
    }
    #endregion
}
