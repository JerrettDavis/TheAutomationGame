using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Client.Stride;

public sealed record RoutingStationView(
    DishRoutingStationId Id,
    string Name,
    string Demand,
    string Policy,
    bool Selected,
    int? Completed,
    int? Shortages,
    int? NetValue);

public sealed record TwoStationRoutingView(
    string Title,
    string Situation,
    IReadOnlyList<RoutingStationView> Stations,
    int TrialCount,
    int CopyCount,
    int? TotalCompleted,
    int? TotalShortages,
    int? TotalNetValue,
    bool OutcomeMet,
    string Evidence,
    string Discovery);

public static class TwoStationRoutingPresenter
{
    public static TwoStationRoutingView Present(
        TwoStationRoutingConfiguration configuration,
        TwoStationRoutingSnapshot snapshot,
        QuestContentDefinition quest,
        int selectedStation)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(quest);
        var latest = snapshot.LatestTrial;
        var stations = configuration.Stations.Select((profile, index) =>
        {
            var result = latest?.Stations.Single(station => station.Station == profile.Id);
            return new RoutingStationView(profile.Id, profile.DisplayName, profile.DemandKind.ToString().ToUpperInvariant(),
                PolicyLabel(snapshot.PolicyFor(profile.Id)), index == selectedStation, result?.CompletedDishes,
                result?.ServiceShortages, result?.NetValue);
        }).ToArray();
        var outcomeMet = latest is { TotalShortages: 0 } && snapshot.Policies.Values.Distinct().Count() > 1;
        var evidence = latest is null
            ? "Run both stations to expose the consequence of the current choices."
            : $"Trial {latest.Sequence}: {latest.TotalCompleted} completed, {latest.TotalShortages} shortages, net {latest.TotalNetValue}.";
        return new(quest.Narrative?.Title ?? "TWO STATIONS, ONE PROBLEM",
            quest.Narrative?.Situation ?? quest.Objective, stations, snapshot.Trials.Count, snapshot.CopyCount,
            latest?.TotalCompleted, latest?.TotalShortages, latest?.TotalNetValue, outcomeMet, evidence,
            outcomeMet ? quest.Narrative?.Discovery ?? "Each station needs a choice fitted to its demand." : "");
    }

    public static string PolicyLabel(ProcessRoutingPolicy policy) => policy switch
    {
        ProcessRoutingPolicy.CapturedOrder => "CAPTURED ORDER",
        ProcessRoutingPolicy.PlatesFirst => "PLATES FIRST",
        ProcessRoutingPolicy.GlassesFirst => "GLASSES FIRST",
        _ => throw new ArgumentOutOfRangeException(nameof(policy)),
    };
}
