using Automation.Client.Stride;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class DishRoomModulePlanTests
{
    [Fact]
    public void AuthoredPlanContainsEveryRequiredReusableRoomModule()
    {
        var plan = DishRoomModulePlan.Create(DishStationPlacements.Linear);

        Assert.Equal(104, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.Floor));
        Assert.Contains(plan.Modules, module => module.Kind == DishRoomModuleKind.BackWall);
        Assert.Contains(plan.Modules, module => module.Kind == DishRoomModuleKind.SideWall);
        Assert.Equal(3, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.DoorFrame));
        Assert.Equal(2, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.Counter));
        Assert.Single(plan.Modules, module => module.Kind == DishRoomModuleKind.WasherZone);
        Assert.Single(plan.Modules, module => module.Kind == DishRoomModuleKind.WasherModel);
        Assert.Equal(2, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.Rack));
        Assert.Single(plan.Modules, module => module.Kind == DishRoomModuleKind.ServicePass);
        Assert.Equal(5, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.WorkZone));
        Assert.True(plan.Modules.Count(module => module.Kind == DishRoomModuleKind.EquipmentDetail) >= 18);
        Assert.Equal(2, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.UtilityTrim));
        Assert.Equal(3, plan.Modules.Count(module => module.Kind == DishRoomModuleKind.LightFixture));
        Assert.Equal(plan.Modules.Count, plan.Modules.Select(module => module.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(plan.Modules, module =>
        {
            Assert.True(module.Size.X > 0);
            Assert.True(module.Size.Y > 0);
            Assert.True(module.Size.Z > 0);
        });
    }

    [Fact]
    public void FixtureModulesFollowAuthoritativePlacementWhileRoomShellStaysAuthored()
    {
        var linear = DishRoomModulePlan.Create(DishStationPlacements.Linear);
        var flowCell = DishRoomModulePlan.Create(DishStationPlacements.UShapedCell);

        Assert.Equal(DishStationPlacements.Linear.Washer.X, linear.Required("washer.model").Position.X);
        Assert.Equal(DishStationPlacements.Linear.Washer.Y, linear.Required("washer.model").Position.Z);
        Assert.Equal(DishStationPlacements.UShapedCell.DryRestock.X, flowCell.Required("rack.clean").Position.X);
        Assert.Equal(DishStationPlacements.UShapedCell.DryRestock.Y, flowCell.Required("rack.clean").Position.Z);
        Assert.NotEqual(linear.Required("rack.clean").Position, flowCell.Required("rack.clean").Position);

        var linearShell = linear.Modules.Where(module => module.Kind is DishRoomModuleKind.Floor or DishRoomModuleKind.BackWall
            or DishRoomModuleKind.SideWall or DishRoomModuleKind.DoorFrame).ToArray();
        var flowShell = flowCell.Modules.Where(module => module.Kind is DishRoomModuleKind.Floor or DishRoomModuleKind.BackWall
            or DishRoomModuleKind.SideWall or DishRoomModuleKind.DoorFrame).ToArray();
        Assert.Equal(linearShell, flowShell);
    }

    [Fact]
    public void BuildingPresentationPlanCannotChangeSimulationOrCareerSave()
    {
        var world = IntegrationTestScenario.World();
        world.ExecuteNow(new ConfigureDishStationLayoutCommand(world.Tick, DishStationLayout.UShapedCell));
        var before = DishStationSaveStore.Serialize(world);
        var layoutBefore = world.Snapshot().Layout;

        var plan = DishRoomModulePlan.Create(world.Placements);

        Assert.NotEmpty(plan.Modules);
        Assert.Equal(layoutBefore, world.Snapshot().Layout);
        Assert.Equal(before, DishStationSaveStore.Serialize(world));
        Assert.DoesNotContain("DishRoom", before, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 0, 440, 146)]
    [InlineData(1, 0, 476, 160)]
    [InlineData(0, 1, 404, 160)]
    [InlineData(7, 1, 656, 258)]
    public void NativeOrthographicCameraMatchesExistingAuthoritativeProjection(int x, int y, float expectedX, float expectedY)
    {
        var projected = DishRoomCameraProjection.ProjectFloor(new FloorCell(x, y), IsometricCamera.Default);

        Assert.InRange(projected.X, expectedX - 0.6f, expectedX + 0.6f);
        Assert.InRange(projected.Y, expectedY - 0.6f, expectedY + 0.6f);
    }

    [Fact]
    public void NativeProjectionTracksPresentationPanAndZoomWithoutChangingFloorIdentity()
    {
        var camera = new IsometricCamera(50, -20, 1.25f);

        var projected = DishRoomCameraProjection.ProjectFloor(new FloorCell(7, 1), camera);

        Assert.InRange(projected.X, 759.4f, 760.6f);
        Assert.InRange(projected.Y, 265.4f, 266.6f);
    }
}
