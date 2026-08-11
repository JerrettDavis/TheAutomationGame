using Automation.Domain;
using Automation.Simulation;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Automation.Client.Stride;

internal readonly record struct IsometricCamera(float OffsetX, float OffsetY, float Zoom)
{
    public static IsometricCamera Default => new(0, 0, 1);

    public IsometricCamera Pan(float x, float y) => this with
    {
        OffsetX = Math.Clamp(OffsetX + x, -220, 220),
        OffsetY = Math.Clamp(OffsetY + y, -120, 120),
    };

    public IsometricCamera ZoomBy(float amount) => this with { Zoom = Math.Clamp(Zoom + amount, 0.7f, 1.4f) };
}

internal static class IsometricStationScene
{
    private const float OriginX = 440;
    private const float OriginY = 146;
    private const float TileWidth = 72;
    private const float TileHeight = 28;

    private static readonly StationVisual[] Stations =
    [
        new("SCRAPE", DishState.Dirty, new Color(115, 81, 58)),
        new("RACK", DishState.Scraped, new Color(58, 96, 116)),
        new("WASHER", DishState.Racked, new Color(48, 86, 126)),
        new("UNLOAD", DishState.WashedInMachine, new Color(49, 111, 108)),
        new("DRY + STOCK", DishState.CleanWet, new Color(65, 116, 72)),
    ];

