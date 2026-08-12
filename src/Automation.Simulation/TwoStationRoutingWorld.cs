using Automation.Domain;

namespace Automation.Simulation;

public interface ITwoStationRoutingCommand
{
    SimulationTick ExecuteAtTick { get; }
}

public sealed record SetRoutingStationPolicyCommand(
    SimulationTick ExecuteAtTick,
    DishRoutingStationId Station,
    ProcessRoutingPolicy Policy) : ITwoStationRoutingCommand;

public sealed record CopyRoutingStationPolicyCommand(
    SimulationTick ExecuteAtTick,
    DishRoutingStationId Source,
    DishRoutingStationId Destination) : ITwoStationRoutingCommand;

public sealed record RunTwoStationRoutingTrialCommand(
    SimulationTick ExecuteAtTick) : ITwoStationRoutingCommand;

public sealed record RoutingStationTrialResult(
    DishRoutingStationId Station,
    string DisplayName,
    DishKind DemandKind,
    ProcessRoutingPolicy Policy,
    int CompletedDishes,
    int ServiceShortages,
    int WorkerActions,
    int WorkerTravelSteps,
    int ThroughputValue,
    int TotalCost,
    int NetValue);

public sealed record TwoStationRoutingTrialResult(
    int Sequence,
    int Seed,
    int HorizonTicks,
    IReadOnlyList<RoutingStationTrialResult> Stations)
{
    public int TotalCompleted => Stations.Sum(station => station.CompletedDishes);
    public int TotalShortages => Stations.Sum(station => station.ServiceShortages);
    public int TotalNetValue => Stations.Sum(station => station.NetValue);
}

public sealed record TwoStationRoutingSnapshot(
    SimulationTick Tick,
    IReadOnlyDictionary<DishRoutingStationId, ProcessRoutingPolicy> Policies,
    IReadOnlyList<TwoStationRoutingTrialResult> Trials,
    int CopyCount)
{
    public TwoStationRoutingTrialResult? LatestTrial => Trials.LastOrDefault();
    public ProcessRoutingPolicy PolicyFor(DishRoutingStationId station) => Policies[station];
}

public enum RecordedTwoStationRoutingCommandKind
{
    SetPolicy,
    CopyPolicy,
    RunTrial,
}

