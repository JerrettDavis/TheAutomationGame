using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Simulation;

public sealed record AutomationRuleEditDraft(
    AutomationRuleId RuleId,
    bool Enabled,
    ImmutableArray<AutomationObservable> Conditions,
    DishAction Action,
    ImmutableArray<AutomationIrDiagnostic> Diagnostics);

public static class DishStationAutomationRuleEditor
{
    public static readonly AutomationRuleId PlayerRuleId = new("automation.rule.dish-station.player-start-washer");

    private static readonly AutomationObservable[] SupportedConditions =
    [
        AutomationObservable.RackPresent,
        AutomationObservable.ReportedReady,
        AutomationObservable.PhysicalReady,
    ];

    public static AutomationRuleEditDraft Begin(AutomationRule activeRule)
    {
        ArgumentNullException.ThrowIfNull(activeRule);
        var conditions = ReadConditions(activeRule.Condition);
        var action = activeRule.Effects.OfType<IssueDishActionAutomationEffect>().FirstOrDefault()?.Action ?? DishAction.StartWasher;
        return Create(activeRule.Enabled, conditions, action);
    }

    public static AutomationRuleEditDraft SetEnabled(AutomationRuleEditDraft draft, bool enabled) =>
        Create(enabled, draft.Conditions, draft.Action);

    public static AutomationRuleEditDraft ToggleCondition(AutomationRuleEditDraft draft, AutomationObservable observable)
    {
        var conditions = draft.Conditions.Contains(observable)
            ? draft.Conditions.Remove(observable)
            : draft.Conditions.Add(observable);
        return Create(draft.Enabled, conditions, draft.Action);
    }

    public static AutomationRuleEditDraft SetAction(AutomationRuleEditDraft draft, DishAction action) =>
        Create(draft.Enabled, draft.Conditions, action);

    public static AutomationRule Compile(AutomationRuleEditDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!draft.Diagnostics.IsDefaultOrEmpty)
            throw new AutomationIrValidationException(draft.Diagnostics);
        var rule = BuildRule(draft.Enabled, draft.Conditions, draft.Action);
        AutomationRuleEvaluator.Validate(rule);
        return rule;
    }

    public static WasherAutomationPolicy PolicyFor(AutomationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!rule.Enabled) return WasherAutomationPolicy.Off;
        return ReadConditions(rule.Condition).Contains(AutomationObservable.PhysicalReady)
            ? WasherAutomationPolicy.CorroboratedReady
            : WasherAutomationPolicy.ReportedReadyOnly;
    }

    private static AutomationRuleEditDraft Create(
        bool enabled,
        IEnumerable<AutomationObservable> conditions,
        DishAction action)
    {
        var ordered = conditions
            .Distinct()
            .OrderBy(observable => Array.IndexOf(SupportedConditions, observable))
            .ToImmutableArray();
        var diagnostics = ImmutableArray.CreateBuilder<AutomationIrDiagnostic>();
        foreach (var observable in ordered)
            if (!SupportedConditions.Contains(observable))
                diagnostics.Add(new("condition", $"{observable} is not available in the washer rule editor."));
        if (!ordered.Contains(AutomationObservable.RackPresent))
            diagnostics.Add(new("condition.rack-present", "Rack Present is required before Start Washer can be issued."));
        if (!ordered.Contains(AutomationObservable.ReportedReady) && !ordered.Contains(AutomationObservable.PhysicalReady))
            diagnostics.Add(new("condition.readiness", "Select at least one readiness condition."));
        if (action != DishAction.StartWasher)
            diagnostics.Add(new("effect.action", "The v1 washer editor can issue only Start Washer."));

        if (diagnostics.Count == 0)
        {
            try
            {
                AutomationRuleEvaluator.Validate(BuildRule(enabled, ordered, action));
            }
            catch (AutomationIrValidationException exception)
            {
                diagnostics.AddRange(exception.Diagnostics);
            }
        }
        return new(PlayerRuleId, enabled, ordered, action, diagnostics.ToImmutable());
    }

    private static AutomationRule BuildRule(
        bool enabled,
        ImmutableArray<AutomationObservable> conditions,
        DishAction action) => new(
        PlayerRuleId,
        enabled,
        new AutomationAllCondition(conditions.Select(IsTrue).Cast<AutomationCondition>().ToImmutableArray()),
        [new IssueDishActionAutomationEffect(action)]);

    private static ImmutableArray<AutomationObservable> ReadConditions(AutomationCondition condition) => condition switch
    {
        AutomationAllCondition all => all.Conditions
            .OfType<AutomationCompareCondition>()
            .Where(compare => compare.Operator == AutomationCompareOperator.Equal &&
                compare.Left is AutomationObservableRef &&
                compare.Right is AutomationBooleanConstant { Value: true })
            .Select(compare => ((AutomationObservableRef)compare.Left).Observable)
            .ToImmutableArray(),
        _ => [],
    };

    private static AutomationCompareCondition IsTrue(AutomationObservable observable) => new(
        new AutomationObservableRef(observable),
        AutomationCompareOperator.Equal,
        new AutomationBooleanConstant(true));
}
