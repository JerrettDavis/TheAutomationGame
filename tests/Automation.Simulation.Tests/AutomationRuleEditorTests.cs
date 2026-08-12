using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class AutomationRuleEditorTests
{
    [Fact]
    public void PlayerDraftAppliesReportedRuleAndStartsWasherAuthoritatively()
    {
        var world = World();
        PrepareRack(world);

        Assert.True(world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick)).Success);
        Assert.True(world.ExecuteNow(new SetAutomationRuleEnabledCommand(world.Tick, true)).Success);
        Assert.True(world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick)).Success);
        world.Advance();

        var snapshot = world.Snapshot();
        Assert.True(snapshot.Automation.ActiveRule.Enabled);
        Assert.Equal(DishStationAutomationRuleEditor.PlayerRuleId, snapshot.Automation.ActiveRule.Id);
        Assert.Equal(WasherAutomationPolicy.ReportedReadyOnly, snapshot.Automation.Policy);
        Assert.True(world.WasherRunning);
        Assert.Equal(1, world.At(DishState.Washing).Plates);
        Assert.True(Assert.Single(snapshot.Automation.RuleTrace).Evaluation.Outcomes.Single().Success);
    }

    [Fact]
    public void InvalidDraftKeepsTargetedDiagnosticsAndCannotApply()
    {
        var world = World();
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.RackPresent));
        world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.ReportedReady));
        world.ExecuteNow(new SetAutomationRuleActionCommand(world.Tick, DishAction.Scrape));

        var draft = world.Snapshot().Automation.ActiveEdit!;
        Assert.Contains(draft.Diagnostics, diagnostic => diagnostic.Path == "condition.rack-present");
        Assert.Contains(draft.Diagnostics, diagnostic => diagnostic.Path == "condition.readiness");
        Assert.Contains(draft.Diagnostics, diagnostic => diagnostic.Path == "effect.action");
        var result = world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick));
        Assert.False(result.Success);
        Assert.NotNull(world.Snapshot().Automation.ActiveEdit);
        Assert.False(world.Snapshot().Automation.ActiveRule.Enabled);
    }

    [Fact]
    public void SamePlayerRuleCanBeRefinedToPreventCapturedUnsafeRequest()
    {
        var world = World();
        PrepareRack(world);
        ApplyReportedRule(world);
        world.Advance();
        PrepareRack(world);
        world.Advance();
        Assert.True(world.Snapshot().Automation.Incident.Recorded);

        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));
        Assert.True(world.Snapshot().Automation.Incident.LastReplayWouldStart);
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.PhysicalReady));
        Assert.True(world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick)).Success);
        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));

        var snapshot = world.Snapshot().Automation;
        Assert.Equal(DishStationAutomationRuleEditor.PlayerRuleId, snapshot.ActiveRule.Id);
        Assert.Equal(WasherAutomationPolicy.CorroboratedReady, snapshot.Policy);
        Assert.False(snapshot.Incident.LastReplayWouldStart);
        Assert.False(snapshot.RuleTrace[^1].Evaluation.ConditionMatched);
    }

    [Fact]
    public void ReplaySaveReconstructsAppliedRuleAndOpenDraft()
    {
        var applied = World();
        ApplyReportedRule(applied);
        var appliedRestored = DishStationWorld.Restore(applied.CreateReplaySave()).Snapshot().Automation;
        Assert.Equal(DishStationAutomationRuleEditor.PlayerRuleId, appliedRestored.ActiveRule.Id);
        Assert.True(appliedRestored.ActiveRule.Enabled);

        applied.ExecuteNow(new BeginAutomationRuleEditCommand(applied.Tick));
        applied.ExecuteNow(new ToggleAutomationRuleConditionCommand(applied.Tick, AutomationObservable.PhysicalReady));
        applied.ExecuteNow(new SetAutomationRuleEnabledCommand(applied.Tick, false));
        var draftRestored = DishStationWorld.Restore(applied.CreateReplaySave()).Snapshot().Automation.ActiveEdit!;
        Assert.False(draftRestored.Enabled);
        Assert.Contains(AutomationObservable.PhysicalReady, draftRestored.Conditions);
        Assert.Empty(draftRestored.Diagnostics);
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

    private static void ApplyReportedRule(DishStationWorld world)
    {
        Assert.True(world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick)).Success);
        Assert.True(world.ExecuteNow(new SetAutomationRuleEnabledCommand(world.Tick, true)).Success);
        Assert.True(world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick)).Success);
    }

    private static void PrepareRack(DishStationWorld world)
    {
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
    }
}
