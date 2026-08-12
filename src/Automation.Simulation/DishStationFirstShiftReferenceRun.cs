using Automation.Domain;

namespace Automation.Simulation;

public static class DishStationFirstShiftReferenceRun
{
    public const int CompletionTicks = 330;

    public static void Schedule(DishStationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.Schedule(new PerformDishActionCommand(new(1), DishAction.Scrape, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(2), DishAction.Rack, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(3), DishAction.StartWasher, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(24), DishAction.Unload, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(25), DishAction.DryAndRestock, DishKind.Plate));
        world.Schedule(new SetRushCommand(new(26), true));
        world.Schedule(new InspectProcessCommand(new(31)));
        world.Schedule(new ConfirmBottleneckCommand(new(32), DishState.Dirty));
        world.Schedule(new ConfigureDishStationLayoutCommand(new(33), DishStationLayout.UShapedCell));
        world.Schedule(new PerformDishActionCommand(new(33), DishAction.Scrape, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(34), DishAction.Rack, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(35), DishAction.StartWasher, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(56), DishAction.Unload, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(57), DishAction.DryAndRestock, DishKind.Glass));
        world.Schedule(new SetNewHireEnabledCommand(new(61), true));
        world.Schedule(new TrainNewHireCommand(new(62), DishProcessSpecification.HappyPath));
        world.Schedule(new TrainNewHireCommand(new(76), DishProcessSpecification.RushAware));
        world.Schedule(new TrainNewHireCommand(new(158), DishProcessSpecification.FullyDocumented));
        world.Schedule(new BeginAutomationRuleEditCommand(new(200)));
        world.Schedule(new SetAutomationRuleEnabledCommand(new(200), true));
        world.Schedule(new ApplyAutomationRuleEditCommand(new(200)));
        world.Schedule(new SaveAutomationRulePresetCommand(new(200), AutomationPresetSlot.Baseline));
        world.Schedule(new InspectAutomationIncidentCommand(new(241)));
        world.Schedule(new ReplayAutomationIncidentCommand(new(242)));
        world.Schedule(new BeginAutomationRuleEditCommand(new(243)));
        world.Schedule(new ToggleAutomationRuleConditionCommand(new(243), AutomationObservable.PhysicalReady));
        world.Schedule(new ApplyAutomationRuleEditCommand(new(243)));
        world.Schedule(new SaveAutomationRulePresetCommand(new(243), AutomationPresetSlot.Variant));
        world.Schedule(new ReplayAutomationIncidentCommand(new(244)));
        world.Schedule(new RunAutomationRuleComparisonCommand(new(244)));
        world.Schedule(new StartShiftTrialCommand(new(262)));
    }

    public static DishStationWorld Run(int seed, DishStationScenarioConfiguration scenario, int ticks = CompletionTicks)
    {
        var world = new DishStationWorld(seed, scenario);
        world.ExecuteNow(new CompleteIntroCommand(world.Tick, GuidanceMode.Contextual));
        Schedule(world);
        for (var tick = 0; tick < ticks; tick++) world.Advance();
        return world;
    }
}
