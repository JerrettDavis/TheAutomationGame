using Automation.Domain;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace Automation.Client.Stride;

internal sealed class DishRoomNativeScene : IDisposable
{
    private const float WasherScale = 1.8f;

    private readonly Game game;
    private readonly Scene rootScene;
    private readonly CameraComponent camera;
    private readonly Dictionary<string, Entity> entities = new(StringComparer.Ordinal);
    private readonly Dictionary<ModuleStyle, Model> primitiveModels = new();
    private Model? washerModel;
    private DishStationPlacements placements;

    private DishRoomNativeScene(Game game, DishStationPlacements placements, PresentationCatalog catalog)
    {
        this.game = game;
        this.placements = placements;
        rootScene = game.SceneSystem.SceneInstance.RootScene;

        camera = new CameraComponent
        {
            Projection = CameraProjectionMode.Orthographic,
            OrthographicSize = DishRoomCameraProjection.BaseOrthographicSize,
            NearClipPlane = 0.1f,
            FarClipPlane = 100,
            UseCustomViewMatrix = true,
            UseCustomProjectionMatrix = true,
        };
        AddEntity(new Entity("DishRoom.Camera") { camera });

        game.SceneSystem.GraphicsCompositor?.Dispose();
        game.SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(
            enablePostEffects: false,
            camera: camera,
            clearColor: new Color4(0.025f, 0.04f, 0.055f, 1));

        var ambient = new LightAmbient { Color = new ColorRgbProvider(new Color3(0.74f, 0.78f, 0.82f)) };
        AddEntity(new Entity("DishRoom.Ambient") { new LightComponent { Type = ambient, Intensity = 1.1f } });
        var keyLight = new Entity("DishRoom.Key")
        {
            new LightComponent { Type = new LightDirectional(), Intensity = 1.5f },
        };
        keyLight.Transform.Rotation = Quaternion.RotationYawPitchRoll(-0.7f, -0.9f, 0);
        AddEntity(keyLight);

        var washer = catalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        if (!string.IsNullOrWhiteSpace(washer.ModelContentUrl) && game.Content.Exists(washer.ModelContentUrl))
            washerModel = game.Content.Load<Model>(washer.ModelContentUrl);

        Build(DishRoomModulePlan.Create(placements));
        UpdateCamera(IsometricCamera.Default);
    }

    public static DishRoomNativeScene? TryCreate(Game game, DishStationPlacements placements, PresentationCatalog catalog,
        out string status)
    {
        try
        {
            var scene = new DishRoomNativeScene(game, placements, catalog);
            status = "native";
            return scene;
        }
        catch (Exception exception)
        {
            game.SceneSystem.Visible = false;
            game.SceneSystem.GraphicsCompositor?.Dispose();
            game.SceneSystem.GraphicsCompositor = new GraphicsCompositor();
            status = $"fallback:{exception.GetType().Name}";
            return null;
        }
    }

    public void Synchronize(DishStationPlacements current)
    {
        if (current == placements) return;
        placements = current;
        foreach (var module in DishRoomModulePlan.Create(current).Modules)
            if (entities.TryGetValue(module.Id, out var entity)) ApplyTransform(entity, module);
    }

    public void UpdateCamera(IsometricCamera state)
    {
        var frame = DishRoomCameraProjection.Resolve(state);
        camera.OrthographicSize = frame.OrthographicSize;
        camera.ViewMatrix = frame.ViewMatrix;
        camera.ProjectionMatrix = DishRoomCameraProjection.Projection(state,
            game.GraphicsDevice.Presenter.BackBuffer.Width, game.GraphicsDevice.Presenter.BackBuffer.Height);
    }

    public void Dispose()
    {
        foreach (var entity in entities.Values) rootScene.Entities.Remove(entity);
        entities.Clear();
    }

    private void Build(DishRoomModulePlan plan)
    {
        foreach (var module in plan.Modules)
        {
            var model = module.Kind == DishRoomModuleKind.WasherModel && washerModel is not null
                ? washerModel
                : PrimitiveModel(module);
            var entity = new Entity(module.Id) { new ModelComponent(model) };
            ApplyTransform(entity, module);
            AddEntity(entity, module.Id);
        }
    }

    private Model PrimitiveModel(DishRoomModule module)
    {
        var nativeSize = DishRoomCameraProjection.ToNative(module.Size);
        var style = new ModuleStyle(module.Kind, nativeSize, module.Color);
        if (primitiveModels.TryGetValue(style, out var cached)) return cached;

        var descriptor = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(module.Color)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            },
        };
        var material = Material.New(game.GraphicsDevice, descriptor);
        var primitive = new CubeProceduralModel { Size = nativeSize };
        primitive.SetMaterial("Material", material);
        var model = primitive.Generate(game.Services);
        primitiveModels.Add(style, model);
        return model;
    }

    private void ApplyTransform(Entity entity, DishRoomModule module)
    {
        if (module.Kind != DishRoomModuleKind.WasherModel || washerModel is null)
        {
            entity.Transform.Position = DishRoomCameraProjection.ToNative(module.Position);
            entity.Transform.UpdateWorldMatrix();
            return;
        }

        var bounds = washerModel.BoundingBox;
        var center = (bounds.Minimum + bounds.Maximum) * 0.5f;
        var anchor = DishRoomCameraProjection.ToNative(module.Position);
        entity.Transform.Scale = new Vector3(WasherScale);
        entity.Transform.Position = new Vector3(
            anchor.X - center.X * WasherScale,
            -bounds.Minimum.Y * WasherScale,
            anchor.Z - center.Z * WasherScale);
        entity.Transform.UpdateWorldMatrix();
    }

    private void AddEntity(Entity entity, string? id = null)
    {
        rootScene.Entities.Add(entity);
        entities.Add(id ?? entity.Name, entity);
    }

    private readonly record struct ModuleStyle(DishRoomModuleKind Kind, Vector3 Size, Color Color);
}
