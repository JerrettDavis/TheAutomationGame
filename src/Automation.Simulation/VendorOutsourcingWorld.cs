using Automation.Domain;

namespace Automation.Simulation;

public interface IVendorOutsourcingCommand
{
    SimulationTick ExecuteAtTick { get; }
}

public sealed record SelectVendorProposalCommand(
    SimulationTick ExecuteAtTick,
    VendorProposalId Proposal) : IVendorOutsourcingCommand;

public sealed record RunVendorProposalTrialCommand(
    SimulationTick ExecuteAtTick) : IVendorOutsourcingCommand;

public enum VendorIncidentPhase
{
    BoundaryMismatch,
    ResponseStarted,
    ManualFallback,
    RootCauseExplained,
    ServiceRestored,
}

public sealed record VendorIncidentTraceEntry(
    int Tick,
    VendorIncidentPhase Phase,
    string Observable,
    VendorKnowledgeOwner KnowledgeOwner);

public sealed record VendorProposalTrialResult(
    int Sequence,
    VendorProposalId Proposal,
    string DisplayName,
    VendorSourcingMode Sourcing,
    VendorIntegrationBoundary Boundary,
    VendorKnowledgeOwner KnowledgeOwner,
    int HorizonTicks,
    int IncidentAtTick,
    int SupportResponseTicks,
    bool TraceAvailable,
    bool ManualFallbackAvailable,
    int NormalCost,
    int NormalNetValue,
    int RequestsHandled,
    int RequestsMissed,
    int FallbackRequests,
    int ThroughputValue,
    int ShortageCost,
    int FallbackLaborCost,
    int IncidentTotalCost,
    int IncidentNetValue,
    IReadOnlyList<VendorIncidentTraceEntry> Trace)
{
    public bool Viable => RequestsHandled > 0 && IncidentNetValue > 0;
}

public sealed record VendorOutsourcingSnapshot(
    SimulationTick Tick,
    VendorProposalId SelectedProposal,
    IReadOnlyList<VendorProposalTrialResult> Trials)
{
    public VendorProposalTrialResult? LatestTrial => Trials.LastOrDefault();
    public int ComparedProposalCount => Trials.Select(trial => trial.Proposal).Distinct().Count();
}

public enum RecordedVendorCommandKind
{
    SelectProposal,
    RunTrial,
}

