using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class VendorComparisonPresenterTests
{
    [Fact]
    public void ComparisonShowsViableChoicesWithDistinctCostAvailabilityAndOwnership()
    {
        var world = new VendorOutsourcingWorld(DishStationVendorContent.Configuration);
        Run(world, VendorProposalId.ManagedVendor);
        Run(world, VendorProposalId.ObservableVendor);

        var view = VendorComparisonPresenter.Present(world.Configuration, world.Snapshot(),
            DishStationVendorContent.Quest);
        var managed = view.Proposals.Single(item => item.Id == VendorProposalId.ManagedVendor);
        var observable = view.Proposals.Single(item => item.Id == VendorProposalId.ObservableVendor);

        Assert.True(managed.Viable);
        Assert.True(observable.Viable);
        Assert.Contains("M4", managed.IncidentOutcome!, StringComparison.Ordinal);
        Assert.Contains("M0", observable.IncidentOutcome!, StringComparison.Ordinal);
        Assert.Contains("VENDOR ONLY", managed.Ownership, StringComparison.Ordinal);
        Assert.Contains("SHARED", observable.Ownership, StringComparison.Ordinal);
        Assert.NotEqual(managed.Risk, observable.Risk);
        Assert.Contains("COMPARISON COMPLETE", view.Outcome, StringComparison.Ordinal);
        Assert.DoesNotContain("correct choice", AllText(view), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectedProposalTraceUsesOnlyAuthoritativeIncidentEvidence()
    {
        var world = new VendorOutsourcingWorld(DishStationVendorContent.Configuration);
        Run(world, VendorProposalId.ObservableVendor);

        var view = VendorComparisonPresenter.Present(world.Configuration, world.Snapshot(),
            DishStationVendorContent.Quest);

        Assert.Equal(5, view.SelectedTrace.Count);
        Assert.Contains(view.SelectedTrace, line => line.Contains("MANUAL FALLBACK", StringComparison.Ordinal));
        Assert.Contains(view.SelectedTrace, line => line.Contains("EXCEPTION", StringComparison.Ordinal));
        Assert.Contains(view.SelectedTrace, line => line.Contains("SPECIAL", StringComparison.Ordinal));
    }

    private static void Run(VendorOutsourcingWorld world, VendorProposalId proposal)
    {
        world.ExecuteNow(new SelectVendorProposalCommand(world.Tick, proposal));
        world.ExecuteNow(new RunVendorProposalTrialCommand(world.Tick));
    }

    private static string AllText(VendorComparisonView view) => string.Join(' ', view.Title, view.Speaker,
        view.Pitch, view.SharedIncident, view.Outcome, string.Join(' ', view.Proposals.Select(card =>
            $"{card.Title} {card.Source} {card.Boundary} {card.Contract} {card.Ownership} {card.NormalEconomy} {card.Risk} {card.IncidentOutcome}")),
        string.Join(' ', view.SelectedTrace));
}
