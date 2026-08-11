using Automation.Domain;

namespace Automation.Simulation;

public enum AutomationTraceOutcome
{
    PolicyConfigured,
    AutomaticStart,
    UnsafeStartRequested,
    UnsafeStartPrevented,
    IncidentInspected,
    ReplayWouldStart,
    ReplayPrevented,
}

public readonly record struct AutomationTraceEntry(
    SimulationTick Tick,
    AutomationTraceOutcome Outcome,
    DishKind? Kind,
    bool ReportedReady,
    bool PhysicalReady,
    WasherAutomationPolicy Policy);

public readonly record struct AutomationIncidentSnapshot(
    bool Recorded,
    SimulationTick OccurredAt,
    DishKind Kind,
    WasherAutomationPolicy OriginalPolicy,
    bool ReportedReady,
    bool PhysicalReady,
    int ReplayCount,
    bool HasReplay,
    WasherAutomationPolicy LastReplayPolicy,
    bool LastReplayWouldStart,
    bool RegressionPassed);

internal readonly record struct AutomationIncidentRecord(
    SimulationTick OccurredAt,
    DishKind Kind,
    WasherAutomationPolicy OriginalPolicy,
    bool ReportedReady,
    bool PhysicalReady);
