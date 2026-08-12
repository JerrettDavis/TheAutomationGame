using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class ProductionContentValidationTests
{
    private const string MinimalHash = "947cf1d85ff60b1f44a3457c8cd28dc54f53e5aad88c2de7ffb0be7929b3c2cf";
    private const string FirstShiftHash = "33d12d1f5612c3140d6e5bcf76ec6e6fc01ca4182efe0abf413dc972e2e0df64";

    [Fact]
    public void EveryCheckedInValidBundleCompilesToItsDeterministicManifest()
    {
        var minimal = ContentCompilerV1.CompileFile(ContentTestPaths.MinimalFixture);
        var firstShift = ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift);

        Assert.Equal(MinimalHash, minimal.Manifest.Sha256);
        Assert.Equal(FirstShiftHash, firstShift.Manifest.Sha256);
        Assert.Equal(firstShift.Manifest.Sha256,
            ContentCompilerV1.Compile(File.ReadAllText(ContentTestPaths.FirstShift), "copy.yaml").Manifest.Sha256);
    }

    [Fact]
    public void ProductionDefinitionsCarryRequiredPresentationFallbacks()
    {
        var catalogs = new[]
        {
            ContentCompilerV1.CompileFile(ContentTestPaths.MinimalFixture),
            ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift),
        };

        Assert.All(catalogs.SelectMany(catalog => catalog.Workstations), workstation =>
            Assert.StartsWith("presentation.fallback.", workstation.PresentationFallback.Value, StringComparison.Ordinal));
    }

    [Fact]
    public void FirstShiftAdapterAcceptsExactlyEveryAuthoritativeQuestBeat()
    {
        var narrative = DishStationFirstHoursContent.Compile(File.ReadAllText(ContentTestPaths.FirstShift), "first-shift.yaml");
        var authored = narrative.Quests.SelectMany(quest => quest.Steps).Select(step => step.Id).ToHashSet(StringComparer.Ordinal);
        var expected = Enum.GetValues<DishTutorialStage>()
            .Select(stage => StageToken(stage.ToString()))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(expected, authored);
    }

    private static string StageToken(string value) => string.Concat(value.SelectMany((character, index) =>
        index > 0 && char.IsUpper(character)
            ? new[] { '-', char.ToLowerInvariant(character) }
            : new[] { char.ToLowerInvariant(character) }));
}
