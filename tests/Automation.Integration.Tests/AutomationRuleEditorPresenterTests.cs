using Automation.Client.Stride;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class AutomationRuleEditorPresenterTests
{
    [Fact]
    public void PresenterShowsEditableRuleValidationActionAndLatestTrace()
    {
        var world = World();
        PrepareRack(world);
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new SetAutomationRuleEnabledCommand(world.Tick, true));
        world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick));
        world.Advance();
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));

        var view = AutomationRuleEditorPresenter.Present(world.Snapshot().Automation, 2);

        Assert.Equal(DishStationAutomationRuleEditor.PlayerRuleId.Value, view.RuleId);
        Assert.True(view.CanApply);
        Assert.Equal("VALID — READY TO APPLY", view.Validation);
        Assert.Equal(AutomationRuleEditorPresenter.RowCount, view.Rows.Count);
        Assert.True(view.Rows[2].Selected);
        Assert.Equal("REQUIRED", view.Rows.Single(row => row.Label == "AND REPORTED READY").Value);
        Assert.Equal("START WASHER", view.Rows.Single(row => row.Label == "THEN").Value);
        Assert.False(view.Rows.Single(row => row.Label == "THEN").Editable);
        Assert.Contains(view.TraceLines, line => line.Contains("MATCHED YES", StringComparison.Ordinal));
        Assert.Contains(view.TraceLines, line => line.Contains("EFFECT 0 START WASHER SELECTED", StringComparison.Ordinal));
        Assert.Contains(view.TraceLines, line => line.Contains("COMMAND ACCEPTED", StringComparison.Ordinal));
    }

    [Fact]
    public void PresenterShowsTargetedInvalidDraftMessage()
    {
        var world = World();
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.RackPresent));

        var view = AutomationRuleEditorPresenter.Present(world.Snapshot().Automation, 99);

        Assert.False(view.CanApply);
        Assert.StartsWith("BLOCKED — RACK PRESENT IS REQUIRED", view.Validation, StringComparison.Ordinal);
        Assert.True(view.Rows[^1].Selected);
    }

    [Fact]
    public void PresenterExplainsControlledBaselineVariantOutcome()
    {
        var world = World();
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new SetAutomationRuleEnabledCommand(world.Tick, true));
        world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Baseline));
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.PhysicalReady));
        world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick));
        world.ExecuteNow(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Variant));
        world.ExecuteNow(new RunAutomationRuleComparisonCommand(world.Tick, 50));
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));

        var view = AutomationRuleEditorPresenter.Present(world.Snapshot().Automation, 0);

        Assert.Contains("REPORTED READY", view.BaselinePreset, StringComparison.Ordinal);
        Assert.Contains("REPORTED + PHYSICAL", view.VariantPreset, StringComparison.Ordinal);
        Assert.Contains(view.ComparisonLines, line => line == "VERDICT  VARIANT BETTER");
        Assert.Contains(view.ComparisonLines, line => line.Contains("SAME SCENARIO", StringComparison.Ordinal));
        Assert.Contains(view.ComparisonLines, line => line.StartsWith("COMPLETED", StringComparison.Ordinal));
        Assert.Contains(view.ComparisonLines, line => line == "WHY  BASE MATCHED YES");
        Assert.Contains(view.ComparisonLines, line => line == "WHY  VAR PHYSICAL READY = FALSE -> NO");
    }

    private static DishStationWorld World() => new(42, IntegrationTestScenario.Reference with
    {
        InitialDirty = new(6, 2, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        WasherCycleTicks = 2,
        DemandIntervalTicks = 2,
        StickyReadyFaultAfterAutomatedStarts = 1,
        StickyReadyFaultPermillePerStart = 0,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
    });

    private static void PrepareRack(DishStationWorld world)
    {
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
    }
}
