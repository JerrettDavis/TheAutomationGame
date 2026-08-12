using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class VendorOutsourcingConfigurationTests
{
    [Fact]
    public void ConcreteProposalsRetainDistinctOwnershipContractAndFallbackTerms()
    {
        var configuration = Configuration().Validate();

        Assert.Equal(3, configuration.Proposals.Length);
        var managed = configuration.Proposals.Single(item => item.Id == VendorProposalId.ManagedVendor);
        var observable = configuration.Proposals.Single(item => item.Id == VendorProposalId.ObservableVendor);
        Assert.Equal(VendorKnowledgeOwner.VendorOnly, managed.KnowledgeOwner);
        Assert.False(managed.TraceAvailable);
        Assert.False(managed.ManualFallbackAvailable);
        Assert.Equal(VendorKnowledgeOwner.Shared, observable.KnowledgeOwner);
        Assert.True(observable.TraceAvailable);
        Assert.True(observable.ManualFallbackAvailable);
        Assert.True(managed.SupportResponseTicks > observable.SupportResponseTicks);
    }

    [Fact]
    public void InvalidBoundaryBundlesAndMissingMismatchFailFast()
    {
        var source = Configuration();
        var invalidManaged = source.Proposals.Single(item => item.Id == VendorProposalId.ManagedVendor) with
        {
            TraceAvailable = true,
        };
        Assert.Throws<ArgumentException>(() => (source with
        {
            Proposals = source.Proposals.Select(item => item.Id == invalidManaged.Id ? invalidManaged : item).ToImmutableArray(),
        }).Validate());
        Assert.Throws<ArgumentException>(() => (source with { VendorRareTrayCode = source.LocalRareTrayCode }).Validate());
    }

    internal static VendorOutsourcingConfiguration Configuration() => new(
        ImmutableArray.Create(
            new VendorProposalConfiguration(VendorProposalId.BuildInHouse, "Build in house",
                VendorSourcingMode.InternalBuild, VendorIntegrationBoundary.PlayerOwned, VendorKnowledgeOwner.RestaurantTeam,
                1, 220, 0, 60, true, false, 0),
            new VendorProposalConfiguration(VendorProposalId.ManagedVendor, "Sam's managed package",
                VendorSourcingMode.VendorPackage, VendorIntegrationBoundary.VendorManaged, VendorKnowledgeOwner.VendorOnly,
                4, 60, 40, 0, false, false, 0),
            new VendorProposalConfiguration(VendorProposalId.ObservableVendor, "Observable vendor package",
                VendorSourcingMode.VendorPackage, VendorIntegrationBoundary.PlayerOwnedAdapter, VendorKnowledgeOwner.Shared,
                2, 140, 70, 0, true, true, 20)),
        8, 3, 80, 30, "exception", "special");
}
