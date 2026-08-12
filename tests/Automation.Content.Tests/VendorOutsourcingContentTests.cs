using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class VendorOutsourcingContentTests
{
    [Fact]
    public void ProductionAuthorsSamThreeContractsAndOneExplicitBoundaryMismatch()
    {
        var configuration = DishStationVendorContent.Configuration;
        var sam = DishStationFirstHoursContent.Catalog.Characters
            .Single(character => character.Id.Value == "character.recurring.sam-rivera");

        Assert.Equal("Sam Rivera", sam.DisplayName);
        Assert.Equal("exception", configuration.LocalRareTrayCode);
        Assert.Equal("special", configuration.VendorRareTrayCode);
        Assert.Equal(3, configuration.Proposals.Length);
        Assert.All(configuration.Proposals, proposal => Assert.True(proposal.SetupCost >= 0));
        var managed = configuration.Proposals.Single(proposal => proposal.Id == VendorProposalId.ManagedVendor);
        var observable = configuration.Proposals.Single(proposal => proposal.Id == VendorProposalId.ObservableVendor);
        Assert.False(managed.TraceAvailable);
        Assert.False(managed.ManualFallbackAvailable);
        Assert.True(observable.TraceAvailable);
        Assert.True(observable.ManualFallbackAvailable);
        Assert.True(managed.SupportResponseTicks > observable.SupportResponseTicks);
        Assert.Contains("different cost, availability, and understanding",
            DishStationVendorContent.Quest.Narrative!.UnlockRationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidVendorBoundaryTermsFailAtTheirAuthoredPaths()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift);
        var duplicate = yaml.Replace("- id: observable-vendor", "- id: managed-vendor", StringComparison.Ordinal);
        var error = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(duplicate));
        Assert.Contains(error.Diagnostics, diagnostic => diagnostic.Path.Contains("vendor_outsourcing.proposals", StringComparison.Ordinal));

        var invalidFallback = yaml.Replace("fallback_labor_cost_per_request: 0\r\n        - id: observable-vendor",
            "fallback_labor_cost_per_request: 10\r\n        - id: observable-vendor", StringComparison.Ordinal).Replace(
            "fallback_labor_cost_per_request: 0\n        - id: observable-vendor",
            "fallback_labor_cost_per_request: 10\n        - id: observable-vendor", StringComparison.Ordinal);
        Assert.NotEqual(yaml, invalidFallback);
        error = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(invalidFallback));
        Assert.Contains(error.Diagnostics, diagnostic => diagnostic.Path.Contains("vendor_outsourcing", StringComparison.Ordinal));
    }
}
