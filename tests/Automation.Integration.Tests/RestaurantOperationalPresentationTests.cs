using Automation.Client.Stride;
using Automation.Domain;

namespace Automation.Integration.Tests;

public sealed class RestaurantOperationalPresentationTests
{
    [Fact]
    public void EveryDishFamilyHasADistinctNonColorSilhouette()
    {
        var silhouettes = Enum.GetValues<DishKind>()
            .Select(RestaurantOperationalPresentation.DishSilhouette)
            .ToArray();

        Assert.Equal(Enum.GetValues<DishKind>().Length, silhouettes.Distinct().Count());
        Assert.Equal(RestaurantDishSilhouette.PlateOval,
            RestaurantOperationalPresentation.DishSilhouette(DishKind.Plate));
        Assert.Equal(RestaurantDishSilhouette.GlassTumbler,
            RestaurantOperationalPresentation.DishSilhouette(DishKind.Glass));
        Assert.Equal(RestaurantDishSilhouette.TrayRectangle,
            RestaurantOperationalPresentation.DishSilhouette(DishKind.Tray));
    }

    [Fact]
    public void WasherStateLanguageIsDeterministicAndAttentionHasPriority()
    {
        Assert.Equal(WasherVisualState.Idle,
            RestaurantOperationalPresentation.Washer(false, false, 0, 0, false).State);
        Assert.Equal(WasherVisualState.Ready,
            RestaurantOperationalPresentation.Washer(false, false, 2, 0, false).State);
        Assert.Equal(WasherVisualState.Active,
            RestaurantOperationalPresentation.Washer(true, true, 0, 0, false).State);
        Assert.Equal(WasherVisualState.Complete,
            RestaurantOperationalPresentation.Washer(false, true, 0, 1, false).State);

        var attention = RestaurantOperationalPresentation.Washer(true, true, 2, 1, true);
        Assert.Equal(WasherVisualState.Attention, attention.State);
        Assert.Equal("ATTN", attention.Label);
    }
}
