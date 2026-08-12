using Automation.Content;
using Automation.Simulation;
using Stride.Audio;
using Stride.Core.Serialization.Contents;

namespace Automation.Client.Stride;

public enum AudioCue
{
    Ambience,
    Work,
    WasherStart,
    WasherLoop,
    WasherComplete,
    Blocked,
    Failure,
    QuestSuccess,
    UiConfirm,
}

public readonly record struct AudioCueEmission(AudioCue Cue, string Caption, float BaseGain, bool Looping = false);

public static class AudioCueCatalog
{
    public const string AmbienceUrl = "Audio/DishRoomAmbience";
    public static string ContentUrl(AudioCue cue) => cue switch
    {
        AudioCue.Ambience => AmbienceUrl,
        AudioCue.Work => "Audio/Work",
        AudioCue.WasherStart => "Audio/WasherStart",
        AudioCue.WasherLoop => "Audio/WasherLoop",
        AudioCue.WasherComplete => "Audio/WasherComplete",
        AudioCue.Blocked => "Audio/Blocked",
        AudioCue.Failure => "Audio/Failure",
        AudioCue.QuestSuccess => "Audio/QuestSuccess",
        AudioCue.UiConfirm => "Audio/UiConfirm",
        _ => throw new ArgumentOutOfRangeException(nameof(cue)),
    };

    public static float BaseGain(AudioCue cue) => cue switch
    {
        AudioCue.Ambience => 0.18f,
        AudioCue.Work => 0.45f,
        AudioCue.WasherStart or AudioCue.WasherComplete => 0.55f,
        AudioCue.WasherLoop => 0.16f,
        AudioCue.Blocked => 0.5f,
        AudioCue.Failure => 0.65f,
        AudioCue.QuestSuccess => 0.6f,
        AudioCue.UiConfirm => 0.38f,
        _ => 0.5f,
    };

    public static float EffectiveGain(AudioCue cue, int masterVolumePercent) =>
        BaseGain(cue) * Math.Clamp(masterVolumePercent, 0, 100) / 100f;
}

public sealed class DishStationAudioRouter
{
    private int observedNotifications;
    private int observedWorkerActions;
    private int observedCompletedQuests;
    private bool initialized;

    public void Initialize(DishStationSnapshot snapshot, int notificationCount)
    {
        observedNotifications = notificationCount;
        observedWorkerActions = snapshot.NewHire.ActionsCompleted;
        observedCompletedQuests = snapshot.Progression.Quests.Count(quest => quest.Complete);
        initialized = true;
    }

    public void Start(Action<AudioCueEmission> emit) =>
        emit(new(AudioCue.Ambience, "SOUND • DISH-ROOM AMBIENCE", AudioCueCatalog.BaseGain(AudioCue.Ambience), Looping: true));

    public void Confirm(Action<AudioCueEmission> emit) =>
        emit(new(AudioCue.UiConfirm, "SOUND • CONFIRMED", AudioCueCatalog.BaseGain(AudioCue.UiConfirm)));

    public void ObserveCommand(ISimulationCommand command, CommandResult result, Action<AudioCueEmission> emit)
    {
        if (!result.Success)
        {
            emit(new(AudioCue.Blocked, $"SOUND • BLOCKED: {result.Message}", AudioCueCatalog.BaseGain(AudioCue.Blocked)));
            return;
        }
        if (command is PerformDishActionCommand or InteractWithDishStationFixtureCommand)
            emit(new(AudioCue.Work, "SOUND • WORK COMPLETED", AudioCueCatalog.BaseGain(AudioCue.Work)));
    }

    public void Observe(DishStationSnapshot snapshot, IReadOnlyList<WorldNotification> notifications, Action<AudioCueEmission> emit)
    {
        if (!initialized)
        {
            Initialize(snapshot, notifications.Count);
            return;
        }
        if (notifications.Count < observedNotifications) observedNotifications = 0;
        for (var index = observedNotifications; index < notifications.Count; index++)
            if (FromNotification(notifications[index]) is { } emission) emit(emission);
        observedNotifications = notifications.Count;

        if (snapshot.NewHire.ActionsCompleted > observedWorkerActions)
            emit(new(AudioCue.Work, "SOUND • NEW HIRE COMPLETED WORK", AudioCueCatalog.BaseGain(AudioCue.Work)));
        observedWorkerActions = snapshot.NewHire.ActionsCompleted;

        var completedQuests = snapshot.Progression.Quests.Count(quest => quest.Complete);
        if (completedQuests > observedCompletedQuests)
        {
            var completed = snapshot.Progression.Quests.LastOrDefault(quest => quest.Complete);
            var title = completed.Complete ? DishStationFirstHoursContent.Quest(completed.Id).Title : "Progress recorded";
            emit(new(AudioCue.QuestSuccess, $"SOUND • QUEST COMPLETE: {title}", AudioCueCatalog.BaseGain(AudioCue.QuestSuccess)));
        }
        observedCompletedQuests = completedQuests;
    }

