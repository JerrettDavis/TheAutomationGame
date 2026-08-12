using Automation.Client.Stride;

namespace Automation.Integration.Tests;

public sealed class ClientScreenRouterTests
{
    [Fact]
    public void NewInstallBeginsAtBriefingAndSavedCareerBeginsAtStartMenu()
    {
        var router = new ClientScreenRouter();

        router.Initialize(hasCareerSave: false);
        Assert.Equal(ClientScreen.Briefing, router.Screen);
        Assert.Equal(ClientModal.None, router.Modal);

        router.Initialize(hasCareerSave: true);
        Assert.Equal(ClientScreen.StartMenu, router.Screen);
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Theory]
    [InlineData(false, ClientScreen.Briefing)]
    [InlineData(true, ClientScreen.Gameplay)]
    public void ResumingCareerRoutesFromAuthoritativeBriefingState(bool briefingComplete, ClientScreen expected)
    {
        var router = new ClientScreenRouter();
        router.Initialize(hasCareerSave: true);

        router.ShowCareer(briefingComplete);

        Assert.Equal(expected, router.Screen);
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void JournalDetailBackAndCloseFollowTheExistingFlow()
    {
        var router = GameplayRouter();

        Assert.True(router.ToggleJournal());
        Assert.Equal(ClientModal.QuestJournal, router.Modal);
        Assert.True(router.ToggleJournalDetail());
        Assert.Equal(ClientModal.QuestDetail, router.Modal);

        router.BackFromJournal();
        Assert.Equal(ClientModal.QuestJournal, router.Modal);
        router.BackFromJournal();
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void JournalToggleClosesDetailDirectly()
    {
        var router = GameplayRouter();
        router.ToggleJournal();
        router.ToggleJournalDetail();

        Assert.False(router.ToggleJournal());
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void HelpAndShiftReportReplaceOtherOverlays()
    {
        var router = GameplayRouter();
        router.ToggleJournal();

        Assert.True(router.ToggleHelp());
        Assert.Equal(ClientModal.Help, router.Modal);
        Assert.True(router.ToggleShiftReport());
        Assert.Equal(ClientModal.ShiftReport, router.Modal);
        Assert.False(router.ToggleShiftReport());
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void GameplayModalCannotLeakOntoBriefing()
    {
        var router = new ClientScreenRouter();
        router.Initialize(hasCareerSave: false);

        Assert.False(router.ToggleJournal());
        Assert.Equal(ClientScreen.Briefing, router.Screen);
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void StartingNewCareerDismissesConfirmationAndShowsBriefing()
    {
        var router = new ClientScreenRouter();
        router.Initialize(hasCareerSave: true);
        router.ShowNewCareerConfirmation();
        Assert.Equal(ClientModal.NewCareerConfirmation, router.Modal);

        router.ShowCareer(briefingComplete: false);

        Assert.Equal(ClientScreen.Briefing, router.Screen);
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void SettingsAreAvailableFromStartMenuAndGameplayButNotBriefing()
    {
        var router = new ClientScreenRouter();
        router.Initialize(hasCareerSave: true);

        Assert.True(router.ToggleSettings());
        Assert.Equal(ClientModal.Settings, router.Modal);
        Assert.False(router.ToggleSettings());

        router.ShowCareer(briefingComplete: false);
        Assert.False(router.ToggleSettings());
        router.ShowCareer(briefingComplete: true);
        Assert.True(router.ToggleSettings());
    }

    [Fact]
    public void ProcessEditorIsGameplayOnlyAndReplacesOtherModals()
    {
        var router = new ClientScreenRouter();
        router.Initialize(hasCareerSave: false);
        Assert.False(router.ToggleProcessEditor());

        router.ShowCareer(briefingComplete: true);
        router.ToggleJournal();
        Assert.True(router.ToggleProcessEditor());
        Assert.Equal(ClientModal.ProcessEditor, router.Modal);
        Assert.False(router.ToggleProcessEditor());
        Assert.Equal(ClientModal.None, router.Modal);
    }

    [Fact]
    public void AutomationEditorIsGameplayOnlyAndReplacesOtherModals()
    {
        var router = new ClientScreenRouter();
        router.Initialize(hasCareerSave: false);
        Assert.False(router.ToggleAutomationEditor());
        router.ShowCareer(briefingComplete: true);
        router.ToggleHelp();
        Assert.True(router.ToggleAutomationEditor());
        Assert.Equal(ClientModal.AutomationEditor, router.Modal);
        Assert.False(router.ToggleAutomationEditor());
        Assert.Equal(ClientModal.None, router.Modal);
    }

    private static ClientScreenRouter GameplayRouter()
    {
        var router = new ClientScreenRouter();
        router.ShowCareer(briefingComplete: true);
        return router;
    }
}
