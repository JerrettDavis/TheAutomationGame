using System.Text;
using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class FirstShiftNarrativeTests
{
    [Fact]
    public void ProductionNarrativePreservesReferenceQuestMetadataOrderAndRewards()
    {
        var quests = DishStationFirstHoursContent.Narrative.Quests;
        var expected = new[]
        {
            (DishStationQuestId.ClockIn, "CLOCK IN", 100, CareerCapability.StateLens),
            (DishStationQuestId.FindTheConstraint, "WHERE DID THE GLASSES GO?", 200, CareerCapability.LayoutEditor),
            (DishStationQuestId.ImproveTheFlow, "DINNER RUSH", 300, CareerCapability.KnowledgeLens),
            (DishStationQuestId.TransferTheWork, "THE NEW HIRE", 300, CareerCapability.ExceptionNotebook),
            (DishStationQuestId.CaptureTheException, "THE RARE TRAY", 400, CareerCapability.AutomationWorkbench),
            (DishStationQuestId.InvestigateTheSignal, "IT SAID IT WAS READY", 500, CareerCapability.RuntimeTrace),
            (DishStationQuestId.ProveTheFix, "PROVE THE FIX", 700, CareerCapability.ResponsibilityMap),
            (DishStationQuestId.OwnTheShift, "OWN THE SHIFT", 900, CareerCapability.ShiftScorecard),
        };

        Assert.Equal(expected.Length, quests.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(index + 1, quests[index].Sequence);
            Assert.Equal(expected[index].Item1, quests[index].Id);
            Assert.Equal(expected[index].Item2, quests[index].Title);
            Assert.Equal(expected[index].Item3, quests[index].ExperienceReward);
            Assert.Equal(expected[index].Item4, quests[index].CapabilityReward);
            Assert.DoesNotContain("press", quests[index].ObservableOutcome, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("key", quests[index].ObservableOutcome, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ProductionNarrativePreservesResolvableStableQuestParticipants()
    {
        var scenarioRoster = DishStationFirstHoursContent.Scenario.Characters.ToHashSet();

        Assert.All(DishStationFirstHoursContent.Quests, quest =>
        {
            Assert.NotEmpty(quest.Participants);
            Assert.All(quest.Participants, participant =>
            {
                Assert.Contains(participant, scenarioRoster);
                var character = DishStationFirstHoursContent.Character(participant);
                Assert.Equal(participant, character.Id);
                Assert.False(string.IsNullOrWhiteSpace(character.DisplayName));
                Assert.StartsWith("role.restaurant.", character.Role.Value, StringComparison.Ordinal);
            });
        });
    }

    [Fact]
    public void ProductionNarrativeCoversEveryAuthoritativeTutorialStageExactlyOnce()
    {
        var authored = DishStationFirstHoursContent.Narrative.Quests
            .SelectMany(quest => quest.Steps)
            .Select(step => step.Id)
            .ToArray();
        var expected = Enum.GetValues<DishTutorialStage>().Select(stage => Token(stage.ToString())).ToArray();

        Assert.Equal(expected.Length, authored.Length);
        Assert.Equal(expected.Order(StringComparer.Ordinal), authored.Order(StringComparer.Ordinal));
        foreach (var stage in Enum.GetValues<DishTutorialStage>())
        {
            var presented = GameplayHudPresenter.GuidedGoalHint(stage, InputBindingProfile.Default);
            Assert.False(string.IsNullOrWhiteSpace(presented));
            Assert.DoesNotContain("{binding}", presented, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ContentOnlyGuidanceChangeReachesClientPresentation()
    {
        var yaml = File.ReadAllText(ProductionNarrativePath());
        var changed = yaml.Replace("RESTOCK ONE CLEAN PLATE", "RESTOCK ONE CLEAN DISH", StringComparison.Ordinal);
        Assert.NotEqual(yaml, changed);

        var baselineCatalog = ContentCompilerV1.Compile(yaml, "first-shift.yaml");
        var changedCatalog = ContentCompilerV1.Compile(changed, "changed-first-shift.yaml");
        Assert.NotEqual(baselineCatalog.Manifest.Sha256, changedCatalog.Manifest.Sha256);
        var narrative = DishStationFirstHoursContent.FromCatalog(changedCatalog, "changed-first-shift.yaml");
        var presented = GameplayHudPresenter.GuidedGoalHint(
            DishTutorialStage.RestockFirstDish,
            InputBindingProfile.Default,
            narrative);

        Assert.Equal("RESTOCK ONE CLEAN DISH", presented);
    }

    [Fact]
    public void AuthoredLogicalInputPlaceholderUsesCurrentBinding()
    {
        var bindings = InputBindingProfile.Default.WithBinding(GameInputAction.ToggleRush, KeyboardKey.Digit5);

        var presented = GameplayHudPresenter.GuidedGoalHint(DishTutorialStage.EnableDinnerRush, bindings);

        Assert.Equal("LET TESSA OPEN DINNER SERVICE WITH 5", presented);
        Assert.DoesNotContain("{binding}", presented, StringComparison.Ordinal);
    }

    [Fact]
    public void RewardDriftFromAuthoritativeProgressionFailsClearly()
    {
        var yaml = File.ReadAllText(ProductionNarrativePath());
        var changed = yaml.Replace("experience: 100", "experience: 101", StringComparison.Ordinal);

        var exception = Assert.Throws<ContentCompilationException>(() =>
            DishStationFirstHoursContent.Compile(changed, "bad-reward.yaml"));

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("narrative.reward.experience", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("authoritative ClockIn", StringComparison.Ordinal));
    }

    private static string ProductionNarrativePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheAutomationGame.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "content", "restaurant", "first-shift.yaml");
    }

    private static string Token(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character)) result.Append('-');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
