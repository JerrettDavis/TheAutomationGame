using Automation.Client.Stride;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class AudioFeedbackTests
{
    [Fact]
    public void RequiredCueCatalogHasUniqueContentAndVolumeControlsEffectiveGain()
    {
        var cues = Enum.GetValues<AudioCue>();

        Assert.Equal(9, cues.Length);
        Assert.Equal(cues.Length, cues.Select(AudioCueCatalog.ContentUrl).Distinct(StringComparer.Ordinal).Count());
        Assert.All(cues, cue =>
        {
            Assert.StartsWith("Audio/", AudioCueCatalog.ContentUrl(cue), StringComparison.Ordinal);
            Assert.True(AudioCueCatalog.BaseGain(cue) > 0);
            Assert.Equal(0, AudioCueCatalog.EffectiveGain(cue, 0));
            Assert.Equal(AudioCueCatalog.BaseGain(cue), AudioCueCatalog.EffectiveGain(cue, 100), 3);
        });
    }

    [Fact]
    public void UiConfirmationAndWasherLoopHaveDistinctAccessibleRouting()
    {
        var router = new DishStationAudioRouter();
        var emissions = new List<AudioCueEmission>();

        router.Confirm(emissions.Add);
        var washerStart = DishStationAudioRouter.FromNotification(
            new WorldNotification(new SimulationTick(4), "Washer started", "Visible running state."));

        Assert.Equal(AudioCue.UiConfirm, Assert.Single(emissions).Cue);
        Assert.Contains("CONFIRMED", emissions[0].Caption, StringComparison.Ordinal);
        Assert.Equal(AudioCue.WasherStart, washerStart?.Cue);
        Assert.NotEqual(AudioCueCatalog.ContentUrl(AudioCue.WasherStart), AudioCueCatalog.ContentUrl(AudioCue.WasherLoop));
        Assert.True(AudioCueCatalog.BaseGain(AudioCue.WasherLoop) < AudioCueCatalog.BaseGain(AudioCue.WasherStart));
    }

    [Fact]
    public void EveryAcceptedCueHasValidDeterministicMonoPcmSource()
    {
        var audioDirectory = Path.Combine(RepositoryRoot(), "src", "Automation.Client.Stride", "Resources", "Audio");

        foreach (var cue in Enum.GetValues<AudioCue>())
        {
            var path = Path.Combine(audioDirectory, $"{AudioCueCatalog.ContentUrl(cue)["Audio/".Length..]}.wav");
            Assert.True(File.Exists(path), path);
            using var reader = new BinaryReader(File.OpenRead(path));
            Assert.Equal("RIFF", new string(reader.ReadChars(4)));
            Assert.True(reader.ReadInt32() > 36);
            Assert.Equal("WAVE", new string(reader.ReadChars(4)));
            Assert.Equal("fmt ", new string(reader.ReadChars(4)));
            Assert.Equal(16, reader.ReadInt32());
            Assert.Equal(1, reader.ReadInt16());
            Assert.Equal(1, reader.ReadInt16());
            Assert.Equal(22_050, reader.ReadInt32());
            Assert.Equal(44_100, reader.ReadInt32());
            Assert.Equal(2, reader.ReadInt16());
            Assert.Equal(16, reader.ReadInt16());
            Assert.Equal("data", new string(reader.ReadChars(4)));
            Assert.True(reader.ReadInt32() > 0);
        }
    }

    [Fact]
    public void AuthoritativeCoreLoopRoutesWorkWasherAndQuestCuesExactlyOnce()
    {
        var world = IntegrationTestScenario.World();
        var router = new DishStationAudioRouter();
        var emissions = new List<AudioCueEmission>();
        router.Initialize(world.Snapshot(), world.Notifications.Count);
        router.Start(emissions.Add);

        Execute(world, router, emissions, new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        Execute(world, router, emissions, new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        Execute(world, router, emissions, new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate));
        for (var tick = 0; tick < world.Configuration.WasherCycleTicks; tick++)
        {
            world.Advance();
            router.Observe(world.Snapshot(), world.Notifications, emissions.Add);
        }
        Execute(world, router, emissions, new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Plate));
        Execute(world, router, emissions, new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, DishKind.Plate));

        Assert.Equal(1, emissions.Count(item => item.Cue == AudioCue.Ambience));
        Assert.Equal(5, emissions.Count(item => item.Cue == AudioCue.Work));
        Assert.Equal(1, emissions.Count(item => item.Cue == AudioCue.WasherStart));
        Assert.Equal(1, emissions.Count(item => item.Cue == AudioCue.WasherComplete));
        Assert.Equal(1, emissions.Count(item => item.Cue == AudioCue.QuestSuccess));

        var beforeDuplicateObservation = emissions.Count;
        router.Observe(world.Snapshot(), world.Notifications, emissions.Add);
        Assert.Equal(beforeDuplicateObservation, emissions.Count);
    }

    [Fact]
    public void RejectedCommandAndFailureNotificationUseDistinctCuesAndVisibleCaptions()
    {
        var world = IntegrationTestScenario.World();
        var router = new DishStationAudioRouter();
        var emissions = new List<AudioCueEmission>();
        var rejected = world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));

        router.ObserveCommand(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate), rejected, emissions.Add);
        var failure = DishStationAudioRouter.FromNotification(
            new WorldNotification(world.Tick, "Automation incident", "Reported ready disagreed with physical state."));

        Assert.False(rejected.Success);
        Assert.Equal(AudioCue.Blocked, Assert.Single(emissions).Cue);
        Assert.Equal(AudioCue.Failure, failure?.Cue);
        Assert.Contains("BLOCKED", emissions[0].Caption, StringComparison.Ordinal);
        Assert.Contains("FAILURE", failure?.Caption, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryInformationBearingCueHasCaptionAndRoutingDoesNotMutateWorldOrSave()
    {
        var world = IntegrationTestScenario.World();
        var before = DishStationSaveStore.Serialize(world);
        var router = new DishStationAudioRouter();
        var emissions = new List<AudioCueEmission>();
        router.Initialize(world.Snapshot(), world.Notifications.Count);
        router.Start(emissions.Add);
        router.Observe(world.Snapshot(), world.Notifications, emissions.Add);
        foreach (var title in new[] { "Washer started", "Cycle complete", "Automation incident" })
            if (DishStationAudioRouter.FromNotification(new WorldNotification(world.Tick, title, "Visible detail.")) is { } emission)
                emissions.Add(emission);

        Assert.All(emissions, emission =>
        {
            Assert.StartsWith("SOUND • ", emission.Caption, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(emission.Caption));
        });
        Assert.Equal(before, DishStationSaveStore.Serialize(world));
    }

    private static void Execute(DishStationWorld world, DishStationAudioRouter router, List<AudioCueEmission> emissions,
        ISimulationCommand command)
    {
        var result = world.ExecuteNow(command);
        Assert.True(result.Success);
        router.ObserveCommand(command, result, emissions.Add);
        router.Observe(world.Snapshot(), world.Notifications, emissions.Add);
    }

    private static string RepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "TheAutomationGame.sln"))) return current.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
