using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class ProductionContentValidationTests
{
    private const string MinimalHash = "47083818e7da2e1f6b1708112fd43f5afa5879549a2f1f5bcb81a7e312651e86";
    private const string FirstShiftHash = "1714f45a8a2dd35b0ac82dece38716e275e305867f324b22627685e5c230782d";

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
