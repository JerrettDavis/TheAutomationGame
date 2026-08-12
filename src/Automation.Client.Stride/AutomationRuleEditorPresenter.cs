using Automation.Domain;
using Automation.Simulation;

namespace Automation.Client.Stride;

public sealed record AutomationRuleEditorRow(
    string Label,
    string Value,
    bool Editable,
    bool Selected);

public sealed record AutomationRuleEditorView(
    string RuleId,
    IReadOnlyList<AutomationRuleEditorRow> Rows,
    string Validation,
    bool CanApply,
    IReadOnlyList<string> TraceLines,
    string BaselinePreset,
    string VariantPreset,
    IReadOnlyList<string> ComparisonLines);

public static class AutomationRuleEditorPresenter
{
    public const int RowCount = 5;

    public static AutomationRuleEditorView Present(AutomationSnapshot automation, int selectedIndex)
    {
        var draft = automation.ActiveEdit ?? throw new InvalidOperationException("No automation rule draft is active.");
        var selected = Math.Clamp(selectedIndex, 0, RowCount - 1);
        var rows = new[]
        {
            Row("RULE ENABLED", draft.Enabled ? "YES" : "NO", true, 0, selected),
            Row("WHEN RACK PRESENT", State(draft, AutomationObservable.RackPresent), true, 1, selected),
            Row("AND REPORTED READY", State(draft, AutomationObservable.ReportedReady), true, 2, selected),
            Row("AND PHYSICAL READY", State(draft, AutomationObservable.PhysicalReady), true, 3, selected),
            Row("THEN", draft.Action == DishAction.StartWasher ? "START WASHER" : Split(draft.Action.ToString()), false, 4, selected),
        };
        var validation = draft.Diagnostics.IsDefaultOrEmpty
            ? "VALID — READY TO APPLY"
            : $"BLOCKED — {draft.Diagnostics[0].Message.ToUpperInvariant()}";
        return new(draft.RuleId.Value, rows, validation, draft.Diagnostics.IsDefaultOrEmpty,
            TraceLines(automation.RuleTrace.LastOrDefault()?.Evaluation),
            PresetLine("BASELINE", automation.Comparison.Baseline),
            PresetLine("VARIANT", automation.Comparison.Variant),
            ComparisonLines(automation.Comparison.LatestResult));
    }

    private static AutomationRuleEditorRow Row(
        string label, string value, bool editable, int index, int selected) =>
        new(label, value, editable, index == selected);

    private static string State(AutomationRuleEditDraft draft, AutomationObservable observable) =>
        draft.Conditions.Contains(observable) ? "REQUIRED" : "NOT USED";

    private static IReadOnlyList<string> TraceLines(AutomationRuleEvaluationTrace? trace)
    {
        if (trace is null) return ["NO EVALUATION TRACE YET"];
        var lines = new List<string>
        {
            $"RULE {trace.RuleId.Value}",
            $"MATCHED {YesNo(trace.ConditionMatched)}  EFFECTS {trace.SelectedEffects.Length}  OUTCOMES {trace.Outcomes.Length}",
        };
        lines.AddRange(trace.ObservedValues
            .Where(value => Enum.TryParse<AutomationObservable>(value.Reference, out _))
            .Take(4)
            .Select(value =>
            $"INPUT {Split(value.Reference)} = {value.Value.ToString().ToUpperInvariant()}"));
        lines.AddRange(trace.Predicates.TakeLast(2).Select(predicate =>
            $"TEST {predicate.Expression.ToUpperInvariant()} -> {YesNo(predicate.Result)}"));
        lines.AddRange(trace.SelectedEffects.Take(1).Select(selected =>
            $"EFFECT {selected.Order} {EffectName(selected.Effect)} SELECTED"));
        lines.AddRange(trace.Outcomes.TakeLast(1).Select(outcome =>
            $"COMMAND {(outcome.Success ? "ACCEPTED" : "REJECTED")} — {outcome.Message.ToUpperInvariant()}"));
        return lines;
    }

    private static string YesNo(bool value) => value ? "YES" : "NO";

    private static string EffectName(AutomationEffect effect) => effect switch
    {
        IssueDishActionAutomationEffect issue => Split(issue.Action.ToString()),
        _ => Split(effect.GetType().Name),
    };

    private static string PresetLine(string label, AutomationRulePreset? preset)
    {
        if (preset is null) return $"{label}  NOT SAVED";
        var physical = ((AutomationAllCondition)preset.Rule.Condition).Conditions
            .OfType<AutomationCompareCondition>()
            .Any(condition => condition.Left is AutomationObservableRef { Observable: AutomationObservable.PhysicalReady });
        return $"{label}  T{preset.CapturedAt.Value}  {(physical ? "REPORTED + PHYSICAL" : "REPORTED READY")}";
    }

    private static IReadOnlyList<string> ComparisonLines(AutomationComparisonResult? result)
    {
        if (result is null) return ["NO CONTROLLED COMPARISON YET"];
        var baseline = result.Baseline.Metrics;
        var variant = result.Variant.Metrics;
        var physicalPredicate = result.Variant.FirstReadinessDivergence?.Evaluation.Predicates
            .FirstOrDefault(predicate => predicate.Expression.StartsWith(nameof(AutomationObservable.PhysicalReady), StringComparison.Ordinal));
        return
        [
            $"VERDICT  {Split(result.Verdict.ToString())}",
            $"CONTROL  SEED {result.Baseline.Seed}  {result.Baseline.HorizonTicks} TICKS  SAME SCENARIO",
            "METRIC             BASE  VAR   DELTA",
            Metric("COMPLETED", baseline.Completed, variant.Completed),
            Metric("SHORTAGES", baseline.ServiceShortages, variant.ServiceShortages),
            Metric("AUTO STARTS", baseline.AutomatedStarts, variant.AutomatedStarts),
            Metric("INCIDENTS", baseline.UnsafeIncidents, variant.UnsafeIncidents),
            Metric("PREVENTED", baseline.PreventedUnsafeStarts, variant.PreventedUnsafeStarts),
            $"WHY  BASE MATCHED {YesNo(result.Baseline.FirstReadinessDivergence?.Evaluation.ConditionMatched ?? false)}",
            $"WHY  VAR PHYSICAL READY = FALSE -> {YesNo(physicalPredicate?.Result ?? false)}",
        ];
    }

    private static string Metric(string label, int baseline, int variant)
    {
        var delta = variant - baseline;
        var formatted = delta > 0 ? $"+{delta}" : delta.ToString();
        return $"{label,-18} {baseline,4} {variant,4} {formatted,5}";
    }

    private static string Split(string value) => string.Concat(value.Select((character, index) =>
        index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]) ? $" {character}" : character.ToString())).ToUpperInvariant();
}
