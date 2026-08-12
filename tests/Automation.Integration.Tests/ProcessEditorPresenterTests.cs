using Automation.Client.Stride;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class ProcessEditorPresenterTests
{
    [Fact]
    public void PresenterSeparatesVersionsDraftAssignmentsAndValidation()
    {
        var world = CapturedWorld();
        world.ExecuteNow(new BeginProcessEditCommand(world.Tick, new(1)));
        var valid = ProcessEditorPresenter.Present(world.Snapshot().ProcessCapture, 1);

        Assert.Equal(1, valid.BaselineVersion);
        Assert.Equal(1, valid.CurrentVersion);
        Assert.Equal(1, valid.BasedOnVersion);
        Assert.Equal("CAPTURED ORDER", valid.Routing);
        Assert.True(valid.CanApply);
        Assert.Equal("VALID — READY TO APPLY", valid.Validation);
        Assert.Equal("PLAYER", valid.Steps[1].Assignment);
        Assert.True(valid.Steps[1].Selected);

        var rack = world.Snapshot().ProcessCapture.ActiveEdit!.Steps.Single(step => step.Action == DishAction.Rack);
        world.ExecuteNow(new MoveProcessStepCommand(world.Tick, rack.Id, 1));
        var invalid = ProcessEditorPresenter.Present(world.Snapshot().ProcessCapture, 2);
        Assert.False(invalid.CanApply);
        Assert.StartsWith("BLOCKED —", invalid.Validation, StringComparison.Ordinal);
        Assert.Contains("EXPECTS", invalid.Validation, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, ClientScreen.Gameplay, ClientModal.None, true)]
    [InlineData(false, ClientScreen.Gameplay, ClientModal.ProcessEditor, false)]
    [InlineData(false, ClientScreen.Gameplay, ClientModal.AutomationEditor, false)]
    [InlineData(true, ClientScreen.Gameplay, ClientModal.None, false)]
    [InlineData(false, ClientScreen.Briefing, ClientModal.None, false)]
    public void SimulationPausePolicyTreatsEditorAsPaused(
        bool paused, ClientScreen screen, ClientModal modal, bool expected) =>
        Assert.Equal(expected, ClientSimulationPolicy.ShouldAdvance(paused, screen, modal));

    private static DishStationWorld CapturedWorld()
    {
        var world = IntegrationTestScenario.World();
        world.ExecuteNow(new StartProcessCaptureCommand(world.Tick, "Dish flow"));
        Perform(world, DishAction.Scrape);
        Perform(world, DishAction.Rack);
        Perform(world, DishAction.StartWasher);
        for (var tick = 0; tick < world.Configuration.WasherCycleTicks; tick++) world.Advance();
        Perform(world, DishAction.Unload);
        Perform(world, DishAction.DryAndRestock);
        world.ExecuteNow(new CompleteProcessCaptureCommand(world.Tick));
        return world;
    }

    private static void Perform(DishStationWorld world, DishAction action) =>
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, action, DishKind.Plate)).Success);
}
