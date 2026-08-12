using Automation.Content;

namespace Automation.Content.Tests;

public sealed class ScenarioNarrativeContentTests
{
    [Fact]
    public void ProductionFirstShiftDefinesCompleteBriefingAndDebriefWhileMinimalRemainsCompatible()
    {
        var production = ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift);
        var chapter = production.Scenarios.Single(scenario =>
            scenario.Id.Value == DishStationFirstHoursContent.ScenarioId).Narrative;

        Assert.NotNull(chapter);
        Assert.Equal("ROSSI'S / FIRST SHIFT", chapter.ChapterTitle);
        Assert.Equal(3, chapter.Briefing.Length);
        Assert.All(chapter.Briefing, page =>
        {
            Assert.False(string.IsNullOrWhiteSpace(page.Title));
            Assert.False(string.IsNullOrWhiteSpace(page.Body));
        });
        Assert.False(string.IsNullOrWhiteSpace(chapter.DebriefSummary));
        Assert.Equal(3, chapter.DebriefQuestions.Length);
        Assert.Null(Assert.Single(ContentCompilerV1.CompileFile(ContentTestPaths.MinimalFixture).Scenarios).Narrative);
    }

    [Fact]
    public void IncompleteScenarioNarrativeFailsAtSemanticPath()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift).Replace(
            "      chapter_title: ROSSI'S / FIRST SHIFT",
            "      chapter_title:",
            StringComparison.Ordinal);

        var failure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(yaml, "missing-chapter-title.yaml"));

        Assert.Contains(failure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("narrative.chapter_title", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("required", StringComparison.Ordinal));
    }

    [Fact]
    public void FirstShiftAdapterRequiresExactBriefingAndDebriefCardinality()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift).Replace(
            "      debrief_summary:",
            "        - title: EXTRA PAGE\n          body: This valid generic page exceeds the bounded first-shift flow.\n      debrief_summary:",
            StringComparison.Ordinal);

        var failure = Assert.Throws<ContentCompilationException>(() =>
            DishStationFirstHoursContent.Compile(yaml, "extra-briefing.yaml"));

        Assert.Contains(failure.Diagnostics, diagnostic =>
            diagnostic.Path.Contains("narrative.briefing", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("exactly three", StringComparison.Ordinal));
    }
}