public sealed record RecordedVendorCommand(
    RecordedVendorCommandKind Kind,
    long ExecuteAtTick,
    VendorProposalId Proposal = default)
{
    public static RecordedVendorCommand FromCommand(IVendorOutsourcingCommand command) => command switch
    {
        SelectVendorProposalCommand value => new(RecordedVendorCommandKind.SelectProposal,
            value.ExecuteAtTick.Value, value.Proposal),
        RunVendorProposalTrialCommand value => new(RecordedVendorCommandKind.RunTrial, value.ExecuteAtTick.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(command), command.GetType().Name,
            "Unsupported vendor outsourcing command."),
    };

    public IVendorOutsourcingCommand ToCommand() => Kind switch
    {
        RecordedVendorCommandKind.SelectProposal => new SelectVendorProposalCommand(new(ExecuteAtTick), Proposal),
        RecordedVendorCommandKind.RunTrial => new RunVendorProposalTrialCommand(new(ExecuteAtTick)),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
}

public sealed record VendorOutsourcingReplaySave(
    int SchemaVersion,
    VendorOutsourcingConfiguration Configuration,
    RecordedVendorCommand[] Commands)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed class VendorOutsourcingWorld
{
    private readonly List<VendorProposalTrialResult> trials = new(6);
    private readonly List<RecordedVendorCommand> commandJournal = new(12);
    private bool replaying;

    public VendorOutsourcingWorld(VendorOutsourcingConfiguration configuration)
    {
        Configuration = (configuration ?? throw new ArgumentNullException(nameof(configuration))).Validate();
        SelectedProposal = VendorProposalId.BuildInHouse;
    }

    public VendorOutsourcingConfiguration Configuration { get; }
    public VendorProposalId SelectedProposal { get; private set; }
    public SimulationTick Tick { get; private set; }

    public CommandResult ExecuteNow(IVendorOutsourcingCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ExecuteAtTick != Tick)
            return CommandResult.Rejected($"Vendor command expected tick {Tick.Value}, not {command.ExecuteAtTick.Value}.");
        var result = command switch
        {
            SelectVendorProposalCommand select => SelectProposal(select.Proposal),
            RunVendorProposalTrialCommand => RunTrial(),
            _ => CommandResult.Rejected($"Unknown vendor command {command.GetType().Name}."),
        };
        if (!result.Success) return result;
        if (!replaying) commandJournal.Add(RecordedVendorCommand.FromCommand(command));
        Tick += 1;
        return result;
    }

    public VendorOutsourcingSnapshot Snapshot() => new(Tick, SelectedProposal, trials.ToArray());

    public VendorOutsourcingReplaySave CreateReplaySave() => new(
        VendorOutsourcingReplaySave.CurrentSchemaVersion, Configuration, commandJournal.ToArray());

    public static VendorOutsourcingWorld Restore(VendorOutsourcingReplaySave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.SchemaVersion != VendorOutsourcingReplaySave.CurrentSchemaVersion)
            throw new NotSupportedException($"Vendor replay schema {save.SchemaVersion} is not supported.");
        var world = new VendorOutsourcingWorld(save.Configuration) { replaying = true };
        foreach (var recorded in save.Commands)
        {
            var result = world.ExecuteNow(recorded.ToCommand());
            if (!result.Success) throw new InvalidOperationException($"Vendor replay command was rejected: {result.Message}");
        }
        world.replaying = false;
        world.commandJournal.AddRange(save.Commands);
        return world;
    }

    private CommandResult SelectProposal(VendorProposalId proposal)
    {
        if (!Enum.IsDefined(proposal) || !Configuration.Proposals.Any(item => item.Id == proposal))
            return CommandResult.Rejected("Unknown vendor proposal.");
        SelectedProposal = proposal;
        return CommandResult.Accepted($"Selected {Terms(proposal).DisplayName} for review.");
    }

    private CommandResult RunTrial()
    {
        var terms = Terms(SelectedProposal);
        var recoveryTick = Configuration.IncidentAtTick + terms.SupportResponseTicks;
        var unavailableRequests = Math.Min(terms.SupportResponseTicks,
            Configuration.TrialHorizonTicks - Configuration.IncidentAtTick + 1);
        var fallbackRequests = terms.ManualFallbackAvailable ? unavailableRequests : 0;
        var missedRequests = terms.ManualFallbackAvailable ? 0 : unavailableRequests;
        var handledRequests = Configuration.TrialHorizonTicks - missedRequests;
        var normalCost = terms.SetupCost + terms.RecurringCost + terms.MaintenanceCost;
        var normalNet = Configuration.TrialHorizonTicks * Configuration.ServiceValuePerRequest - normalCost;
        var throughputValue = handledRequests * Configuration.ServiceValuePerRequest;
        var shortageCost = missedRequests * Configuration.ShortageCostPerRequest;
        var fallbackLaborCost = fallbackRequests * terms.FallbackLaborCostPerRequest;
        var totalCost = normalCost + shortageCost + fallbackLaborCost;
        var trace = BuildTrace(terms, recoveryTick, fallbackRequests);
        trials.Add(new(trials.Count + 1, terms.Id, terms.DisplayName, terms.Sourcing, terms.Boundary,
            terms.KnowledgeOwner, Configuration.TrialHorizonTicks, Configuration.IncidentAtTick,
            terms.SupportResponseTicks, terms.TraceAvailable, terms.ManualFallbackAvailable,
            normalCost, normalNet, handledRequests, missedRequests, fallbackRequests, throughputValue,
            shortageCost, fallbackLaborCost, totalCost, throughputValue - totalCost, trace));
        return CommandResult.Accepted($"{terms.DisplayName} trial handled {handledRequests} and missed {missedRequests} service requests.");
    }

    private IReadOnlyList<VendorIncidentTraceEntry> BuildTrace(
        VendorProposalConfiguration terms,
        int recoveryTick,
        int fallbackRequests)
    {
        var trace = new List<VendorIncidentTraceEntry>(5)
        {
            new(Configuration.IncidentAtTick, VendorIncidentPhase.BoundaryMismatch,
                terms.TraceAvailable
                    ? $"Local code '{Configuration.LocalRareTrayCode}' did not map to contracted vendor code '{Configuration.VendorRareTrayCode}'."
                    : "The managed package stopped at its external boundary; local operators cannot see the mapping decision.",
                terms.KnowledgeOwner),
            new(Configuration.IncidentAtTick, VendorIncidentPhase.ResponseStarted,
                terms.Sourcing == VendorSourcingMode.InternalBuild
                    ? "Restaurant maintenance began from the owned trace."
                    : $"Support response opened under the {terms.SupportResponseTicks}-tick contract.",
                terms.KnowledgeOwner),
        };
        if (fallbackRequests > 0)
            trace.Add(new(Configuration.IncidentAtTick, VendorIncidentPhase.ManualFallback,
                $"The player-owned adapter kept {fallbackRequests} requests moving through manual fallback.",
                VendorKnowledgeOwner.RestaurantTeam));
        trace.Add(new(recoveryTick, VendorIncidentPhase.RootCauseExplained,
            $"The '{Configuration.LocalRareTrayCode}' to '{Configuration.VendorRareTrayCode}' boundary mapping was made explicit.",
            terms.Sourcing == VendorSourcingMode.InternalBuild ? VendorKnowledgeOwner.RestaurantTeam : VendorKnowledgeOwner.Shared));
        trace.Add(new(recoveryTick, VendorIncidentPhase.ServiceRestored,
            "Automated routing resumed with the rare-tray mapping defined.",
            terms.Sourcing == VendorSourcingMode.InternalBuild ? VendorKnowledgeOwner.RestaurantTeam : VendorKnowledgeOwner.Shared));
        return trace;
    }

    private VendorProposalConfiguration Terms(VendorProposalId proposal) =>
        Configuration.Proposals.Single(item => item.Id == proposal);
}
