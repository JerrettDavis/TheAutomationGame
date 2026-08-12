using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class PatternKnowledgeContentTests
{
    [Fact]
    public void ProductionPatternOverlayKeepsTheNameHiddenUntilEvidenceAndAuthorsTheReveal()
    {
        var definition = DishStationPatternContent.Strategy;

        Assert.Equal(new PatternId("pattern.strategy"), definition.PatternId);
        Assert.Equal("strategy", definition.ExternalCatalogId);
        Assert.Equal("REUSABLE ROUTING CHOICE", definition.PreNameTitle);
        Assert.DoesNotContain(definition.ExternalCatalogId, definition.PreNameTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("STRATEGY", definition.Naming.ConventionalName);
        Assert.Equal("STRATEGY PATTERN", definition.Naming.DisplayTitle);
        Assert.Equal(3, definition.Naming.Structure.Length);
        Assert.Equal(2, definition.Naming.Benefits.Length);
        Assert.Equal(2, definition.Naming.Costs.Length);
        Assert.All(definition.Naming.Structure, Assert.NotEmpty);
        Assert.All(definition.Naming.Benefits, Assert.NotEmpty);
        Assert.All(definition.Naming.Costs, Assert.NotEmpty);
        Assert.Equal(2, definition.MinimumEvidence);
        Assert.True(definition.RequiresApplication);
        Assert.Equal([PatternProblemSignature.InterchangeablePolicy], definition.ProblemSignatures.ToArray());
        Assert.Equal([new ContentId(DishStationTwoStationsContent.QuestId)], definition.PrimaryEncounters.ToArray());
    }

    [Fact]
    public void PatternSchemaRejectsARevealingPreNameTitleAndUnknownQuest()
    {
        var revealing = ProductionYaml().Replace("pre_name_title: REUSABLE ROUTING CHOICE",
            "pre_name_title: STRATEGY", StringComparison.Ordinal);
        var error = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(revealing));
        Assert.Contains(error.Diagnostics, diagnostic => diagnostic.Path.Contains("pre_name_title", StringComparison.Ordinal));

        var missingQuest = ProductionYaml().Replace("quest.restaurant.two-stations.one-problem]",
            "quest.restaurant.missing]", StringComparison.Ordinal);
        error = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(missingQuest));
        Assert.Contains(error.Diagnostics, diagnostic => diagnostic.Path.Contains("primary_encounters", StringComparison.Ordinal));

        var missingTradeoff = ProductionYaml().Replace(
            "      costs:\r\n        - EVERY ADDED POLICY NEEDS A CLEAR SELECTION RULE AND ITS OWN VALIDATION.\r\n        - COPYING A POLICY WITHOUT ITS CONTEXT CAN PRODUCE A PLAUSIBLE BUT WRONG RESULT.",
            "      costs: []", StringComparison.Ordinal).Replace(
            "      costs:\n        - EVERY ADDED POLICY NEEDS A CLEAR SELECTION RULE AND ITS OWN VALIDATION.\n        - COPYING A POLICY WITHOUT ITS CONTEXT CAN PRODUCE A PLAUSIBLE BUT WRONG RESULT.",
            "      costs: []", StringComparison.Ordinal);
        error = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(missingTradeoff));
        Assert.Contains(error.Diagnostics, diagnostic => diagnostic.Path.Contains("naming.costs", StringComparison.Ordinal));
    }

    private static string ProductionYaml() => File.ReadAllText(Path.Combine(RepositoryRoot(), "content", "restaurant", "first-shift.yaml"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheAutomationGame.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
