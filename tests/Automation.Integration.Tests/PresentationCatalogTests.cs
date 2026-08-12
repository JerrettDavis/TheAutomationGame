using Automation.Client.Stride;
using Automation.Persistence;
using Automation.Simulation;
using Stride.Core.Mathematics;

namespace Automation.Integration.Tests;

public sealed class PresentationCatalogTests
{
    [Fact]
    public void DefaultCatalogProvidesStableWasherWorkerAndDishEntries()
    {
        var catalog = PresentationCatalog.Default;

        var washer = catalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        var player = catalog.Resolve(PresentationIds.Player, PresentationIds.FallbackActor);
        var worker = catalog.Resolve(PresentationIds.NewHire, PresentationIds.FallbackActor);
        var plate = catalog.Resolve(PresentationIds.Plate, PresentationIds.FallbackItem);
        var glass = catalog.Resolve(PresentationIds.Glass, PresentationIds.FallbackItem);
        var tray = catalog.Resolve(PresentationIds.Tray, PresentationIds.FallbackItem);

        Assert.Equal(PresentationKind.Workstation, washer.Kind);
        Assert.Equal(WasherAssetPresentation.ModelContentUrl, washer.ModelContentUrl);
        Assert.Equal(PresentationKind.Actor, player.Kind);
        Assert.Equal(PresentationKind.Actor, worker.Kind);
        Assert.NotEqual(player.PrimaryColor, worker.PrimaryColor);
        Assert.Equal(PresentationKind.Item, plate.Kind);
        Assert.NotEqual(plate.PrimaryColor, glass.PrimaryColor);
        Assert.NotEqual(glass.PrimaryColor, tray.PrimaryColor);
    }

    [Fact]
    public void MissingMappingsAndUnavailableWasherAssetResolveToTypedFallbacks()
    {
        var catalog = PresentationCatalog.Default;

        var missingItem = catalog.Resolve(new PresentationId("presentation.item.not-installed"), PresentationIds.FallbackItem);
        var washer = catalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        var washerFallback = catalog.ResolveFallback(washer);

        Assert.Equal(PresentationIds.FallbackItem, missingItem.Id);
        Assert.Equal(PresentationIds.FallbackWorkstation, washerFallback.Id);
        Assert.Null(washerFallback.ProjectionResourceSuffix);
    }

    [Fact]
    public void SwappingPresentationMappingDoesNotChangeSimulationOrSaveIdentity()
    {
        var world = IntegrationTestScenario.World();
        var saveBefore = DishStationSaveStore.Serialize(world);
        var snapshotBefore = world.Snapshot();
        var playerCellBefore = world.PlayerCell;
        var original = PresentationCatalog.Default.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        var replacement = original with
        {
            PrimaryColor = Color.LightGreen,
            ModelContentUrl = "Imported/Alternate/Washer",
            ProjectionResourceSuffix = ".alternate-washer.png",
        };

        var swapped = PresentationCatalog.Default.With(replacement);
        var resolved = swapped.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);

        Assert.Equal(PresentationIds.Washer, resolved.Id);
        Assert.Equal("Imported/Alternate/Washer", resolved.ModelContentUrl);
        Assert.Equal(Color.LightGreen, WasherAssetPresentation.Tint(resolved, selected: false, bottleneck: false));
        Assert.Equal(playerCellBefore, world.PlayerCell);
        Assert.Equal(snapshotBefore.Layout, world.Snapshot().Layout);
        Assert.Equal(saveBefore, DishStationSaveStore.Serialize(world));
        Assert.DoesNotContain("presentation.", saveBefore, StringComparison.Ordinal);
    }
}
