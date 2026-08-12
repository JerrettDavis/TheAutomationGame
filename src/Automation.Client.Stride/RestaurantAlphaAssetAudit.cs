namespace Automation.Client.Stride;

public enum RestaurantAssetCategory
{
    Room,
    Equipment,
    Item,
    Cast,
    Ui,
    Audio,
    Vfx,
}

public enum RestaurantAssetShippingStatus
{
    Placeholder,
    FallbackOnly,
    ApprovedAlpha,
    Production,
}

public sealed record RestaurantPresentationSurface(
    string Id,
    RestaurantAssetCategory Category,
    RestaurantAssetShippingStatus Status,
    string Source,
    string License,
    string Limitation,
    string ReplacementTrigger,
    bool CriticalPath,
    bool HasAccessibleEquivalent,
    IReadOnlySet<string> States);

public static class RestaurantAlphaAssetAudit
{
    public static readonly IReadOnlySet<string> RequiredOperationalStates = new HashSet<string>(StringComparer.Ordinal)
    {
        "idle", "ready", "active", "complete", "blocked", "selected", "interactable",
    };

    public static IReadOnlyList<RestaurantPresentationSurface> AcceptedSurfaces { get; } =
    [
        Surface("restaurant.room.modular-kit", RestaurantAssetCategory.Room,
            "code-native:DishRoomModulePlan", "Project-authored",
            "Procedural flat-material kit; no authored texture pass or distant LOD.",
            "Replace when a reviewed restaurant environment kit meets the same anchors and readability checks."),
        Surface("restaurant.equipment.station-family", RestaurantAssetCategory.Equipment,
            "code-native:IsometricStationScene", "Project-authored",
            "Scrape, rack, unload, stock, and service use stylized procedural geometry.",
            "Replace as a family when authored props preserve silhouettes, interaction anchors, and state overlays.",
            states: RequiredOperationalStates),
        Surface("restaurant.equipment.kenney-washer", RestaurantAssetCategory.Equipment,
            "src/Automation.Client.Stride/Resources/Imported/KenneyFurnitureKit/washer.glb", "CC0 1.0",
            "Licensed prototype model is color-treated by client overlays and has no authored animation.",
            "Replace when the washer has reviewed idle, running, complete, and fault animation without losing fallback support.",
            states: RequiredOperationalStates),
        Surface("restaurant.items.dish-family", RestaurantAssetCategory.Item,
            "code-native:IsometricStationScene.DrawDishStack", "Project-authored",
            "Plate, glass, and tray are compact procedural marks rather than textured 3D inventory.",
            "Replace when authored instanced items remain distinct by silhouette at default and minimum zoom."),
        Surface("restaurant.cast.world-rig", RestaurantAssetCategory.Cast,
            "code-native:SharedCharacterRig", "Project-authored",
            "Player and new hire use a procedural rig with idle, walk, and work poses only.",
            "Replace when a shared authored rig covers carry, inspect, talk, and attention within actor budgets."),
        Surface("restaurant.cast.dialogue-identities", RestaurantAssetCategory.Cast,
            "code-native:RestaurantCastBadgeCatalog", "Project-authored",
            "Named cast use color-and-monogram badges instead of illustrated portraits.",
            "Replace when all first-shift speakers have one coherent reviewed portrait set."),
        Surface("restaurant.ui.semantic-language", RestaurantAssetCategory.Ui,
            "code-native:DishStationGame+PixelFont", "Project-authored",
            "Pixel glyphs, text labels, panels, and state colors form the approved alpha UI kit.",
            "Replace only through an accessibility-reviewed UI/icon pass that preserves text labels."),
        Surface("restaurant.audio.core-cues", RestaurantAssetCategory.Audio,
            "src/Automation.Client.Stride/Resources/Audio/PROVENANCE.md", "Project-authored under project MIT license",
            "Nine synthesized mono cues; no spatialization, dialogue, music bus, or dynamic mix.",
            "Replace when reviewed recordings cover the same routed events and retain captions.",
            hasAccessibleEquivalent: true),
        Surface("restaurant.vfx.operational-states", RestaurantAssetCategory.Vfx,
            "code-native:IsometricStationScene", "Project-authored",
            "Geometry overlays and bounded pulses replace particles in the alpha presentation.",
            "Replace when a reviewed VFX family is equally readable in reduced-motion mode.",
            hasAccessibleEquivalent: true, states: RequiredOperationalStates),
    ];

    public static IReadOnlyList<string> Validate(IEnumerable<RestaurantPresentationSurface> surfaces)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        var rows = surfaces.ToArray();
        var issues = new List<string>();

        foreach (var category in Enum.GetValues<RestaurantAssetCategory>())
            if (!rows.Any(row => row.Category == category)) issues.Add($"Missing required category: {category}.");

        foreach (var duplicate in rows.GroupBy(row => row.Id, StringComparer.Ordinal).Where(group => group.Count() > 1))
            issues.Add($"Duplicate surface ID: {duplicate.Key}.");

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Id) || string.IsNullOrWhiteSpace(row.Source) || string.IsNullOrWhiteSpace(row.License))
                issues.Add($"Surface '{row.Id}' is missing identity, source, or license.");
            if (row.CriticalPath && row.Status is RestaurantAssetShippingStatus.Placeholder or RestaurantAssetShippingStatus.FallbackOnly)
                issues.Add($"Critical surface '{row.Id}' is not accepted for shipping.");
            if (row.Status == RestaurantAssetShippingStatus.ApprovedAlpha &&
                (string.IsNullOrWhiteSpace(row.Limitation) || string.IsNullOrWhiteSpace(row.ReplacementTrigger)))
                issues.Add($"Approved-alpha surface '{row.Id}' needs a limitation and replacement trigger.");
            if (row.Category is RestaurantAssetCategory.Audio or RestaurantAssetCategory.Vfx && !row.HasAccessibleEquivalent)
                issues.Add($"Information-bearing surface '{row.Id}' lacks an accessible equivalent.");
        }

        var stateCoverage = rows.Where(row => row.Category is RestaurantAssetCategory.Equipment or RestaurantAssetCategory.Vfx)
            .SelectMany(row => row.States).ToHashSet(StringComparer.Ordinal);
        foreach (var state in RequiredOperationalStates)
            if (!stateCoverage.Contains(state)) issues.Add($"Operational state is not presented: {state}.");

        return issues;
    }

    private static RestaurantPresentationSurface Surface(
        string id,
        RestaurantAssetCategory category,
        string source,
        string license,
        string limitation,
        string replacementTrigger,
        bool criticalPath = true,
        bool hasAccessibleEquivalent = true,
        IReadOnlySet<string>? states = null) =>
        new(id, category, RestaurantAssetShippingStatus.ApprovedAlpha, source, license, limitation,
            replacementTrigger, criticalPath, hasAccessibleEquivalent, states ?? new HashSet<string>(StringComparer.Ordinal));
}
