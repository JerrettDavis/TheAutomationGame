using System.Collections.Immutable;

namespace Automation.Domain;

public readonly record struct PatternId
{
    public PatternId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("pattern.", StringComparison.Ordinal) ||
            value.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '-')))
            throw new ArgumentException("Pattern ID must be a lowercase semantic ID beginning with 'pattern.'.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct PatternEvidenceId
{
    public PatternEvidenceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
            throw new ArgumentException("Pattern evidence ID must be nonempty and contain no whitespace.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public enum PatternProblemSignature
{
    InterchangeablePolicy,
}

public enum PatternKnowledgeMilestone
{
    Encountered,
    Applied,
    Recognized,
    Named,
    Transferred,
    StressTested,
    Composed,
    Expressed,
    Mastered,
}

public sealed record PatternEvidence(
    PatternEvidenceId Id,
    PatternId Pattern,
    PatternKnowledgeMilestone Milestone,
    string IndustryId,
    string ScenarioId,
    string QuestId,
    string Place,
    PatternProblemSignature ProblemSignature,
    string Problem,
    string SolutionShape,
    string Consequence,
    int Sequence,
    string ReplayReference)
{
    public PatternEvidence Validate()
    {
        if (Milestone is PatternKnowledgeMilestone.Recognized or PatternKnowledgeMilestone.Named or PatternKnowledgeMilestone.Mastered)
            throw new ArgumentException($"{Milestone} is a knowledge conclusion, not a direct evidence kind.");
        Require(IndustryId, nameof(IndustryId));
        Require(ScenarioId, nameof(ScenarioId));
        Require(QuestId, nameof(QuestId));
        Require(Place, nameof(Place));
        Require(Problem, nameof(Problem));
        Require(SolutionShape, nameof(SolutionShape));
        Require(Consequence, nameof(Consequence));
        Require(ReplayReference, nameof(ReplayReference));
        if (Sequence <= 0) throw new ArgumentOutOfRangeException(nameof(Sequence));
        return this;
    }

    private static void Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Pattern evidence text must be nonempty.", name);
    }
}

public sealed record PatternKnowledge(
    PatternId Pattern,
    ImmutableArray<PatternKnowledgeMilestone> Milestones,
    ImmutableArray<PatternEvidence> Evidence)
{
    public static PatternKnowledge Empty(PatternId pattern) => new(pattern, [], []);

    public bool Has(PatternKnowledgeMilestone milestone) => Milestones.Contains(milestone);

    public PatternKnowledge Record(PatternEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        evidence.Validate();
        if (evidence.Pattern != Pattern) throw new ArgumentException("Evidence belongs to another pattern.", nameof(evidence));
        if (Evidence.Any(item => item.Id == evidence.Id)) return this;
        return this with
        {
            Evidence = Evidence.Add(evidence).OrderBy(item => item.Sequence).ThenBy(item => item.Id.Value).ToImmutableArray(),
            Milestones = AddMilestone(Milestones, evidence.Milestone),
        };
    }

    public PatternKnowledge Conclude(PatternKnowledgeMilestone milestone, PatternEvidenceId basis)
    {
        if (milestone is not (PatternKnowledgeMilestone.Recognized or PatternKnowledgeMilestone.Named or PatternKnowledgeMilestone.Mastered))
            throw new ArgumentException($"{milestone} must be recorded as direct evidence.", nameof(milestone));
        if (!Evidence.Any(item => item.Id == basis))
            throw new ArgumentException("A knowledge conclusion must cite evidence already in the journal.", nameof(basis));
        return this with { Milestones = AddMilestone(Milestones, milestone) };
    }

    private static ImmutableArray<PatternKnowledgeMilestone> AddMilestone(
        ImmutableArray<PatternKnowledgeMilestone> milestones,
        PatternKnowledgeMilestone milestone) => milestones.Contains(milestone)
        ? milestones
        : milestones.Add(milestone).Order().ToImmutableArray();
}

public sealed record PatternKnowledgeProfile(ImmutableArray<PatternKnowledge> Patterns)
{
    public static PatternKnowledgeProfile Empty { get; } = new([]);

    public PatternKnowledge For(PatternId pattern) =>
        Patterns.SingleOrDefault(item => item.Pattern == pattern) ?? PatternKnowledge.Empty(pattern);

    public PatternKnowledgeProfile Put(PatternKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        var remaining = Patterns.Where(item => item.Pattern != knowledge.Pattern);
        return new(remaining.Append(knowledge).OrderBy(item => item.Pattern.Value).ToImmutableArray());
    }
}
