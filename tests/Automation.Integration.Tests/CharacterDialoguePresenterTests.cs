using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;

namespace Automation.Integration.Tests;

public sealed class CharacterDialoguePresenterTests
{
    [Fact]
    public void PresentationResolvesStableSpeakerIdentityAndAuthoredLine()
    {
        var router = new CharacterDialogueRouter(DishStationFirstHoursContent.Catalog);
        var bark = Assert.IsType<ResolvedCharacterBark>(router.Resolve(
            new(new SimulationTick(236), DishStationNarrativeEventKind.AutomationIncident, DishStationQuestId.InvestigateTheSignal)));

        var presentation = CharacterDialoguePresenter.Present(bark);

        Assert.Equal("DEVON PRICE", presentation.Speaker);
        Assert.Equal("MAINTENANCE SUPPORT", presentation.Role);
        Assert.Equal("READY IS WHAT THE PANEL REPORTS. THE MACHINE IS STILL OCCUPIED.", presentation.Line);
        Assert.Equal(CharacterDialoguePriority.Critical, presentation.Priority);
        Assert.Equal("DP", presentation.Badge.Monogram);
        Assert.Equal(RestaurantCastBadgeCatalog.Resolve("character.restaurant.devon-price"), presentation.Badge);
    }

    [Fact]
    public void EveryAuthoredFirstShiftCharacterHasADistinctApprovedBadge()
    {
        var characters = DishStationFirstHoursContent.Catalog.Characters;

        Assert.Equal(characters.Length, RestaurantCastBadgeCatalog.All.Count);
        Assert.All(characters, character =>
            Assert.False(string.IsNullOrWhiteSpace(RestaurantCastBadgeCatalog.Resolve(character.Id.Value).Monogram)));
        Assert.Equal(RestaurantCastBadgeCatalog.All.Count,
            RestaurantCastBadgeCatalog.All.Values.Select(badge => badge.Color).Distinct().Count());
    }
}
