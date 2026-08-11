namespace Automation.Domain;

public readonly record struct WasherAutomationPolicy(
    bool Enabled,
    bool RequirePhysicalReady)
{
    public static WasherAutomationPolicy Off => new(false, false);
    public static WasherAutomationPolicy ReportedReadyOnly => new(true, false);
    public static WasherAutomationPolicy CorroboratedReady => new(true, true);
}
