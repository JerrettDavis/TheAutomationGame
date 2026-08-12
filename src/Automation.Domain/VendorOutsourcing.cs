using System.Collections.Immutable;

namespace Automation.Domain;

public enum VendorProposalId
{
    BuildInHouse,
    ManagedVendor,
    ObservableVendor,
}

public enum VendorSourcingMode
{
    InternalBuild,
    VendorPackage,
}

public enum VendorIntegrationBoundary
{
    PlayerOwned,
    VendorManaged,
    PlayerOwnedAdapter,
}

public enum VendorKnowledgeOwner
{
    RestaurantTeam,
    VendorOnly,
    Shared,
}

public sealed record VendorProposalConfiguration(
    VendorProposalId Id,
    string DisplayName,
    VendorSourcingMode Sourcing,
    VendorIntegrationBoundary Boundary,
    VendorKnowledgeOwner KnowledgeOwner,
    int SupportResponseTicks,
    int SetupCost,
    int RecurringCost,
    int MaintenanceCost,
    bool TraceAvailable,
    bool ManualFallbackAvailable,
    int FallbackLaborCostPerRequest)
{
    public VendorProposalConfiguration Validate()
    {
        if (!Enum.IsDefined(Id)) throw new ArgumentOutOfRangeException(nameof(Id));
        if (!Enum.IsDefined(Sourcing)) throw new ArgumentOutOfRangeException(nameof(Sourcing));
        if (!Enum.IsDefined(Boundary)) throw new ArgumentOutOfRangeException(nameof(Boundary));
        if (!Enum.IsDefined(KnowledgeOwner)) throw new ArgumentOutOfRangeException(nameof(KnowledgeOwner));
        if (string.IsNullOrWhiteSpace(DisplayName)) throw new ArgumentException("Vendor proposal display name is required.", nameof(DisplayName));
        if (SupportResponseTicks is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(SupportResponseTicks));
        if (SetupCost < 0 || RecurringCost < 0 || MaintenanceCost < 0 || FallbackLaborCostPerRequest < 0)
            throw new ArgumentOutOfRangeException(nameof(SetupCost), "Vendor proposal costs cannot be negative.");
        if (!ManualFallbackAvailable && FallbackLaborCostPerRequest != 0)
            throw new ArgumentException("Fallback labor cost requires an available manual fallback.", nameof(FallbackLaborCostPerRequest));
        if (Sourcing == VendorSourcingMode.InternalBuild && (RecurringCost != 0 || KnowledgeOwner != VendorKnowledgeOwner.RestaurantTeam || Boundary != VendorIntegrationBoundary.PlayerOwned))
            throw new ArgumentException("The in-house proposal must retain its boundary and knowledge without a vendor fee.");
        if (Sourcing == VendorSourcingMode.VendorPackage && RecurringCost == 0)
            throw new ArgumentException("A vendor package requires a recurring contract cost.", nameof(RecurringCost));
        if (Boundary == VendorIntegrationBoundary.VendorManaged && (TraceAvailable || ManualFallbackAvailable || KnowledgeOwner != VendorKnowledgeOwner.VendorOnly))
            throw new ArgumentException("The managed boundary keeps diagnosis and fallback inside the vendor contract.");
        if (Boundary == VendorIntegrationBoundary.PlayerOwnedAdapter && (!TraceAvailable || !ManualFallbackAvailable || KnowledgeOwner != VendorKnowledgeOwner.Shared))
            throw new ArgumentException("The observable adapter requires a shared trace, shared understanding, and manual fallback.");
        return this with { DisplayName = DisplayName.Trim() };
    }
}

public sealed record VendorOutsourcingConfiguration(
    ImmutableArray<VendorProposalConfiguration> Proposals,
    int TrialHorizonTicks,
    int IncidentAtTick,
    int ServiceValuePerRequest,
    int ShortageCostPerRequest,
    string LocalRareTrayCode,
    string VendorRareTrayCode)
{
    public VendorOutsourcingConfiguration Validate()
    {
        if (Proposals.IsDefaultOrEmpty || Proposals.Length != 3)
            throw new ArgumentException("The restaurant vendor episode requires three concrete proposals.", nameof(Proposals));
        var proposals = Proposals.Select(proposal => proposal.Validate()).ToImmutableArray();
        if (proposals.Select(proposal => proposal.Id).Distinct().Count() != proposals.Length ||
            Enum.GetValues<VendorProposalId>().Except(proposals.Select(proposal => proposal.Id)).Any())
            throw new ArgumentException("The in-house, managed-vendor, and observable-vendor proposals are all required exactly once.", nameof(Proposals));
        if (TrialHorizonTicks is < 2 or > 10_000) throw new ArgumentOutOfRangeException(nameof(TrialHorizonTicks));
        if (IncidentAtTick < 1 || IncidentAtTick >= TrialHorizonTicks) throw new ArgumentOutOfRangeException(nameof(IncidentAtTick));
        if (ServiceValuePerRequest <= 0) throw new ArgumentOutOfRangeException(nameof(ServiceValuePerRequest));
        if (ShortageCostPerRequest < 0) throw new ArgumentOutOfRangeException(nameof(ShortageCostPerRequest));
        RequireToken(LocalRareTrayCode, nameof(LocalRareTrayCode));
        RequireToken(VendorRareTrayCode, nameof(VendorRareTrayCode));
        if (string.Equals(LocalRareTrayCode, VendorRareTrayCode, StringComparison.Ordinal))
            throw new ArgumentException("The episode requires an explicit local/vendor rare-tray mapping mismatch.");
        return this with { Proposals = proposals };
    }

    private static void RequireToken(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(character => !(char.IsLower(character) || character is '-')))
            throw new ArgumentException("Boundary codes must use lowercase token syntax.", name);
    }
}
