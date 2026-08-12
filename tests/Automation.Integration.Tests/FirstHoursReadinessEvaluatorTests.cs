using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;
using Automation.Tools;

namespace Automation.Integration.Tests;

public sealed class FirstHoursReadinessEvaluatorTests
{
    [Fact]
    public void FiveHumanSessionsAtFixedThresholdsPassWhileFixturesStayExcluded()
    {
        var sessions = Enumerable.Range(1, 5).Select(index => Session(
            $"human-{index}",
            FirstHoursParticipantKind.Human,
            novice: index <= 2,
            guidance: index == 1 ? GuidanceMode.Contextual : GuidanceMode.Guided,
            strong: index <= 4,
            strategy: index <= 3,
            actionHelp: index == 5,
            blocker: index == 5 ? "late-control-stall" : null)).Append(
            Session("fixture-perfect", FirstHoursParticipantKind.SyntheticFixture, novice: true,
                guidance: GuidanceMode.Contextual, strong: true, strategy: true)).ToArray();

        var report = FirstHoursReadinessEvaluator.Evaluate(sessions);

        Assert.True(report.GatePassed);
        Assert.Equal(5, report.HumanSessions);
        Assert.Equal(1, report.SyntheticFixtures);
        Assert.All(report.Criteria, criterion => Assert.Equal(FirstHoursGateStatus.Pass, criterion.Status));
        Assert.Contains(report.FollowUps, followUp => followUp.Source == "blocker:late-control-stall" && followUp.Disposition == "MONITOR");
    }

    [Fact]
    public void PerfectSyntheticFixturesCannotSatisfyHumanGate()
    {
        var fixtures = Enumerable.Range(1, 8).Select(index => Session(
            $"fixture-{index}", FirstHoursParticipantKind.SyntheticFixture, true, GuidanceMode.Guided,
            strong: true, strategy: true));

        var report = FirstHoursReadinessEvaluator.Evaluate(fixtures);

        Assert.False(report.GatePassed);
        Assert.Equal(0, report.HumanSessions);
        Assert.Equal(8, report.SyntheticFixtures);
        Assert.All(report.Criteria, criterion => Assert.Equal(FirstHoursGateStatus.Pending, criterion.Status));
        Assert.Contains("0 human session(s); 8 synthetic fixture(s) excluded", FirstHoursReadinessEvaluator.ToMarkdown(report), StringComparison.Ordinal);
    }

    [Fact]
    public void RecurringBlockerAndWeakComprehensionFailWithPrioritizedOwnedFollowUps()
    {
        var sessions = Enumerable.Range(1, 5).Select(index => Session(
            $"human-{index}", FirstHoursParticipantKind.Human, index <= 2,
            index == 1 ? GuidanceMode.Contextual : GuidanceMode.Guided,
            strong: index <= 3,
            strategy: index <= 2,
            blocker: index <= 2 ? "cannot-find-interact" : null,
            issues: index <= 2
                ? [new("ui-interact-affordance", "Interaction prompt was overlooked.", "UX/S036", FirstHoursIssueDisposition.Backlog)]
                : [])).ToArray();

        var report = FirstHoursReadinessEvaluator.Evaluate(sessions);

        Assert.False(report.GatePassed);
        Assert.Equal(FirstHoursGateStatus.Fail, report.Criteria.Single(item => item.Id == "movement-interaction").Status);
        Assert.Equal(FirstHoursGateStatus.Fail, report.Criteria.Single(item => item.Id == "strategy-before-name").Status);
        Assert.Equal(FirstHoursGateStatus.Fail, report.Criteria.Single(item => item.Id == "progression-blockers").Status);
        Assert.Contains(report.FollowUps, item => item.Priority == 1 && item.Source == "blocker:cannot-find-interact");
        Assert.Contains(report.FollowUps, item => item.Priority == 1 && item.Source == "issue:ui-interact-affordance" && item.Owner == "UX/S036");
    }

