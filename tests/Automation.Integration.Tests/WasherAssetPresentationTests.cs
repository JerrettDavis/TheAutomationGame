using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Automation.Client.Stride;
using Stride.Core.Mathematics;

namespace Automation.Integration.Tests;

public sealed class WasherAssetPresentationTests
{
    [Fact]
    public void AuthoredProjectionUsesStableFloorAnchoredTransformAndStateTint()
    {
        var presentation = PresentationCatalog.Default.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        var destination = WasherAssetPresentation.Destination(new Vector2(400, 250), 1.25f, presentation);

        Assert.Equal(new RectangleF(361.25f, 147.5f, 77.5f, 102.5f), destination);
        Assert.Equal(Color.White, WasherAssetPresentation.Tint(presentation, selected: false, bottleneck: false));
        Assert.NotEqual(Color.White, WasherAssetPresentation.Tint(presentation, selected: true, bottleneck: false));
        Assert.NotEqual(Color.White, WasherAssetPresentation.Tint(presentation, selected: false, bottleneck: true));
    }

    [Fact]
    public void LicensedProjectionIsEmbeddedAndMatchesRecordedArtifact()
    {
        var assembly = typeof(WasherAssetPresentation).Assembly;
        var resourceName = Assert.Single(assembly.GetManifestResourceNames(), name =>
            name.EndsWith(WasherAssetPresentation.ProjectionResourceSuffix, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        Assert.Equal("D50F01DB2D5320F925CBFEB201E884E6937510578F669D65D5EB8DF8606C9F80", Hash(stream));
    }

    [Fact]
    public void LicensedGlbIsValidAndRegisteredAsCanonicalStrideModel()
    {
        var root = FindRepositoryRoot();
        var glbPath = Path.Combine(root, "src", "Automation.Client.Stride", "Resources", "Imported", "KenneyFurnitureKit", "washer.glb");
        var packagePath = Path.Combine(root, "src", "Automation.Client.Stride", "Automation.Client.Stride.sdpkg");
        var modelPath = Path.Combine(root, "src", "Automation.Client.Stride", "Assets", "Imported", "KenneyFurnitureKit", "Washer.sdm3d");

        using var stream = File.OpenRead(glbPath);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        Assert.Equal(0x46546C67u, reader.ReadUInt32());
        Assert.Equal(2u, reader.ReadUInt32());
        Assert.Equal(stream.Length, reader.ReadUInt32());
        var jsonLength = reader.ReadUInt32();
        Assert.Equal(0x4E4F534Au, reader.ReadUInt32());
        using var json = JsonDocument.Parse(reader.ReadBytes(checked((int)jsonLength)));

        Assert.NotEmpty(json.RootElement.GetProperty("meshes").EnumerateArray());
        Assert.NotEmpty(json.RootElement.GetProperty("materials").EnumerateArray());
        stream.Position = 0;
        Assert.Equal("0C9704DF1817D2699B72305B96CDE097C9558BACABAEF2FD5AB1FF4C03B81ABF", Hash(stream));
        Assert.Contains(WasherAssetPresentation.ModelContentUrl, File.ReadAllText(packagePath), StringComparison.Ordinal);
        Assert.Contains("washer.glb", File.ReadAllText(modelPath), StringComparison.OrdinalIgnoreCase);
    }

    private static string Hash(Stream stream) => Convert.ToHexString(SHA256.HashData(stream));

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "TheAutomationGame.sln"))) return current.FullName;
        throw new DirectoryNotFoundException("Could not locate TheAutomationGame.sln.");
    }

}
