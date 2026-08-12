using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Client.Stride;

public sealed record VendorProposalCardView(
    VendorProposalId Id,
    string Title,
    bool Selected,
    string Source,
    string Boundary,
    string Contract,
    string Ownership,
    string NormalEconomy,
    string Risk,
    string? IncidentOutcome,
    bool? Viable);

public sealed record VendorComparisonView(
    string Title,
    string Speaker,
    string Pitch,
    string SharedIncident,
    IReadOnlyList<VendorProposalCardView> Proposals,
    IReadOnlyList<string> SelectedTrace,
    string Outcome);

public static class VendorComparisonPresenter
{
    public static VendorComparisonView Present(
        VendorOutsourcingConfiguration configuration,
        VendorOutsourcingSnapshot snapshot,
        QuestContentDefinition quest)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(quest);
        var cards = configuration.Proposals.Select(terms =>
        {
            var trial = snapshot.Trials.LastOrDefault(item => item.Proposal == terms.Id);
            return new VendorProposalCardView(
                terms.Id,
                terms.DisplayName.ToUpperInvariant(),
                snapshot.SelectedProposal == terms.Id,
                terms.Sourcing == VendorSourcingMode.InternalBuild ? "SOURCE  IN-HOUSE" : "SOURCE  SAM / PACKAGE",
                $"BOUNDARY  {Boundary(terms.Boundary)}",
                $"SLA {terms.SupportResponseTicks}T  TRACE {(terms.TraceAvailable ? "YES" : "NO")}  FALLBACK {(terms.ManualFallbackAvailable ? "YES" : "NO")}",
                $"KNOWLEDGE  {Knowledge(terms.KnowledgeOwner)}",
                $"NORMAL  COST {terms.SetupCost + terms.RecurringCost + terms.MaintenanceCost}  /  NET {configuration.TrialHorizonTicks * configuration.ServiceValuePerRequest - terms.SetupCost - terms.RecurringCost - terms.MaintenanceCost}",
                Risk(terms.Id),
                trial is null ? null : $"TRIAL  H{trial.RequestsHandled} M{trial.RequestsMissed}  COST {trial.IncidentTotalCost}  NET {trial.IncidentNetValue}",
                trial?.Viable);
        }).ToArray();
        var selectedTrial = snapshot.Trials.LastOrDefault(item => item.Proposal == snapshot.SelectedProposal);
        var trace = selectedTrial?.Trace.Select(entry =>
            $"T{entry.Tick}  {Phase(entry.Phase)}  •  {entry.Observable.ToUpperInvariant()}").ToArray() ?? [];
        var completed = snapshot.ComparedProposalCount >= 2;
        return new(
            quest.Narrative?.Title ?? "BUY THE BOX",
            "SAM RIVERA  •  VENDOR / INTEGRATOR",
            quest.Narrative?.Situation.ToUpperInvariant() ?? quest.Objective.ToUpperInvariant(),
            $"SHARED INCIDENT  •  LOCAL {configuration.LocalRareTrayCode.ToUpperInvariant()} MEETS CONTRACT {configuration.VendorRareTrayCode.ToUpperInvariant()} AT T{configuration.IncidentAtTick}",
            cards,
            trace,
            completed
                ? "COMPARISON COMPLETE  •  EACH VIABLE CHOICE MOVES COST, DOWNTIME, AND UNDERSTANDING DIFFERENTLY."
                : "RUN AT LEAST TWO PROPOSALS UNDER THE SAME INCIDENT. THERE IS NO UNIVERSAL CORRECT CONTRACT.");
    }

    private static string Boundary(VendorIntegrationBoundary value) => value switch
    {
        VendorIntegrationBoundary.PlayerOwned => "PLAYER OWNED",
        VendorIntegrationBoundary.VendorManaged => "VENDOR MANAGED",
        VendorIntegrationBoundary.PlayerOwnedAdapter => "PLAYER ADAPTER",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Knowledge(VendorKnowledgeOwner value) => value switch
    {
        VendorKnowledgeOwner.RestaurantTeam => "RESTAURANT TEAM",
        VendorKnowledgeOwner.VendorOnly => "VENDOR ONLY",
        VendorKnowledgeOwner.Shared => "SHARED",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Risk(VendorProposalId value) => value switch
    {
        VendorProposalId.BuildInHouse => "RISK  MAINTENANCE LOAD",
        VendorProposalId.ManagedVendor => "RISK  SUPPORT DELAY / VENDOR-ONLY TRACE",
        VendorProposalId.ObservableVendor => "RISK  CONTRACT + FALLBACK LABOR",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Phase(VendorIncidentPhase value) => value switch
    {
        VendorIncidentPhase.BoundaryMismatch => "BOUNDARY MISMATCH",
        VendorIncidentPhase.ResponseStarted => "RESPONSE STARTED",
        VendorIncidentPhase.ManualFallback => "MANUAL FALLBACK",
        VendorIncidentPhase.RootCauseExplained => "ROOT CAUSE",
        VendorIncidentPhase.ServiceRestored => "RESTORED",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
