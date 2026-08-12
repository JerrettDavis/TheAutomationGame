namespace Automation.Content.Tests;

internal static class ContentTestPaths
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();
    public static string MinimalFixture => Path.Combine(RepositoryRoot, "content", "fixtures", "schema-v1", "minimal-restaurant.yaml");
    public static string FirstShift => Path.Combine(RepositoryRoot, "content", "restaurant", "first-shift.yaml");
    public static string InvalidCases => Path.Combine(RepositoryRoot, "content", "fixtures", "schema-v1", "invalid", "cases.json");
    public static string SeededScenarioTemplate => Path.Combine(RepositoryRoot, "content", "templates", "proofs", "seeded-scenario.template.yaml");
    public static string WorkstationTemplate(string family) => Path.Combine(RepositoryRoot, "content", "templates", "workstations", $"{family}.template.yaml");
    public static string IncidentTemplate(string family) => Path.Combine(RepositoryRoot, "content", "templates", "incidents", $"{family}.template.yaml");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheAutomationGame.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
