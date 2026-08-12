using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class AutomationIrTests
{
    [Fact]
    public void ValuesComparisonsAndCompositionEvaluateDeterministicallyWithEveryNodeTraced()
    {
        var rule = new AutomationRule(
            new("automation.rule.proof.composed"),
            true,
            new AutomationAllCondition(
            [
                Compare(new AutomationObservableRef(AutomationObservable.RackCount), AutomationCompareOperator.GreaterThan, new AutomationIntegerConstant(0)),
                new AutomationAnyCondition(
                [
                    Compare(new AutomationObservableRef(AutomationObservable.ReportedReady), AutomationCompareOperator.Equal, new AutomationBooleanConstant(false)),
                    new AutomationNotCondition(Compare(new AutomationObservableRef(AutomationObservable.PhysicalReady), AutomationCompareOperator.Equal, new AutomationBooleanConstant(false))),
                ]),
            ]),
            [new IssueDishActionAutomationEffect(DishAction.StartWasher)]);
        var context = new AutomationEvaluationContext(2, true, true);

        var first = AutomationRuleEvaluator.Evaluate(rule, context);
        var second = AutomationRuleEvaluator.Evaluate(rule, context);

        Assert.Equal(first.ConditionMatched, second.ConditionMatched);
        Assert.Equal(first.SelectedEffects.ToArray(), second.SelectedEffects.ToArray());
        Assert.Equal(first.Trace.ObservedValues.ToArray(), second.Trace.ObservedValues.ToArray());
        Assert.Equal(first.Trace.Predicates.ToArray(), second.Trace.Predicates.ToArray());
        Assert.Equal(first.Trace.SelectedEffects.ToArray(), second.Trace.SelectedEffects.ToArray());
        Assert.True(first.ConditionMatched);
        Assert.Single(first.SelectedEffects);
        Assert.Equal(6, first.Trace.ObservedValues.Length);
        Assert.Equal(6, first.Trace.Predicates.Length);
        Assert.All(first.Trace.Predicates, predicate => Assert.False(string.IsNullOrWhiteSpace(predicate.Path)));
        Assert.Contains(first.Trace.Predicates, predicate => predicate.Expression == "ALL" && predicate.Result);
        Assert.Contains(first.Trace.Predicates, predicate => predicate.Expression == "ANY" && predicate.Result);
        Assert.Contains(first.Trace.Predicates, predicate => predicate.Expression == "NOT" && predicate.Result);
        Assert.Equal(AutomationValue.From(2), context.Resolve(AutomationObservable.RackCount));
        Assert.Equal(AutomationValue.From(true), context.Resolve(AutomationObservable.RackPresent));
    }

    [Fact]
    public void DisabledRuleSelectsNoEffectsAndDoesNotObserveInputs()
    {
        var rule = Rule(false, Compare(
            new AutomationObservableRef(AutomationObservable.ReportedReady),
            AutomationCompareOperator.Equal,
            new AutomationBooleanConstant(true)));

        var result = AutomationRuleEvaluator.Evaluate(rule, new(1, true, true));

        Assert.False(result.ConditionMatched);
        Assert.Empty(result.SelectedEffects);
        Assert.Empty(result.Trace.ObservedValues);
        Assert.Empty(result.Trace.Predicates);
        Assert.False(result.Trace.Enabled);
    }

    [Fact]
    public void ValidationReportsTargetedTypeOperatorShapeAndIdDiagnostics()
    {
        var invalid = new AutomationRule(
            new("Bad Rule"),
            true,
            new AutomationAllCondition(
            [
                Compare(new AutomationBooleanConstant(true), AutomationCompareOperator.GreaterThan, new AutomationIntegerConstant(0)),
                new AutomationAnyCondition([]),
            ]),
            []);

        var exception = Assert.Throws<AutomationIrValidationException>(() => AutomationRuleEvaluator.Validate(invalid));

        Assert.Contains(exception.Diagnostics, item => item.Path == "id");
        Assert.Contains(exception.Diagnostics, item => item.Path == "condition[0]" && item.Message.Contains("same type", StringComparison.Ordinal));
        Assert.Contains(exception.Diagnostics, item => item.Path == "condition[0].operator" && item.Message.Contains("Boolean", StringComparison.Ordinal));
        Assert.Contains(exception.Diagnostics, item => item.Path == "condition[1]" && item.Message.Contains("at least one", StringComparison.Ordinal));
        Assert.Contains(exception.Diagnostics, item => item.Path == "effects");
    }

    [Fact]
    public void EffectOutcomesAttachToImmutableEvaluationTraceInOrder()
    {
        var result = AutomationRuleEvaluator.Evaluate(Rule(true,
            Compare(new AutomationObservableRef(AutomationObservable.RackPresent), AutomationCompareOperator.Equal, new AutomationBooleanConstant(true))),
            new(1, true, true));
        var effect = Assert.Single(result.SelectedEffects);

        var trace = AutomationRuleEvaluator.WithOutcomes(result.Trace,
        [
            new(0, effect, true, "Washer started."),
        ]);

        var outcome = Assert.Single(trace.Outcomes);
        Assert.Equal(0, outcome.Order);
        Assert.True(outcome.Success);
        Assert.Equal("Washer started.", outcome.Message);
        Assert.Empty(result.Trace.Outcomes);
    }

    private static AutomationCompareCondition Compare(
        AutomationValueRef left,
        AutomationCompareOperator op,
        AutomationValueRef right) => new(left, op, right);

    private static AutomationRule Rule(bool enabled, AutomationCondition condition) => new(
        new("automation.rule.proof.washer-start"), enabled, condition,
        ImmutableArray.Create<AutomationEffect>(new IssueDishActionAutomationEffect(DishAction.StartWasher)));
}
