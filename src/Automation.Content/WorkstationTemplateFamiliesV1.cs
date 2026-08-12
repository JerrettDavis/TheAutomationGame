using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Content;

public static class WorkstationTemplateFamiliesV1
{
    public static ImmutableArray<WorkstationTemplateFamily> Supported { get; } =
    [
        WorkstationTemplateFamily.Manual,
        WorkstationTemplateFamily.Batch,
        WorkstationTemplateFamily.Buffer,
        WorkstationTemplateFamily.Inspection,
        WorkstationTemplateFamily.Service,
    ];

    public static bool IsSupported(WorkstationTemplateFamily family) => Supported.Contains(family);

    public static string UnsupportedReason(WorkstationTemplateFamily family) => family switch
    {
        WorkstationTemplateFamily.Transport =>
            "Transport workstations require a queued work-item movement primitive; current dish-station walking measures handling travel but does not transport work between workstation queues.",
        _ when IsSupported(family) => throw new InvalidOperationException($"{family} is supported."),
        _ => throw new ArgumentOutOfRangeException(nameof(family)),
    };
}

public static class DishStationWorkstationTemplateAdapter
{
    public static DishStationScenarioConfiguration Apply(
        DishStationScenarioConfiguration baseline,
        IEnumerable<WorkstationContentDefinition> workstations)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(workstations);
        BatchWorkstationBehaviorContentDefinition? batch = null;
        BufferWorkstationBehaviorContentDefinition? buffer = null;
        ServiceWorkstationBehaviorContentDefinition? service = null;

        foreach (var workstation in workstations)
        {
            switch (workstation.Behavior)
            {
                case BatchWorkstationBehaviorContentDefinition value:
                    if (batch is not null) throw new InvalidDataException("Dish-station template composition supports one batch washer.");
                    batch = value;
                    break;
                case BufferWorkstationBehaviorContentDefinition value:
                    if (buffer is not null) throw new InvalidDataException("Dish-station template composition supports one rack buffer.");
                    buffer = value;
                    break;
                case ServiceWorkstationBehaviorContentDefinition value:
                    if (service is not null) throw new InvalidDataException("Dish-station template composition supports one service demand surface.");
                    service = value;
                    break;
            }
        }

        return (baseline with
        {
            WasherCycleTicks = batch?.CycleTicks ?? baseline.WasherCycleTicks,
            RackCapacity = buffer?.Capacity ?? baseline.RackCapacity,
            DemandKind = service?.DemandKind ?? baseline.DemandKind,
            DemandIntervalTicks = service?.RequestIntervalTicks ?? baseline.DemandIntervalTicks,
        }).Validate();
    }
}
