using Automation.Client.Stride;

namespace Automation.Integration.Tests;

public sealed class RestaurantAlphaAssetAuditTests
{
    [Fact]
    public void AcceptedFirstChapterHasEveryCategoryAndNoUnreviewedSurface()
    {
        var surfaces = RestaurantAlphaAssetAudit.AcceptedSurfaces;

        Assert.Empty(RestaurantAlphaAssetAudit.Validate(surfaces));
        Assert.All(Enum.GetValues<RestaurantAssetCategory>(), category =>
            Assert.Contains(surfaces, surface => surface.Category == category));
        Assert.DoesNotContain(surfaces, surface => surface.Status is
            RestaurantAssetShippingStatus.Placeholder or RestaurantAssetShippingStatus.FallbackOnly);
        Assert.All(surfaces, surface =>
        {
            Assert.False(string.IsNullOrWhiteSpace(surface.Source));
            Assert.False(string.IsNullOrWhiteSpace(surface.License));
            Assert.False(string.IsNullOrWhiteSpace(surface.Limitation));
            Assert.False(string.IsNullOrWhiteSpace(surface.ReplacementTrigger));
        });
    }

    [Fact]
    public void AuditRejectsFallbackCriticalAudioWithoutCaptionAndMissingStateCoverage()
    {
        var invalid = RestaurantAlphaAssetAudit.AcceptedSurfaces
            .Where(surface => surface.Category is not RestaurantAssetCategory.Equipment and not RestaurantAssetCategory.Vfx)
            .Select(surface => surface.Category == RestaurantAssetCategory.Audio
                ? surface with
                {
                    Status = RestaurantAssetShippingStatus.FallbackOnly,
                    HasAccessibleEquivalent = false,
                }
                : surface)
            .ToArray();

        var issues = RestaurantAlphaAssetAudit.Validate(invalid);

        Assert.Contains(issues, issue => issue.Contains("Missing required category: Equipment", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.Contains("not accepted for shipping", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.Contains("lacks an accessible equivalent", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.Contains("Operational state is not presented", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptedExternalSourcesAndProvenanceFilesExist()
    {
        var root = RepositoryRoot();
        var externalPaths = RestaurantAlphaAssetAudit.AcceptedSurfaces
            .Select(surface => surface.Source)
            .Where(source => !source.StartsWith("code-native:", StringComparison.Ordinal));

        Assert.All(externalPaths, source => Assert.True(File.Exists(Path.Combine(root, source)), source));
        Assert.True(File.Exists(Path.Combine(root,
            "src", "Automation.Client.Stride", "Assets", "imported", "kenney-furniture-kit", "PROVENANCE.md")));
    }

    private static string RepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "TheAutomationGame.sln"))) return current.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
