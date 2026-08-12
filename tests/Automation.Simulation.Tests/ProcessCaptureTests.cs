using System.Text.Json;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class ProcessCaptureTests
{
    [Fact]
    public void ManualWorkflowBecomesOwnedVersionedArtifactWithOrderedAuthoritativeSteps()
    {
        var world = TestDishStationScenario.World(42);

        Assert.True(world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "Restore a plate")).Success);
        Perform(world, DishAction.Scrape);
        Perform(world, DishAction.Rack);
        Perform(world, DishAction.StartWasher);
        Advance(world, world.Configuration.WasherCycleTicks);
        Perform(world, DishAction.Unload);
        Perform(world, DishAction.DryAndRestock);
        Assert.True(world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick)).Success);

        var capture = world.Snapshot().ProcessCapture;
        var artifact = Assert.Single(capture.Artifacts);
        Assert.Null(capture.Active);
        Assert.Equal(new PlayerProcessArtifactId(1), artifact.Id);
        Assert.Equal(new ActorId(0), artifact.Owner);
        Assert.Equal("Restore a plate", artifact.Name);
        Assert.Equal(1, artifact.Baseline.Version);
        Assert.Equal(1, artifact.Current.Version);
        Assert.Equal(artifact.Baseline, artifact.Current);
        Assert.Equal(new ProcessCaptureId(1), artifact.Current.Provenance.CaptureId);
        Assert.Equal(ProcessCaptureSource.ManualPlayerWork, artifact.Current.Provenance.Source);
        Assert.Equal(42, artifact.Current.Provenance.WorldSeed);
        Assert.Equal(new ActorId(0), artifact.Current.Provenance.CapturedBy);
        Assert.Equal(new SimulationTick(0), artifact.Current.Provenance.StartedAt);
        Assert.Equal(new SimulationTick(20), artifact.Current.Provenance.CompletedAt);

        var expected = new[]
        {
            (DishAction.Scrape, DishStationFixture.Scrape, DishState.Dirty, DishState.Scraped),
            (DishAction.Rack, DishStationFixture.Rack, DishState.Scraped, DishState.Racked),
            (DishAction.StartWasher, DishStationFixture.Washer, DishState.Racked, DishState.Washing),
            (DishAction.Unload, DishStationFixture.Unload, DishState.WashedInMachine, DishState.CleanWet),
            (DishAction.DryAndRestock, DishStationFixture.DryRestock, DishState.CleanWet, DishState.Available),
        };
        Assert.Equal(expected.Length, artifact.Current.Steps.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            var step = artifact.Current.Steps[index];
            Assert.Equal(index + 1, step.Sequence);
            Assert.Equal(expected[index].Item1, step.Action);
            Assert.Equal(expected[index].Item2, step.Workstation);
            Assert.Equal(expected[index].Item3, step.InputState);
            Assert.Equal(expected[index].Item4, step.OutputState);
            Assert.Equal(DishKind.Plate, step.ItemKind);
            Assert.Equal(new ActorId(0), step.Actor);
        }
        Assert.Equal(7, capture.Events.Count);
        Assert.Equal(ProcessCaptureEventKind.Started, capture.Events[0].Kind);
        Assert.Equal(ProcessCaptureEventKind.Completed, capture.Events[^1].Kind);
    }

    [Fact]
    public void FailedAndNonPlayerTransitionsAreNotCapturedAsManualSteps()
    {
        var configuration = TestDishStationScenario.Reference with
        {
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.FullyDocumented,
            WorkerActionIntervalTicks = 1,
        };
        var world = new DishStationWorld(42, configuration);
        Assert.True(world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "Only my work")).Success);

        world.Advance();
        Assert.Empty(world.Snapshot().ProcessCapture.Active!.Steps);
        Assert.False(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Plate)).Success);
        Assert.Empty(world.Snapshot().ProcessCapture.Active!.Steps);
        Perform(world, DishAction.Scrape);

        var step = Assert.Single(world.Snapshot().ProcessCapture.Active!.Steps);
        Assert.Equal(DishAction.Scrape, step.Action);
        Assert.Equal(new ActorId(0), step.Actor);
    }

    [Fact]
    public void CaptureLifecycleRejectsAmbiguousOrEmptyCompletion()
    {
        var world = TestDishStationScenario.World();

        Assert.False(world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick)).Success);
        Assert.False(world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "   ")).Success);
        Assert.True(world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "One step")).Success);
        Assert.False(world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "Nested")).Success);
        Assert.False(world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick)).Success);
        Perform(world, DishAction.Scrape);
        Assert.True(world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick)).Success);
    }

    [Fact]
    public void ReplayReconstructsActiveAndCompletedCaptureExactly()
    {
        var original = TestDishStationScenario.World(17);
        original.ExecuteNow(new StartProcessCaptureCommand(original.Tick, "Replay proof"));
        Perform(original, DishAction.Scrape);
        Perform(original, DishAction.Rack);

        var activeRestored = DishStationWorld.Restore(original.CreateReplaySave());
        Assert.Equal(Json(original.Snapshot().ProcessCapture), Json(activeRestored.Snapshot().ProcessCapture));

        CompleteRemaining(original);
        CompleteRemaining(activeRestored);
        Assert.Equal(Json(original.Snapshot().ProcessCapture), Json(activeRestored.Snapshot().ProcessCapture));
        Assert.Equal(Json(original.CreateReplaySave()), Json(activeRestored.CreateReplaySave()));
    }

    private static void CompleteRemaining(DishStationWorld world)
    {
        Perform(world, DishAction.StartWasher);
        Advance(world, world.Configuration.WasherCycleTicks);
        Perform(world, DishAction.Unload);
        Perform(world, DishAction.DryAndRestock);
        Assert.True(world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick)).Success);
    }

    private static void Perform(DishStationWorld world, DishAction action) =>
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, action, DishKind.Plate)).Success);

    private static void Advance(DishStationWorld world, int ticks)
    {
        for (var index = 0; index < ticks; index++) world.Advance();
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
