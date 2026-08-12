using System.Globalization;
using System.Text;
using Automation.Persistence;

namespace Automation.Tools;

public enum FirstHoursGateStatus
{
    Pending,
    Pass,
    Fail,
}

public sealed record FirstHoursReadinessSession(
    string DirectoryPath,
    FirstHoursFacilitatorObservation Observation,
    FirstHoursPlaytestEvidence? CompletionEvidence);

public sealed record FirstHoursReadinessCriterion(
    string Id,
    string Requirement,
    string Observed,
    FirstHoursGateStatus Status);

public sealed record FirstHoursReadinessFollowUp(
    int Priority,
    string Source,
    string Finding,
    string Owner,
    string Disposition);

public sealed record FirstHoursReadinessReport(
    int HumanSessions,
    int SyntheticFixtures,
    IReadOnlyList<FirstHoursReadinessSession> Sessions,
    IReadOnlyList<FirstHoursReadinessCriterion> Criteria,
    IReadOnlyList<FirstHoursReadinessFollowUp> FollowUps)
{
    public bool GatePassed => HumanSessions >= FirstHoursReadinessEvaluator.MinimumHumanSessions &&
                              Criteria.All(criterion => criterion.Status == FirstHoursGateStatus.Pass);
}

public static class FirstHoursReadinessCohort
{
    public static IReadOnlyList<FirstHoursReadinessSession> LoadDirectory(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException($"Playtest root not found: {fullRoot}");

        var sessions = new List<FirstHoursReadinessSession>();
        foreach (var directory in Directory.GetDirectories(fullRoot).Order(StringComparer.Ordinal))
        {
            var observationPath = Path.Combine(directory, "facilitator-observation.json");
            if (!File.Exists(observationPath))
                throw new InvalidDataException($"Session '{Path.GetFileName(directory)}' has no facilitator-observation.json.");
            var observation = FirstHoursFacilitatorObservationStore.LoadFile(observationPath);
            if (!string.Equals(observation.SessionId, Path.GetFileName(directory), StringComparison.Ordinal))
                throw new InvalidDataException($"Observation session '{observation.SessionId}' does not match directory '{Path.GetFileName(directory)}'.");

            var evidencePath = Path.Combine(directory, "first-hours-evidence.json");
            var evidence = File.Exists(evidencePath) ? FirstHoursPlaytestEvidenceStore.LoadFile(evidencePath) : null;
            if (evidence is not null && !string.Equals(evidence.SessionId, observation.SessionId, StringComparison.Ordinal))
                throw new InvalidDataException($"Completion evidence session '{evidence.SessionId}' does not match observation '{observation.SessionId}'.");
            sessions.Add(new(directory, observation, evidence));
        }
        return sessions;
    }
}

public static class FirstHoursReadinessEvaluator
{
    public const int MinimumHumanSessions = 5;
    public const int MinimumVocabularyNovices = 2;
    public const double MinimumWallMinutes = 45;
    public const double MaximumWallMinutes = 120;

