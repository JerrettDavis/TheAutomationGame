using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class DishStationRulesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(300, 3)]
    [InlineData(600, 4)]
    [InlineData(1000, 5)]
    [InlineData(1500, 6)]
    [InlineData(2500, 6)]
    [InlineData(3000, 7)]
    [InlineData(3400, 7)]
    public void CareerLevelsFollowStableExperienceThresholds(int experience, int level)
    {
        Assert.Equal(level, DishStationProgressionRules.LevelForExperience(experience));
    }

    [Fact]
    public void EveryFirstHoursQuestRewardsExperienceAndANewCapability()
    {
        var quests = Enum.GetValues<DishStationQuestId>();
        Assert.All(quests, quest => Assert.True(DishStationProgressionRules.ExperienceReward(quest) > 0));
        Assert.Equal(quests.Length, quests.Select(DishStationProgressionRules.CapabilityReward).Distinct().Count());
        Assert.Equal(7, DishStationProgressionRules.MaximumLevel);
    }
    [Theory]
    [InlineData(DishAction.Scrape, DishState.Dirty, DishState.Scraped)]
    [InlineData(DishAction.Rack, DishState.Scraped, DishState.Racked)]
    [InlineData(DishAction.StartWasher, DishState.Racked, DishState.Washing)]
    [InlineData(DishAction.DryAndRestock, DishState.CleanWet, DishState.Available)]
    public void ActionsHaveExplicitTransitions(DishAction action, DishState source, DishState destination)
    {
        Assert.Equal(source, DishStationRules.RequiredState(action));
        Assert.Equal(destination, DishStationRules.ResultState(action));
    }
}
