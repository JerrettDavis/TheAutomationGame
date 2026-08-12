using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class FirstShiftNarrativePassTests
{
    private static readonly string[] ForbiddenProductionTerms =
        ["simulation", "renderer", "playtest", "developer", "debug", "god mode", " tick", "ticks"];

    [Fact]
    public void BriefingAndDebriefPresentationComeFromCompiledScenarioNarrative()
    {
        var yaml = File.ReadAllText(ProductionNarrativePath());
        var changed = yaml.Replace("SERVICE OPENS SOON", "DINNER STARTS SOON", StringComparison.Ordinal)
            .Replace("Avery gives you ownership", "Avery trusts you with ownership", StringComparison.Ordinal);
        var narrative = DishStationFirstHoursContent.Compile(changed, "changed-chapter.yaml");

        var briefing = FirstShiftNarrativePresenter.Briefing(0, narrative);
        var debrief = FirstShiftNarrativePresenter.Debrief(narrative);

        Assert.Equal("DINNER STARTS SOON", briefing.Title);
        Assert.Contains("AVERY CHEN", briefing.Body, StringComparison.Ordinal);
        Assert.Contains("AVERY TRUSTS YOU WITH OWNERSHIP", debrief.Summary, StringComparison.Ordinal);
        Assert.Equal(3, debrief.Questions.Count);
        var title = FirstShiftNarrativePresenter.WindowTitle(false, false, DishStationQuestId.InvestigateTheSignal, narrative);
        Assert.Equal("The Automation Game — ROSSI'S / FIRST SHIFT — IT SAID IT WAS READY", title);
        Assert.DoesNotContain('[', title);
        Assert.DoesNotContain("tick", title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionChapterUsesNamedCharacterBeatsWithoutDeveloperLanguage()
    {
        var narrative = DishStationFirstHoursContent.Narrative;
        var playerText = string.Join('\n',
            narrative.Chapter.Briefing.SelectMany(page => new[] { page.Title, page.Body })
                .Concat([narrative.Chapter.ChapterTitle, narrative.Chapter.DebriefSummary])
                .Concat(narrative.Chapter.DebriefQuestions)
                .Concat(narrative.Quests.SelectMany(quest => new[]
                    { quest.Title, quest.Situation, quest.ObservableOutcome, quest.Discovery, quest.UnlockRationale }
                    .Concat(quest.Steps.Select(step => step.Text))))
                .Concat(DishStationFirstHoursContent.Catalog.Characters.SelectMany(character => character.Barks.Select(bark => bark.Line))));

        foreach (var name in new[] { "Avery", "Ray", "Jules", "Tessa", "Devon" })
            Assert.Contains(name, playerText, StringComparison.Ordinal);
        foreach (var forbidden in ForbiddenProductionTerms)
            Assert.DoesNotContain(forbidden, playerText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullFirstShiftCompletesAndReplaysWithoutDeveloperCommandsOrLeakingDiagnostics()
    {
        var world = DishStationFirstShiftReferenceRun.Run(42, DishStationFirstHoursContent.ScenarioConfiguration);
        var snapshot = world.Snapshot();

        Assert.Equal(DishTutorialStage.EpisodeComplete, snapshot.TutorialStage);
        Assert.Equal(ShiftTrialStatus.Passed, snapshot.ShiftTrial.Status);
        Assert.All(snapshot.Progression.Quests, quest => Assert.True(quest.Complete));
        Assert.Equal(3400, snapshot.Progression.Experience);

        var save = world.CreateReplaySave();
        var forbiddenCommands = new[]
        {
            RecordedCommandKind.AddDirtyDishes,
            RecordedCommandKind.ConfigureDishSupply,
            RecordedCommandKind.ResetDishStation,
            RecordedCommandKind.InjectStickyReadyFault,
            RecordedCommandKind.ConfigureWasherAutomation,
        };
        Assert.DoesNotContain(save.CommandInvocations, invocation => forbiddenCommands.Contains(invocation.Command.CommandKind));

        var visibleRuntimeText = string.Join('\n', world.Notifications.Select(notification => $"{notification.Title}: {notification.Message}"));
        foreach (var forbidden in ForbiddenProductionTerms)
            Assert.DoesNotContain(forbidden, visibleRuntimeText, StringComparison.OrdinalIgnoreCase);

        var restored = DishStationWorld.Restore(save).Snapshot();
        Assert.Equal(snapshot.ShiftTrial, restored.ShiftTrial);
        Assert.Equal(snapshot.ShiftReport, restored.ShiftReport);
        Assert.Equal(snapshot.Progression.Quests.ToArray(), restored.Progression.Quests.ToArray());
        Assert.Equal(snapshot.NarrativeEvents.ToArray(), restored.NarrativeEvents.ToArray());
    }

    private static string ProductionNarrativePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheAutomationGame.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "content", "restaurant", "first-shift.yaml");
    }
}
