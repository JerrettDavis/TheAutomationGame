using Automation.Content;

namespace Automation.Client.Stride;

public sealed record CharacterBarkPresentation(
    string Speaker,
    string Role,
    string Line,
    CharacterDialoguePriority Priority);

public static class CharacterDialoguePresenter
{
    public static CharacterBarkPresentation Present(
        ResolvedCharacterBark bark,
        CompiledContentCatalogV1? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(bark);
        var character = (catalog ?? DishStationFirstHoursContent.Catalog).Characters
            .SingleOrDefault(character => character.Id == bark.Speaker)
            ?? throw new InvalidDataException($"Dialogue bark '{bark.Id}' references missing speaker '{bark.Speaker}'.");
        var roleToken = character.Role.Value[(character.Role.Value.LastIndexOf('.') + 1)..];
        return new(
            character.DisplayName.ToUpperInvariant(),
            string.Join(' ', roleToken.Split('-', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant(),
            bark.Line.ToUpperInvariant(),
            bark.Priority);
    }
}
