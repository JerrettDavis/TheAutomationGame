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

public sealed record PatternKnowledgeConclusion(
    PatternKnowledgeMilestone Milestone,
    PatternEvidenceId Basis)
{
    public PatternKnowledgeConclusion Validate()
    {
        if (Milestone is not (PatternKnowledgeMilestone.Recognized or PatternKnowledgeMilestone.Named or PatternKnowledgeMilestone.Mastered))
            throw new ArgumentException($"{Milestone} is direct evidence, not a knowledge conclusion.");
        return this;
    }
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
    public ImmutableArray<PatternKnowledgeConclusion> Conclusions { get; init; } = [];

    public static PatternKnowledge Empty(PatternId pattern) => new(pattern, [], []);

    public bool Has(PatternKnowledgeMilestone milestone) => Milestones.Contains(milestone);

    public PatternKnowledge Validate()
    {
        if (Milestones.IsDefault || Evidence.IsDefault || Conclusions.IsDefault)
            throw new ArgumentException("Pattern knowledge collections must be initialized.");
        if (Milestones.Distinct().Count() != Milestones.Length)
            throw new ArgumentException("Pattern milestones must be unique.");
        if (Evidence.Select(item => item.Id).Distinct().Count() != Evidence.Length)
            throw new ArgumentException("Pattern evidence IDs must be unique.");
        if (Conclusions.Select(item => item.Milestone).Distinct().Count() != Conclusions.Length)
            throw new ArgumentException("Pattern conclusions must be unique by milestone.");
        foreach (var evidence in Evidence)
        {
            evidence.Validate();
            if (evidence.Pattern != Pattern) throw new ArgumentException("Evidence belongs to another pattern.");
        }
        foreach (var conclusion in Conclusions)
        {
            conclusion.Validate();
            if (!Evidence.Any(item => item.Id == conclusion.Basis))
                throw new ArgumentException("A pattern conclusion cites missing evidence.");
        }
        var expected = Evidence.Select(item => item.Milestone)
            .Concat(Conclusions.Select(item => item.Milestone)).Distinct().Order().ToArray();
        if (!Milestones.SequenceEqual(expected))
            throw new ArgumentException("Pattern milestones must be backed by evidence or a cited conclusion.");
        if (Has(PatternKnowledgeMilestone.Named) && !Has(PatternKnowledgeMilestone.Recognized))
            throw new ArgumentException("Named pattern knowledge must also be recognized.");
        if (Has(PatternKnowledgeMilestone.Mastered) && !Has(PatternKnowledgeMilestone.Named))
            throw new ArgumentException("Mastered pattern knowledge must also be named.");
        return this;
    }

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
        if (milestone == PatternKnowledgeMilestone.Named && !Has(PatternKnowledgeMilestone.Recognized))
            throw new InvalidOperationException("A pattern cannot be named before it is recognized.");
        if (milestone == PatternKnowledgeMilestone.Mastered && !Has(PatternKnowledgeMilestone.Named))
            throw new InvalidOperationException("A pattern cannot be mastered before it is named.");
        if (Conclusions.Any(item => item.Milestone == milestone)) return this;
        return this with
        {
            Milestones = AddMilestone(Milestones, milestone),
            Conclusions = Conclusions.Add(new(milestone, basis)).OrderBy(item => item.Milestone).ToImmutableArray(),
        };
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

    public PatternKnowledgeProfile Validate()
    {
        if (Patterns.IsDefault) throw new ArgumentException("Pattern profile collection must be initialized.");
        if (Patterns.Select(item => item.Pattern).Distinct().Count() != Patterns.Length)
            throw new ArgumentException("Pattern journals must be unique.");
        foreach (var knowledge in Patterns) knowledge.Validate();
        return this;
    }
}
