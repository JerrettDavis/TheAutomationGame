using Automation.Domain;
using Automation.Simulation;
using Stride.Core.Mathematics;

namespace Automation.Client.Stride;

public enum DishRoomModuleKind
{
    Floor,
    BackWall,
    SideWall,
    DoorFrame,
    Counter,
    WasherZone,
    WasherModel,
    Rack,
    ServicePass,
    WorkZone,
    EquipmentDetail,
    UtilityTrim,
    LightFixture,
}

public readonly record struct DishRoomModule(
    string Id,
    DishRoomModuleKind Kind,
    Vector3 Position,
    Vector3 Size,
    Color Color,
    PresentationId? Presentation = null);

public sealed class DishRoomModulePlan
{
    private DishRoomModulePlan(IReadOnlyList<DishRoomModule> modules) => Modules = modules;

    public IReadOnlyList<DishRoomModule> Modules { get; }

    public static DishRoomModulePlan Create(DishStationPlacements placements)
    {
        var modules = new List<DishRoomModule>(180);
        AddFloor(modules);
        AddWallsAndOpening(modules);
        AddFixtureModules(modules, placements);
        return new DishRoomModulePlan(modules);
    }

    public DishRoomModule Required(string id) => Modules.Single(module => module.Id == id);

    private static void AddFloor(List<DishRoomModule> modules)
    {
        for (var x = FloorCell.MinimumX; x <= FloorCell.MaximumX; x++)
            for (var z = FloorCell.MinimumY; z <= FloorCell.MaximumY; z++)
            {
                var shade = (x + z) % 2 == 0 ? new Color(65, 77, 78) : new Color(57, 70, 72);
                modules.Add(new($"floor.{x}.{z}", DishRoomModuleKind.Floor, new Vector3(x, -0.06f, z),
                    new Vector3(0.98f, 0.10f, 0.98f), shade));
            }
    }

    private static void AddWallsAndOpening(List<DishRoomModule> modules)
    {
        for (var x = FloorCell.MinimumX; x <= FloorCell.MaximumX; x++)
        {
            if (x is 5 or 6) continue;
            modules.Add(new($"wall.back.{x}", DishRoomModuleKind.BackWall, new Vector3(x, 1.15f, -0.52f),
                new Vector3(0.98f, 2.3f, 0.18f), new Color(72, 87, 91)));
        }
        for (var z = FloorCell.MinimumY; z <= FloorCell.MaximumY; z++)
            modules.Add(new($"wall.side.{z}", DishRoomModuleKind.SideWall, new Vector3(-0.52f, 1.15f, z),
                new Vector3(0.18f, 2.3f, 0.98f), new Color(64, 79, 84)));

        var trim = new Color(129, 105, 68);
        modules.Add(new("opening.left", DishRoomModuleKind.DoorFrame, new Vector3(4.72f, 1.15f, -0.48f),
            new Vector3(0.18f, 2.3f, 0.24f), trim));
        modules.Add(new("opening.right", DishRoomModuleKind.DoorFrame, new Vector3(6.28f, 1.15f, -0.48f),
            new Vector3(0.18f, 2.3f, 0.24f), trim));
        modules.Add(new("opening.lintel", DishRoomModuleKind.DoorFrame, new Vector3(5.5f, 2.18f, -0.48f),
            new Vector3(1.74f, 0.24f, 0.24f), trim));

        var utility = new Color(41, 58, 62);
        modules.Add(new("trim.back", DishRoomModuleKind.UtilityTrim, new Vector3(6f, 0.16f, -0.39f),
            new Vector3(12.8f, 0.22f, 0.12f), utility));
        modules.Add(new("trim.side", DishRoomModuleKind.UtilityTrim, new Vector3(-0.39f, 0.16f, 3.5f),
            new Vector3(0.12f, 0.22f, 7.8f), utility));
        var light = new Color(232, 224, 184);
        foreach (var (id, x) in new[] { ("light.left", 2.5f), ("light.center", 6f), ("light.right", 9.5f) })
            modules.Add(new(id, DishRoomModuleKind.LightFixture, new Vector3(x, 2.48f, 2.5f),
                new Vector3(1.8f, 0.08f, 0.34f), light));
    }

