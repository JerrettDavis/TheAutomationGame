using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class CharacterDialogueContentTests
{
    [Fact]
    public void ProductionBarksResolveNamedCharactersForThreeAuthoritativeContexts()
    {
        var catalog = ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift);
        var router = new CharacterDialogueRouter(catalog);
        var expected = new[]
        {
            (new DishStationNarrativeEvent(new(30), DishStationNarrativeEventKind.QueuePressure, DishStationQuestId.FindTheConstraint),
                "dialogue.restaurant.tessa.glass-pressure", "character.restaurant.tessa-brooks", CharacterDialoguePriority.Important),
            (new DishStationNarrativeEvent(new(236), DishStationNarrativeEventKind.AutomationIncident, DishStationQuestId.InvestigateTheSignal),
                "dialogue.restaurant.devon.ready-disagrees", "character.restaurant.devon-price", CharacterDialoguePriority.Critical),
            (new DishStationNarrativeEvent(new(285), DishStationNarrativeEventKind.ShiftSucceeded, DishStationQuestId.OwnTheShift),
                "dialogue.restaurant.avery.shift-held", "character.restaurant.avery-chen", CharacterDialoguePriority.Important),
        };

        foreach (var (narrativeEvent, barkId, speakerId, priority) in expected)
        {
            var bark = Assert.IsType<ResolvedCharacterBark>(router.Resolve(narrativeEvent));
            Assert.Equal(barkId, bark.Id.Value);
            Assert.Equal(speakerId, bark.Speaker.Value);
            Assert.Equal(priority, bark.Priority);
            Assert.InRange(bark.Line.Length, 1, 160);
        }
    }

    [Fact]
    public void PriorityAndCooldownResolutionAreDeterministic()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var extra = """
              - id: dialogue.restaurant.tessa.glass-pressure-critical
                quest: quest.restaurant.first-shift.find-the-constraint
                trigger: queue-pressure
                priority: critical
                cooldown_ticks: 60
                line: Glass service is stopped. We need the queue cause now.
        """;
        var withPriority = ReplaceOnce(yaml,
            "      - id: dialogue.restaurant.tessa.glass-pressure\n",
            $"{extra}\n      - id: dialogue.restaurant.tessa.glass-pressure\n");
        var priorityRouter = new CharacterDialogueRouter(ContentCompilerV1.Compile(withPriority, "priority.yaml"));

        var first = Assert.IsType<ResolvedCharacterBark>(priorityRouter.Resolve(
            new(new(30), DishStationNarrativeEventKind.QueuePressure, DishStationQuestId.FindTheConstraint)));
        Assert.Equal("dialogue.restaurant.tessa.glass-pressure-critical", first.Id.Value);

        var productionRouter = new CharacterDialogueRouter(ContentCompilerV1.Compile(yaml, "cooldown.yaml"));
        Assert.NotNull(productionRouter.Resolve(new(new(30), DishStationNarrativeEventKind.QueuePressure, DishStationQuestId.FindTheConstraint)));
        Assert.Null(productionRouter.Resolve(new(new(89), DishStationNarrativeEventKind.QueuePressure, DishStationQuestId.FindTheConstraint)));
        Assert.NotNull(productionRouter.Resolve(new(new(90), DishStationNarrativeEventKind.QueuePressure, DishStationQuestId.FindTheConstraint)));
    }

    [Theory]
    [InlineData("trigger: queue-pressure", "trigger: unknown-pressure", "trigger", "Unsupported value")]
    [InlineData("priority: important", "priority: urgent", "priority", "Unsupported value")]
    [InlineData("cooldown_ticks: 60", "cooldown_ticks: -1", "cooldown_ticks", "non-negative")]
    public void InvalidBarkFieldsFailAtSemanticPaths(string find, string replacement, string path, string message)
    {
        var yaml = ReplaceOnce(File.ReadAllText(ContentTestPaths.FirstShift), find, replacement);

        var failure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(yaml, "invalid-bark.yaml"));

        Assert.Contains(failure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains(path, StringComparison.Ordinal) &&
            diagnostic.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateUnknownAndNonparticipantBarkReferencesFailClearly()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var duplicate = ReplaceOnce(yaml,
            "id: dialogue.restaurant.devon.ready-disagrees",
            "id: dialogue.restaurant.tessa.glass-pressure");
        var duplicateFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(duplicate, "duplicate-bark.yaml"));
        Assert.Contains(duplicateFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("barks", StringComparison.Ordinal) && diagnostic.Message.Contains("globally unique", StringComparison.Ordinal));

        var unknown = ReplaceOnce(yaml,
            "quest: quest.restaurant.first-shift.find-the-constraint\n        trigger: queue-pressure",
            "quest: quest.restaurant.first-shift.missing\n        trigger: queue-pressure");
        var unknownFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(unknown, "unknown-bark-quest.yaml"));
        Assert.Contains(unknownFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("barks", StringComparison.Ordinal) && diagnostic.Message.Contains("Unknown quest", StringComparison.Ordinal));

        var nonparticipant = ReplaceOnce(yaml,
            "quest: quest.restaurant.first-shift.find-the-constraint\n        trigger: queue-pressure",
            "quest: quest.restaurant.first-shift.investigate-the-signal\n        trigger: queue-pressure");
        var nonparticipantFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(nonparticipant, "nonparticipant-bark.yaml"));
        Assert.Contains(nonparticipantFailure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("barks", StringComparison.Ordinal) && diagnostic.Message.Contains("not a participant", StringComparison.Ordinal));
    }

    private static string ReplaceOnce(string value, string find, string replacement)
    {
        var index = value.IndexOf(find, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Expected fixture text was not found: {find}");
        return string.Concat(value.AsSpan(0, index), replacement, value.AsSpan(index + find.Length));
    }
}
