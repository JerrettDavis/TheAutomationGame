using Automation.Content;
using Automation.Domain;

namespace Automation.Persistence;

public static class PatternNamingService
{
    public static PatternKnowledgeProfile RecordReflection(
        PatternKnowledgeProfile profile,
        PatternContentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(definition);
        var knowledge = profile.For(definition.PatternId);
        if (knowledge.Has(PatternKnowledgeMilestone.Named)) return profile;
        if (!knowledge.Has(PatternKnowledgeMilestone.Recognized))
            throw new InvalidOperationException("The conventional name cannot be recorded before qualifying evidence is recognized.");

        var basis = knowledge.Conclusions
            .SingleOrDefault(item => item.Milestone == PatternKnowledgeMilestone.Recognized)?.Basis
            ?? knowledge.Evidence.LastOrDefault(item => item.Milestone == PatternKnowledgeMilestone.Applied)?.Id
            ?? throw new InvalidOperationException("Recognized pattern knowledge has no application evidence for naming.");
        return profile.Put(knowledge.Conclude(PatternKnowledgeMilestone.Named, basis));
    }
}
