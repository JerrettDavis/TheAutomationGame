using System.Collections.Immutable;

namespace Automation.Domain;

public readonly record struct ProcessCaptureId(int Value);
public readonly record struct PlayerProcessArtifactId(int Value);
public readonly record struct ProcessStepId(int Value);

public enum ProcessCaptureSource
{
    ManualPlayerWork,
}

public enum ProcessCaptureEventKind
{
    Started,
    StepCaptured,
    Completed,
    DraftStarted,
    DraftChanged,
    VersionApplied,
    DraftDiscarded,
}

public enum ProcessRoutingPolicy
{
    CapturedOrder,
    PlatesFirst,
    GlassesFirst,
}

public sealed record CapturedProcessStep(
    ProcessStepId Id,
    int Sequence,
    SimulationTick ObservedAt,
    ActorId Actor,
    DishStationFixture Workstation,
    DishAction Action,
    DishKind ItemKind,
    DishState InputState,
    DishState OutputState,
    ActorId AssignedActor);

public sealed record ProcessCaptureProvenance(
    ProcessCaptureId CaptureId,
    ProcessCaptureSource Source,
    int WorldSeed,
    ActorId CapturedBy,
    SimulationTick StartedAt,
    SimulationTick CompletedAt);

public sealed record PlayerProcessVersion(
    int Version,
    ImmutableArray<CapturedProcessStep> Steps,
    ProcessCaptureProvenance Provenance,
    ProcessRoutingPolicy RoutingPolicy,
    ProcessEditProvenance? EditProvenance);

public sealed record ProcessEditProvenance(
    int BasedOnVersion,
    SimulationTick AppliedAt,
    ActorId Editor);

public sealed record PlayerOwnedProcessArtifact(
    PlayerProcessArtifactId Id,
    ActorId Owner,
    string Name,
    PlayerProcessVersion Baseline,
    PlayerProcessVersion Current);

public sealed record ActiveProcessCapture(
    ProcessCaptureId Id,
    string Name,
    SimulationTick StartedAt,
    ImmutableArray<CapturedProcessStep> Steps);

public sealed record ProcessCaptureEvent(
    SimulationTick Tick,
    ProcessCaptureId CaptureId,
    ProcessCaptureEventKind Kind,
    int? StepSequence,
    DishAction? Action);

public sealed record ProcessEditDiagnostic(
    string Code,
    string Message,
    ProcessStepId? StepId = null);

public sealed record ProcessEditDraft(
    PlayerProcessArtifactId ArtifactId,
    int BasedOnVersion,
    ImmutableArray<CapturedProcessStep> Steps,
    ProcessRoutingPolicy RoutingPolicy,
    ImmutableArray<ProcessEditDiagnostic> Diagnostics);

public sealed record ProcessCaptureSnapshot(
    ActiveProcessCapture? Active,
    IReadOnlyList<PlayerOwnedProcessArtifact> Artifacts,
    IReadOnlyList<ProcessCaptureEvent> Events,
    ProcessEditDraft? ActiveEdit,
    PlayerProcessArtifactId? AppliedArtifactId);
