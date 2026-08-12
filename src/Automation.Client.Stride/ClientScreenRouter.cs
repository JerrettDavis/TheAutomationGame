namespace Automation.Client.Stride;

public enum ClientScreen
{
    StartMenu,
    Briefing,
    Gameplay,
}

public enum ClientModal
{
    None,
    NewCareerConfirmation,
    Help,
    QuestJournal,
    QuestDetail,
    ShiftReport,
    Settings,
    ProcessEditor,
    AutomationEditor,
    TwoStationRouting,
    PatternCodex,
}

public sealed class ClientScreenRouter
{
    public ClientScreen Screen { get; private set; } = ClientScreen.Briefing;

    public ClientModal Modal { get; private set; }

    public bool JournalVisible => Modal is ClientModal.QuestJournal or ClientModal.QuestDetail;

    public void Initialize(bool hasCareerSave)
    {
        Screen = hasCareerSave ? ClientScreen.StartMenu : ClientScreen.Briefing;
        Modal = ClientModal.None;
    }

    public void ShowCareer(bool briefingComplete)
    {
        Screen = briefingComplete ? ClientScreen.Gameplay : ClientScreen.Briefing;
        Modal = ClientModal.None;
    }

    public void ShowNewCareerConfirmation()
    {
        if (Screen == ClientScreen.StartMenu)
            Modal = ClientModal.NewCareerConfirmation;
    }

    public void DismissNewCareerConfirmation()
    {
        if (Modal == ClientModal.NewCareerConfirmation)
            Modal = ClientModal.None;
    }

    public bool ToggleJournal()
    {
        if (!IsGameplay()) return false;
        Modal = JournalVisible ? ClientModal.None : ClientModal.QuestJournal;
        return JournalVisible;
    }

    public bool ToggleJournalDetail()
    {
        if (!IsGameplay()) return false;
        Modal = Modal switch
        {
            ClientModal.QuestJournal => ClientModal.QuestDetail,
            ClientModal.QuestDetail => ClientModal.QuestJournal,
            _ => Modal,
        };
        return Modal == ClientModal.QuestDetail;
    }

    public void BackFromJournal()
    {
        if (!IsGameplay()) return;
        Modal = Modal switch
        {
            ClientModal.QuestDetail => ClientModal.QuestJournal,
            ClientModal.QuestJournal => ClientModal.None,
            _ => Modal,
        };
    }

    public bool ToggleHelp()
    {
        if (!IsGameplay()) return false;
        Modal = Modal == ClientModal.Help ? ClientModal.None : ClientModal.Help;
        return Modal == ClientModal.Help;
    }

    public bool ToggleShiftReport()
    {
        if (!IsGameplay()) return false;
        Modal = Modal == ClientModal.ShiftReport ? ClientModal.None : ClientModal.ShiftReport;
        return Modal == ClientModal.ShiftReport;
    }

    public bool ToggleSettings()
    {
        if (Screen == ClientScreen.Briefing) return false;
        Modal = Modal == ClientModal.Settings ? ClientModal.None : ClientModal.Settings;
        return Modal == ClientModal.Settings;
    }

    public bool ToggleProcessEditor()
    {
        if (!IsGameplay()) return false;
        Modal = Modal == ClientModal.ProcessEditor ? ClientModal.None : ClientModal.ProcessEditor;
        return Modal == ClientModal.ProcessEditor;
    }

    public bool ToggleAutomationEditor()
    {
        if (!IsGameplay()) return false;
        Modal = Modal == ClientModal.AutomationEditor ? ClientModal.None : ClientModal.AutomationEditor;
        return Modal == ClientModal.AutomationEditor;
    }

    public bool ToggleTwoStationRouting()
    {
        if (!IsGameplay()) return false;
        Modal = Modal == ClientModal.TwoStationRouting ? ClientModal.None : ClientModal.TwoStationRouting;
        return Modal == ClientModal.TwoStationRouting;
    }

    public bool TogglePatternCodex()
    {
        if (!IsGameplay()) return false;
        Modal = Modal == ClientModal.PatternCodex ? ClientModal.None : ClientModal.PatternCodex;
        return Modal == ClientModal.PatternCodex;
    }

    private bool IsGameplay() => Screen == ClientScreen.Gameplay;
}
