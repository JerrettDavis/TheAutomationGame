using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class AutomationPresetComparisonTests
{
    [Fact]
    public void BaselineAndVariantPreserveDistinctImmutableAppliedRules()
    {
        var world = World();
        ApplyReported(world);
        Assert.True(world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Baseline)).Success);
        RefinePhysical(world);
        Assert.True(world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Variant)).Success);

        var comparison = world.Snapshot().Automation.Comparison;
        Assert.NotNull(comparison.Baseline);
        Assert.NotNull(comparison.Variant);
        Assert.DoesNotContain(AutomationObservable.PhysicalReady, Conditions(comparison.Baseline!.Rule));
        Assert.Contains(AutomationObservable.PhysicalReady, Conditions(comparison.Variant!.Rule));
        Assert.Equal(DishStationAutomationRuleEditor.PlayerRuleId, comparison.Baseline.Rule.Id);
        Assert.Equal(DishStationAutomationRuleEditor.PlayerRuleId, comparison.Variant.Rule.Id);
    }

    [Fact]
    public void ControlledComparisonUsesIdenticalInputsAndShowsVariantReliabilityAndThroughputGain()
    {
        var world = ComparedWorld();
        var result = world.Snapshot().Automation.Comparison.LatestResult!;

        Assert.Equal(result.Baseline.Seed, result.Variant.Seed);
        Assert.Equal(result.Baseline.HorizonTicks, result.Variant.HorizonTicks);
        Assert.Equal(result.Baseline.Scenario, result.Variant.Scenario);
        Assert.Equal(AutomationComparisonVerdict.VariantBetter, result.Verdict);
        Assert.True(result.Baseline.Metrics.UnsafeIncidents > result.Variant.Metrics.UnsafeIncidents);
        Assert.Equal(0, result.Variant.Metrics.UnsafeIncidents);
        Assert.True(result.Variant.Metrics.PreventedUnsafeStarts > 0);
        Assert.True(result.Variant.Metrics.Completed > result.Baseline.Metrics.Completed);
        Assert.NotNull(result.Baseline.FirstReadinessDivergence);
        Assert.NotNull(result.Variant.FirstReadinessDivergence);
        Assert.True(result.Baseline.FirstReadinessDivergence!.ReportedReady);
        Assert.False(result.Baseline.FirstReadinessDivergence.PhysicalReady);
        Assert.True(result.Baseline.FirstReadinessDivergence.Evaluation.ConditionMatched);
        Assert.False(result.Variant.FirstReadinessDivergence!.Evaluation.ConditionMatched);
        Assert.Contains(result.Variant.FirstReadinessDivergence.Evaluation.Predicates, predicate =>
            predicate.Expression.StartsWith(nameof(AutomationObservable.PhysicalReady), StringComparison.Ordinal) && !predicate.Result);
    }

    [Fact]
    public void ComparisonRejectsMissingSlotsAndInvalidHorizonWithoutChangingLiveWorld()
    {
        var world = World();
        ApplyReported(world);
        var before = world.Snapshot();

        Assert.False(world.ExecuteNow(new RunAutomationRuleComparisonCommand(world.Tick, 12)).Success);
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Baseline));
        Assert.False(world.ExecuteNow(new RunAutomationRuleComparisonCommand(world.Tick, 12)).Success);
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Variant));
        Assert.False(world.ExecuteNow(new RunAutomationRuleComparisonCommand(world.Tick, 0)).Success);

        Assert.Equal(before.Tick, world.Tick);
        Assert.Equal(before.Dishes, world.Snapshot().Dishes);
        Assert.Null(world.Snapshot().Automation.Comparison.LatestResult);
    }

    [Fact]
    public void ReplaySaveReconstructsPresetsAndComparisonEvidence()
    {
        var world = ComparedWorld();
        var restored = DishStationWorld.Restore(world.CreateReplaySave());
        var expected = world.Snapshot().Automation.Comparison;
        var actual = restored.Snapshot().Automation.Comparison;

        Assert.Equal(expected.Baseline!.Slot, actual.Baseline!.Slot);
        Assert.Equal(expected.Baseline.CapturedAt, actual.Baseline.CapturedAt);
        Assert.Equal(Conditions(expected.Baseline.Rule), Conditions(actual.Baseline.Rule));
        Assert.Equal(expected.Variant!.Slot, actual.Variant!.Slot);
        Assert.Equal(Conditions(expected.Variant.Rule), Conditions(actual.Variant.Rule));
        Assert.Equal(expected.LatestResult!.Verdict, actual.LatestResult!.Verdict);
        Assert.Equal(expected.LatestResult.Baseline.Metrics, actual.LatestResult.Baseline.Metrics);
        Assert.Equal(expected.LatestResult.Variant.Metrics, actual.LatestResult.Variant.Metrics);
        Assert.Equal(
            expected.LatestResult.Variant.FirstReadinessDivergence!.Evaluation.Predicates.ToArray(),
            actual.LatestResult.Variant.FirstReadinessDivergence!.Evaluation.Predicates.ToArray());
    }

    [Fact]
    public void ControlledTrialsDoNotMutateTheLiveStation()
    {
        var world = World();
        ApplyReported(world);
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Baseline));
        RefinePhysical(world);
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Variant));
        var before = world.Snapshot();

        Assert.True(world.ExecuteNow(new RunAutomationRuleComparisonCommand(world.Tick, 16)).Success);
        var after = world.Snapshot();

        Assert.Equal(before.Tick, after.Tick);
        Assert.Equal(before.Dishes, after.Dishes);
        Assert.Equal(before.Completed, after.Completed);
        Assert.Equal(before.ServiceShortages, after.ServiceShortages);
        Assert.Equal(before.Automation.ActiveRule.Id, after.Automation.ActiveRule.Id);
        Assert.Equal(before.Automation.AutomatedStarts, after.Automation.AutomatedStarts);
        Assert.Equal(before.Automation.Incidents, after.Automation.Incidents);
    }

    private static DishStationWorld ComparedWorld()
    {
        var world = World();
        ApplyReported(world);
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Baseline));
        RefinePhysical(world);
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Variant));
        Assert.True(world.ExecuteNow(new RunAutomationRuleComparisonCommand(world.Tick, 16)).Success);
        return world;
    }

    private static DishStationWorld World() => new(42, TestDishStationScenario.Reference with
    {
        InitialDirty = new(6, 2, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        WasherCycleTicks = 2,
        DemandIntervalTicks = 2,
        StickyReadyFaultAfterAutomatedStarts = 1,
        StickyReadyFaultPermillePerStart = 0,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
        InitialNewHireEnabled = false,
    });

    private static void ApplyReported(DishStationWorld world)
    {
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new SetAutomationRuleEnabledCommand(world.Tick, true));
        Assert.True(world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick)).Success);
    }

    private static void RefinePhysical(DishStationWorld world)
    {
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.PhysicalReady));
        Assert.True(world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick)).Success);
    }

    private static IReadOnlyCollection<AutomationObservable> Conditions(AutomationRule rule) =>
        ((AutomationAllCondition)rule.Condition).Conditions
        .OfType<AutomationCompareCondition>()
        .Select(condition => ((AutomationObservableRef)condition.Left).Observable)
        .ToArray();
}
