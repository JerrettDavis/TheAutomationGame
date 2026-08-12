using Automation.Domain;
using Automation.Simulation;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Automation.Client.Stride;

public readonly record struct IsometricCamera(float OffsetX, float OffsetY, float Zoom)
{
    public const float MinimumOffsetX = -220;
    public const float MaximumOffsetX = 220;
    public const float MinimumOffsetY = -120;
    public const float MaximumOffsetY = 120;
    public const float MinimumZoom = 0.7f;
    public const float MaximumZoom = 1.4f;

    public static IsometricCamera Default => new(0, 0, 1);

    public IsometricCamera Pan(float x, float y) => this with
    {
        OffsetX = Math.Clamp(OffsetX + x, MinimumOffsetX, MaximumOffsetX),
        OffsetY = Math.Clamp(OffsetY + y, MinimumOffsetY, MaximumOffsetY),
    };

    public IsometricCamera ZoomBy(float amount) => this with { Zoom = Math.Clamp(Zoom + amount, MinimumZoom, MaximumZoom) };
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
        Texture? washerProjection,
        PresentationCatalog presentationCatalog,
        bool renderRoomGeometry,
        DishStationSnapshot snapshot,
        DishKind selectedKind,
        int selectedStation,
        bool showProcess,
        IsometricCamera camera,
        DishStationCharacterFrame characters,
        bool reducedMotion,
        bool placementMode,
        DishStationFixture placementFixture,
        FloorCell previewCell,
        bool previewValid,
        DishStationFixture? hoveredFixture,
        Color hoverColor,
        float interactionPulse)
    {
        if (renderRoomGeometry) DrawFloor(batch, pixel, diamond, camera);
        else PixelFont.Draw(batch, pixel, "MODULAR DISH ROOM / AUTHORITATIVE PROJECTION", 42, 445, 1, new Color(145, 164, 166));
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
            if (fixtureIndex == 5) DrawService(batch, pixel, diamond, snapshot, camera, hoveredFixture, hoverColor, interactionPulse, renderRoomGeometry);
            else DrawStation(batch, pixel, diamond, washerProjection, presentationCatalog, snapshot, selectedKind, fixtureIndex, selectedStation, showProcess, camera,
                renderRoomGeometry,
                hoveredFixture, hoverColor, interactionPulse, reducedMotion);
        }
        if (placementMode) DrawPlacementPreview(batch, pixel, diamond, placementFixture, previewCell, previewValid, camera);
        DrawWorker(batch, pixel, diamond, presentationCatalog, characters.Worker, camera, reducedMotion, snapshot.NewHire.Id);
        DrawPlayer(batch, pixel, diamond, presentationCatalog, characters.Player, camera, reducedMotion);
        DrawLegend(batch, pixel, snapshot);
    }

    public static DishStationFixture? HitTest(float screenX, float screenY, DishStationPlacements placements, IsometricCamera camera,
        PresentationCatalog? presentationCatalog = null, bool washerAssetAvailable = true)
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
            if (ContainsFixture(screenX, screenY, fixture, placements, camera, presentationCatalog ?? PresentationCatalog.Default,
                    washerAssetAvailable)) return fixture;
        }
        return null;
    }

    internal static Vector2 FixtureCenter(DishStationFixture fixture, DishStationPlacements placements, IsometricCamera camera,
        PresentationCatalog? presentationCatalog = null, bool washerAssetAvailable = true)
    {
        var cell = placements.At(fixture);
        var point = Project(cell.X, cell.Y, camera);
        var catalog = presentationCatalog ?? PresentationCatalog.Default;
        var washer = ResolveWasher(catalog, washerAssetAvailable);
        var bodyHeight = (fixture == DishStationFixture.Service ? 38 : fixture == DishStationFixture.Washer ? washer.Height : 34) * camera.Zoom;
        return new Vector2(point.X, point.Y - bodyHeight);
    }

    private static bool ContainsFixture(float x, float y, DishStationFixture fixture, DishStationPlacements placements, IsometricCamera camera,
        PresentationCatalog presentationCatalog, bool washerAssetAvailable)
    {
        var cell = placements.At(fixture);
        var point = Project(cell.X, cell.Y, camera);
        var scale = camera.Zoom;
        var service = fixture == DishStationFixture.Service;
        var washer = fixture == DishStationFixture.Washer;
        var washerPresentation = ResolveWasher(presentationCatalog, washerAssetAvailable);
        var width = (service ? 100 : washer ? washerPresentation.Width : 76) * scale;
        var topHeight = (service ? 38 : 34) * scale;
        var bodyHeight = (service ? 38 : washer ? washerPresentation.Height : 34) * scale;
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

    private static void DrawStation(SpriteBatch batch, Texture pixel, Texture diamond, Texture? washerProjection,
        PresentationCatalog presentationCatalog, DishStationSnapshot snapshot,
        DishKind selectedKind, int index, int selectedStation, bool showProcess, IsometricCamera camera,
        bool renderRoomGeometry, DishStationFixture? hoveredFixture, Color hoverColor, float interactionPulse, bool reducedMotion)
    {
        var station = Stations[index];
        var position = StationPosition(index, snapshot.Layout.Placements);
        var point = Project(position.X, position.Y, camera);
        var scale = camera.Zoom;
        var washerPresentation = presentationCatalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        var renderedWasher = washerProjection is null ? presentationCatalog.ResolveFallback(washerPresentation) : washerPresentation;
        var width = (index == 2 ? renderedWasher.Width : 76) * scale;
        var topHeight = 34 * scale;
        var bodyHeight = (index == 2 ? renderedWasher.Height : 34) * scale;
        var selected = index == selectedStation;
        var bottleneck = showProcess && snapshot.Bottleneck == station.QueueState;
        var hovered = hoveredFixture == (DishStationFixture)index;
        if (hovered)
            DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight, width + (10 + interactionPulse) * scale,
                topHeight + (8 + interactionPulse * 0.5f) * scale, hoverColor);

        if (renderRoomGeometry)
        {
            DrawDiamond(batch, diamond, point.X + 5 * scale, point.Y + 8 * scale, width + 12 * scale, topHeight + 8 * scale, new Color(22, 29, 31, 190));
            if (index == 2 && washerProjection is not null)
            {
                batch.Draw(washerProjection, WasherAssetPresentation.Destination(point, scale, washerPresentation),
                    WasherAssetPresentation.Tint(washerPresentation, selected, bottleneck), Color.Black);
            }
            else
            {
                var bodyColor = index == 2 ? renderedWasher.SecondaryColor : Darken(station.Color);
                var topColor = index == 2 ? renderedWasher.PrimaryColor : station.Color;
                DrawRect(batch, pixel, point.X - width / 2, point.Y - bodyHeight, width, bodyHeight, bodyColor);
                DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight, width, topHeight, selected ? Color.Yellow : bottleneck ? Color.OrangeRed : topColor);
                DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight - 2 * scale, width - 8 * scale, topHeight - 5 * scale, topColor);
            }
        }
        if (selected || bottleneck)
        {
            DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight, width + 8 * scale, topHeight + 6 * scale,
                selected ? new Color(255, 220, 72, 150) : new Color(255, 96, 72, 150));
        }

        DrawStationSilhouette(batch, pixel, diamond, index, point, width, bodyHeight, scale, station.Color);

        if (index == 2)
        {
            var complete = snapshot.At(DishState.WashedInMachine).Total > 0;
            var attention = snapshot.Automation.Halted || snapshot.Incidents.Active.Count > 0;
            var ready = snapshot.At(DishState.Racked).Total > 0 && !snapshot.WasherOccupied;
            var state = attention ? "ATTN" : snapshot.WasherRunning ? "RUN" : complete ? "DONE" : ready ? "READY" : "IDLE";
            var stateColor = attention ? Color.OrangeRed : snapshot.WasherRunning ? Color.MediumTurquoise :
                complete ? Color.LightGreen : ready ? Color.Goldenrod : new Color(145, 164, 166);
            var pulse = snapshot.WasherRunning && !reducedMotion ? interactionPulse * 0.45f : 0;
            DrawDiamond(batch, diamond, point.X + width * 0.34f, point.Y - bodyHeight - (9 + pulse) * scale,
                (11 + pulse) * scale, (7 + pulse * 0.5f) * scale, stateColor);
            PixelFont.Draw(batch, pixel, state, point.X - 21 * scale, point.Y - bodyHeight - 35 * scale, 1, stateColor, 10);
        }

        var counts = snapshot.At(station.QueueState);
        if (index == 2)
        {
            var washing = snapshot.At(DishState.Washing);
            counts = new DishCounts(counts.Plates + washing.Plates, counts.Glasses + washing.Glasses, counts.Trays + washing.Trays);
        }

        DrawDishStack(batch, pixel, diamond, presentationCatalog, point.X - 21 * scale, point.Y - bodyHeight - 13 * scale, counts, scale);
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
        DishStationFixture? hoveredFixture, Color hoverColor, float interactionPulse, bool renderRoomGeometry)
    {
        var serviceCell = snapshot.Layout.Placements.Service;
        var point = Project(serviceCell.X, serviceCell.Y, camera);
        var scale = camera.Zoom;
        if (hoveredFixture == DishStationFixture.Service)
            DrawDiamond(batch, diamond, point.X, point.Y - 38 * scale, (110 + interactionPulse) * scale,
                (46 + interactionPulse * 0.5f) * scale, hoverColor);
        if (renderRoomGeometry)
        {
            DrawRect(batch, pixel, point.X - 50 * scale, point.Y - 38 * scale, 100 * scale, 38 * scale, new Color(38, 77, 47));
            DrawDiamond(batch, diamond, point.X, point.Y - 38 * scale, 100 * scale, 38 * scale, snapshot.ServiceShortages > 0 ? Color.OrangeRed : new Color(72, 137, 81));
        }
        var counts = snapshot.At(DishState.Available);
        PixelFont.Draw(batch, pixel, "SERVICE", point.X - 43 * scale, point.Y + 8 * scale, 1, Color.White);
        PixelFont.Draw(batch, pixel, $"P{counts.Plates} G{counts.Glasses} T{counts.Trays}", point.X - 43 * scale, point.Y + 21 * scale, 1, Color.LightGray);
    }

    private static void DrawWorker(SpriteBatch batch, Texture pixel, Texture diamond, PresentationCatalog presentationCatalog,
        CharacterVisualPose pose, IsometricCamera camera, bool reducedMotion, ActorId id)
    {
        var presentation = presentationCatalog.Resolve(PresentationIds.NewHire, PresentationIds.FallbackActor);
        DrawCharacter(batch, pixel, diamond, pose, camera, presentation, $"A{id.Value}", reducedMotion);
    }

    private static void DrawPlayer(SpriteBatch batch, Texture pixel, Texture diamond, PresentationCatalog presentationCatalog,
        CharacterVisualPose pose, IsometricCamera camera, bool reducedMotion)
    {
        var presentation = presentationCatalog.Resolve(PresentationIds.Player, PresentationIds.FallbackActor);
        DrawCharacter(batch, pixel, diamond, pose, camera, presentation, "YOU", reducedMotion);
    }

    private static void DrawPlacementPreview(SpriteBatch batch, Texture pixel, Texture diamond, DishStationFixture fixture, FloorCell cell, bool valid, IsometricCamera camera)
    {
        var point = Project(cell.X, cell.Y, camera);
        var color = valid ? new Color(92, 220, 126, 185) : new Color(240, 74, 67, 185);
        DrawDiamond(batch, diamond, point.X, point.Y - 5 * camera.Zoom, 82 * camera.Zoom, 38 * camera.Zoom, color);
        PixelFont.Draw(batch, pixel, $"PLACE {FixtureLabel(fixture)}  {cell.X},{cell.Y}", point.X - 55 * camera.Zoom, point.Y + 20 * camera.Zoom, 1, color, 22);
    }

    private static void DrawCharacter(SpriteBatch batch, Texture pixel, Texture diamond, CharacterVisualPose pose,
        IsometricCamera camera, PresentationDefinition presentation, string label, bool reducedMotion)
    {
        if (!pose.Visible) return;
        var point = Project(pose.Cell.X, pose.Cell.Y, camera);
        var scale = camera.Zoom;
        var rig = SharedCharacterRig.Resolve(pose, reducedMotion);
        var bob = rig.BodyBob * scale;
        var bodyWidth = presentation.Width * scale;
        var bodyHeight = presentation.Height * 0.5f * scale;
        var headWidth = MathF.Min(14, presentation.Width) * scale;
        if (rig.SelectionVisible)
            DrawDiamond(batch, diamond, point.X, point.Y + 1 * scale, (presentation.Width + 22) * scale, 17 * scale, new Color(255, 220, 72, 205));
        DrawDiamond(batch, diamond, point.X, point.Y, 29 * scale, 13 * scale, new Color(18, 24, 27, 210));

        var leftLegY = point.Y - 10 * scale + rig.LeftStride * scale;
        var rightLegY = point.Y - 10 * scale + rig.RightStride * scale;
        DrawRect(batch, pixel, point.X - 5 * scale, leftLegY, 4 * scale, 10 * scale, presentation.SecondaryColor);
        DrawRect(batch, pixel, point.X + 1 * scale, rightLegY, 4 * scale, 10 * scale, presentation.SecondaryColor);

        var bodyY = point.Y - 9 * scale - bodyHeight - bob;
        DrawRect(batch, pixel, point.X - bodyWidth * 0.5f, bodyY, bodyWidth, bodyHeight, presentation.PrimaryColor);
        var reachX = rig.FacingX * rig.WorkReach * 10 * scale;
        var reachY = rig.FacingY * rig.WorkReach * 6 * scale;
        DrawRect(batch, pixel, point.X - bodyWidth * 0.5f - 4 * scale + reachX, bodyY + 3 * scale + reachY, 4 * scale, 14 * scale, presentation.PrimaryColor);
        DrawRect(batch, pixel, point.X + bodyWidth * 0.5f + reachX, bodyY + 3 * scale + reachY, 4 * scale, 14 * scale, presentation.PrimaryColor);

        var headY = bodyY - 11 * scale;
        DrawRect(batch, pixel, point.X - headWidth * 0.5f, headY, headWidth, 12 * scale, new Color(226, 183, 150));
        DrawRect(batch, pixel, point.X - headWidth * 0.5f, headY, headWidth, 3 * scale, presentation.SecondaryColor);
        DrawRect(batch, pixel,
            point.X - 1.5f * scale + rig.FacingX * 4 * scale,
            headY + 5 * scale + rig.FacingY * 2 * scale,
            3 * scale, 3 * scale, new Color(37, 43, 45));
        if (pose.Animation == CharacterAnimationState.Work)
            DrawDiamond(batch, diamond, point.X + rig.FacingX * 14 * scale, point.Y - 19 * scale + rig.FacingY * 7 * scale,
                7 * scale, 5 * scale, Color.White);
        PixelFont.Draw(batch, pixel, label, point.X - 13 * scale, headY - 14 * scale, 1,
            rig.SelectionVisible ? Color.Yellow : presentation.PrimaryColor);
    }

    private static void DrawDishStack(SpriteBatch batch, Texture pixel, Texture diamond, PresentationCatalog presentationCatalog,
        float x, float y, DishCounts counts, float scale)
    {
        var offset = 0;
        DrawItemMarks(DishKind.Plate, counts.Plates, PresentationIds.Plate);
        DrawItemMarks(DishKind.Glass, counts.Glasses, PresentationIds.Glass);
        DrawItemMarks(DishKind.Tray, counts.Trays, PresentationIds.Tray);

        void DrawItemMarks(DishKind kind, int count, PresentationId id)
        {
            var item = presentationCatalog.Resolve(id, PresentationIds.FallbackItem);
            for (var index = 0; index < Math.Min(count, 2); index++, offset++)
            {
                var itemX = x + offset * 7 * scale;
                var itemY = y - offset * 3 * scale;
                switch (kind)
                {
                    case DishKind.Glass:
                        DrawRect(batch, pixel, itemX - 4 * scale, itemY - 11 * scale, 8 * scale, 11 * scale, item.PrimaryColor);
                        DrawRect(batch, pixel, itemX - 2 * scale, itemY - 9 * scale, 4 * scale, 7 * scale, new Color(20, 35, 40));
                        break;
                    case DishKind.Tray:
                        DrawRect(batch, pixel, itemX - 11 * scale, itemY - 6 * scale, 22 * scale, 9 * scale, item.SecondaryColor);
                        DrawRect(batch, pixel, itemX - 8 * scale, itemY - 4 * scale, 16 * scale, 5 * scale, item.PrimaryColor);
                        break;
                    default:
                        DrawDiamond(batch, diamond, itemX, itemY, item.Width * scale, item.Height * scale, item.PrimaryColor);
                        DrawDiamond(batch, diamond, itemX, itemY - scale, item.Width * 0.5f * scale, item.Height * 0.45f * scale, item.SecondaryColor);
                        break;
                }
            }
        }
    }

    private static void DrawStationSilhouette(SpriteBatch batch, Texture pixel, Texture diamond, int index, Vector2 point,
        float width, float bodyHeight, float scale, Color color)
    {
        switch (index)
        {
            case 0: // scrape basin and raised splash guard
                DrawRect(batch, pixel, point.X - width * 0.38f, point.Y - bodyHeight - 8 * scale,
                    width * 0.76f, 5 * scale, Darken(color));
                DrawDiamond(batch, diamond, point.X, point.Y - bodyHeight - 1 * scale,
                    width * 0.58f, 17 * scale, new Color(39, 75, 88));
                break;
            case 1: // open dirty rack
            case 4: // clean stock rack
                DrawRect(batch, pixel, point.X - width * 0.35f, point.Y - bodyHeight - 17 * scale, 4 * scale, 20 * scale, color);
                DrawRect(batch, pixel, point.X + width * 0.30f, point.Y - bodyHeight - 17 * scale, 4 * scale, 20 * scale, color);
                DrawRect(batch, pixel, point.X - width * 0.35f, point.Y - bodyHeight - 15 * scale, width * 0.69f, 3 * scale, color);
                DrawRect(batch, pixel, point.X - width * 0.35f, point.Y - bodyHeight - 7 * scale, width * 0.69f, 3 * scale, color);
                break;
            case 2: // washer door seam
                DrawRect(batch, pixel, point.X - width * 0.28f, point.Y - bodyHeight * 0.64f,
                    width * 0.56f, 3 * scale, new Color(124, 153, 165));
                break;
            case 3: // unload drain surface
                for (var stripe = -2; stripe <= 2; stripe++)
                    DrawRect(batch, pixel, point.X + stripe * 10 * scale - scale, point.Y - bodyHeight - 3 * scale,
                        2 * scale, 9 * scale, new Color(123, 166, 163));
                break;
        }
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

    private static Vector2 Project(float x, float y, IsometricCamera camera) => new(
        OriginX + camera.OffsetX + (x - y) * TileWidth * 0.5f * camera.Zoom,
        OriginY + camera.OffsetY + (x + y) * TileHeight * 0.5f * camera.Zoom);

    private static Color Darken(Color color) => new((byte)(color.R * 0.55f), (byte)(color.G * 0.55f), (byte)(color.B * 0.55f), color.A);

    private static PresentationDefinition ResolveWasher(PresentationCatalog catalog, bool assetAvailable)
    {
        var presentation = catalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        return assetAvailable ? presentation : catalog.ResolveFallback(presentation);
    }

    private static void DrawRect(SpriteBatch batch, Texture pixel, float x, float y, float width, float height, Color color) =>
        batch.Draw(pixel, new RectangleF(x, y, width, height), color, Color.Black);

    private static void DrawDiamond(SpriteBatch batch, Texture texture, float centerX, float centerY, float width, float height, Color color) =>
        batch.Draw(texture, new RectangleF(centerX - width / 2, centerY - height / 2, width, height), color, Color.Black);

    private readonly record struct StationVisual(string Name, DishState QueueState, Color Color);
}
