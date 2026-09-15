using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Persists the player's progress — wallet balance, the machine in every slot, machines bought and upgrade levels —
/// as JSON under <see cref="Application.persistentDataPath"/> and restores it on start-up. Nothing in the production
/// line is saved: pieces on the belt, boxes on the counter and the truck load restart empty. Any change marks the
/// save dirty; writes are batched to one per <c>writeInterval</c> and flushed when the app pauses, loses focus or
/// quits. Runs its Start after every other system so the defaults they set up can be overwritten.
/// </summary>
[DefaultExecutionOrder(100)]
public class SaveManager : MonoBehaviour
{
    #region Private Serialized Fields
    [SerializeField, Tooltip("Wallet whose balance is saved.")]
    private Wallet wallet;

    [SerializeField, Tooltip("Machine line whose slots and purchase count are saved.")]
    private GrinderLine grinderLine;

    [SerializeField, Tooltip("Upgrades whose levels are saved.")]
    private UpgradeManager upgrades;

    [SerializeField, Tooltip("File name inside Application.persistentDataPath.")]
    private string fileName = "save.json";

    [SerializeField, Tooltip("Restore the save file on start. Untick to always start fresh, e.g. while tuning the " +
        "economy; the file is still written.")]
    private bool loadOnStart = true;

    [SerializeField, Min(0f), Tooltip("Minimum seconds between two writes; changes in between land in one write.")]
    private float writeInterval = 1f;
    #endregion

    #region Private Fields
    private const string TempSuffix = ".tmp";

    private bool isDirty;
    private float nextWriteTime;
    #endregion

    #region Public Properties
    public string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    public bool HasSaveFile => File.Exists(FilePath);
    #endregion

    #region Events
    /// <summary>Raised after a save file has been restored into the game systems.</summary>
    public event Action<SaveManager> OnLoaded;

    /// <summary>Raised after the save file has been written.</summary>
    public event Action<SaveManager> OnSaved;
    #endregion

    #region MonoBehaviour Lifecycle
    private void OnEnable()
    {
        wallet.OnBalanceChanged += HandleBalanceChanged;
        grinderLine.OnChanged += HandleLineChanged;
        upgrades.OnChanged += HandleUpgradesChanged;
    }

    private void Start()
    {
        if (loadOnStart)
            Load();
    }

    private void Update()
    {
        if (isDirty && Time.unscaledTime >= nextWriteTime)
            Save();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.OnBalanceChanged -= HandleBalanceChanged;

        if (grinderLine != null)
            grinderLine.OnChanged -= HandleLineChanged;

        if (upgrades != null)
            upgrades.OnChanged -= HandleUpgradesChanged;
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            SaveIfDirty();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveIfDirty();
    }

    private void OnApplicationQuit() => SaveIfDirty();
    #endregion

    #region Public Methods
    /// <summary>Writes the current progress to disk right away.</summary>
    public void Save()
    {
        isDirty = false;
        nextWriteTime = Time.unscaledTime + writeInterval;

        if (WriteFile(Capture()))
            OnSaved?.Invoke(this);
    }

    /// <summary>Restores the save file into the game systems; false when there is none or it is unreadable.</summary>
    public bool Load()
    {
        SaveData data = ReadFile();
        if (data == null)
            return false;

        Apply(data);
        // Restoring raises every change event; the file already holds this state, so don't write it straight back.
        isDirty = false;
        OnLoaded?.Invoke(this);
        return true;
    }

    /// <summary>Deletes the save file so the next start is a fresh game. Nothing in the running game changes.</summary>
    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        isDirty = false;

        try
        {
            File.Delete(FilePath);
            File.Delete(FilePath + TempSuffix);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not delete save file '{FilePath}': {e.Message}", this);
        }
    }
    #endregion

    #region Private Methods
    private void HandleBalanceChanged(double balance) => isDirty = true;

    private void HandleLineChanged(GrinderLine line) => isDirty = true;

    private void HandleUpgradesChanged(UpgradeManager manager) => isDirty = true;

    private void SaveIfDirty()
    {
        if (isDirty)
            Save();
    }

    private SaveData Capture()
    {
        return new SaveData
        {
            SavedAtUtc = DateTime.UtcNow.ToString("o"),
            Balance = wallet.Balance,
            MachinesBought = grinderLine.MachinesBought,
            MachineLevels = grinderLine.GetMachineLevels(),
            UpgradeLevels = upgrades.GetLevels(),
        };
    }

    private void Apply(SaveData data)
    {
        // Upgrades first so machines restored below pick up their multipliers as they spawn.
        upgrades.RestoreLevels(data.UpgradeLevels);
        grinderLine.RestoreMachines(data.MachineLevels, data.MachinesBought);
        wallet.SetBalance(data.Balance);
    }

    /// <summary>
    /// Writes to a temp file and swaps it in, so a kill mid-write leaves either the old file or a complete new one.
    /// </summary>
    private bool WriteFile(SaveData data)
    {
        string path = FilePath;
        string tempPath = path + TempSuffix;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
            File.Delete(path);
            File.Move(tempPath, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not write save file '{path}': {e.Message}", this);
            return false;
        }
    }

    private SaveData ReadFile()
    {
        string path = FilePath;
        string tempPath = path + TempSuffix;

        try
        {
            // A leftover temp file with no main file means the swap was interrupted after the write completed.
            if (!File.Exists(path) && File.Exists(tempPath))
                File.Move(tempPath, path);

            if (!File.Exists(path))
                return null;

            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null || data.Version > SaveData.CurrentVersion)
            {
                Debug.LogWarning($"Ignoring save file '{path}': unreadable or newer than this build.", this);
                return null;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not read save file '{path}': {e.Message}", this);
            return null;
        }
    }
    #endregion
}
