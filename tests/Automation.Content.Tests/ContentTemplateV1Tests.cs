using System.Text.Json;
using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class ContentTemplateV1Tests
{
    [Fact]
    public void IdenticalLogicalInputsProduceByteStableExpansionHashesAndProvenance()
    {
        var template = ContentTemplateCompilerV1.CompileFile(ContentTestPaths.SeededScenarioTemplate);
        var first = template.Expand(new Dictionary<string, string>
        {
            ["facility-slug"] = "proof-house",
            ["rack-capacity"] = "12",
        }, "proof-5");
        var second = template.Expand(new Dictionary<string, string>
        {
            ["rack-capacity"] = "012",
            ["facility-slug"] = "proof-house",
        }, "proof-5");

        Assert.Equal(first.ExpandedYaml, second.ExpandedYaml);
        Assert.Equal(first.Catalog.Manifest.Sha256, second.Catalog.Manifest.Sha256);
        Assert.Equal(first.ExpansionSha256, second.ExpansionSha256);
        Assert.Equal(first.Provenance.TemplateId, second.Provenance.TemplateId);
        Assert.Equal(first.Provenance.TemplateVersion, second.Provenance.TemplateVersion);
        Assert.Equal(first.Provenance.NamedSeed, second.Provenance.NamedSeed);
        Assert.Equal(first.Provenance.Parameters, second.Provenance.Parameters);
        Assert.Equal(first.Provenance.VariantSelections, second.Provenance.VariantSelections);
        Assert.Equal("glass", first.Provenance.VariantSelections["demand-kind"]);
        Assert.DoesNotContain("{{", first.ExpandedYaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangedSeedOnlyChangesDeclaredVariantAndDerivedHashes()
    {
        var template = ContentTemplateCompilerV1.CompileFile(ContentTestPaths.SeededScenarioTemplate);
        var parameters = Parameters();
        var tray = template.Expand(parameters, "proof-0");
        var plate = template.Expand(parameters, "proof-1");

        Assert.Equal("tray", tray.Provenance.VariantSelections["demand-kind"]);
        Assert.Equal("plate", plate.Provenance.VariantSelections["demand-kind"]);
        Assert.Equal(NormalizeDemand(tray.ExpandedYaml), NormalizeDemand(plate.ExpandedYaml));
        Assert.Equal(NonScenarioCatalogJson(tray.Catalog), NonScenarioCatalogJson(plate.Catalog));
        var trayScenario = Assert.Single(tray.Catalog.Scenarios);
        var plateScenario = Assert.Single(plate.Catalog.Scenarios);
        Assert.Equal(DishKind.Tray, trayScenario.DishStation!.DemandKind);
        Assert.Equal(DishKind.Plate, plateScenario.DishStation!.DemandKind);
        Assert.Equal(
            JsonSerializer.Serialize(trayScenario with { DishStation = trayScenario.DishStation with { DemandKind = DishKind.Glass } }),
            JsonSerializer.Serialize(plateScenario with { DishStation = plateScenario.DishStation with { DemandKind = DishKind.Glass } }));
        Assert.NotEqual(tray.Catalog.Manifest.Sha256, plate.Catalog.Manifest.Sha256);
        Assert.NotEqual(tray.ExpansionSha256, plate.ExpansionSha256);
    }

    [Theory]
    [InlineData("missing", "parameters.rack-capacity", "was not supplied")]
    [InlineData("extra", "parameters.surprise", "is not declared")]
    [InlineData("invalid", "parameters.rack-capacity", "invalid for parameter kind positive_integer")]
    [InlineData("seed", "named_seed", "Named seed is required")]
    public void InvalidExpansionInputsFailAtTargetedPaths(string caseName, string path, string message)
    {
        var template = ContentTemplateCompilerV1.CompileFile(ContentTestPaths.SeededScenarioTemplate);
        var parameters = Parameters();
        string? seed = "proof-5";
        if (caseName == "missing") parameters.Remove("rack-capacity");
        if (caseName == "extra") parameters["surprise"] = "value";
        if (caseName == "invalid") parameters["rack-capacity"] = "0";
        if (caseName == "seed") seed = null;

        var exception = Assert.Throws<ContentCompilationException>(() => template.Expand(parameters, seed));

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path == path && diagnostic.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void NamedSeedIsRejectedWhenTemplateHasNoVariableFields()
    {
        var yaml = TemplateYaml()
            .Replace("variants:\n  demand-kind:\n    kind: token\n    options: [plate, glass, tray]\n\n", "", StringComparison.Ordinal)
            .Replace("{{variant:demand-kind}}", "glass", StringComparison.Ordinal);
        var template = ContentTemplateCompilerV1.Compile(yaml, "fixed.template.yaml");

        var exception = Assert.Throws<ContentCompilationException>(() => template.Expand(Parameters(), "unnecessary"));

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path == "named_seed" && diagnostic.Message.Contains("only allowed", StringComparison.Ordinal));
    }

    [Fact]
    public void ExpandedContentStillPassesOrdinarySemanticValidation()
    {
        var yaml = TemplateYaml().Replace(
            "id: facility.restaurant.{{parameter:facility-slug}}",
            "id: facility.restaurant.declared",
            StringComparison.Ordinal);
        var template = ContentTemplateCompilerV1.Compile(yaml, "broken-expansion.template.yaml");

        var exception = Assert.Throws<ContentCompilationException>(() => template.Expand(Parameters(), "proof-5"));

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Source == "broken-expansion.template.yaml#expanded" &&
            diagnostic.Message.Contains("Unknown facility reference", StringComparison.Ordinal));
    }

    [Fact]
    public void UndeclaredAndUnusedPlaceholdersFailDuringTemplateCompilation()
    {
        var undeclared = TemplateYaml().Replace("{{parameter:rack-capacity}}", "{{parameter:not-declared}}", StringComparison.Ordinal);
        var exception = Assert.Throws<ContentCompilationException>(() =>
            ContentTemplateCompilerV1.Compile(undeclared, "bad-template.yaml"));

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path == "content" && diagnostic.Message.Contains("Undeclared parameter", StringComparison.Ordinal));
        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path == "parameters.rack-capacity" && diagnostic.Message.Contains("unused", StringComparison.Ordinal));
    }

    private static Dictionary<string, string> Parameters() => new(StringComparer.Ordinal)
    {
        ["facility-slug"] = "proof-house",
        ["rack-capacity"] = "12",
    };

    private static string TemplateYaml() => File.ReadAllText(ContentTestPaths.SeededScenarioTemplate).ReplaceLineEndings("\n");

    private static string NormalizeDemand(string yaml) => yaml
        .Replace("demand_kind: tray", "demand_kind: <declared-variant>", StringComparison.Ordinal)
        .Replace("demand_kind: plate", "demand_kind: <declared-variant>", StringComparison.Ordinal)
        .Replace("demand_kind: glass", "demand_kind: <declared-variant>", StringComparison.Ordinal);

    private static string NonScenarioCatalogJson(CompiledContentCatalogV1 catalog) => JsonSerializer.Serialize(new
    {
        catalog.Industries,
        catalog.Facilities,
        catalog.Items,
        catalog.Workstations,
        catalog.Processes,
        catalog.Quests,
        catalog.Characters,
    });
}
