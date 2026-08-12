using System.Reflection;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Automation.Client.Stride;

public static class WasherAssetPresentation
{
    public const string ModelContentUrl = "Imported/KenneyFurnitureKit/Washer";
    public const string ProjectionResourceSuffix = ".Assets.imported.kenney_furniture_kit.washer_SW.png";
    public const float Width = 62;
    public const float Height = 82;

    public static RectangleF Destination(Vector2 floorAnchor, float scale, PresentationDefinition definition) =>
        new(floorAnchor.X - definition.Width * scale * 0.5f, floorAnchor.Y - definition.Height * scale,
            definition.Width * scale, definition.Height * scale);

    public static Texture? TryLoadProjection(GraphicsDevice device, string? resourceSuffix)
    {
        if (string.IsNullOrWhiteSpace(resourceSuffix)) return null;
        var assembly = typeof(WasherAssetPresentation).Assembly;
        var resourceName = ProjectionResourceName(assembly, resourceSuffix);
        if (resourceName is null) return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        return stream is null
            ? null
            : Texture.Load(device, stream, TextureFlags.ShaderResource, GraphicsResourceUsage.Immutable, true);
    }

    public static Color Tint(PresentationDefinition definition, bool selected, bool bottleneck) =>
        selected ? new Color(255, 244, 176) : bottleneck ? new Color(255, 190, 170) : definition.PrimaryColor;

    public static bool HasEmbeddedProjection() => ProjectionResourceName(typeof(WasherAssetPresentation).Assembly, ProjectionResourceSuffix) is not null;

    private static string? ProjectionResourceName(Assembly assembly, string resourceSuffix) => assembly.GetManifestResourceNames()
        .SingleOrDefault(name => name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
}
