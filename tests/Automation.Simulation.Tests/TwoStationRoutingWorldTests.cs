using System.Collections.Immutable;
using System.Text.Json;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class TwoStationRoutingWorldTests
{
    [Fact]
    public void SameDecisionSlotAcceptsDifferentPoliciesForTwoStations()
    {
        var world = World();

        Assert.True(world.ExecuteNow(new SetRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.MainDishRoom, ProcessRoutingPolicy.GlassesFirst)).Success);
        Assert.True(world.ExecuteNow(new SetRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst)).Success);
        Assert.True(world.ExecuteNow(new RunTwoStationRoutingTrialCommand(world.Tick)).Success);

        var trial = Assert.Single(world.Snapshot().Trials);
        Assert.Equal(ProcessRoutingPolicy.GlassesFirst,
            trial.Stations.Single(station => station.Station == DishRoutingStationId.MainDishRoom).Policy);
        Assert.Equal(ProcessRoutingPolicy.PlatesFirst,
            trial.Stations.Single(station => station.Station == DishRoutingStationId.PatioServiceStation).Policy);
        Assert.Equal(0, trial.TotalShortages);
        Assert.True(trial.TotalCompleted > 0);
    }

    [Fact]
    public void CopiedPolicyStartsSecondStationButRefittingItImprovesMeasuredOutcome()
    {
        var world = World();

        Assert.True(world.ExecuteNow(new CopyRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation)).Success);
        Assert.True(world.ExecuteNow(new RunTwoStationRoutingTrialCommand(world.Tick)).Success);
        var copied = world.Snapshot().LatestTrial!;

        Assert.True(world.ExecuteNow(new SetRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst)).Success);
        Assert.True(world.ExecuteNow(new RunTwoStationRoutingTrialCommand(world.Tick)).Success);
        var fitted = world.Snapshot().LatestTrial!;

        Assert.Equal(1, world.Snapshot().CopyCount);
        Assert.True(copied.TotalShortages > fitted.TotalShortages);
        Assert.Equal(0, fitted.TotalShortages);
        Assert.Equal(copied.Stations.Single(station => station.Station == DishRoutingStationId.MainDishRoom),
            fitted.Stations.Single(station => station.Station == DishRoutingStationId.MainDishRoom));
    }

    [Fact]
    public void InvalidPolicyCommandsDoNotMutateAuthoritativeState()
    {
        var world = World();
        var before = Json(world.Snapshot());

        var unknownPolicy = world.ExecuteNow(new SetRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.MainDishRoom, (ProcessRoutingPolicy)999));
        var sameStationCopy = world.ExecuteNow(new CopyRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.MainDishRoom));
        var wrongTick = world.ExecuteNow(new RunTwoStationRoutingTrialCommand(new(4)));

        Assert.False(unknownPolicy.Success);
        Assert.False(sameStationCopy.Success);
        Assert.False(wrongTick.Success);
        Assert.Equal(before, Json(world.Snapshot()));
    }

    [Fact]
    public void ReplayReconstructsPoliciesCopyHistoryAndTrialsExactly()
    {
        var original = World(17);
        original.ExecuteNow(new CopyRoutingStationPolicyCommand(original.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
        original.ExecuteNow(new RunTwoStationRoutingTrialCommand(original.Tick));
        original.ExecuteNow(new SetRoutingStationPolicyCommand(original.Tick,
            DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
        original.ExecuteNow(new RunTwoStationRoutingTrialCommand(original.Tick));

        var restored = TwoStationRoutingWorld.Restore(original.CreateReplaySave());

        Assert.Equal(Json(original.Snapshot()), Json(restored.Snapshot()));
        Assert.Equal(Json(original.CreateReplaySave()), Json(restored.CreateReplaySave()));
    }

    private static TwoStationRoutingWorld World(int seed = 42) => new(seed, Configuration());

    private static TwoStationRoutingConfiguration Configuration() => new(
        TestDishStationScenario.Reference with
        {
            InitialDirty = new(1, 1, 0),
            InitialAvailable = new(0, 0, 0),
            ArrivalIntervalTicks = 1000,
            GlassEveryArrivals = 2,
            RackCapacity = 4,
            WasherCycleTicks = 1,
            WorkerActionIntervalTicks = 1,
            FlowCellWorkerActionIntervalTicks = 1,
            DemandIntervalTicks = 5,
            InitialRushEnabled = false,
            InitialNewHireEnabled = false,
            InitialNewHireSpecification = default,
            InitialAutomationPolicy = WasherAutomationPolicy.Off,
            InitialLayout = DishStationLayout.Linear,
        },
        ImmutableArray.Create(
            new DishRoutingStationProfile(DishRoutingStationId.MainDishRoom, "Main dish room",
                new(1, 1, 0), DishKind.Glass, ProcessRoutingPolicy.GlassesFirst),
            new DishRoutingStationProfile(DishRoutingStationId.PatioServiceStation, "Patio service station",
                new(1, 1, 0), DishKind.Plate, ProcessRoutingPolicy.GlassesFirst)),
        5);

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
