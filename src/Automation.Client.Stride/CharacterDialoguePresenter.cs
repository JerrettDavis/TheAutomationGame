using Automation.Content;
using Stride.Core.Mathematics;

namespace Automation.Client.Stride;

public readonly record struct CastBadgePresentation(string Monogram, Color Color);

public static class RestaurantCastBadgeCatalog
{
    private static readonly IReadOnlyDictionary<string, CastBadgePresentation> Badges =
        new Dictionary<string, CastBadgePresentation>(StringComparer.Ordinal)
        {
            ["character.restaurant.avery-chen"] = new("AC", new Color(74, 169, 210)),
            ["character.restaurant.ray-morales"] = new("RM", new Color(221, 151, 72)),
            ["character.restaurant.jules-martin"] = new("JM", new Color(185, 111, 210)),
            ["character.restaurant.tessa-brooks"] = new("TB", new Color(83, 190, 139)),
            ["character.restaurant.devon-price"] = new("DP", new Color(232, 108, 82)),
            ["character.recurring.sam-rivera"] = new("SR", new Color(157, 126, 224)),
        };

    public static IReadOnlyDictionary<string, CastBadgePresentation> All => Badges;

    public static CastBadgePresentation Resolve(string characterId) =>
        Badges.TryGetValue(characterId, out var badge)
            ? badge
            : throw new KeyNotFoundException($"No restaurant cast badge is registered for '{characterId}'.");
}

public sealed record CharacterBarkPresentation(
    string Speaker,
    string Role,
    string Line,
    CharacterDialoguePriority Priority,
    CastBadgePresentation Badge);

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
            bark.Priority,
            RestaurantCastBadgeCatalog.Resolve(character.Id.Value));
    }
}
