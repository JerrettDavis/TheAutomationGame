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

        Assert.Equal(7, cues.Length);
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
}