    private static void AddFixtureModules(List<DishRoomModule> modules, DishStationPlacements placements)
    {
        AddWorkZone(modules, "zone.scrape", placements.Scrape, new Color(89, 68, 51));
        AddWorkZone(modules, "zone.rack", placements.Rack, new Color(45, 76, 91));
        AddWorkZone(modules, "zone.unload", placements.Unload, new Color(42, 83, 82));
        AddWorkZone(modules, "zone.stock", placements.DryRestock, new Color(50, 91, 57));
        AddWorkZone(modules, "zone.service", placements.Service, new Color(52, 94, 59));
        AddAt(modules, "counter.scrape", DishRoomModuleKind.Counter, placements.Scrape,
            new Vector3(0.92f, 0.78f, 0.92f), new Color(112, 86, 63), 0.39f);
        AddAt(modules, "counter.unload", DishRoomModuleKind.Counter, placements.Unload,
            new Vector3(0.92f, 0.78f, 0.92f), new Color(70, 105, 104), 0.39f);
        AddAt(modules, "rack.dirty", DishRoomModuleKind.Rack, placements.Rack,
            new Vector3(0.82f, 1.08f, 0.82f), new Color(74, 109, 126), 0.54f);
        AddAt(modules, "rack.clean", DishRoomModuleKind.Rack, placements.DryRestock,
            new Vector3(0.82f, 1.08f, 0.82f), new Color(80, 126, 86), 0.54f);
        AddAt(modules, "washer.zone", DishRoomModuleKind.WasherZone, placements.Washer,
            new Vector3(1.18f, 0.08f, 1.18f), new Color(61, 118, 155), 0.01f);
        AddAt(modules, "washer.model", DishRoomModuleKind.WasherModel, placements.Washer,
            new Vector3(0.9f, 1.15f, 0.8f), new Color(48, 86, 126), 0.575f, PresentationIds.Washer);
        AddAt(modules, "service.pass", DishRoomModuleKind.ServicePass, placements.Service,
            new Vector3(1.65f, 0.96f, 0.78f), new Color(69, 123, 76), 0.48f);

        AddDetail(modules, "scrape.splash", placements.Scrape, 0, 0.38f, 0.84f,
            new Vector3(0.84f, 0.36f, 0.10f), new Color(128, 102, 78));
        AddDetail(modules, "scrape.basin", placements.Scrape, 0, 0, 0.80f,
            new Vector3(0.58f, 0.06f, 0.50f), new Color(48, 78, 88));
        AddDetail(modules, "unload.drain", placements.Unload, 0, 0, 0.80f,
            new Vector3(0.72f, 0.05f, 0.58f), new Color(102, 143, 141));
        AddRackDetails(modules, "dirty", placements.Rack, new Color(100, 143, 162));
        AddRackDetails(modules, "clean", placements.DryRestock, new Color(104, 158, 111));
        AddDetail(modules, "service.top", placements.Service, 0, 0, 0.99f,
            new Vector3(1.76f, 0.08f, 0.88f), new Color(93, 150, 99));
    }

    private static void AddWorkZone(List<DishRoomModule> modules, string id, FloorCell cell, Color color) =>
        AddDetail(modules, id, cell, 0, 0, 0.012f, new Vector3(1.15f, 0.025f, 1.15f), color, DishRoomModuleKind.WorkZone);

    private static void AddRackDetails(List<DishRoomModule> modules, string prefix, FloorCell cell, Color color)
    {
        foreach (var x in new[] { -0.34f, 0.34f })
            foreach (var z in new[] { -0.34f, 0.34f })
                AddDetail(modules, $"rack.{prefix}.post.{x}.{z}", cell, x, z, 1.12f,
                    new Vector3(0.06f, 1.18f, 0.06f), color);
        foreach (var height in new[] { 0.70f, 1.08f, 1.46f })
            AddDetail(modules, $"rack.{prefix}.shelf.{height}", cell, 0, 0, height,
                new Vector3(0.76f, 0.05f, 0.76f), color);
    }

    private static void AddDetail(List<DishRoomModule> modules, string id, FloorCell cell, float xOffset, float zOffset,
        float height, Vector3 size, Color color, DishRoomModuleKind kind = DishRoomModuleKind.EquipmentDetail) =>
        modules.Add(new(id, kind, new Vector3(cell.X + xOffset, height, cell.Y + zOffset), size, color));

    private static void AddAt(List<DishRoomModule> modules, string id, DishRoomModuleKind kind, FloorCell cell,
        Vector3 size, Color color, float height, PresentationId? presentation = null) =>
        modules.Add(new(id, kind, new Vector3(cell.X, height, cell.Y), size, color, presentation));
}
