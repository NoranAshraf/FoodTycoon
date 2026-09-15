using UnityEngine;

/// <summary>
/// Applies the device-facing runtime settings once at start-up. Android players default to 30 fps when no target is
/// set; the belt and box animations need 60 to look smooth.
/// </summary>
public class PerformanceSettings : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Min(-1), Tooltip("Frames per second the player aims for; -1 uses the platform default (30 on Android).")]
    private int targetFrameRate = 60;
    #endregion

    #region MonoBehaviour Lifecycle
    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
    }
    #endregion
}