    public static FirstHoursReadinessReport Evaluate(IEnumerable<FirstHoursReadinessSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var all = sessions.OrderBy(session => session.Observation.SessionId, StringComparer.Ordinal).ToArray();
        var duplicate = all.GroupBy(session => session.Observation.SessionId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Session '{duplicate.Key}' appears more than once.");
        foreach (var session in all)
        {
            session.Observation.Validate();
            if (session.CompletionEvidence is not null &&
                !string.Equals(session.Observation.SessionId, session.CompletionEvidence.SessionId, StringComparison.Ordinal))
                throw new InvalidDataException($"Session '{session.Observation.SessionId}' has mismatched completion evidence.");
            if (session.CompletionEvidence is { } evidence &&
                (!evidence.Onboarding.Complete || !evidence.ShiftReport.Available ||
                 evidence.ShiftTrial.Status != Automation.Simulation.ShiftTrialStatus.Passed ||
                 evidence.Quests.Length == 0 || evidence.Quests.Any(quest => !quest.Complete) ||
                 evidence.WallClockSeconds < 0))
                throw new InvalidDataException($"Session '{session.Observation.SessionId}' has incomplete or invalid first-hours completion evidence.");
        }

        var human = all.Where(session => session.Observation.ParticipantKind == FirstHoursParticipantKind.Human).ToArray();
        var enough = human.Length >= MinimumHumanSessions;
        var required80 = Required(human.Length, 0.8);
        var required60 = Required(human.Length, 0.6);
        var completedWithoutHelp = human.Count(session => session.CompletionEvidence is not null &&
                                                          !session.Observation.ActionDirectedFacilitatorHelp);
        var movementAndInteraction = human.Count(session => session.Observation.MovementDiscoveredWithoutCoaching &&
                                                        session.Observation.InteractionDiscoveredWithoutCoaching);
        var bottleneck = human.Count(session => session.Observation.MeaningfulBottleneckIdentifiedCausally);
        var readiness = human.Count(session => session.Observation.ReportedVsPhysicalReadinessUnderstood);
        var proof = human.Count(session => session.Observation.ReplayProofValueArticulated);
        var strategy = human.Count(session => session.Observation.StrategyExpressedBeforeNaming);
        var novices = human.Count(session => session.Observation.VocabularyNovice);
        var guided = human.Count(session => session.CompletionEvidence?.Onboarding.GuidanceMode.ToString() == "Guided");
        var contextual = human.Count(session => session.CompletionEvidence?.Onboarding.GuidanceMode.ToString() == "Contextual");
        var completed = human.Where(session => session.CompletionEvidence is not null).ToArray();
        var inEnvelope = completed.Count(session =>
        {
            var minutes = session.CompletionEvidence!.WallClockSeconds / 60d;
            return minutes is >= MinimumWallMinutes and <= MaximumWallMinutes;
        });
        var durationRequired = Required(completed.Length, 0.8);
        var blockerGroups = human.Select(session => Normalize(session.Observation.PrimaryProgressionBlocker))
            .Where(blocker => blocker is not null)
            .GroupBy(blocker => blocker!, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).ToArray();
        var maximumBlockerRecurrence = blockerGroups.FirstOrDefault()?.Count() ?? 0;

        var criteria = new List<FirstHoursReadinessCriterion>
        {
            Criterion("sample", $"At least {MinimumHumanSessions} human sessions", $"{human.Length} human; {all.Length - human.Length} synthetic fixture", enough, enough),
            Criterion("novice-representation", $"At least {MinimumVocabularyNovices} vocabulary novices", $"{novices}/{human.Length}", novices >= MinimumVocabularyNovices, enough),
            Criterion("guidance-coverage", "Guided and Contextual each represented", $"Guided {guided}; Contextual {contextual}", guided >= 1 && contextual >= 1, enough),
            Criterion("unassisted-completion", "At least 80% complete without action-directed help", $"{completedWithoutHelp}/{human.Length}; need {required80}", completedWithoutHelp >= required80, enough),
            Criterion("movement-interaction", "At least 80% discover movement and contextual interaction without coaching", $"{movementAndInteraction}/{human.Length}; need {required80}", movementAndInteraction >= required80, enough),
            Criterion("bottleneck", "At least 80% causally identify a meaningful bottleneck", $"{bottleneck}/{human.Length}; need {required80}", bottleneck >= required80, enough),
            Criterion("readiness-disagreement", "At least 80% explain reported versus physical readiness", $"{readiness}/{human.Length}; need {required80}", readiness >= required80, enough),
            Criterion("replay-proof", "At least 80% articulate why replay/proof matters", $"{proof}/{human.Length}; need {required80}", proof >= required80, enough),
            Criterion("strategy-before-name", "At least 60% express the Strategy shape before naming", $"{strategy}/{human.Length}; need {required60}", strategy >= required60, enough),
            Criterion("progression-blockers", "No progression blocker occurs in more than one session", maximumBlockerRecurrence == 0 ? "None recorded" : $"Maximum recurrence {maximumBlockerRecurrence}", maximumBlockerRecurrence <= 1, enough),
            Criterion("critical-issues", "Every critical UI/accessibility issue has an owner and disposition", $"{human.Sum(session => session.Observation.CriticalIssues.Length)} recorded", true, enough),
            Criterion("duration", "At least 80% of completed shifts finish in 45–120 wall-clock minutes", $"{inEnvelope}/{completed.Length}; need {durationRequired}", completed.Length > 0 && inEnvelope >= durationRequired, enough),
        };

        var followUps = new List<FirstHoursReadinessFollowUp>();
        foreach (var criterion in criteria.Where(item => item.Status != FirstHoursGateStatus.Pass))
            followUps.Add(new(criterion.Status == FirstHoursGateStatus.Fail ? 1 : 3, criterion.Id,
                $"{criterion.Requirement}: {criterion.Observed}", "S035 study owner", criterion.Status.ToString().ToUpperInvariant()));
        foreach (var blocker in blockerGroups)
            followUps.Add(new(blocker.Count() > 1 ? 1 : 2, $"blocker:{blocker.Key}",
                $"Observed in {blocker.Count()} human session(s).", "S035 study owner", blocker.Count() > 1 ? "REQUIRES FIX" : "MONITOR"));
        foreach (var issue in human.SelectMany(session => session.Observation.CriticalIssues)
                     .GroupBy(issue => issue.Code, StringComparer.Ordinal)
                     .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal))
        {
            var first = issue.First();
            followUps.Add(new(issue.Count() > 1 ? 1 : 2, $"issue:{issue.Key}",
                $"{first.Summary} Observed in {issue.Count()} human session(s).", first.Owner, first.Disposition.ToString().ToUpperInvariant()));
        }

        return new(human.Length, all.Length - human.Length, all, criteria,
            followUps.OrderBy(item => item.Priority).ThenBy(item => item.Source, StringComparer.Ordinal).ToArray());
    }

