using Automation.Content;

namespace Automation.Content.Tests;

public sealed class ContentCompilerV1Tests
{
    private const string ExpectedHash = "47083818e7da2e1f6b1708112fd43f5afa5879549a2f1f5bcb81a7e312651e86";

    [Fact]
    public void MinimalRestaurantFixtureCompilesAllV1KindsToDeterministicImmutableCatalog()
    {
        var path = ContentTestPaths.MinimalFixture;
        var first = ContentCompilerV1.CompileFile(path);
        var second = ContentCompilerV1.Compile(File.ReadAllText(path), "copy.yaml");

        Assert.Equal(1, first.Manifest.SchemaVersion);
        Assert.Equal(10, first.Manifest.DefinitionCount);
        Assert.All(Enum.GetValues<ContentDefinitionKind>(), kind => Assert.Equal(1, first.Manifest.Counts[kind]));
        Assert.Single(first.Industries);
        Assert.Single(first.Facilities);
        Assert.Single(first.Items);
        Assert.Single(first.Workstations);
        Assert.Single(first.Processes);
        Assert.Single(first.Scenarios);
        Assert.Single(first.Quests);
        Assert.Single(first.Characters);
        Assert.Single(first.Incidents);
        Assert.Single(first.Patterns);
        Assert.Equal(ExpectedHash, first.Manifest.Sha256);
        Assert.Equal(first.Manifest.Sha256, second.Manifest.Sha256);
        Assert.Equal(first.Manifest.Counts.OrderBy(pair => pair.Key), second.Manifest.Counts.OrderBy(pair => pair.Key));
        Assert.Equal("scenario.restaurant.schema-proof", first.Quests[0].Scenario.Value);
        Assert.Equal("presentation.fallback.workstation", first.Workstations[0].PresentationFallback.Value);
        Assert.False(first.Items.IsDefault);
    }

    [Fact]
    public void UnsupportedVersionFailsAtVersionPath() =>
        AssertDiagnostic(Yaml().Replace("schema_version: 1", "schema_version: 2", StringComparison.Ordinal),
            "schema_version", "Unsupported schema version 2");

    [Fact]
    public void MalformedIdFailsWithSemanticPath() =>
        AssertDiagnostic(Yaml().Replace("industry.restaurant", "Industry.Restaurant", StringComparison.Ordinal),
            "industries[0].id", "not a valid semantic content ID");

    [Fact]
    public void DuplicateGlobalIdFailsClearly()
    {
        var duplicate = "  - id: industry.restaurant\n    display_name: Duplicate Restaurant\n\n";
        AssertDiagnostic(Yaml().Replace("items:\n", duplicate + "items:\n", StringComparison.Ordinal),
            "industries", "Duplicate global content ID 'industry.restaurant'");
    }

    [Fact]
    public void UnknownReferenceNamesExpectedTypeAndId() =>
        AssertDiagnostic(Yaml().Replace("workstation.restaurant.washer.standard]", "workstation.restaurant.missing]", StringComparison.Ordinal),
            "facility[facility.restaurant.rossis.back-of-house].workstations", "Unknown workstation reference 'workstation.restaurant.missing'");

    [Fact]
    public void WrongReferenceTypeNamesActualAndExpectedTypes() =>
        AssertDiagnostic(Yaml().Replace("workstations: [workstation.restaurant.washer.standard]", "workstations: [item.restaurant.dish.plate]", StringComparison.Ordinal),
            "facility[facility.restaurant.rossis.back-of-house].workstations", "has type Item, expected Workstation");

    [Fact]
    public void UnknownItemStateFailsAtWorkstationStatePath() =>
        AssertDiagnostic(Yaml().Replace("output_state: clean", "output_state: polished", StringComparison.Ordinal),
            "workstation[workstation.restaurant.washer.standard].output_state", "does not exist");

    [Fact]
    public void DisallowedProcessCycleFailsClearly()
    {
        var original = "    steps:\n      - id: wash\n        workstation: workstation.restaurant.washer.standard\n    routes: []";
        var cyclic = "    steps:\n      - id: wash\n        workstation: workstation.restaurant.washer.standard\n      - id: return\n        workstation: workstation.restaurant.washer.standard\n    routes:\n      - from: wash\n        to: return\n      - from: return\n        to: wash";
        AssertDiagnostic(Yaml().Replace(original, cyclic, StringComparison.Ordinal),
            "process[process.restaurant.dishwashing.minimal].routes", "contains a cycle");
    }

    [Fact]
    public void UnsupportedQuestMetricFailsClearly() =>
        AssertDiagnostic(Yaml().Replace("service.available.count", "service.magic.score", StringComparison.Ordinal),
            "quest[quest.restaurant.schema-proof.restore-service].completion.metric", "Unknown metric 'service.magic.score'");

    [Fact]
    public void MalformedYamlIncludesSourceLineAndColumn()
    {
        var exception = Assert.Throws<ContentCompilationException>(() =>
            ContentCompilerV1.Compile("schema_version: 1\nindustries:\n  - id: [unterminated", "broken.yaml"));

        var diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("broken.yaml", diagnostic.Source);
        Assert.NotNull(diagnostic.Line);
        Assert.NotNull(diagnostic.Column);
        Assert.Contains("broken.yaml(", diagnostic.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownYamlPropertyIsRejectedRatherThanIgnored()
    {
        var exception = Assert.Throws<ContentCompilationException>(() =>
            ContentCompilerV1.Compile(Yaml().Replace("display_name: Restaurant", "display_name: Restaurant\n    mystery_flag: true", StringComparison.Ordinal), "unknown.yaml"));

        Assert.Contains(exception.Diagnostics, diagnostic => diagnostic.Message.Contains("mystery_flag", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertDiagnostic(string yaml, string path, string message)
    {
        var exception = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(yaml, "invalid.yaml"));
        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path == path && diagnostic.Message.Contains(message, StringComparison.Ordinal));
    }

    private static string Yaml() => File.ReadAllText(ContentTestPaths.MinimalFixture).ReplaceLineEndings("\n");
}
