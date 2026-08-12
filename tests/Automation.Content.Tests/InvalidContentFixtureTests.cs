using System.Text.Json;
using Automation.Content;

namespace Automation.Content.Tests;

public sealed class InvalidContentFixtureTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void SeededBadFixtureFailsWithTargetedDiagnostic(InvalidContentCase testCase)
    {
        var yaml = File.ReadAllText(testCase.Base switch
        {
            "minimal" => ContentTestPaths.MinimalFixture,
            "first_shift" => ContentTestPaths.FirstShift,
            _ => throw new InvalidDataException($"Unknown invalid-case base '{testCase.Base}'."),
        }).ReplaceLineEndings("\n");

        foreach (var mutation in testCase.Mutations)
        {
            Assert.Contains(mutation.Find, yaml, StringComparison.Ordinal);
            yaml = yaml.Replace(mutation.Find, mutation.Replace, StringComparison.Ordinal);
        }

        var exception = Assert.Throws<ContentCompilationException>(() =>
        {
            if (testCase.Mode == "first_shift") DishStationFirstHoursContent.Compile(yaml, $"{testCase.Name}.yaml");
            else ContentCompilerV1.Compile(yaml, $"{testCase.Name}.yaml");
        });

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Source == $"{testCase.Name}.yaml" &&
            diagnostic.Path == testCase.ExpectedPath &&
            diagnostic.Message.Contains(testCase.ExpectedMessage, StringComparison.Ordinal));
    }

    public static TheoryData<InvalidContentCase> Cases
    {
        get
        {
            var json = File.ReadAllText(ContentTestPaths.InvalidCases);
            var cases = JsonSerializer.Deserialize<List<InvalidContentCase>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException("Invalid content case catalog was empty.");
            var data = new TheoryData<InvalidContentCase>();
            foreach (var testCase in cases) data.Add(testCase);
            return data;
        }
    }
}

public sealed record InvalidContentCase(
    string Name,
    string Base,
    string Mode,
    IReadOnlyList<ContentMutation> Mutations,
    string ExpectedPath,
    string ExpectedMessage)
{
    public override string ToString() => Name;
}

public sealed record ContentMutation(string Find, string Replace);
