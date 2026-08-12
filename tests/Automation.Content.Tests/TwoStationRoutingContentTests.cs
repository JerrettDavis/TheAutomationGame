using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class TwoStationRoutingContentTests
{
    [Fact]
    public void ProductionAuthorsTwoConcreteStationsInTheSamePolicyDecisionSlot()
    {
        var configuration = DishStationTwoStationsContent.Configuration;

        Assert.Equal(5, configuration.TrialHorizonTicks);
        Assert.Equal(2, configuration.Stations.Length);
        var main = configuration.Stations.Single(station => station.Id == DishRoutingStationId.MainDishRoom);
        var patio = configuration.Stations.Single(station => station.Id == DishRoutingStationId.PatioServiceStation);
        Assert.Equal(DishKind.Glass, main.DemandKind);
        Assert.Equal(DishKind.Plate, patio.DemandKind);
        Assert.Equal(ProcessRoutingPolicy.GlassesFirst, main.InitialPolicy);
        Assert.Equal(ProcessRoutingPolicy.GlassesFirst, patio.InitialPolicy);
        Assert.Equal("TWO STATIONS, ONE PROBLEM", DishStationTwoStationsContent.Quest.Narrative!.Title);
        Assert.DoesNotContain("Strategy", DishStationTwoStationsContent.Quest.Narrative.Discovery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidStationPolicyFailsAtItsAuthoredSemanticPath()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift).Replace(
            "          initial_policy: glasses-first",
            "          initial_policy: fastest-first",
            StringComparison.Ordinal);

        var failure = Assert.Throws<ContentCompilationException>(() =>
            ContentCompilerV1.Compile(yaml, "invalid-routing-policy.yaml"));

        Assert.Contains(failure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("two_station_routing.stations", StringComparison.Ordinal) &&
            diagnostic.Path.EndsWith("initial_policy", StringComparison.Ordinal));
    }

    [Fact]
    public void TwoStationConfigurationChangesParticipateInTheDeterministicManifest()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var changed = yaml.Replace("      trial_horizon_ticks: 5", "      trial_horizon_ticks: 6", StringComparison.Ordinal);

        var baseline = ContentCompilerV1.Compile(yaml, "baseline.yaml");
        var modified = ContentCompilerV1.Compile(changed, "modified.yaml");

        Assert.NotEqual(baseline.Manifest.Sha256, modified.Manifest.Sha256);
        Assert.Equal(6, modified.Scenarios.Single(scenario =>
            scenario.Id.Value == DishStationTwoStationsContent.ScenarioId).TwoStationRouting!.TrialHorizonTicks);
    }
}