    [Fact]
    public void ObservationStoreRoundTripsAndRejectsUnownedOrDuplicateIssues()
    {
        var root = Path.Combine(Path.GetTempPath(), $"automation-readiness-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "observation.json");
        try
        {
            var valid = Observation("human-1", FirstHoursParticipantKind.Human, true, true, true,
                issues: [new("contrast", "Critical contrast failure.", "UX/S036", FirstHoursIssueDisposition.Fixed)]);
            FirstHoursFacilitatorObservationStore.SaveFileAtomic(path, valid);

            var restored = FirstHoursFacilitatorObservationStore.LoadFile(path);
            Assert.Equal(valid with { CriticalIssues = [] }, restored with { CriticalIssues = [] });
            Assert.Equal(valid.CriticalIssues, restored.CriticalIssues);
            Assert.False(File.Exists(path + ".tmp"));

            var unowned = valid with { CriticalIssues = [new("focus", "Focus lost.", "", FirstHoursIssueDisposition.Backlog)] };
            Assert.Throws<InvalidDataException>(() => FirstHoursFacilitatorObservationStore.SaveFileAtomic(path, unowned));
            var duplicate = valid with { CriticalIssues = [valid.CriticalIssues[0], valid.CriticalIssues[0]] };
            Assert.Throws<InvalidDataException>(() => duplicate.Validate());
            Assert.Throws<NotSupportedException>(() => (valid with { SchemaVersion = 1 }).Validate());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void DirectoryLoaderRejectsMissingAndMismatchedHumanEvidence()
    {
        var root = Path.Combine(Path.GetTempPath(), $"automation-readiness-cohort-{Guid.NewGuid():N}");
        var sessionDirectory = Path.Combine(root, "human-1");
        try
        {
            Directory.CreateDirectory(sessionDirectory);
            Assert.Throws<InvalidDataException>(() => FirstHoursReadinessCohort.LoadDirectory(root));

            FirstHoursFacilitatorObservationStore.SaveFileAtomic(
                Path.Combine(sessionDirectory, "facilitator-observation.json"),
                Observation("wrong-session", FirstHoursParticipantKind.Human, true, true, true));
            Assert.Throws<InvalidDataException>(() => FirstHoursReadinessCohort.LoadDirectory(root));

            FirstHoursFacilitatorObservationStore.SaveFileAtomic(
                Path.Combine(sessionDirectory, "facilitator-observation.json"),
                Observation("human-1", FirstHoursParticipantKind.Human, true, true, true));
            FirstHoursPlaytestEvidenceStore.SaveFileAtomic(
                Path.Combine(sessionDirectory, "first-hours-evidence.json"), Evidence("other", GuidanceMode.Guided, 60));
            Assert.Throws<InvalidDataException>(() => FirstHoursReadinessCohort.LoadDirectory(root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void MarkdownReportIsStableAndSavedAtomically()
    {
        var report = FirstHoursReadinessEvaluator.Evaluate([
            Session("fixture", FirstHoursParticipantKind.SyntheticFixture, true, GuidanceMode.Guided, true, true),
        ]);
        var first = FirstHoursReadinessEvaluator.ToMarkdown(report);
        var root = Path.Combine(Path.GetTempPath(), $"automation-readiness-report-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "report.md");
        try
        {
            FirstHoursReadinessEvaluator.SaveMarkdownAtomic(path, report);
            Assert.Equal(first, File.ReadAllText(path));
            Assert.Equal(first, FirstHoursReadinessEvaluator.ToMarkdown(report));
            Assert.Contains("**Gate: NOT READY**", first, StringComparison.Ordinal);
            Assert.Contains("Synthetic fixtures validate this report path but never contribute", first, StringComparison.Ordinal);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static FirstHoursReadinessSession Session(
        string id,
        FirstHoursParticipantKind kind,
        bool novice,
        GuidanceMode guidance,
        bool strong,
        bool strategy,
        bool actionHelp = false,
        string? blocker = null,
        FirstHoursReadinessIssue[]? issues = null) =>
        new(id, Observation(id, kind, novice, strong, strategy, actionHelp, blocker, issues), Evidence(id, guidance, 60));

    private static FirstHoursFacilitatorObservation Observation(
        string id,
        FirstHoursParticipantKind kind,
        bool novice,
        bool strong,
        bool strategy,
        bool actionHelp = false,
        string? blocker = null,
        FirstHoursReadinessIssue[]? issues = null) =>
        new(FirstHoursFacilitatorObservation.CurrentSchemaVersion, id,
            new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero), kind, novice,
            strong, strong, strong, strong, strong, strategy, actionHelp, blocker, issues ?? []);

    private static FirstHoursPlaytestEvidence Evidence(string id, GuidanceMode guidance, double wallMinutes) =>
        new(FirstHoursPlaytestEvidence.CurrentSchemaVersion, id,
            new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero).AddMinutes(wallMinutes),
            wallMinutes * 60, new OnboardingSnapshot(true, guidance), 7, 3400, 600,
            [], default, default, []);
}
