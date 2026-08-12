using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class AutomationIrIntegrationTests
{
    [Fact]
    public void CompatibilityPoliciesCompileToExpectedStableRulesAndUnsafeDecision()
    {
        var reported = DishStationAutomationRules.Evaluate(
            WasherAutomationPolicy.ReportedReadyOnly, 1, reportedReady: true, physicalReady: false);
        var safe = DishStationAutomationRules.Evaluate(
            WasherAutomationPolicy.CorroboratedReady, 1, reportedReady: true, physicalReady: false);

        Assert.True(reported.ConditionMatched);
        Assert.Equal("automation.rule.dish-station.start-washer-reported", reported.Trace.RuleId.Value);
        Assert.Equal(2, reported.Trace.Predicates.Count(predicate => predicate.Expression.Contains("Equal", StringComparison.Ordinal)));
        Assert.IsType<IssueDishActionAutomationEffect>(Assert.Single(reported.SelectedEffects));
        Assert.False(safe.ConditionMatched);
        Assert.Equal("automation.rule.dish-station.start-washer-corroborated", safe.Trace.RuleId.Value);
        Assert.Contains(safe.Trace.ObservedValues, observed =>
            observed.Reference == nameof(AutomationObservable.PhysicalReady) && observed.Value == AutomationValue.From(false));
        Assert.Empty(safe.SelectedEffects);
    }

    [Fact]
    public void LiveRuleEffectExecutesThroughAuthoritativeDishActionAndRecordsOutcome()
    {
        var world = World();
        PrepareRack(world);
        Assert.True(world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.ReportedReadyOnly)).Success);

        world.Advance();

        Assert.True(world.WasherRunning);
        Assert.Equal(1, world.At(DishState.Washing).Plates);
        var entry = Assert.Single(world.Snapshot().Automation.RuleTrace);
        Assert.True(entry.Evaluation.ConditionMatched);
        var outcome = Assert.Single(entry.Evaluation.Outcomes);
        Assert.IsType<IssueDishActionAutomationEffect>(outcome.Effect);
        Assert.True(outcome.Success);
        Assert.Equal("Washer started.", outcome.Message);
    }

    [Fact]
    public void IncidentAndReplayUseIdenticalIrWhileCorroboratedRulePreventsRequest()
    {
        var world = World();
        PrepareRack(world);
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.ReportedReadyOnly));
        world.Advance();
        Assert.True(world.Snapshot().Automation.StickyReadySignal);
        PrepareRack(world);
        world.Advance();
        Assert.True(world.Snapshot().Automation.Incident.Recorded);

        var unsafeEvaluation = world.Snapshot().Automation.RuleTrace[^1].Evaluation;
        Assert.True(unsafeEvaluation.ConditionMatched);
        Assert.False(Assert.Single(unsafeEvaluation.Outcomes).Success);
        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));
        var replayEvaluation = world.Snapshot().Automation.RuleTrace[^1].Evaluation;

        Assert.Equal(unsafeEvaluation.RuleId, replayEvaluation.RuleId);
        Assert.Equal(unsafeEvaluation.ObservedValues.ToArray(), replayEvaluation.ObservedValues.ToArray());
        Assert.Equal(unsafeEvaluation.Predicates.ToArray(), replayEvaluation.Predicates.ToArray());
        Assert.True(replayEvaluation.ConditionMatched);

        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.CorroboratedReady));
        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));
        var safeReplay = world.Snapshot().Automation.RuleTrace[^1].Evaluation;
        Assert.False(safeReplay.ConditionMatched);
        Assert.Empty(safeReplay.SelectedEffects);
        Assert.Contains(safeReplay.Predicates, predicate =>
            predicate.Expression.StartsWith(nameof(AutomationObservable.PhysicalReady), StringComparison.Ordinal) && !predicate.Result);
        Assert.False(world.Snapshot().Automation.Incident.LastReplayWouldStart);
    }

    [Fact]
    public void FixedCommandsProduceIdenticalIrTraceTreesAndOutcomes()
    {
        var first = RunLiveStart();
        var second = RunLiveStart();

        Assert.Equal(first.Count, second.Count);
        for (var index = 0; index < first.Count; index++)
        {
            Assert.Equal(first[index].Tick, second[index].Tick);
            Assert.Equal(first[index].Evaluation.RuleId, second[index].Evaluation.RuleId);
            Assert.Equal(first[index].Evaluation.ObservedValues.ToArray(), second[index].Evaluation.ObservedValues.ToArray());
            Assert.Equal(first[index].Evaluation.Predicates.ToArray(), second[index].Evaluation.Predicates.ToArray());
            Assert.Equal(first[index].Evaluation.SelectedEffects.ToArray(), second[index].Evaluation.SelectedEffects.ToArray());
            Assert.Equal(first[index].Evaluation.Outcomes.ToArray(), second[index].Evaluation.Outcomes.ToArray());
        }
    }

    private static IReadOnlyList<AutomationRuleTraceEntry> RunLiveStart()
    {
        var world = World();
        PrepareRack(world);
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.ReportedReadyOnly));
        world.Advance();
        return world.Snapshot().Automation.RuleTrace;
    }

    private static DishStationWorld World() => new(42, TestDishStationScenario.Reference with
    {
        InitialDirty = new(4, 0, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        WasherCycleTicks = 20,
        StickyReadyFaultAfterAutomatedStarts = 1,
        StickyReadyFaultPermillePerStart = 0,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
    });

    private static void PrepareRack(DishStationWorld world)
    {
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
    }
}
