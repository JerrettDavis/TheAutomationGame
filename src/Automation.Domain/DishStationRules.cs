namespace Automation.Domain;

public static class DishStationRules
{
    public static DishState RequiredState(DishAction action) => action switch
    {
        DishAction.Scrape => DishState.Dirty,
        DishAction.Rack => DishState.Scraped,
        DishAction.StartWasher => DishState.Racked,
        DishAction.Unload => DishState.WashedInMachine,
        DishAction.DryAndRestock => DishState.CleanWet,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public static DishState ResultState(DishAction action) => action switch
    {
        DishAction.Scrape => DishState.Scraped,
        DishAction.Rack => DishState.Racked,
        DishAction.StartWasher => DishState.Washing,
        DishAction.Unload => DishState.CleanWet,
        DishAction.DryAndRestock => DishState.Available,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };
}
