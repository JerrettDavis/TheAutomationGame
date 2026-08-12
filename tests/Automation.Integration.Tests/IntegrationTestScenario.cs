using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

internal static class IntegrationTestScenario
{
    public static DishStationScenarioConfiguration Reference => DishStationFirstHoursContent.ScenarioConfiguration;
    public static DishStationWorld World(int seed = 42) => new(seed, Reference);
}
