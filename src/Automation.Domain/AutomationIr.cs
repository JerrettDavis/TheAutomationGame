using System.Collections.Immutable;
using System.Diagnostics;

namespace Automation.Domain;

public readonly record struct AutomationRuleId(string Value)
{
    public override string ToString() => Value;
}

public enum AutomationValueKind
{
    Boolean,
    Integer,
}

public readonly record struct AutomationValue
{
    private AutomationValue(AutomationValueKind kind, bool boolean, long integer)
    {
        Kind = kind;
        Boolean = boolean;
        Integer = integer;
    }

    public AutomationValueKind Kind { get; }
    public bool Boolean { get; }
    public long Integer { get; }

    public static AutomationValue From(bool value) => new(AutomationValueKind.Boolean, value, 0);
    public static AutomationValue From(long value) => new(AutomationValueKind.Integer, false, value);
    public override string ToString() => Kind == AutomationValueKind.Boolean ? Boolean.ToString() : Integer.ToString();
}

public enum AutomationObservable
{
    RackCount,
    RackPresent,
    ReportedReady,
    PhysicalReady,
}

public readonly record struct AutomationEvaluationContext(
    int RackCount,
    bool ReportedReady,
    bool PhysicalReady)
{
    public AutomationValue Resolve(AutomationObservable observable) => observable switch
    {
        AutomationObservable.RackCount => AutomationValue.From(RackCount),
        AutomationObservable.RackPresent => AutomationValue.From(RackCount > 0),
        AutomationObservable.ReportedReady => AutomationValue.From(ReportedReady),
        AutomationObservable.PhysicalReady => AutomationValue.From(PhysicalReady),
        _ => throw new ArgumentOutOfRangeException(nameof(observable)),
    };
}

public abstract record AutomationValueRef;
public sealed record AutomationBooleanConstant(bool Value) : AutomationValueRef;
public sealed record AutomationIntegerConstant(long Value) : AutomationValueRef;
public sealed record AutomationObservableRef(AutomationObservable Observable) : AutomationValueRef;

public enum AutomationCompareOperator
{
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
}

public abstract record AutomationCondition;
public sealed record AutomationCompareCondition(
    AutomationValueRef Left,
    AutomationCompareOperator Operator,
    AutomationValueRef Right) : AutomationCondition;
public sealed record AutomationAllCondition(ImmutableArray<AutomationCondition> Conditions) : AutomationCondition;
public sealed record AutomationAnyCondition(ImmutableArray<AutomationCondition> Conditions) : AutomationCondition;
public sealed record AutomationNotCondition(AutomationCondition Condition) : AutomationCondition;

public abstract record AutomationEffect;
public sealed record IssueDishActionAutomationEffect(DishAction Action) : AutomationEffect;

public sealed record AutomationRule(
    AutomationRuleId Id,
    bool Enabled,
    AutomationCondition Condition,
    ImmutableArray<AutomationEffect> Effects);

public sealed record AutomationIrDiagnostic(string Path, string Message);

public sealed class AutomationIrValidationException : Exception
{
    public AutomationIrValidationException(IEnumerable<AutomationIrDiagnostic> diagnostics)
        : base(string.Join(Environment.NewLine, diagnostics.Select(item => $"{item.Path}: {item.Message}")))
    {
        Diagnostics = diagnostics.ToImmutableArray();
    }

    public ImmutableArray<AutomationIrDiagnostic> Diagnostics { get; }
}

public sealed record AutomationObservedValue(
    string Path,
    string Reference,
    AutomationValue Value);

public sealed record AutomationPredicateTrace(
    string Path,
    string Expression,
    bool Result);

public sealed record AutomationSelectedEffect(
    int Order,
    AutomationEffect Effect);

public sealed record AutomationEffectOutcome(
    int Order,
    AutomationEffect Effect,
    bool Success,
    string Message);

public sealed record AutomationRuleEvaluationTrace(
    AutomationRuleId RuleId,
    bool Enabled,
    bool ConditionMatched,
    ImmutableArray<AutomationObservedValue> ObservedValues,
    ImmutableArray<AutomationPredicateTrace> Predicates,
    ImmutableArray<AutomationSelectedEffect> SelectedEffects,
    ImmutableArray<AutomationEffectOutcome> Outcomes);

