using Automation.Domain;

namespace Automation.Simulation;

public enum AutomationPresetSlot
{
    Baseline,
    Variant,
}

public enum AutomationComparisonVerdict
{
    BaselineBetter,
    VariantBetter,
    Equivalent,
}

public sealed record AutomationRulePreset(
    AutomationPresetSlot Slot,
    SimulationTick CapturedAt,
    AutomationRule Rule);

public readonly record struct AutomationTrialMetrics(
    int Completed,
    int ServiceShortages,
    int AutomatedStarts,
    int UnsafeIncidents,
    int PreventedUnsafeStarts);

public sealed record AutomationTrialDivergence(
    bool ReportedReady,
    bool PhysicalReady,
    AutomationRuleEvaluationTrace Evaluation);

public sealed record AutomationTrialResult(
    AutomationPresetSlot Slot,
    int Seed,
    int HorizonTicks,
    DishStationScenarioConfiguration Scenario,
    AutomationTrialMetrics Metrics,
    AutomationTrialDivergence? FirstReadinessDivergence);

public sealed record AutomationComparisonResult(
    AutomationTrialResult Baseline,
    AutomationTrialResult Variant,
    AutomationComparisonVerdict Verdict,
    string Summary);

public sealed record AutomationComparisonSnapshot(
    AutomationRulePreset? Baseline,
    AutomationRulePreset? Variant,
    AutomationComparisonResult? LatestResult);

public static class AutomationPresetComparisonRunner
{
    public const int DefaultHorizonTicks = 100;
    public const int MaximumHorizonTicks = 10_000;

    public static AutomationComparisonResult Run(
        int seed,
        DishStationScenarioConfiguration scenario,
        int horizonTicks,
        AutomationRulePreset baseline,
        AutomationRulePreset variant)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(variant);
        if (baseline.Slot != AutomationPresetSlot.Baseline) throw new ArgumentException("Baseline preset has the wrong slot.", nameof(baseline));
        if (variant.Slot != AutomationPresetSlot.Variant) throw new ArgumentException("Variant preset has the wrong slot.", nameof(variant));
        if (horizonTicks is < 1 or > MaximumHorizonTicks) throw new ArgumentOutOfRangeException(nameof(horizonTicks));