    public static AudioCueEmission? FromNotification(WorldNotification notification) => notification.Title switch
    {
        "Washer started" => new(AudioCue.WasherStart, "SOUND • WASHER STARTED", AudioCueCatalog.BaseGain(AudioCue.WasherStart)),
        "Cycle complete" => new(AudioCue.WasherComplete, "SOUND • WASHER CYCLE COMPLETE", AudioCueCatalog.BaseGain(AudioCue.WasherComplete)),
        "Automation incident" or "Reliability window failed" or "Failure reproduced" or
            "Rare tray rework" or "Hypothesis not supported" =>
            new(AudioCue.Failure, $"SOUND • FAILURE: {notification.Title}", AudioCueCatalog.BaseGain(AudioCue.Failure)),
        _ => null,
    };
}

public sealed class DishStationAudioPresenter : IDisposable
{
    private readonly Dictionary<AudioCue, Sound> sounds;
    private readonly Dictionary<AudioCue, SoundInstance> instances;
    private int masterVolumePercent;
    private bool washerLoopPlaying;

    private DishStationAudioPresenter(Dictionary<AudioCue, Sound> sounds, Dictionary<AudioCue, SoundInstance> instances,
        int masterVolumePercent)
    {
        this.sounds = sounds;
        this.instances = instances;
        SetMasterVolume(masterVolumePercent);
    }

    public static DishStationAudioPresenter? TryCreate(ContentManager content, int masterVolumePercent, out string status)
    {
        var sounds = new Dictionary<AudioCue, Sound>();
        var instances = new Dictionary<AudioCue, SoundInstance>();
        try
        {
            foreach (var cue in Enum.GetValues<AudioCue>())
            {
                var sound = content.Load<Sound>(AudioCueCatalog.ContentUrl(cue));
                sounds.Add(cue, sound);
                instances.Add(cue, sound.CreateInstance());
            }
            status = masterVolumePercent == 0 ? "muted" : "ready";
            return new(sounds, instances, masterVolumePercent);
        }
        catch (Exception exception)
        {
            foreach (var instance in instances.Values) instance.Dispose();
            foreach (var sound in sounds.Values) content.Unload(sound);
            status = $"silent:{exception.GetType().Name}";
            return null;
        }
    }

    public void SetMasterVolume(int percent)
    {
        masterVolumePercent = Math.Clamp(percent, 0, 100);
        foreach (var pair in instances)
            pair.Value.Volume = AudioCueCatalog.EffectiveGain(pair.Key, masterVolumePercent);
        if (masterVolumePercent == 0) SynchronizeWasher(running: false);
    }

    public void Play(AudioCueEmission emission)
    {
        if (masterVolumePercent == 0 || !instances.TryGetValue(emission.Cue, out var instance)) return;
        if (emission.Cue == AudioCue.WasherComplete) SynchronizeWasher(running: false);
        instance.Volume = emission.BaseGain * masterVolumePercent / 100f;
        instance.IsLooping = emission.Looping;
        if (emission.Looping) instance.Play();
        else instance.PlayExclusive();
        if (emission.Cue == AudioCue.WasherStart) SynchronizeWasher(running: true);
    }

    public void SynchronizeWasher(bool running)
    {
        running &= masterVolumePercent > 0;
        if (running == washerLoopPlaying || !instances.TryGetValue(AudioCue.WasherLoop, out var loop)) return;
        if (running)
        {
            loop.Volume = AudioCueCatalog.EffectiveGain(AudioCue.WasherLoop, masterVolumePercent);
            loop.IsLooping = true;
            loop.Play();
        }
        else loop.Stop();
        washerLoopPlaying = running;
    }

    public void Dispose()
    {
        foreach (var instance in instances.Values) instance.Dispose();
        instances.Clear();
        sounds.Clear();
    }
}
