using Automation.Content;
using Automation.Domain;

namespace Automation.Client.Stride;

public sealed record PatternCodexEvidenceView(
    string Milestone,
    string Place,
    string Problem,
    string Solution,
    string Consequence,
    string ReplayReference);

public sealed record PatternCodexView(
    string Title,
    string Status,
    string NameStatus,
    string EvidenceSummary,
    IReadOnlyList<PatternCodexEvidenceView> Evidence);

public static class PatternCodexPresenter
{
    public static PatternCodexView Present(PatternContentDefinition definition, PatternKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(knowledge);
        if (knowledge.Pattern != definition.PatternId)
            throw new ArgumentException("Knowledge does not match the Codex definition.", nameof(knowledge));
        var named = knowledge.Has(PatternKnowledgeMilestone.Named);
        var evidence = knowledge.Evidence.Select(item => new PatternCodexEvidenceView(
            item.Milestone.ToString().ToUpperInvariant(), item.Place.ToUpperInvariant(), item.Problem.ToUpperInvariant(),
            item.SolutionShape.ToUpperInvariant(), item.Consequence.ToUpperInvariant(), item.ReplayReference.ToUpperInvariant())).ToArray();
        return new(
            named ? definition.ExternalCatalogId.ToUpperInvariant() : definition.PreNameTitle,
            knowledge.Has(PatternKnowledgeMilestone.Recognized) ? "RECOGNIZED FROM YOUR WORK" : "MORE EVIDENCE NEEDED",
            named ? $"CONVENTIONAL NAME  {definition.ExternalCatalogId.ToUpperInvariant()}" : "CONVENTIONAL NAME  NOT RECORDED",
            $"{evidence.Length} RESTAURANT RECORDS  •  {string.Join(" / ", knowledge.Milestones.Select(item => item.ToString().ToUpperInvariant()))}",
            evidence);
    }
}
