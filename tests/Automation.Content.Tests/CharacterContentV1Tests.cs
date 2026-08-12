using Automation.Content;

namespace Automation.Content.Tests;

public sealed class CharacterContentV1Tests
{
    private static readonly string[] ExpectedIds =
    [
        "character.restaurant.avery-chen",
        "character.restaurant.devon-price",
        "character.restaurant.jules-martin",
        "character.restaurant.ray-morales",
        "character.restaurant.tessa-brooks",
    ];

    [Fact]
    public void ProductionRosterDefinesFiveCompleteStableCharacters()
    {
        var catalog = ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift);

        Assert.Equal(ExpectedIds, catalog.Characters.Select(character => character.Id.Value));
        Assert.All(catalog.Characters, character =>
        {
            Assert.False(string.IsNullOrWhiteSpace(character.Motivation));
            Assert.NotEmpty(character.KnownFacts);
            Assert.NotEmpty(character.BlindSpots);
            Assert.NotEmpty(character.Authority);
            Assert.True(character.Relationships.Length >= 2);
            Assert.StartsWith("role.restaurant.", character.Role.Value, StringComparison.Ordinal);
            Assert.StartsWith("presentation.character.restaurant.", character.Presentation.Value, StringComparison.Ordinal);
            Assert.Equal("presentation.fallback.character", character.PresentationFallback.Value);
        });
    }

    [Fact]
    public void EveryFirstShiftQuestUsesIntendedScenarioRosterParticipants()
    {
        var catalog = ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift);
        var scenario = catalog.Scenarios.Single(candidate => candidate.Id.Value == DishStationFirstHoursContent.ScenarioId);
        var expected = new Dictionary<string, string[]>
        {
            ["clock-in"] = ["character.restaurant.avery-chen", "character.restaurant.ray-morales"],
            ["find-the-constraint"] = ["character.restaurant.ray-morales", "character.restaurant.tessa-brooks"],
            ["improve-the-flow"] = ["character.restaurant.ray-morales", "character.restaurant.tessa-brooks"],
            ["transfer-the-work"] = ["character.restaurant.avery-chen", "character.restaurant.jules-martin", "character.restaurant.ray-morales"],
            ["capture-the-exception"] = ["character.restaurant.jules-martin", "character.restaurant.ray-morales"],
            ["investigate-the-signal"] = ["character.restaurant.avery-chen", "character.restaurant.devon-price"],
            ["prove-the-fix"] = ["character.restaurant.avery-chen", "character.restaurant.devon-price"],
            ["own-the-shift"] = ExpectedIds,
        };

        Assert.Equal(5, scenario.Characters.Length);
        foreach (var quest in catalog.Quests.Where(candidate => candidate.Scenario == scenario.Id))
        {
            var runtimeId = quest.Narrative!.RuntimeId;
            Assert.Equal(expected[runtimeId], quest.Participants.Select(participant => participant.Value));
            Assert.All(quest.Participants, participant => Assert.Contains(participant, scenario.Characters));
        }
    }

    [Fact]
    public void MissingAndOffRosterQuestParticipantsFailAtParticipantPath()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var missing = yaml.Replace(
            "participants: [character.restaurant.avery-chen, character.restaurant.ray-morales]",
            "participants: [character.restaurant.missing, character.restaurant.ray-morales]",
            StringComparison.Ordinal);
        var missingFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(missing, "missing-participant.yaml"));
        Assert.Contains(missingFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("participants", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("Unknown character", StringComparison.Ordinal));

        var extra = """
              - id: character.restaurant.casey-proof
                industry: industry.restaurant
                display_name: Casey Proof
                role: role.restaurant.proof-worker
                motivation: Prove off-roster validation.
                known_facts: [knowledge.restaurant.proof]
                blind_spots: [knowledge.restaurant.proof-gap]
                authority: [authority.restaurant.proof]
                relationships: []
                presentation: presentation.character.restaurant.casey-proof
                presentation_fallback: presentation.fallback.character

            """;
        var offRoster = yaml.Replace("scenarios:\n", $"{extra}scenarios:\n", StringComparison.Ordinal)
            .Replace(
                "participants: [character.restaurant.avery-chen, character.restaurant.ray-morales]",
                "participants: [character.restaurant.casey-proof, character.restaurant.ray-morales]",
                StringComparison.Ordinal);
        var offRosterFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(offRoster, "off-roster.yaml"));
        Assert.Contains(offRosterFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("participants", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("not in scenario", StringComparison.Ordinal));
    }

    [Fact]
    public void SelfAndDuplicateCharacterRelationshipsFailAtRelationshipPath()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var self = ReplaceOnce(yaml,
            "      - character: character.restaurant.ray-morales\n        kind: relationship.restaurant.manager-to-domain-expert",
            "      - character: character.restaurant.avery-chen\n        kind: relationship.restaurant.manager-to-domain-expert");
        var selfFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(self, "self-relationship.yaml"));
        Assert.Contains(selfFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("relationships", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("itself", StringComparison.Ordinal));

        var duplicate = ReplaceOnce(yaml,
            "      - character: character.restaurant.tessa-brooks\n        kind: relationship.restaurant.manager-to-service-liaison",
            "      - character: character.restaurant.ray-morales\n        kind: relationship.restaurant.manager-to-service-liaison");
        var duplicateFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(duplicate, "duplicate-relationship.yaml"));
        Assert.Contains(duplicateFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("relationships", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("unique", StringComparison.Ordinal));
    }

    [Fact]
    public void CrossIndustryCharacterRelationshipFailsAtRelationshipPath()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var changed = ReplaceOnce(yaml,
            "  - id: character.restaurant.ray-morales\n    industry: industry.restaurant",
            "  - id: character.restaurant.ray-morales\n    industry: industry.external");

        var failure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(changed, "cross-industry.yaml"));

        Assert.Contains(failure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("relationships", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("same industry", StringComparison.Ordinal));
    }

    private static string ReplaceOnce(string value, string find, string replacement)
    {
        var index = value.IndexOf(find, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Expected fixture text was not found: {find}");
        return string.Concat(value.AsSpan(0, index), replacement, value.AsSpan(index + find.Length));
    }
}
