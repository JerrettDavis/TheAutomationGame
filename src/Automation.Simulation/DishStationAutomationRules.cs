using Automation.Domain;

namespace Automation.Simulation;

public static class DishStationAutomationRules
{
    public static AutomationRule ForPolicy(WasherAutomationPolicy policy) => new(
        new(policy.RequirePhysicalReady
            ? "automation.rule.dish-station.start-washer-corroborated"
            : "automation.rule.dish-station.start-washer-reported"),
        policy.Enabled,
        new AutomationAllCondition(policy.RequirePhysicalReady
            ?
            [
                IsTrue(AutomationObservable.RackPresent),
                IsTrue(AutomationObservable.ReportedReady),
                IsTrue(AutomationObservable.PhysicalReady),
            ]
            :
            [
                IsTrue(AutomationObservable.RackPresent),
                IsTrue(AutomationObservable.ReportedReady),
            ]),
        [new IssueDishActionAutomationEffect(DishAction.StartWasher)]);

    public static AutomationRuleEvaluationResult Evaluate(
        WasherAutomationPolicy policy,
        int rackCount,
        bool reportedReady,
        bool physicalReady) => AutomationRuleEvaluator.Evaluate(
            ForPolicy(policy),
            new(rackCount, reportedReady, physicalReady));

    private static AutomationCompareCondition IsTrue(AutomationObservable observable) => new(
        new AutomationObservableRef(observable),
        AutomationCompareOperator.Equal,
        new AutomationBooleanConstant(true));
}
