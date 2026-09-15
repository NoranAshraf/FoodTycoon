/// <summary>
/// Gameplay values an <see cref="UpgradeDefinition"/> can scale. Every member is read by exactly the system it names
/// through <see cref="UpgradeManager.GetMultiplier"/>; add a member here (and a reader) for each new kind of effect.
/// </summary>
public enum UpgradeStat
{
    ConveyorSpeed,
    GrindingSpeed,
    GrinderMoney,
    LoadingSpeed,
}
