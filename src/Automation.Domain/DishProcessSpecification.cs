namespace Automation.Domain;

public readonly record struct DishProcessSpecification(
    bool FlowDocumented,
    bool RushGlassPriorityDocumented,
    bool RareTrayHandlingDocumented)
{
    public static DishProcessSpecification HappyPath => new(true, false, false);
    public static DishProcessSpecification RushAware => new(true, true, false);
    public static DishProcessSpecification FullyDocumented => new(true, true, true);
}