    public static string ToMarkdown(FirstHoursReadinessReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var builder = new StringBuilder();
        builder.AppendLine("# S035 Restaurant Human Readiness Report");
        builder.AppendLine();
        builder.AppendLine($"**Gate: {(report.GatePassed ? "PASS" : "NOT READY")}** — {report.HumanSessions} human session(s); {report.SyntheticFixtures} synthetic fixture(s) excluded.");
        builder.AppendLine();
        builder.AppendLine("## Criteria");
        builder.AppendLine();
        builder.AppendLine("| Criterion | Requirement | Observed | Status |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var criterion in report.Criteria)
            builder.AppendLine($"| `{Escape(criterion.Id)}` | {Escape(criterion.Requirement)} | {Escape(criterion.Observed)} | **{criterion.Status.ToString().ToUpperInvariant()}** |");
        builder.AppendLine();
        builder.AppendLine("## Sessions");
        builder.AppendLine();
        builder.AppendLine("| Session | Kind | Complete | Guidance | Wall minutes | Action-directed help | Blocker |");
        builder.AppendLine("|---|---|---:|---|---:|---:|---|");
        foreach (var session in report.Sessions)
        {
            var evidence = session.CompletionEvidence;
            var wall = evidence is null ? "—" : (evidence.WallClockSeconds / 60d).ToString("0.0", CultureInfo.InvariantCulture);
            builder.AppendLine($"| `{Escape(session.Observation.SessionId)}` | {session.Observation.ParticipantKind} | {(evidence is null ? "No" : "Yes")} | {evidence?.Onboarding.GuidanceMode.ToString() ?? "—"} | {wall} | {(session.Observation.ActionDirectedFacilitatorHelp ? "Yes" : "No")} | {Escape(Normalize(session.Observation.PrimaryProgressionBlocker) ?? "None")} |");
        }
        builder.AppendLine();
        builder.AppendLine("## Comprehension observations");
        builder.AppendLine();
        builder.AppendLine("| Session | Novice | Movement + interaction | Bottleneck | Readiness disagreement | Replay/proof | Strategy before name |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var session in report.Sessions)
        {
            var observation = session.Observation;
            builder.AppendLine($"| `{Escape(observation.SessionId)}` | {YesNo(observation.VocabularyNovice)} | {YesNo(observation.MovementDiscoveredWithoutCoaching && observation.InteractionDiscoveredWithoutCoaching)} | {YesNo(observation.MeaningfulBottleneckIdentifiedCausally)} | {YesNo(observation.ReportedVsPhysicalReadinessUnderstood)} | {YesNo(observation.ReplayProofValueArticulated)} | {YesNo(observation.StrategyExpressedBeforeNaming)} |");
        }
        builder.AppendLine();
        builder.AppendLine("## Prioritized follow-ups");
        builder.AppendLine();
        if (report.FollowUps.Count == 0) builder.AppendLine("No follow-up was generated from the recorded cohort.");
        else
        {
            builder.AppendLine("| Priority | Source | Finding | Owner | Disposition |");
            builder.AppendLine("|---:|---|---|---|---|");
            foreach (var followUp in report.FollowUps)
                builder.AppendLine($"| {followUp.Priority} | `{Escape(followUp.Source)}` | {Escape(followUp.Finding)} | {Escape(followUp.Owner)} | {Escape(followUp.Disposition)} |");
        }
        builder.AppendLine();
        builder.AppendLine("Synthetic fixtures validate this report path but never contribute to human thresholds. Raw study-local observations remain outside version control unless explicitly de-identified and approved for retention.");
        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static void SaveMarkdownAtomic(string path, FirstHoursReadinessReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)
                                  ?? throw new ArgumentException("Report path must have a parent directory.", nameof(path)));
        var temporary = fullPath + ".tmp";
        File.WriteAllText(temporary, ToMarkdown(report), new UTF8Encoding(false));
        File.Move(temporary, fullPath, true);
    }

    private static FirstHoursReadinessCriterion Criterion(string id, string requirement, string observed, bool passed, bool enough) =>
        new(id, requirement, observed, enough ? passed ? FirstHoursGateStatus.Pass : FirstHoursGateStatus.Fail : FirstHoursGateStatus.Pending);

    private static int Required(int total, double fraction) => total == 0 ? 0 : (int)Math.Ceiling(total * fraction);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string YesNo(bool value) => value ? "Yes" : "No";
}
