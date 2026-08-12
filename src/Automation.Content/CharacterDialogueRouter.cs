using Automation.Domain;

namespace Automation.Content;

public sealed record ResolvedCharacterBark(
    ContentId Id,
    ContentId Speaker,
    ContentId Quest,
    DishStationNarrativeEventKind Trigger,
    CharacterDialoguePriority Priority,
    SimulationTick Tick,
    string Line);

public sealed class CharacterDialogueRouter
{
    private readonly CompiledContentCatalogV1 catalog;
    private readonly Dictionary<ContentId, long> lastEmittedAt = [];

    public CharacterDialogueRouter(CompiledContentCatalogV1 catalog)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public ResolvedCharacterBark? Resolve(DishStationNarrativeEvent narrativeEvent)
    {
        var quest = catalog.Quests.SingleOrDefault(quest =>
            quest.Narrative is not null &&
            string.Equals(quest.Narrative.RuntimeId, DishStationFirstHoursContent.RuntimeToken(narrativeEvent.Quest), StringComparison.Ordinal));
        if (quest is null) return null;

        var selected = catalog.Characters
            .SelectMany(character => character.Barks.Select(bark => (Speaker: character.Id, Bark: bark)))
            .Where(candidate => candidate.Bark.Quest == quest.Id && candidate.Bark.Trigger == narrativeEvent.Kind)
            .Where(candidate => !lastEmittedAt.TryGetValue(candidate.Bark.Id, out var lastTick) ||
                                narrativeEvent.Tick.Value - lastTick >= candidate.Bark.CooldownTicks)
            .OrderByDescending(candidate => candidate.Bark.Priority)
            .ThenBy(candidate => candidate.Bark.Id.Value, StringComparer.Ordinal)
            .FirstOrDefault();

        if (selected.Bark is null) return null;
        lastEmittedAt[selected.Bark.Id] = narrativeEvent.Tick.Value;
        return new(selected.Bark.Id, selected.Speaker, selected.Bark.Quest, selected.Bark.Trigger,
            selected.Bark.Priority, narrativeEvent.Tick, selected.Bark.Line);
    }

    public void Reset() => lastEmittedAt.Clear();
}
