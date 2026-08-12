using Stride.Core.Mathematics;

namespace Automation.Client.Stride;

public readonly record struct PresentationId
{
    public PresentationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Presentation IDs cannot be empty.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public enum PresentationKind
{
    Workstation,
    Actor,
    Item,
}

public sealed record PresentationDefinition(
    PresentationId Id,
    PresentationKind Kind,
    PresentationId? Fallback,
    Color PrimaryColor,
    Color SecondaryColor,
    float Width,
    float Height,
    string? ModelContentUrl = null,
    string? ProjectionResourceSuffix = null);

public static class PresentationIds
{
    public static readonly PresentationId FallbackWorkstation = new("presentation.fallback.workstation");
    public static readonly PresentationId FallbackActor = new("presentation.fallback.actor");
    public static readonly PresentationId FallbackItem = new("presentation.fallback.item");
    public static readonly PresentationId Washer = new("presentation.workstation.dish-washer.standard");
    public static readonly PresentationId Player = new("presentation.actor.player.standard");
    public static readonly PresentationId NewHire = new("presentation.actor.new-hire.standard");
    public static readonly PresentationId Plate = new("presentation.item.dish.plate");
    public static readonly PresentationId Glass = new("presentation.item.dish.glass");
    public static readonly PresentationId Tray = new("presentation.item.dish.tray");
}

public sealed class PresentationCatalog
{
    private readonly IReadOnlyDictionary<PresentationId, PresentationDefinition> definitions;

    public PresentationCatalog(IEnumerable<PresentationDefinition> definitions)
    {
        var materialized = definitions.ToDictionary(definition => definition.Id);
        ValidateFallback(materialized, PresentationIds.FallbackWorkstation, PresentationKind.Workstation);
        ValidateFallback(materialized, PresentationIds.FallbackActor, PresentationKind.Actor);
        ValidateFallback(materialized, PresentationIds.FallbackItem, PresentationKind.Item);
        foreach (var definition in materialized.Values)
        {
            if (definition.Width <= 0 || definition.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(definitions), $"{definition.Id} must have positive dimensions.");
            if (definition.Fallback is { } fallback &&
                (!materialized.TryGetValue(fallback, out var fallbackDefinition) || fallbackDefinition.Kind != definition.Kind))
                throw new ArgumentException($"{definition.Id} has a missing or incompatible fallback {fallback}.", nameof(definitions));
        }
        this.definitions = materialized;
    }

    public static PresentationCatalog Default { get; } = new(DefaultDefinitions());

    public PresentationDefinition Resolve(PresentationId id, PresentationId categoryFallback)
    {
        if (definitions.TryGetValue(id, out var definition)) return definition;
        return definitions.TryGetValue(categoryFallback, out var fallback)
            ? fallback
            : throw new KeyNotFoundException($"Neither {id} nor fallback {categoryFallback} exists in the presentation catalog.");
    }

    public PresentationDefinition ResolveFallback(PresentationDefinition definition) => definition.Fallback is { } fallback
        ? Resolve(fallback, fallback)
        : definition;

    public PresentationCatalog With(PresentationDefinition replacement) =>
        new(definitions.Values.Where(definition => definition.Id != replacement.Id).Append(replacement));

    private static void ValidateFallback(IReadOnlyDictionary<PresentationId, PresentationDefinition> definitions,
        PresentationId id, PresentationKind kind)
    {
        if (!definitions.TryGetValue(id, out var definition) || definition.Kind != kind || definition.Fallback is not null)
            throw new ArgumentException($"Catalog requires root fallback {id} of kind {kind}.", nameof(definitions));
    }

    private static IEnumerable<PresentationDefinition> DefaultDefinitions()
    {
        yield return new(PresentationIds.FallbackWorkstation, PresentationKind.Workstation, null,
            new Color(48, 86, 126), new Color(26, 47, 69), 76, 48);
        yield return new(PresentationIds.FallbackActor, PresentationKind.Actor, null,
            new Color(185, 111, 210), new Color(18, 24, 27), 14, 38);
        yield return new(PresentationIds.FallbackItem, PresentationKind.Item, null,
            Color.LightGray, new Color(18, 24, 27), 20, 8);
        yield return new(PresentationIds.Washer, PresentationKind.Workstation, PresentationIds.FallbackWorkstation,
            Color.White, new Color(22, 29, 31), WasherAssetPresentation.Width, WasherAssetPresentation.Height,
            WasherAssetPresentation.ModelContentUrl, WasherAssetPresentation.ProjectionResourceSuffix);
        yield return new(PresentationIds.Player, PresentationKind.Actor, PresentationIds.FallbackActor,
            new Color(244, 196, 72), new Color(35, 70, 80), 16, 40);
        yield return new(PresentationIds.NewHire, PresentationKind.Actor, PresentationIds.FallbackActor,
            new Color(185, 111, 210), new Color(18, 24, 27), 14, 38);
        yield return new(PresentationIds.Plate, PresentationKind.Item, PresentationIds.FallbackItem,
            Color.CornflowerBlue, new Color(18, 24, 27), 20, 8);
        yield return new(PresentationIds.Glass, PresentationKind.Item, PresentationIds.FallbackItem,
            Color.MediumTurquoise, new Color(18, 24, 27), 20, 8);
        yield return new(PresentationIds.Tray, PresentationKind.Item, PresentationIds.FallbackItem,
            Color.Goldenrod, new Color(18, 24, 27), 20, 8);
    }
}
