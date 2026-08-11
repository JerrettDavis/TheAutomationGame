using System.Reflection;
using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;
using Automation.Tools;

namespace Automation.Integration.Tests;

public sealed class ArchitectureTests
{
    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void CoreAssembliesDoNotReferenceStride(Assembly assembly)
    {
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name?.StartsWith("Stride.", StringComparison.Ordinal) == true);
    }

    public static TheoryData<Assembly> CoreAssemblies => new()
    {
        typeof(SimulationTick).Assembly,
        typeof(DishStationWorld).Assembly,
        typeof(DishStationEpisodeDefinition).Assembly,
        typeof(SyntheticWorkBenchmark).Assembly,
    };

    [Fact]
    public void FirstPlayableContentDescribesOutcomesAndDiscoverableEvidence()
    {
        var episode = DishStationEpisodeDefinition.FirstPlayable;

        Assert.NotEmpty(episode.Outcomes);
        Assert.All(episode.Outcomes, outcome =>
        {
            Assert.DoesNotContain("press", outcome.PlayerFacingOutcome, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("key", outcome.PlayerFacingOutcome, StringComparison.OrdinalIgnoreCase);
        });
        Assert.All(episode.Discoveries, discovery =>
        {
            Assert.False(string.IsNullOrWhiteSpace(discovery.PlayerFacingClue));
            Assert.False(string.IsNullOrWhiteSpace(discovery.CausalEvidence));
        });
    }

    [Fact]
    public void FirstHoursQuestlineIsOutcomeOrientedAndHasCoherentRewards()
    {
        var quests = DishStationFirstHoursContent.Quests;

        Assert.Equal(Enum.GetValues<DishStationQuestId>().Length, quests.Count);
        Assert.Equal(quests.Count, quests.Select(quest => quest.Id).Distinct().Count());
        Assert.All(quests, quest =>
        {
            Assert.DoesNotContain("press", quest.ObservableOutcome, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("key", quest.ObservableOutcome, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(quest.Discovery));
            Assert.False(string.IsNullOrWhiteSpace(quest.UnlockRationale));
            Assert.Equal(DishStationProgressionRules.ExperienceReward(quest.Id), quest.ExperienceReward);
            Assert.Equal(DishStationProgressionRules.CapabilityReward(quest.Id), quest.CapabilityReward);
        });
    }

    [Fact]
    public void OneHundredThousandActorsRunDeterministicallyHeadlessly()
    {
        var first = SyntheticWorkBenchmark.Run(100_000, 100);
        var second = SyntheticWorkBenchmark.Run(100_000, 100);

        Assert.Equal(10_000_000, first.Transitions);
        Assert.Equal(first.Checksum, second.Checksum);
        Assert.Equal(first.RepresentativeStates, second.RepresentativeStates);
        Assert.Equal(512, first.RepresentativeStates.Length);
    }

    [Fact]
    public void JsonCheckpointRestoresMidpointAndFutureCommandsDeterministically()
    {
        var original = new DishStationWorld(31);
        original.Schedule(new PerformDishActionCommand(new(1), DishAction.Scrape, DishKind.Plate));
        original.Schedule(new PerformDishActionCommand(new(2), DishAction.Rack, DishKind.Plate));
        original.Schedule(new PerformDishActionCommand(new(3), DishAction.StartWasher, DishKind.Plate));
        original.Schedule(new PerformDishActionCommand(new(24), DishAction.Unload, DishKind.Plate));
        original.Schedule(new PerformDishActionCommand(new(25), DishAction.DryAndRestock, DishKind.Plate));
        original.Schedule(new SetRushCommand(new(26), true));
        original.Schedule(new SetNewHireEnabledCommand(new(40), true));
        original.Schedule(new TrainNewHireCommand(new(41), DishProcessSpecification.FullyDocumented));
        original.Schedule(new MovePlayerCommand(new(72), new FloorCell(3, 4)));
        original.Schedule(new PlaceDishStationFixtureCommand(new(73), DishStationFixture.Rack, new FloorCell(4, 3)));
        original.Schedule(new ConfigureDishStationLayoutCommand(new(90), DishStationLayout.UShapedCell));
        original.Schedule(new AddDirtyDishesCommand(new(95), DishKind.Tray, 2));
        Advance(original, 70);

        var json = DishStationSaveStore.Serialize(original);
        var restored = DishStationSaveStore.Deserialize(json);
        AssertWorldEquivalent(original, restored);

        Advance(original, 80);
        Advance(restored, 80);
        AssertWorldEquivalent(original, restored);
        Assert.Equal(DishStationLayout.UShapedCell, restored.Layout);
        Assert.True(Enum.GetValues<DishState>().Sum(state => restored.At(state).Trays) >= 2);
    }

    [Fact]
    public void JsonCheckpointPreservesOnboardingQuestAndLevelProgress()
    {
        var world = new DishStationWorld(57);
        world.ExecuteNow(new CompleteIntroCommand(world.Tick, GuidanceMode.Minimal, true, true));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate));
        Advance(world, world.Configuration.WasherCycleTicks);
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, DishKind.Plate));

        var restored = DishStationSaveStore.Deserialize(DishStationSaveStore.Serialize(world));

        Assert.Equal(new OnboardingSnapshot(true, GuidanceMode.Minimal, true, true), restored.Snapshot().Onboarding);
        Assert.Equal(2, restored.Snapshot().Progression.Level);
        Assert.Equal(100, restored.Snapshot().Progression.Experience);
        Assert.True(restored.Snapshot().Progression.Quest(DishStationQuestId.ClockIn).Complete);
        Assert.Equal(DishStationQuestId.FindTheConstraint, restored.Snapshot().Progression.ActiveQuest);
    }

    [Fact]
    public void AtomicCareerFileReplacesAnOlderCheckpointAndLeavesNoTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"automation-career-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "career.json");
        try
        {
            var early = new DishStationWorld(11);
            early.ExecuteNow(new CompleteIntroCommand(early.Tick, GuidanceMode.Guided));
            DishStationSaveStore.SaveFileAtomic(path, early);

            var later = new DishStationWorld(12);
            later.ExecuteNow(new CompleteIntroCommand(later.Tick, GuidanceMode.Contextual));
            later.Advance();
            DishStationSaveStore.SaveFileAtomic(path, later);
            var restored = DishStationSaveStore.LoadFile(path);

            Assert.Equal(12, restored.Seed);
            Assert.Equal(GuidanceMode.Contextual, restored.Snapshot().Onboarding.GuidanceMode);
            Assert.Equal(1, restored.Tick.Value);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaytestEvidenceRoundTripsAtomicallyWithoutAReplayCheckpoint()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"automation-evidence-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "first-hours.json");
        try
        {
            var snapshot = new DishStationWorld(91).Snapshot();
            var evidence = new FirstHoursPlaytestEvidence(
                FirstHoursPlaytestEvidence.CurrentSchemaVersion,
                "session-91",
                new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 11, 13, 0, 0, TimeSpan.Zero),
                3600,
                snapshot.Onboarding,
                7,
                3400,
                285,
                snapshot.Progression.Quests.ToArray(),
                snapshot.ShiftTrial,
                snapshot.ShiftReport,
                [new(DishTutorialStage.RestockFirstDish, 2, 10, 20)]);

            FirstHoursPlaytestEvidenceStore.SaveFileAtomic(path, evidence);
            var restored = FirstHoursPlaytestEvidenceStore.LoadFile(path);

            Assert.Equal(evidence.SchemaVersion, restored.SchemaVersion);
            Assert.Equal(evidence.SessionId, restored.SessionId);
            Assert.Equal(evidence.StartedAtUtc, restored.StartedAtUtc);
            Assert.Equal(evidence.CompletedAtUtc, restored.CompletedAtUtc);
            Assert.Equal(evidence.WallClockSeconds, restored.WallClockSeconds);
            Assert.Equal(evidence.Onboarding, restored.Onboarding);
            Assert.Equal(evidence.Level, restored.Level);
            Assert.Equal(evidence.Experience, restored.Experience);
            Assert.Equal(evidence.ActiveSimulationTicks, restored.ActiveSimulationTicks);
            Assert.Equal(evidence.Quests, restored.Quests);
            Assert.Equal(evidence.ShiftTrial, restored.ShiftTrial);
            Assert.Equal(evidence.ShiftReport, restored.ShiftReport);
            Assert.Equal(evidence.HandbookVisits, restored.HandbookVisits);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PlaytestEvidenceFactoryRejectsAnIncompleteCareer()
    {
        var snapshot = new DishStationWorld(92).Snapshot();

        Assert.Throws<InvalidOperationException>(() => FirstHoursPlaytestEvidence.Create(
            "incomplete",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            snapshot));
    }

    private static void Advance(DishStationWorld world, int ticks)
    {
        for (var i = 0; i < ticks; i++) world.Advance();
    }

    private static void AssertWorldEquivalent(DishStationWorld expected, DishStationWorld actual)
    {
        var left = expected.Snapshot();
        var right = actual.Snapshot();
        Assert.Equal(left.Tick, right.Tick);
        Assert.Equal(left.Dishes, right.Dishes);
        Assert.Equal(left.Telemetry, right.Telemetry);
        Assert.Equal(left.Bottleneck, right.Bottleneck);
        Assert.Equal(left.WasherRunning, right.WasherRunning);
        Assert.Equal(left.WasherOccupied, right.WasherOccupied);
        Assert.Equal(left.RushEnabled, right.RushEnabled);
        Assert.Equal(left.Completed, right.Completed);
        Assert.Equal(left.ServiceShortages, right.ServiceShortages);
        Assert.Equal(left.TutorialStage, right.TutorialStage);
        Assert.Equal(left.BottleneckHypothesis, right.BottleneckHypothesis);
        Assert.Equal(left.NewHire, right.NewHire);
        Assert.Equal(left.Layout, right.Layout);
        Assert.Equal(left.Automation.Policy, right.Automation.Policy);
        Assert.Equal(left.Automation.ReportedReady, right.Automation.ReportedReady);
        Assert.Equal(left.Automation.PhysicalReady, right.Automation.PhysicalReady);
        Assert.Equal(left.Automation.StickyReadySignal, right.Automation.StickyReadySignal);
        Assert.Equal(left.Automation.Halted, right.Automation.Halted);
        Assert.Equal(left.Automation.AutomatedStarts, right.Automation.AutomatedStarts);
        Assert.Equal(left.Automation.Incidents, right.Automation.Incidents);
        Assert.Equal(left.Automation.PreventedUnsafeStarts, right.Automation.PreventedUnsafeStarts);
        Assert.Equal(left.Automation.Incident, right.Automation.Incident);
        Assert.Equal(left.Automation.Trace.ToArray(), right.Automation.Trace.ToArray());
        Assert.Equal(left.Onboarding, right.Onboarding);
        Assert.Equal(left.Progression.Level, right.Progression.Level);
        Assert.Equal(left.Progression.Experience, right.Progression.Experience);
        Assert.Equal(left.Progression.ActiveQuest, right.Progression.ActiveQuest);
        Assert.Equal(left.Progression.Quests.ToArray(), right.Progression.Quests.ToArray());
        Assert.Equal(left.Progression.UnlockedCapabilities.ToArray(), right.Progression.UnlockedCapabilities.ToArray());
        Assert.Equal(left.ShiftTrial, right.ShiftTrial);
        Assert.Equal(left.ShiftReport, right.ShiftReport);
        Assert.Equal(left.RecentTransitions.ToArray(), right.RecentTransitions.ToArray());
        Assert.Equal(expected.Notifications, actual.Notifications);
    }
}
