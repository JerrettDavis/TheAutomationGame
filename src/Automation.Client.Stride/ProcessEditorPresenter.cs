using Automation.Domain;

namespace Automation.Client.Stride;

public sealed record ProcessEditorStepView(
    ProcessStepId Id,
    int Sequence,
    string Action,
    string Workstation,
    string Transition,
    string Assignment,
    bool Selected);

public sealed record ProcessEditorView(
    PlayerProcessArtifactId ArtifactId,
    int BaselineVersion,
    int CurrentVersion,
    int BasedOnVersion,
    string Name,
    string Routing,
    IReadOnlyList<ProcessEditorStepView> Steps,
    string Validation,
    bool CanApply);

public static class ProcessEditorPresenter
{
    public static ProcessEditorView Present(ProcessCaptureSnapshot capture, int selectedIndex)
    {
        ArgumentNullException.ThrowIfNull(capture);
        var draft = capture.ActiveEdit ?? throw new InvalidOperationException("No process edit draft is active.");
        var artifact = capture.Artifacts.Single(candidate => candidate.Id == draft.ArtifactId);
        var selected = Math.Clamp(selectedIndex, 0, Math.Max(0, draft.Steps.Length - 1));
        var rows = draft.Steps.Select((step, index) => new ProcessEditorStepView(
            step.Id,
            step.Sequence,
            step.Action == DishAction.DryAndRestock ? "DRY + RESTOCK" : Split(step.Action.ToString()),
            Split(step.Workstation.ToString()),
            $"{Split(step.InputState.ToString())} -> {Split(step.OutputState.ToString())}",
            step.AssignedActor.Value == 1 ? "NEW HIRE" : "PLAYER",
            index == selected)).ToArray();
        var validation = draft.Diagnostics.Length == 0
            ? "VALID — READY TO APPLY"
            : $"BLOCKED — {draft.Diagnostics[0].Message.ToUpperInvariant()}";
        return new(
            artifact.Id,
            artifact.Baseline.Version,
            artifact.Current.Version,
            draft.BasedOnVersion,
            artifact.Name,
            Split(draft.RoutingPolicy.ToString()),
            rows,
            validation,
            draft.Diagnostics.Length == 0);
    }

    private static string Split(string value) => string.Concat(value.Select((character, index) =>
        index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]) ? $" {character}" : character.ToString())).ToUpperInvariant();
}