public sealed record AutomationRuleEvaluationResult(
    bool ConditionMatched,
    ImmutableArray<AutomationEffect> SelectedEffects,
    AutomationRuleEvaluationTrace Trace);

public static class AutomationRuleEvaluator
{
    private const int MaximumConditionDepth = 16;

    public static AutomationRuleEvaluationResult Evaluate(AutomationRule rule, AutomationEvaluationContext context)
    {
        Validate(rule);
        var observations = ImmutableArray.CreateBuilder<AutomationObservedValue>();
        var predicates = ImmutableArray.CreateBuilder<AutomationPredicateTrace>();
        var matched = rule.Enabled && EvaluateCondition(rule.Condition, context, "condition", observations, predicates);
        var effects = matched ? rule.Effects : [];
        var selected = effects.Select((effect, index) => new AutomationSelectedEffect(index, effect)).ToImmutableArray();
        var trace = new AutomationRuleEvaluationTrace(
            rule.Id, rule.Enabled, matched, observations.ToImmutable(), predicates.ToImmutable(), selected, []);
        return new(matched, effects, trace);
    }

    public static AutomationRuleEvaluationTrace WithOutcomes(
        AutomationRuleEvaluationTrace trace,
        IEnumerable<AutomationEffectOutcome> outcomes) =>
        trace with { Outcomes = outcomes.ToImmutableArray() };

    public static void Validate(AutomationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var diagnostics = new List<AutomationIrDiagnostic>();
        if (!IsSemanticId(rule.Id.Value)) diagnostics.Add(new("id", "Rule ID must use lowercase semantic ID syntax."));
        if (rule.Condition is null) diagnostics.Add(new("condition", "Rule condition is required."));
        else ValidateCondition(rule.Condition, "condition", 0, diagnostics);
        if (rule.Effects.IsDefaultOrEmpty) diagnostics.Add(new("effects", "At least one effect is required."));
        else for (var index = 0; index < rule.Effects.Length; index++)
        {
            if (rule.Effects[index] is not IssueDishActionAutomationEffect issue || !Enum.IsDefined(issue.Action))
                diagnostics.Add(new($"effects[{index}]", "Unsupported automation effect."));
        }
        if (diagnostics.Count > 0) throw new AutomationIrValidationException(diagnostics);
    }

    private static void ValidateCondition(
        AutomationCondition condition,
        string path,
        int depth,
        List<AutomationIrDiagnostic> diagnostics)
    {
        if (depth > MaximumConditionDepth)
        {
            diagnostics.Add(new(path, $"Condition nesting cannot exceed {MaximumConditionDepth}."));
            return;
        }
        switch (condition)
        {
            case AutomationCompareCondition compare:
                var left = KindOf(compare.Left, $"{path}.left", diagnostics);
                var right = KindOf(compare.Right, $"{path}.right", diagnostics);
                if (left is not null && right is not null && left != right)
                    diagnostics.Add(new(path, $"Compare operands must have the same type; found {left} and {right}."));
                if (left == AutomationValueKind.Boolean && compare.Operator is not (AutomationCompareOperator.Equal or AutomationCompareOperator.NotEqual))
                    diagnostics.Add(new($"{path}.operator", "Boolean values support only Equal and NotEqual."));
                break;
            case AutomationAllCondition all:
                ValidateComposite(all.Conditions, path, depth, diagnostics);
                break;
            case AutomationAnyCondition any:
                ValidateComposite(any.Conditions, path, depth, diagnostics);
                break;
            case AutomationNotCondition not when not.Condition is not null:
                ValidateCondition(not.Condition, $"{path}.not", depth + 1, diagnostics);
                break;
            case AutomationNotCondition:
                diagnostics.Add(new($"{path}.not", "Not condition requires a child condition."));
                break;
            default:
                diagnostics.Add(new(path, "Unsupported automation condition."));
                break;
        }
    }

    private static void ValidateComposite(
        ImmutableArray<AutomationCondition> conditions,
        string path,
        int depth,
        List<AutomationIrDiagnostic> diagnostics)
    {
        if (conditions.IsDefaultOrEmpty)
        {
            diagnostics.Add(new(path, "Composite condition requires at least one child."));
            return;
        }
        for (var index = 0; index < conditions.Length; index++)
            if (conditions[index] is null) diagnostics.Add(new($"{path}[{index}]", "Condition is required."));
            else ValidateCondition(conditions[index], $"{path}[{index}]", depth + 1, diagnostics);
    }

