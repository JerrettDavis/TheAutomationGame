namespace Automation.Client.Stride;

public static class ClientSimulationPolicy
{
    public static bool ShouldAdvance(bool paused, ClientScreen screen, ClientModal modal) =>
        !paused && screen == ClientScreen.Gameplay && modal is not (ClientModal.ProcessEditor or ClientModal.AutomationEditor or ClientModal.TwoStationRouting or ClientModal.PatternCodex);
}
