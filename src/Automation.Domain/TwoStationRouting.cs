using System.Collections.Immutable;

namespace Automation.Domain;

public enum DishRoutingStationId
{
    MainDishRoom,
    PatioServiceStation,
}

public sealed record DishRoutingStationProfile(
    DishRoutingStationId Id,
    string DisplayName,
    DishCounts InitialDirty,
    DishKind DemandKind,
    ProcessRoutingPolicy InitialPolicy)
{
    public DishRoutingStationProfile Validate()
    {
        if (!Enum.IsDefined(Id)) throw new ArgumentOutOfRangeException(nameof(Id));
        if (string.IsNullOrWhiteSpace(DisplayName)) throw new ArgumentException("Routing station display name is required.", nameof(DisplayName));
        if (InitialDirty.Plates < 0 || InitialDirty.Glasses < 0 || InitialDirty.Trays < 0)
            throw new ArgumentOutOfRangeException(nameof(InitialDirty), "Initial dish counts cannot be negative.");
        if (InitialDirty.Plates + InitialDirty.Glasses == 0)
            throw new ArgumentException("A routing station needs initial plate or glass work.", nameof(InitialDirty));
        if (DemandKind is not (DishKind.Plate or DishKind.Glass))
            throw new ArgumentOutOfRangeException(nameof(DemandKind), "The two-station episode currently supports plate or glass service demand.");
        if (!Enum.IsDefined(InitialPolicy)) throw new ArgumentOutOfRangeException(nameof(InitialPolicy));
        return this with { DisplayName = DisplayName.Trim() };
    }
}

public sealed record TwoStationRoutingConfiguration(
    DishStationScenarioConfiguration BaseScenario,
    ImmutableArray<DishRoutingStationProfile> Stations,
    int TrialHorizonTicks)
{
    public TwoStationRoutingConfiguration Validate()
    {
        ArgumentNullException.ThrowIfNull(BaseScenario);
        BaseScenario.Validate();
        if (Stations.IsDefaultOrEmpty || Stations.Length != 2)
            throw new ArgumentException("The episode requires exactly two routing stations.", nameof(Stations));
        var validated = Stations.Select(station => station.Validate()).ToImmutableArray();
        if (validated.Select(station => station.Id).Distinct().Count() != validated.Length)
            throw new ArgumentException("Routing station IDs must be unique.", nameof(Stations));
        if (!validated.Any(station => station.Id == DishRoutingStationId.MainDishRoom) ||
            !validated.Any(station => station.Id == DishRoutingStationId.PatioServiceStation))
            throw new ArgumentException("The main dish room and patio service station are both required.", nameof(Stations));
        if (TrialHorizonTicks is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(TrialHorizonTicks));
        return this with { BaseScenario = BaseScenario.Validate(), Stations = validated };
    }
}
