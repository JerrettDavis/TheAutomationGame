using System.Text.Json;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class ProcessEditorTests
{
    [Fact]
    public void DraftViewsCurrentStepsAndInvalidReorderBlocksApplyWithDiagnostic()
    {
        var world = CapturedWorld();
        Assert.True(world.ExecuteNow(new BeginProcessEditCommand(world.Tick, new(1))).Success);
        var original = world.Snapshot().ProcessCapture.ActiveEdit!;

        Assert.Equal(1, original.BasedOnVersion);
        Assert.Equal(5, original.Steps.Length);
        Assert.Empty(original.Diagnostics);
        var rack = original.Steps.Single(step => step.Action == DishAction.Rack);
        Assert.True(world.ExecuteNow(new MoveProcessStepCommand(world.Tick, rack.Id, 1)).Success);

        var invalid = world.Snapshot().ProcessCapture.ActiveEdit!;
        Assert.Equal(DishAction.StartWasher, invalid.Steps[1].Action);
        Assert.Contains(invalid.Diagnostics, diagnostic => diagnostic.Code == "transition");
        var apply = world.ExecuteNow(new ApplyProcessEditCommand(world.Tick));
        Assert.False(apply.Success);
        Assert.Contains("expects", apply.Message, StringComparison.Ordinal);
        var artifact = Assert.Single(world.Snapshot().ProcessCapture.Artifacts);
        Assert.Equal(1, artifact.Current.Version);
        Assert.Equal(1, artifact.Baseline.Version);
    }

    [Fact]
    public void ValidAssignmentAndRoutingEditsCreateCurrentV2AndPreserveBaselineV1()
    {
        var world = CapturedWorld();
        ApplyPolicy(world, ProcessRoutingPolicy.GlassesFirst);

        var snapshot = world.Snapshot().ProcessCapture;
        var artifact = Assert.Single(snapshot.Artifacts);
        Assert.Equal(new PlayerProcessArtifactId(1), snapshot.AppliedArtifactId);
        Assert.Equal(1, artifact.Baseline.Version);
        Assert.Equal(2, artifact.Current.Version);
        Assert.Equal(ProcessRoutingPolicy.CapturedOrder, artifact.Baseline.RoutingPolicy);
        Assert.Equal(ProcessRoutingPolicy.GlassesFirst, artifact.Current.RoutingPolicy);
        Assert.All(artifact.Baseline.Steps, step => Assert.Equal(new ActorId(0), step.AssignedActor));
        Assert.All(artifact.Current.Steps, step => Assert.Equal(new ActorId(1), step.AssignedActor));
        Assert.Equal(1, artifact.Current.EditProvenance!.BasedOnVersion);
        Assert.Equal(new ActorId(0), artifact.Current.EditProvenance.Editor);
        Assert.Null(artifact.Baseline.EditProvenance);
        Assert.Null(snapshot.ActiveEdit);
    }

    [Fact]
    public void AppliedRoutingChangesDelegatedFlowAndServiceOutcome()
    {
        var platesFirst = RunRerun(ProcessRoutingPolicy.PlatesFirst);
        var glassesFirst = RunRerun(ProcessRoutingPolicy.GlassesFirst);

        Assert.Equal(1, platesFirst.ServiceShortages);
        Assert.Equal(0, glassesFirst.ServiceShortages);
        Assert.Equal(0, platesFirst.At(DishState.Available).Glasses);
        Assert.Equal(0, glassesFirst.At(DishState.Available).Glasses);
        Assert.Contains(glassesFirst.RecentTransitions, transition =>
            transition.Kind == DishKind.Glass && transition.To == DishState.Available && transition.Cause == DishTransitionCause.NewHireWork);
        Assert.DoesNotContain(platesFirst.RecentTransitions, transition =>
            transition.Kind == DishKind.Glass && transition.To == DishState.Available && transition.Tick.Value < 10);
    }

    [Fact]
    public void ReplayReconstructsDraftAndAppliedVersionExactly()
    {
        var original = CapturedWorld(17);
        original.ExecuteNow(new BeginProcessEditCommand(original.Tick, new(1)));
        var firstStep = original.Snapshot().ProcessCapture.ActiveEdit!.Steps[0];
        original.ExecuteNow(new AssignProcessStepCommand(original.Tick, firstStep.Id, new(1)));
        original.ExecuteNow(new SetProcessRoutingPolicyCommand(original.Tick, ProcessRoutingPolicy.GlassesFirst));

        var restoredDraft = DishStationWorld.Restore(original.CreateReplaySave());
        Assert.Equal(Json(original.Snapshot().ProcessCapture), Json(restoredDraft.Snapshot().ProcessCapture));

        AssignRemainingAndApply(original);
        AssignRemainingAndApply(restoredDraft);
        Assert.Equal(Json(original.Snapshot().ProcessCapture), Json(restoredDraft.Snapshot().ProcessCapture));
        Assert.Equal(Json(original.CreateReplaySave()), Json(restoredDraft.CreateReplaySave()));
    }

    private static DishStationSnapshot RunRerun(ProcessRoutingPolicy policy)
    {
        var world = CapturedWorld();
        ApplyPolicy(world, policy);
        Assert.True(world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Dirty, DishKind.Plate, 1)).Success);
        Assert.True(world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Dirty, DishKind.Glass, 1)).Success);
        Assert.True(world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Available, DishKind.Plate, 0)).Success);
        Assert.True(world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Available, DishKind.Glass, 0)).Success);
        Assert.True(world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true)).Success);
        Assert.True(world.ExecuteNow(new SetRushCommand(world.Tick, true)).Success);
        Advance(world, 9);
        return world.Snapshot();
    }

    private static DishStationWorld CapturedWorld(int seed = 42)
    {
        var configuration = TestDishStationScenario.Reference with
        {
            InitialDirty = new(2, 1, 0),
            InitialAvailable = new(0, 0, 0),
            ArrivalIntervalTicks = 1000,
            WasherCycleTicks = 1,
            WorkerActionIntervalTicks = 1,
            FlowCellWorkerActionIntervalTicks = 1,
            DemandKind = DishKind.Glass,
            DemandIntervalTicks = 10,
            InitialRushEnabled = false,
            InitialNewHireEnabled = false,
            InitialNewHireSpecification = DishProcessSpecification.FullyDocumented,
            InitialAutomationPolicy = WasherAutomationPolicy.Off,
        };
        var world = new DishStationWorld(seed, configuration);
        Assert.True(world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "Dish flow")).Success);
        Perform(world, DishAction.Scrape);
        Perform(world, DishAction.Rack);
        Perform(world, DishAction.StartWasher);
        world.Advance();
        Perform(world, DishAction.Unload);
        Perform(world, DishAction.DryAndRestock);
        Assert.True(world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick)).Success);
        return world;
    }

    private static void ApplyPolicy(DishStationWorld world, ProcessRoutingPolicy policy)
    {
        Assert.True(world.ExecuteNow(new BeginProcessEditCommand(world.Tick, new(1))).Success);
        foreach (var step in world.Snapshot().ProcessCapture.ActiveEdit!.Steps)
            Assert.True(world.ExecuteNow(new AssignProcessStepCommand(world.Tick, step.Id, new(1))).Success);
        Assert.True(world.ExecuteNow(new SetProcessRoutingPolicyCommand(world.Tick, policy)).Success);
        Assert.True(world.ExecuteNow(new ApplyProcessEditCommand(world.Tick)).Success);
    }

    private static void AssignRemainingAndApply(DishStationWorld world)
    {
        foreach (var step in world.Snapshot().ProcessCapture.ActiveEdit!.Steps.Where(step => step.AssignedActor.Value != 1))
            Assert.True(world.ExecuteNow(new AssignProcessStepCommand(world.Tick, step.Id, new(1))).Success);
        Assert.True(world.ExecuteNow(new ApplyProcessEditCommand(world.Tick)).Success);
    }

    private static void Perform(DishStationWorld world, DishAction action) =>
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, action, DishKind.Plate)).Success);

    private static void Advance(DishStationWorld world, int ticks)
    {
        for (var index = 0; index < ticks; index++) world.Advance();
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
