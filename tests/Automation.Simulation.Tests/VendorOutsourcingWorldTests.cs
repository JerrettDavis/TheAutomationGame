using System.Collections.Immutable;
using System.Text.Json;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class VendorOutsourcingWorldTests
{
    [Fact]
    public void SameIncidentMakesAllThreeChoicesViableWithDistinctRisks()
    {
        var world = World();
        foreach (var proposal in Enum.GetValues<VendorProposalId>())
        {
            Assert.True(world.ExecuteNow(new SelectVendorProposalCommand(world.Tick, proposal)).Success);
            Assert.True(world.ExecuteNow(new RunVendorProposalTrialCommand(world.Tick)).Success);
        }

        var trials = world.Snapshot().Trials;
        Assert.Equal(3, world.Snapshot().ComparedProposalCount);
        Assert.All(trials, trial => Assert.True(trial.Viable));
        var build = trials.Single(item => item.Proposal == VendorProposalId.BuildInHouse);
        var managed = trials.Single(item => item.Proposal == VendorProposalId.ManagedVendor);
        var observable = trials.Single(item => item.Proposal == VendorProposalId.ObservableVendor);
        Assert.True(managed.NormalNetValue > observable.NormalNetValue);
        Assert.True(observable.NormalNetValue > build.NormalNetValue);
        Assert.Equal(1, build.RequestsMissed);
        Assert.Equal(4, managed.RequestsMissed);
        Assert.Equal(0, observable.RequestsMissed);
        Assert.Equal(2, observable.FallbackRequests);
        Assert.True(observable.IncidentNetValue > build.IncidentNetValue);
        Assert.True(build.IncidentNetValue > managed.IncidentNetValue);
    }

    [Fact]
    public void ContractBoundaryControlsWhatOperatorsSeeAndHowServiceRecovers()
    {
        var managed = Run(VendorProposalId.ManagedVendor);
        var observable = Run(VendorProposalId.ObservableVendor);

        Assert.False(managed.TraceAvailable);
        Assert.Equal(VendorKnowledgeOwner.VendorOnly, managed.KnowledgeOwner);
        Assert.Contains(managed.Trace, entry => entry.Observable.Contains("cannot see", StringComparison.Ordinal));
        Assert.DoesNotContain(managed.Trace, entry => entry.Phase == VendorIncidentPhase.ManualFallback);
        Assert.True(observable.TraceAvailable);
        Assert.Equal(VendorKnowledgeOwner.Shared, observable.KnowledgeOwner);
        Assert.Contains(observable.Trace, entry => entry.Observable.Contains("exception", StringComparison.Ordinal));
        Assert.Contains(observable.Trace, entry => entry.Phase == VendorIncidentPhase.ManualFallback);
        Assert.True(observable.SupportResponseTicks < managed.SupportResponseTicks);
    }

    [Fact]
    public void InvalidCommandsDoNotChangeVendorState()
    {
        var world = World();
        var before = Json(world.Snapshot());

        Assert.False(world.ExecuteNow(new SelectVendorProposalCommand(world.Tick, (VendorProposalId)999)).Success);
        Assert.False(world.ExecuteNow(new RunVendorProposalTrialCommand(new(3))).Success);

        Assert.Equal(before, Json(world.Snapshot()));
    }

    [Fact]
    public void ReplayRestoresProposalComparisonAndCausalTraceExactly()
    {
        var original = World();
        original.ExecuteNow(new SelectVendorProposalCommand(original.Tick, VendorProposalId.ManagedVendor));
        original.ExecuteNow(new RunVendorProposalTrialCommand(original.Tick));
        original.ExecuteNow(new SelectVendorProposalCommand(original.Tick, VendorProposalId.ObservableVendor));
        original.ExecuteNow(new RunVendorProposalTrialCommand(original.Tick));

        var restored = VendorOutsourcingWorld.Restore(original.CreateReplaySave());

        Assert.Equal(Json(original.Snapshot()), Json(restored.Snapshot()));
        Assert.Equal(Json(original.CreateReplaySave()), Json(restored.CreateReplaySave()));
    }

    private static VendorProposalTrialResult Run(VendorProposalId proposal)
    {
        var world = World();
        world.ExecuteNow(new SelectVendorProposalCommand(world.Tick, proposal));
        world.ExecuteNow(new RunVendorProposalTrialCommand(world.Tick));
        return Assert.Single(world.Snapshot().Trials);
    }

    private static VendorOutsourcingWorld World() => new(Configuration());

    private static VendorOutsourcingConfiguration Configuration() => new(
        ImmutableArray.Create(
            new VendorProposalConfiguration(VendorProposalId.BuildInHouse, "Build in house",
                VendorSourcingMode.InternalBuild, VendorIntegrationBoundary.PlayerOwned, VendorKnowledgeOwner.RestaurantTeam,
                1, 220, 0, 60, true, false, 0),
            new VendorProposalConfiguration(VendorProposalId.ManagedVendor, "Sam's managed package",
                VendorSourcingMode.VendorPackage, VendorIntegrationBoundary.VendorManaged, VendorKnowledgeOwner.VendorOnly,
                4, 60, 40, 0, false, false, 0),
            new VendorProposalConfiguration(VendorProposalId.ObservableVendor, "Observable vendor package",
                VendorSourcingMode.VendorPackage, VendorIntegrationBoundary.PlayerOwnedAdapter, VendorKnowledgeOwner.Shared,
                2, 140, 70, 0, true, true, 20)),
        8, 3, 80, 30, "exception", "special");

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