    private static AutomationValueKind? KindOf(
        AutomationValueRef? value,
        string path,
        List<AutomationIrDiagnostic> diagnostics) => value switch
        {
            AutomationBooleanConstant => AutomationValueKind.Boolean,
            AutomationIntegerConstant => AutomationValueKind.Integer,
            AutomationObservableRef { Observable: AutomationObservable.RackCount } => AutomationValueKind.Integer,
            AutomationObservableRef { Observable: AutomationObservable.RackPresent or AutomationObservable.ReportedReady or AutomationObservable.PhysicalReady } => AutomationValueKind.Boolean,
            _ => AddUnknown(path, diagnostics),
        };

    private static bool EvaluateCondition(
        AutomationCondition condition,
        AutomationEvaluationContext context,
        string path,
        ImmutableArray<AutomationObservedValue>.Builder observations,
        ImmutableArray<AutomationPredicateTrace>.Builder predicates)
    {
        bool result;
        string expression;
        switch (condition)
        {
            case AutomationCompareCondition compare:
                var left = Resolve(compare.Left, context, $"{path}.left", observations);
                var right = Resolve(compare.Right, context, $"{path}.right", observations);
                result = Compare(left, compare.Operator, right);
                expression = $"{ReferenceName(compare.Left)} {compare.Operator} {ReferenceName(compare.Right)}";
                break;
            case AutomationAllCondition all:
                result = true;
                for (var index = 0; index < all.Conditions.Length; index++)
                    result &= EvaluateCondition(all.Conditions[index], context, $"{path}.all[{index}]", observations, predicates);
                expression = "ALL";
                break;
            case AutomationAnyCondition any:
                result = false;
                for (var index = 0; index < any.Conditions.Length; index++)
                    result |= EvaluateCondition(any.Conditions[index], context, $"{path}.any[{index}]", observations, predicates);
                expression = "ANY";
                break;
            case AutomationNotCondition not:
                result = !EvaluateCondition(not.Condition, context, $"{path}.not", observations, predicates);
                expression = "NOT";
                break;
            default:
                throw new UnreachableException();
        }
        predicates.Add(new(path, expression, result));
        return result;
    }

    private static AutomationValue Resolve(
        AutomationValueRef value,
        AutomationEvaluationContext context,
        string path,
        ImmutableArray<AutomationObservedValue>.Builder observations)
    {
        var resolved = value switch
        {
            AutomationBooleanConstant constant => AutomationValue.From(constant.Value),
            AutomationIntegerConstant constant => AutomationValue.From(constant.Value),
            AutomationObservableRef observable => context.Resolve(observable.Observable),
            _ => throw new UnreachableException(),
        };
        observations.Add(new(path, ReferenceName(value), resolved));
        return resolved;
    }

    private static bool Compare(AutomationValue left, AutomationCompareOperator op, AutomationValue right) => op switch
    {
        AutomationCompareOperator.Equal => left == right,
        AutomationCompareOperator.NotEqual => left != right,
        AutomationCompareOperator.LessThan => left.Integer < right.Integer,
        AutomationCompareOperator.LessThanOrEqual => left.Integer <= right.Integer,
        AutomationCompareOperator.GreaterThan => left.Integer > right.Integer,
        AutomationCompareOperator.GreaterThanOrEqual => left.Integer >= right.Integer,
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    private static string ReferenceName(AutomationValueRef value) => value switch
    {
        AutomationBooleanConstant constant => constant.Value.ToString(),
        AutomationIntegerConstant constant => constant.Value.ToString(),
        AutomationObservableRef observable => observable.Observable.ToString(),
        _ => "unknown",
    };

    private static bool IsSemanticId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith("automation.rule.", StringComparison.Ordinal) &&
        value.All(character => char.IsLower(character) || char.IsDigit(character) || character is '.' or '-');

    private static AutomationValueKind? AddUnknown(string path, List<AutomationIrDiagnostic> diagnostics)
    {
        diagnostics.Add(new(path, "Unsupported automation value reference."));
        return null;
    }
}