    public static Texture CreateDiamondTexture(GraphicsDevice device, GraphicsContext context)
    {
        const int width = 64;
        const int height = 32;
        var colors = new Color[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var normalized = MathF.Abs((x + 0.5f - width / 2f) / (width / 2f)) +
                                 MathF.Abs((y + 0.5f - height / 2f) / (height / 2f));
                colors[y * width + x] = normalized <= 1 ? Color.White : Color.Transparent;
            }

        var texture = Texture.New2D(device, width, height, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
        texture.SetData(context.CommandList, colors);
        return texture;
    }

    public static void Draw(
        SpriteBatch batch,
        Texture pixel,
        Texture diamond,
        DishStationSnapshot snapshot,
        DishKind selectedKind,
        int selectedStation,
        bool showProcess,
        IsometricCamera camera,
        Vector2 visualPlayerCell,
        bool placementMode,
        DishStationFixture placementFixture,
        FloorCell previewCell,
        bool previewValid,
        DishStationFixture? hoveredFixture,
        Color hoverColor,
        float interactionPulse)
    {
        DrawFloor(batch, pixel, diamond, camera);
        if (showProcess) DrawFlow(batch, diamond, camera, snapshot.Layout.Placements);

        Span<int> drawOrder = stackalloc int[6] { 0, 1, 2, 3, 4, 5 };
        for (var i = 1; i < drawOrder.Length; i++)
        {
            var candidate = drawOrder[i];
            var candidateDepth = FixtureDepth(candidate, snapshot.Layout.Placements);
            var insertion = i;
            while (insertion > 0 && FixtureDepth(drawOrder[insertion - 1], snapshot.Layout.Placements) > candidateDepth)
            {
                drawOrder[insertion] = drawOrder[insertion - 1];
                insertion--;
            }
            drawOrder[insertion] = candidate;
        }
        foreach (var fixtureIndex in drawOrder)
        {
            if (fixtureIndex == 5) DrawService(batch, pixel, diamond, snapshot, camera, hoveredFixture, hoverColor, interactionPulse);
            else DrawStation(batch, pixel, diamond, snapshot, selectedKind, fixtureIndex, selectedStation, showProcess, camera,
                hoveredFixture, hoverColor, interactionPulse);
        }
        if (placementMode) DrawPlacementPreview(batch, pixel, diamond, placementFixture, previewCell, previewValid, camera);
        DrawWorker(batch, pixel, diamond, snapshot.NewHire, snapshot.Layout.Placements, camera);
        DrawPlayer(batch, pixel, diamond, visualPlayerCell, camera);
        DrawLegend(batch, pixel, snapshot);
    }

    public static DishStationFixture? HitTest(float screenX, float screenY, DishStationPlacements placements, IsometricCamera camera)
    {
        Span<int> drawOrder = stackalloc int[6] { 0, 1, 2, 3, 4, 5 };
        for (var i = 1; i < drawOrder.Length; i++)
        {
            var candidate = drawOrder[i];
            var insertion = i;
            while (insertion > 0 && FixtureDepth(drawOrder[insertion - 1], placements) > FixtureDepth(candidate, placements))
            {
                drawOrder[insertion] = drawOrder[insertion - 1];
                insertion--;
            }
            drawOrder[insertion] = candidate;
        }

        // Test front-to-back so an overlapping fixture owns the pixels that were drawn last.
        for (var order = drawOrder.Length - 1; order >= 0; order--)
        {
            var fixture = (DishStationFixture)drawOrder[order];
            if (ContainsFixture(screenX, screenY, fixture, placements, camera)) return fixture;
        }
        return null;
    }

    internal static Vector2 FixtureCenter(DishStationFixture fixture, DishStationPlacements placements, IsometricCamera camera)
    {
        var cell = placements.At(fixture);
        var point = Project(cell.X, cell.Y, camera);
        var bodyHeight = (fixture == DishStationFixture.Service ? 38 : fixture == DishStationFixture.Washer ? 48 : 34) * camera.Zoom;
        return new Vector2(point.X, point.Y - bodyHeight);
    }

    private static bool ContainsFixture(float x, float y, DishStationFixture fixture, DishStationPlacements placements, IsometricCamera camera)
    {
        var cell = placements.At(fixture);
        var point = Project(cell.X, cell.Y, camera);
        var scale = camera.Zoom;
        var service = fixture == DishStationFixture.Service;
        var width = (service ? 100 : 76) * scale;
        var topHeight = (service ? 38 : 34) * scale;
        var bodyHeight = (service ? 38 : fixture == DishStationFixture.Washer ? 48 : 34) * scale;
        var topY = point.Y - bodyHeight;
        var inTop = MathF.Abs(x - point.X) / (width * 0.5f) + MathF.Abs(y - topY) / (topHeight * 0.5f) <= 1;
        var inBody = x >= point.X - width * 0.5f && x <= point.X + width * 0.5f && y >= topY && y <= point.Y;
        var labelWidth = (service ? 92 : 94) * scale;
        var labelTop = point.Y + (service ? 5 : 9) * scale;
        var labelHeight = (service ? 32 : 34) * scale;
        var inLabel = x >= point.X - labelWidth * 0.5f && x <= point.X + labelWidth * 0.5f && y >= labelTop && y <= labelTop + labelHeight;
        return inTop || inBody || inLabel;
    }

    public static FloorCell? FloorHitTest(float screenX, float screenY, IsometricCamera camera)
    {
        var projectedX = (screenX - OriginX - camera.OffsetX) / (TileWidth * 0.5f * camera.Zoom);
        var projectedY = (screenY - OriginY - camera.OffsetY) / (TileHeight * 0.5f * camera.Zoom);
        var cell = new FloorCell((int)MathF.Round((projectedX + projectedY) * 0.5f), (int)MathF.Round((projectedY - projectedX) * 0.5f));
        return cell.IsInsideDishStation ? cell : null;
    }

    private static void DrawFloor(SpriteBatch batch, Texture pixel, Texture diamond, IsometricCamera camera)
    {
        for (var depth = 0; depth <= 19; depth++)
            for (var x = 0; x <= 12; x++)
            {
                var y = depth - x;
                if (y < 0 || y > 7) continue;
                var point = Project(x, y, camera);
                var shade = (x + y) % 2 == 0 ? new Color(52, 66, 69) : new Color(46, 60, 63);
                DrawDiamond(batch, diamond, point.X, point.Y, TileWidth * camera.Zoom, TileHeight * camera.Zoom, new Color(25, 34, 37));
                DrawDiamond(batch, diamond, point.X, point.Y - camera.Zoom, (TileWidth - 3) * camera.Zoom, (TileHeight - 2) * camera.Zoom, shade);
            }

        PixelFont.Draw(batch, pixel, "SANDBOX FLOOR / SIMULATION PROJECTION", 42, 445, 1, new Color(145, 164, 166));
    }

    private static void DrawFlow(SpriteBatch batch, Texture diamond, IsometricCamera camera, DishStationPlacements placements)
    {
        for (var segment = 0; segment < Stations.Length; segment++)
        {
            var from = segment == 0 ? CellVector(placements.Scrape) : StationPosition(segment - 1, placements);
            var to = StationPosition(segment, placements);
            for (var step = 1; step <= 5; step++)
            {
                var t = step / 6f;
                var point = Project(MathUtil.Lerp(from.X, to.X, t), MathUtil.Lerp(from.Y, to.Y, t), camera);
                DrawDiamond(batch, diamond, point.X, point.Y - 3 * camera.Zoom, 8 * camera.Zoom, 5 * camera.Zoom, Color.Goldenrod);
            }
        }
    }

    private static void DrawStation(SpriteBatch batch, Texture pixel, Texture diamond, DishStationSnapshot snapshot,
        DishKind selectedKind, int index, int selectedStation, bool showProcess, IsometricCamera camera,
        DishStationFixture? hoveredFixture, Color hoverColor, float interactionPulse)
    {
        var station = Stations[index];
        var position = StationPosition(index, snapshot.Layout.Placements);
        var point = Project(position.X, position.Y, camera);
        var scale = camera.Zoom;
        var width = 76 * scale;
        var topHeight = 34 * scale;
        var bodyHeight = (index == 2 ? 48 : 34) * scale;
        var selected = index == selectedStation;
        var bottleneck = showProcess && snapshot.Bottleneck == station.QueueState;
        var hovered = hoveredFixture == (DishStationFixture)index;
        if (hovered)
            DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight, width + (10 + interactionPulse) * scale,
                topHeight + (8 + interactionPulse * 0.5f) * scale, hoverColor);

        DrawDiamond(batch, diamond, point.X + 5 * scale, point.Y + 8 * scale, width + 12 * scale, topHeight + 8 * scale, new Color(22, 29, 31, 190));
        DrawRect(batch, pixel, point.X - width / 2, point.Y - bodyHeight, width, bodyHeight, Darken(station.Color));
        DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight, width, topHeight, selected ? Color.Yellow : bottleneck ? Color.OrangeRed : station.Color);
        DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight - 2 * scale, width - 8 * scale, topHeight - 5 * scale, station.Color);

        var counts = snapshot.At(station.QueueState);
        if (index == 2)
        {
            var washing = snapshot.At(DishState.Washing);
            counts = new DishCounts(counts.Plates + washing.Plates, counts.Glasses + washing.Glasses, counts.Trays + washing.Trays);
        }

        DrawDishStack(batch, pixel, diamond, point.X - 21 * scale, point.Y - bodyHeight - 13 * scale, counts, scale);
        var labelX = point.X - 45 * scale;
        var labelY = point.Y + 12 * scale;
        DrawRect(batch, pixel, labelX - 3, labelY - 3, 94 * scale, showProcess ? 42 : 30, new Color(19, 27, 31, 220));
        PixelFont.Draw(batch, pixel, station.Name, labelX, labelY, 1, selected ? Color.Yellow : Color.White, 16);
        PixelFont.Draw(batch, pixel, $"P{counts.Plates} G{counts.Glasses} T{counts.Trays}", labelX, labelY + 13, 1, Color.LightGray);
        if (showProcess)
        {
            var metric = snapshot.MetricAt(station.QueueState);
            PixelFont.Draw(batch, pixel, $"AGE {metric.OldestAge(selectedKind)}  LOAD {metric.TotalItemTicks}", labelX, labelY + 26, 1, bottleneck ? Color.OrangeRed : new Color(184, 173, 112));
        }
    }

    private static void DrawService(SpriteBatch batch, Texture pixel, Texture diamond, DishStationSnapshot snapshot, IsometricCamera camera,
        DishStationFixture? hoveredFixture, Color hoverColor, float interactionPulse)
    {
        var serviceCell = snapshot.Layout.Placements.Service;
        var point = Project(serviceCell.X, serviceCell.Y, camera);
        var scale = camera.Zoom;
        if (hoveredFixture == DishStationFixture.Service)
            DrawDiamond(batch, diamond, point.X, point.Y - 38 * scale, (110 + interactionPulse) * scale,
                (46 + interactionPulse * 0.5f) * scale, hoverColor);
        DrawRect(batch, pixel, point.X - 50 * scale, point.Y - 38 * scale, 100 * scale, 38 * scale, new Color(38, 77, 47));
        DrawDiamond(batch, diamond, point.X, point.Y - 38 * scale, 100 * scale, 38 * scale, snapshot.ServiceShortages > 0 ? Color.OrangeRed : new Color(72, 137, 81));
        var counts = snapshot.At(DishState.Available);
        PixelFont.Draw(batch, pixel, "SERVICE", point.X - 43 * scale, point.Y + 8 * scale, 1, Color.White);
        PixelFont.Draw(batch, pixel, $"P{counts.Plates} G{counts.Glasses} T{counts.Trays}", point.X - 43 * scale, point.Y + 21 * scale, 1, Color.LightGray);
    }

    private static void DrawWorker(SpriteBatch batch, Texture pixel, Texture diamond, NewHireSnapshot worker, DishStationPlacements placements, IsometricCamera camera)
    {
        if (!worker.Enabled) return;
        var index = ActionStation(worker.LastAction);
        var position = StationPosition(index, placements);
        var point = Project(position.X + 0.7f, position.Y + 0.1f, camera);
        DrawPawn(batch, pixel, diamond, point, camera.Zoom, new Color(185, 111, 210), $"A{worker.Id.Value}");
    }

    private static void DrawPlayer(SpriteBatch batch, Texture pixel, Texture diamond, Vector2 playerCell, IsometricCamera camera)
    {
        var point = Project(playerCell.X - 0.35f, playerCell.Y - 0.2f, camera);
        DrawPawn(batch, pixel, diamond, point, camera.Zoom, Color.Yellow, "YOU");
    }

    private static void DrawPlacementPreview(SpriteBatch batch, Texture pixel, Texture diamond, DishStationFixture fixture, FloorCell cell, bool valid, IsometricCamera camera)
    {
        var point = Project(cell.X, cell.Y, camera);
        var color = valid ? new Color(92, 220, 126, 185) : new Color(240, 74, 67, 185);
        DrawDiamond(batch, diamond, point.X, point.Y - 5 * camera.Zoom, 82 * camera.Zoom, 38 * camera.Zoom, color);
        PixelFont.Draw(batch, pixel, $"PLACE {FixtureLabel(fixture)}  {cell.X},{cell.Y}", point.X - 55 * camera.Zoom, point.Y + 20 * camera.Zoom, 1, color, 22);
    }

    private static void DrawPawn(SpriteBatch batch, Texture pixel, Texture diamond, Vector2 point, float scale, Color color, string label)
    {
        DrawDiamond(batch, diamond, point.X, point.Y, 26 * scale, 12 * scale, new Color(18, 24, 27, 190));
        DrawRect(batch, pixel, point.X - 5 * scale, point.Y - 29 * scale, 10 * scale, 23 * scale, color);
        DrawRect(batch, pixel, point.X - 7 * scale, point.Y - 38 * scale, 14 * scale, 12 * scale, color);
        PixelFont.Draw(batch, pixel, label, point.X - 13 * scale, point.Y - 52 * scale, 1, color);
    }

    private static void DrawDishStack(SpriteBatch batch, Texture pixel, Texture diamond, float x, float y, DishCounts counts, float scale)
    {
        var total = Math.Min(counts.Total, 6);
        var color = counts.Trays > 0 ? Color.Goldenrod : counts.Glasses > 0 ? Color.MediumTurquoise : Color.CornflowerBlue;
        for (var i = 0; i < total; i++)
            DrawDiamond(batch, diamond, x + i * 5 * scale, y - i * 3 * scale, 20 * scale, 8 * scale, color);
    }

    private static void DrawLegend(SpriteBatch batch, Texture pixel, DishStationSnapshot snapshot)
    {
        PixelFont.Draw(batch, pixel, $"NEW HIRE {(snapshot.NewHire.Enabled ? "ON SHIFT" : "OFF SHIFT")}  AUTO {(snapshot.Automation.Policy.Enabled ? snapshot.Automation.Halted ? "HALTED" : "ACTIVE" : "OFF")}", 700, 445, 1, Color.LightGray);
    }

    private static Vector2 StationPosition(int index, DishStationPlacements placements) =>
        CellVector(placements.At((DishStationFixture)Math.Clamp(index, 0, Stations.Length - 1)));

    private static Vector2 CellVector(FloorCell cell) => new(cell.X, cell.Y);

    private static int FixtureDepth(int index, DishStationPlacements placements)
    {
        var cell = placements.At((DishStationFixture)index);
        return cell.X + cell.Y;
    }

    private static string FixtureLabel(DishStationFixture fixture) => fixture switch
    {
        DishStationFixture.DryRestock => "DRY + STOCK",
        _ => fixture.ToString().ToUpperInvariant(),
    };

    private static int ActionStation(DishAction? action) => action switch
    {
        DishAction.Scrape => 0,
        DishAction.Rack => 1,
        DishAction.StartWasher => 2,
        DishAction.Unload => 3,
        DishAction.DryAndRestock => 4,
        _ => 0,
    };

    private static Vector2 Project(float x, float y, IsometricCamera camera) => new(
        OriginX + camera.OffsetX + (x - y) * TileWidth * 0.5f * camera.Zoom,
        OriginY + camera.OffsetY + (x + y) * TileHeight * 0.5f * camera.Zoom);

    private static Color Darken(Color color) => new((byte)(color.R * 0.55f), (byte)(color.G * 0.55f), (byte)(color.B * 0.55f), color.A);

    private static void DrawRect(SpriteBatch batch, Texture pixel, float x, float y, float width, float height, Color color) =>
        batch.Draw(pixel, new RectangleF(x, y, width, height), color, Color.Black);

    private static void DrawDiamond(SpriteBatch batch, Texture texture, float centerX, float centerY, float width, float height, Color color) =>
        batch.Draw(texture, new RectangleF(centerX - width / 2, centerY - height / 2, width, height), color, Color.Black);

    private readonly record struct StationVisual(string Name, DishState QueueState, Color Color);
}
