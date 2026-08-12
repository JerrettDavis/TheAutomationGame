using Automation.Content;
using Automation.Domain;

namespace Automation.Client.Stride;

public sealed record FirstShiftBriefingPresentation(string Title, string Body);

public sealed record FirstShiftDebriefPresentation(
    string ChapterTitle,
    string Summary,
    IReadOnlyList<string> Questions);

public static class FirstShiftNarrativePresenter
{
    public static FirstShiftBriefingPresentation Briefing(
        int page,
        DishStationFirstShiftNarrative? narrative = null)
    {
        var chapter = (narrative ?? DishStationFirstHoursContent.Narrative).Chapter;
        if (page < 0 || page >= chapter.Briefing.Length) throw new ArgumentOutOfRangeException(nameof(page));
        var briefing = chapter.Briefing[page];
        return new(briefing.Title.ToUpperInvariant(), briefing.Body.ToUpperInvariant());
    }

    public static FirstShiftDebriefPresentation Debrief(DishStationFirstShiftNarrative? narrative = null)
    {
        var chapter = (narrative ?? DishStationFirstHoursContent.Narrative).Chapter;
        return new(
            chapter.ChapterTitle.ToUpperInvariant(),
            chapter.DebriefSummary.ToUpperInvariant(),
            chapter.DebriefQuestions.Select(question => question.ToUpperInvariant()).ToArray());
    }

    public static string WindowTitle(
        bool careerMenu,
        bool briefing,
        DishStationQuestId? activeQuest,
        DishStationFirstShiftNarrative? narrative = null)
    {
        var firstShift = narrative ?? DishStationFirstHoursContent.Narrative;
        var moment = careerMenu ? "CAREER" : briefing ? "STARTING BRIEF" :
            activeQuest is { } quest ? firstShift.Quest(quest).Title : "SHIFT COMPLETE";
        return $"The Automation Game — {firstShift.Chapter.ChapterTitle} — {moment}";
    }
}