        var controlled = (scenario with
        {
            InitialRushEnabled = false,
            InitialNewHireEnabled = false,
            InitialNewHireSpecification = default,
            InitialAutomationPolicy = WasherAutomationPolicy.Off,
        }).Validate();
        var baselineResult = RunArm(seed, controlled, horizonTicks, baseline);
        var variantResult = RunArm(seed, controlled, horizonTicks, variant);
        var verdict = Compare(baselineResult.Metrics, variantResult.Metrics);
        var summary = verdict switch
        {
            AutomationComparisonVerdict.VariantBetter => "Variant improved the measured reliability/throughput outcome.",
            AutomationComparisonVerdict.BaselineBetter => "Baseline retained the better measured reliability/throughput outcome.",
            _ => "Baseline and variant were equivalent on the measured outcome.",
        };
        return new(baselineResult, variantResult, verdict, summary);
    }

    private static AutomationTrialResult RunArm(
        int seed,
        DishStationScenarioConfiguration scenario,
        int horizonTicks,
        AutomationRulePreset preset)
    {
        var world = new DishStationWorld(seed, scenario);
        ApplyPreset(world, preset.Rule);
        world.ExecuteNow(new SetRushCommand(world.Tick, true));
        AutomationTrialDivergence? firstDivergence = null;

        for (var tick = 0; tick < horizonTicks; tick++)
        {
            OperateSupportWork(world, scenario.RackCapacity);
            world.Advance();
            var automation = world.Snapshot().Automation;
            if (firstDivergence is null && automation.ReportedReady && !automation.PhysicalReady &&
                automation.RuleTrace.LastOrDefault() is { } latest && EvaluatedPhysicalNotReady(latest.Evaluation))
                firstDivergence = new(true, false, latest.Evaluation);
        }

        OperateSupportWork(world, scenario.RackCapacity);
        var snapshot = world.Snapshot();
        return new(
            preset.Slot,
            seed,
            horizonTicks,
            scenario,
            new(snapshot.Completed, snapshot.ServiceShortages, snapshot.Automation.AutomatedStarts,
                snapshot.Automation.Incidents, snapshot.Automation.PreventedUnsafeStarts),
            firstDivergence);
    }

    private static void ApplyPreset(DishStationWorld world, AutomationRule rule)
    {
        AutomationRuleEvaluator.Validate(rule);
        world.ExecuteNow(new BeginAutomationRuleEditCommand(world.Tick));
        var draft = world.Snapshot().Automation.ActiveEdit!;
        if (draft.Enabled != rule.Enabled)
            world.ExecuteNow(new SetAutomationRuleEnabledCommand(world.Tick, rule.Enabled));
        var desired = Conditions(rule);
        foreach (var observable in new[]
                 {
                     AutomationObservable.RackPresent,
                     AutomationObservable.ReportedReady,
                     AutomationObservable.PhysicalReady,
                 })
            if (world.Snapshot().Automation.ActiveEdit!.Conditions.Contains(observable) != desired.Contains(observable))
                world.ExecuteNow(new ToggleAutomationRuleConditionCommand(world.Tick, observable));
        var action = rule.Effects.OfType<IssueDishActionAutomationEffect>().Single().Action;
        world.ExecuteNow(new SetAutomationRuleActionCommand(world.Tick, action));
        var applied = world.ExecuteNow(new ApplyAutomationRuleEditCommand(world.Tick));
        if (!applied.Success) throw new InvalidOperationException($"Comparison preset could not be applied: {applied.Message}");
    }

    private static HashSet<AutomationObservable> Conditions(AutomationRule rule) => rule.Condition switch
    {
        AutomationAllCondition all => all.Conditions
            .OfType<AutomationCompareCondition>()
            .Where(condition => condition.Operator == AutomationCompareOperator.Equal &&
                                condition.Left is AutomationObservableRef &&
                                condition.Right is AutomationBooleanConstant { Value: true })
            .Select(condition => ((AutomationObservableRef)condition.Left).Observable)
            .ToHashSet(),
        _ => [],
    };

    private static void OperateSupportWork(DishStationWorld world, int rackCapacity)
    {
        foreach (var kind in Enum.GetValues<DishKind>())
        {
            while (world.At(DishState.WashedInMachine).For(kind) > 0)
                world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, kind));
            while (world.At(DishState.CleanWet).For(kind) > 0)
                world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, kind));
        }
        foreach (var kind in Enum.GetValues<DishKind>())
        {
            while (world.At(DishState.Dirty).For(kind) > 0 && world.At(DishState.Racked).Total < rackCapacity)
            {
                if (!world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, kind)).Success) break;
                if (!world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, kind)).Success) break;
            }
        }
    }

    private static bool EvaluatedPhysicalNotReady(AutomationRuleEvaluationTrace trace) =>
        trace.ObservedValues.Any(value => value.Reference == nameof(AutomationObservable.PhysicalReady) &&
                                          value.Value == AutomationValue.From(false)) ||
        trace.Outcomes.Any(outcome => !outcome.Success &&
                                      outcome.Message.Contains("Physical washer state", StringComparison.Ordinal));

    private static AutomationComparisonVerdict Compare(AutomationTrialMetrics baseline, AutomationTrialMetrics variant)
    {
        if (baseline.UnsafeIncidents != variant.UnsafeIncidents)
            return variant.UnsafeIncidents < baseline.UnsafeIncidents
                ? AutomationComparisonVerdict.VariantBetter
                : AutomationComparisonVerdict.BaselineBetter;
        if (baseline.ServiceShortages != variant.ServiceShortages)
            return variant.ServiceShortages < baseline.ServiceShortages
                ? AutomationComparisonVerdict.VariantBetter
                : AutomationComparisonVerdict.BaselineBetter;
        if (baseline.Completed != variant.Completed)
            return variant.Completed > baseline.Completed
                ? AutomationComparisonVerdict.VariantBetter
                : AutomationComparisonVerdict.BaselineBetter;
        return AutomationComparisonVerdict.Equivalent;
    }
}