public sealed record RecordedTwoStationRoutingCommand(
    RecordedTwoStationRoutingCommandKind Kind,
    long ExecuteAtTick,
    DishRoutingStationId Station = default,
    DishRoutingStationId Source = default,
    DishRoutingStationId Destination = default,
    ProcessRoutingPolicy Policy = default)
{
    public static RecordedTwoStationRoutingCommand FromCommand(ITwoStationRoutingCommand command) => command switch
    {
        SetRoutingStationPolicyCommand value => new(RecordedTwoStationRoutingCommandKind.SetPolicy,
            value.ExecuteAtTick.Value, Station: value.Station, Policy: value.Policy),
        CopyRoutingStationPolicyCommand value => new(RecordedTwoStationRoutingCommandKind.CopyPolicy,
            value.ExecuteAtTick.Value, Source: value.Source, Destination: value.Destination),
        RunTwoStationRoutingTrialCommand value => new(RecordedTwoStationRoutingCommandKind.RunTrial,
            value.ExecuteAtTick.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(command), command.GetType().Name, "Unsupported two-station routing command."),
    };

    public ITwoStationRoutingCommand ToCommand() => Kind switch
    {
        RecordedTwoStationRoutingCommandKind.SetPolicy => new SetRoutingStationPolicyCommand(new(ExecuteAtTick), Station, Policy),
        RecordedTwoStationRoutingCommandKind.CopyPolicy => new CopyRoutingStationPolicyCommand(new(ExecuteAtTick), Source, Destination),
        RecordedTwoStationRoutingCommandKind.RunTrial => new RunTwoStationRoutingTrialCommand(new(ExecuteAtTick)),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
}

public sealed record TwoStationRoutingReplaySave(
    int SchemaVersion,
    int Seed,
    TwoStationRoutingConfiguration Configuration,
    RecordedTwoStationRoutingCommand[] Commands)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed class TwoStationRoutingWorld
{
    private readonly Dictionary<DishRoutingStationId, ProcessRoutingPolicy> policies;
    private readonly List<TwoStationRoutingTrialResult> trials = new(4);
    private readonly List<RecordedTwoStationRoutingCommand> commandJournal = new(8);
    private int copyCount;
    private bool replaying;

    public TwoStationRoutingWorld(int seed, TwoStationRoutingConfiguration configuration)
    {
        Seed = seed;
        Configuration = (configuration ?? throw new ArgumentNullException(nameof(configuration))).Validate();
        policies = Configuration.Stations.ToDictionary(station => station.Id, station => station.InitialPolicy);
    }

    public int Seed { get; }
    public TwoStationRoutingConfiguration Configuration { get; }
    public SimulationTick Tick { get; private set; }

    public CommandResult ExecuteNow(ITwoStationRoutingCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ExecuteAtTick != Tick)
            return CommandResult.Rejected($"Two-station command expected tick {Tick.Value}, not {command.ExecuteAtTick.Value}.");

        var result = command switch
        {
            SetRoutingStationPolicyCommand set => SetPolicy(set.Station, set.Policy),
            CopyRoutingStationPolicyCommand copy => CopyPolicy(copy.Source, copy.Destination),
            RunTwoStationRoutingTrialCommand => RunTrial(),
            _ => CommandResult.Rejected($"Unknown two-station command {command.GetType().Name}."),
        };
        if (!result.Success) return result;
        if (!replaying) commandJournal.Add(RecordedTwoStationRoutingCommand.FromCommand(command));
        Tick += 1;
        return result;
    }

    public TwoStationRoutingSnapshot Snapshot() => new(
        Tick,
        policies.ToDictionary(pair => pair.Key, pair => pair.Value),
        trials.ToArray(),
        copyCount);

    public TwoStationRoutingReplaySave CreateReplaySave() => new(
        TwoStationRoutingReplaySave.CurrentSchemaVersion,
        Seed,
        Configuration,
        commandJournal.ToArray());

    public static TwoStationRoutingWorld Restore(TwoStationRoutingReplaySave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.SchemaVersion != TwoStationRoutingReplaySave.CurrentSchemaVersion)
            throw new NotSupportedException($"Two-station replay schema {save.SchemaVersion} is not supported.");
        var world = new TwoStationRoutingWorld(save.Seed, save.Configuration) { replaying = true };
        foreach (var recorded in save.Commands)
        {
            var result = world.ExecuteNow(recorded.ToCommand());
            if (!result.Success) throw new InvalidOperationException($"Two-station replay command was rejected: {result.Message}");
        }
        world.replaying = false;
        world.commandJournal.AddRange(save.Commands);
        return world;
    }

    private CommandResult SetPolicy(DishRoutingStationId station, ProcessRoutingPolicy policy)
    {
        if (!policies.ContainsKey(station)) return CommandResult.Rejected($"Routing station {station} does not exist.");
        if (!Enum.IsDefined(policy)) return CommandResult.Rejected("Unknown routing policy.");
        policies[station] = policy;
        return CommandResult.Accepted($"{Profile(station).DisplayName} routing set to {policy}.");
    }

    private CommandResult CopyPolicy(DishRoutingStationId source, DishRoutingStationId destination)
    {
        if (!policies.ContainsKey(source)) return CommandResult.Rejected($"Routing station {source} does not exist.");
        if (!policies.ContainsKey(destination)) return CommandResult.Rejected($"Routing station {destination} does not exist.");
        if (source == destination) return CommandResult.Rejected("Choose a different destination station.");
        policies[destination] = policies[source];
        copyCount++;
        return CommandResult.Accepted($"Copied {Profile(source).DisplayName} routing to {Profile(destination).DisplayName}.");
    }

    private CommandResult RunTrial()
    {
        var results = Configuration.Stations
            .Select(profile => RunStation(profile, policies[profile.Id]))
            .ToArray();
        trials.Add(new(trials.Count + 1, Seed, Configuration.TrialHorizonTicks, results));
        return CommandResult.Accepted($"Routing trial {trials.Count} completed with {results.Sum(result => result.ServiceShortages)} shortages.");
    }

    private RoutingStationTrialResult RunStation(DishRoutingStationProfile profile, ProcessRoutingPolicy policy)
    {
        var scenario = (Configuration.BaseScenario with
        {
            InitialDirty = profile.InitialDirty,
            InitialAvailable = new(0, 0, 0),
            InitialRushEnabled = true,
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.FullyDocumented,
            InitialNewHireRoutingPolicy = policy,
            InitialAutomationPolicy = WasherAutomationPolicy.Off,
            DemandKind = profile.DemandKind,
        }).Validate();
        var world = new DishStationWorld(StationSeed(profile.Id), scenario);
        for (var tick = 0; tick < Configuration.TrialHorizonTicks; tick++) world.Advance();
        var snapshot = world.Snapshot();
        return new(profile.Id, profile.DisplayName, profile.DemandKind, policy,
            snapshot.Completed, snapshot.ServiceShortages, snapshot.Economy.WorkerActions,
            snapshot.Layout.NewHireTravelSteps, snapshot.Economy.ThroughputValue,
            snapshot.Economy.TotalCost, snapshot.Economy.NetValue);
    }

    private int StationSeed(DishRoutingStationId station) => unchecked(Seed * 397 ^ (int)station * 7919);
    private DishRoutingStationProfile Profile(DishRoutingStationId station) =>
        Configuration.Stations.Single(profile => profile.Id == station);
}
