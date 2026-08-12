using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Automation.Domain;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Automation.Content;

public static partial class ContentCompilerV1
{
    public const int SchemaVersion = 1;
    private static readonly HashSet<string> SupportedQuestMetrics = new(StringComparer.Ordinal)
    {
        "service.available.count",
        "service.shortage.count",
        "process.completed.count",
    };
    private static readonly HashSet<string> SupportedQuestOperators = new(StringComparer.Ordinal)
    {
        "equal",
        "greater_than_or_equal",
        "less_than",
    };
    private static readonly HashSet<string> DishKindTokens = new(StringComparer.Ordinal) { "plate", "glass", "tray" };
    private static readonly HashSet<string> KnowledgeTokens = new(StringComparer.Ordinal) { "none", "happy-path", "rush-aware", "fully-documented" };
    private static readonly HashSet<string> AutomationTokens = new(StringComparer.Ordinal) { "off", "reported-ready-only", "corroborated-ready" };
    private static readonly HashSet<string> LayoutTokens = new(StringComparer.Ordinal) { "linear", "u-shaped-cell" };
    private static readonly HashSet<string> RoutingStationTokens = new(StringComparer.Ordinal) { "main-dish-room", "patio-service-station" };
    private static readonly HashSet<string> RoutingPolicyTokens = new(StringComparer.Ordinal) { "captured-order", "plates-first", "glasses-first" };
    private static readonly HashSet<string> RoutingDemandTokens = new(StringComparer.Ordinal) { "plate", "glass" };
    private static readonly HashSet<string> ManualActionTokens = new(StringComparer.Ordinal) { "scrape", "rack", "unload", "dry-and-restock" };
    private static readonly HashSet<string> BufferOrderingTokens = new(StringComparer.Ordinal) { "fifo" };
    private static readonly HashSet<string> InspectionObservationTokens = new(StringComparer.Ordinal) { "state-counts" };
    private static readonly HashSet<string> IncidentScopeTokens = new(StringComparer.Ordinal) { "dish-station" };
    private static readonly HashSet<string> IncidentSensorTokens = new(StringComparer.Ordinal) { "reported-ready-stuck-true" };
    private static readonly HashSet<string> IncidentResourceTokens = new(StringComparer.Ordinal) { "washer" };
    private static readonly HashSet<string> IncidentWorkerTokens = new(StringComparer.Ordinal) { "new-hire" };
    private static readonly HashSet<string> DialogueTriggerTokens = new(StringComparer.Ordinal) { "queue-pressure", "automation-incident", "shift-succeeded" };
    private static readonly HashSet<string> DialoguePriorityTokens = new(StringComparer.Ordinal) { "ambient", "important", "critical" };

    public static CompiledContentCatalogV1 CompileFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Compile(File.ReadAllText(path), Path.GetFullPath(path));
    }

    public static CompiledContentCatalogV1 Compile(string yaml, string source = "<memory>")
    {
        ArgumentNullException.ThrowIfNull(yaml);
        var diagnostics = new List<ContentDiagnostic>();
        RawContentBundle raw;
        try
        {
            raw = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithDuplicateKeyChecking()
                .Build()
                .Deserialize<RawContentBundle>(yaml) ?? new RawContentBundle();
        }
        catch (YamlException exception)
        {
            throw new ContentCompilationException([
                new(source, "$", exception.InnerException?.Message ?? exception.Message,
                    checked((int)exception.Start.Line + 1), checked((int)exception.Start.Column + 1)),
            ]);
        }

        if (raw.SchemaVersion != SchemaVersion)
            diagnostics.Add(new(source, "schema_version",
                $"Unsupported schema version {raw.SchemaVersion?.ToString(CultureInfo.InvariantCulture) ?? "<missing>"}; expected {SchemaVersion}."));

        ValidateRaw(raw, source, diagnostics);
        ThrowIfAny(diagnostics);

        var industries = raw.Industries.Select(item => new IndustryContentDefinition(Id(item.Id), item.DisplayName!)).OrderBy(item => item.Id.Value).ToImmutableArray();
        var facilities = raw.Facilities.Select(item => new FacilityContentDefinition(Id(item.Id), Id(item.Industry), item.DisplayName!,
            Ids(item.Workstations))).OrderBy(item => item.Id.Value).ToImmutableArray();
        var items = raw.Items.Select(item => new ItemContentDefinition(Id(item.Id), Id(item.Industry), item.DisplayName!,
            item.States!.ToImmutableArray())).OrderBy(item => item.Id.Value).ToImmutableArray();
        var workstations = raw.Workstations.Select(item => new WorkstationContentDefinition(Id(item.Id), Id(item.Industry), item.DisplayName!,
            Ids(item.AcceptedItems), item.InputState!, item.OutputState!, Id(item.Presentation), Id(item.PresentationFallback),
            CompileWorkstationBehavior(item)))
            .OrderBy(item => item.Id.Value).ToImmutableArray();
        var processes = raw.Processes.Select(item => new ProcessContentDefinition(Id(item.Id), Id(item.Industry),
            item.Steps!.Select(step => new ProcessStepContentDefinition(step.Id!, Id(step.Workstation))).ToImmutableArray(),
            (item.Routes ?? []).Select(route => new ProcessRouteContentDefinition(route.From!, route.To!)).ToImmutableArray(),
            item.AllowCycles)).OrderBy(item => item.Id.Value).ToImmutableArray();
        var scenarios = raw.Scenarios.Select(item =>
        {
            var dishStation = item.DishStation is null ? null : CompileDishStation(item.DishStation);
            return new ScenarioContentDefinition(Id(item.Id), Id(item.Industry), Id(item.Facility),
                Ids(item.Processes), Ids(item.Items), Ids(item.Characters), item.Seed!,
                item.Narrative is null ? null : new ScenarioNarrativeContentDefinition(
                    item.Narrative.ChapterTitle!,
                    item.Narrative.Briefing.Select(page => new ScenarioBriefingPageContentDefinition(page.Title!, page.Body!)).ToImmutableArray(),
                    item.Narrative.DebriefSummary!, item.Narrative.DebriefQuestions.ToImmutableArray()),
                dishStation,
                item.TwoStationRouting is null ? null : CompileTwoStationRouting(item.TwoStationRouting, dishStation!));
        }).OrderBy(item => item.Id.Value).ToImmutableArray();
        var quests = raw.Quests.Select(item => new QuestContentDefinition(Id(item.Id), Id(item.Scenario), Ids(item.Participants), item.Objective!,
            new(item.Completion!.Metric!, item.Completion.Operator!, item.Completion.Value!.Value),
            item.Narrative is null ? null : new QuestNarrativeContentDefinition(
                item.Narrative.RuntimeId!, item.Narrative.Sequence!.Value, item.Narrative.Title!, item.Narrative.Situation!,
                item.Narrative.Discovery!, item.Narrative.UnlockRationale!, item.Narrative.Reward!.Experience!.Value,
                Id(item.Narrative.Reward.Capability),
                item.Narrative.Steps!.Select(step => new QuestNarrativeStepContentDefinition(step.Id!, step.Text!, step.InputAction)).ToImmutableArray())))
            .OrderBy(item => item.Id.Value).ToImmutableArray();
        var characters = raw.Characters.Select(item => new CharacterContentDefinition(Id(item.Id), Id(item.Industry), item.DisplayName!,
            Id(item.Role), item.Motivation!, Ids(item.KnownFacts), Ids(item.BlindSpots), Ids(item.Authority),
            (item.Relationships ?? []).Select(relationship => new CharacterRelationshipContentDefinition(Id(relationship.Character), Id(relationship.Kind)))
                .OrderBy(relationship => relationship.Character.Value).ToImmutableArray(),
            (item.Barks ?? []).Select(bark => new CharacterBarkContentDefinition(Id(bark.Id), Id(bark.Quest),
                    DialogueTrigger(bark.Trigger!), DialoguePriority(bark.Priority!), bark.CooldownTicks!.Value, bark.Line!))
                .OrderBy(bark => bark.Id.Value).ToImmutableArray(),
            Id(item.Presentation), Id(item.PresentationFallback))).OrderBy(item => item.Id.Value).ToImmutableArray();
        var incidents = raw.Incidents.Select(CompileIncident).OrderBy(item => item.Id.Value).ToImmutableArray();

        ValidateGraph(industries, facilities, items, workstations, processes, scenarios, quests, characters, incidents, source, diagnostics);
        ThrowIfAny(diagnostics);

        var counts = ImmutableDictionary<ContentDefinitionKind, int>.Empty
            .Add(ContentDefinitionKind.Industry, industries.Length)
            .Add(ContentDefinitionKind.Facility, facilities.Length)
            .Add(ContentDefinitionKind.Item, items.Length)
            .Add(ContentDefinitionKind.Workstation, workstations.Length)
            .Add(ContentDefinitionKind.Process, processes.Length)
            .Add(ContentDefinitionKind.Scenario, scenarios.Length)
            .Add(ContentDefinitionKind.Quest, quests.Length)
            .Add(ContentDefinitionKind.Character, characters.Length)
            .Add(ContentDefinitionKind.Incident, incidents.Length);
        var definitionCount = counts.Values.Sum();
        var hash = Hash(industries, facilities, items, workstations, processes, scenarios, quests, characters, incidents);
        return new(industries, facilities, items, workstations, processes, scenarios, quests, characters, incidents,
            new(SchemaVersion, definitionCount, counts, hash));
    }

    private static void ValidateRaw(RawContentBundle raw, string source, List<ContentDiagnostic> diagnostics)
    {
        string activePath = "$";
        ValidateDefinitions(raw.Industries, "industries", "industry.", source, diagnostics, item =>
            Require(item.DisplayName, "display_name", "Display name is required."));
        ValidateDefinitions(raw.Facilities, "facilities", "facility.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            Require(item.DisplayName, "display_name", "Display name is required.");
            RequireIds(item.Workstations, "workstations");
        });
        ValidateDefinitions(raw.Items, "items", "item.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            Require(item.DisplayName, "display_name", "Display name is required.");
            if (item.States is null || item.States.Count == 0) Add("states", "At least one item state is required.");
            else
            {
                for (var index = 0; index < item.States.Count; index++)
                    if (!StatePattern().IsMatch(item.States[index] ?? "")) Add($"states[{index}]", "State must use lowercase semantic-token syntax.");
                if (item.States.Distinct(StringComparer.Ordinal).Count() != item.States.Count) Add("states", "Item states must be unique.");
            }
        });
        ValidateDefinitions(raw.Workstations, "workstations", "workstation.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            Require(item.DisplayName, "display_name", "Display name is required.");
            RequireIds(item.AcceptedItems, "accepted_items");
            RequireState(item.InputState, "input_state");
            RequireState(item.OutputState, "output_state");
            RequireId(item.Presentation, "presentation", "presentation.");
            RequireId(item.PresentationFallback, "presentation_fallback", "presentation.fallback.");
            var behaviorCount = new object?[] { item.Manual, item.Batch, item.Buffer, item.Inspection, item.Service }.Count(value => value is not null);
            if (behaviorCount > 1) Add("behavior", "Workstation must declare at most one behavior family.");
            if (item.Manual is { } manual)
                RequireToken(manual.Action, "manual.action", ManualActionTokens);
            if (item.Batch is { } batch)
            {
                RequirePositive(batch.Capacity, "batch.capacity");
                if (batch.Capacity is not null and not 1)
                    Add("batch.capacity", "Current authoritative dish-station batches have capacity 1.");
                RequirePositive(batch.CycleTicks, "batch.cycle_ticks");
            }
            if (item.Buffer is { } buffer)
            {
                RequirePositive(buffer.Capacity, "buffer.capacity");
                RequireToken(buffer.Ordering, "buffer.ordering", BufferOrderingTokens);
            }
            if (item.Inspection is { } inspection)
                RequireToken(inspection.Observation, "inspection.observation", InspectionObservationTokens);
            if (item.Service is { } service)
            {
                RequireToken(service.DemandKind, "service.demand_kind", DishKindTokens);
                RequirePositive(service.RequestIntervalTicks, "service.request_interval_ticks");
            }
        });
        ValidateDefinitions(raw.Processes, "processes", "process.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            if (item.Steps is null || item.Steps.Count == 0) Add("steps", "At least one process step is required.");
            else for (var index = 0; index < item.Steps.Count; index++)
            {
                if (!StepIdPattern().IsMatch(item.Steps[index].Id ?? "")) Add($"steps[{index}].id", "Step ID must use lowercase token syntax.");
                RequireId(item.Steps[index].Workstation, $"steps[{index}].workstation", "workstation.");
            }
            for (var index = 0; index < (item.Routes?.Count ?? 0); index++)
            {
                Require(item.Routes![index].From, $"routes[{index}].from", "Route source is required.");
                Require(item.Routes[index].To, $"routes[{index}].to", "Route destination is required.");
            }
        });
        ValidateDefinitions(raw.Scenarios, "scenarios", "scenario.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            RequireId(item.Facility, "facility", "facility.");
            RequireIds(item.Processes, "processes");
            RequireIds(item.Items, "items");
            RequireIds(item.Characters, "characters");
            Require(item.Seed, "seed", "Named deterministic seed is required.");
            if (item.Narrative is { } narrative)
            {
                Require(narrative.ChapterTitle, "narrative.chapter_title", "Chapter title is required.");
                if (narrative.Briefing is null || narrative.Briefing.Count == 0) Add("narrative.briefing", "At least one briefing page is required.");
                else for (var index = 0; index < narrative.Briefing.Count; index++)
                {
                    Require(narrative.Briefing[index].Title, $"narrative.briefing[{index}].title", "Briefing title is required.");
                    Require(narrative.Briefing[index].Body, $"narrative.briefing[{index}].body", "Briefing body is required.");
                }
                Require(narrative.DebriefSummary, "narrative.debrief_summary", "Debrief summary is required.");
                if (narrative.DebriefQuestions is null || narrative.DebriefQuestions.Count == 0)
                    Add("narrative.debrief_questions", "At least one debrief question is required.");
                else for (var index = 0; index < narrative.DebriefQuestions.Count; index++)
                    Require(narrative.DebriefQuestions[index], $"narrative.debrief_questions[{index}]", "Debrief question is required.");
            }
            if (item.DishStation is { } scenario)
            {
                RequireCounts(scenario.InitialDirty, "dish_station.initial_dirty");
                RequireCounts(scenario.InitialAvailable, "dish_station.initial_available");
                RequirePositive(scenario.ArrivalIntervalTicks, "dish_station.arrival_interval_ticks");
                RequirePositive(scenario.GlassEveryArrivals, "dish_station.glass_every_arrivals");
                RequirePositive(scenario.RackCapacity, "dish_station.rack_capacity");
                RequirePositive(scenario.WasherCycleTicks, "dish_station.washer_cycle_ticks");
                RequirePositive(scenario.WorkerActionIntervalTicks, "dish_station.worker_action_interval_ticks");
                RequirePositive(scenario.FlowCellWorkerActionIntervalTicks, "dish_station.flow_cell_worker_action_interval_ticks");
                RequireNonNegative(scenario.StickyReadyFaultAfterAutomatedStarts, "dish_station.sticky_ready_fault_after_automated_starts");
                if (scenario.StickyReadyFaultPermillePerStart is null or < 0 or > 1000)
                    Add("dish_station.sticky_ready_fault_permille_per_start", "Sticky-ready fault permille must be between 0 and 1000.");
                RequireToken(scenario.DemandKind, "dish_station.demand_kind", DishKindTokens);
                RequirePositive(scenario.DemandIntervalTicks, "dish_station.demand_interval_ticks");
                if (scenario.InitialRushEnabled is null) Add("dish_station.initial_rush_enabled", "Initial rush flag is required.");
                if (scenario.InitialNewHireEnabled is null) Add("dish_station.initial_new_hire_enabled", "Initial new-hire flag is required.");
                RequireToken(scenario.InitialNewHireKnowledge, "dish_station.initial_new_hire_knowledge", KnowledgeTokens);
                RequireToken(scenario.InitialAutomationPolicy, "dish_station.initial_automation_policy", AutomationTokens);
                RequireToken(scenario.InitialLayout, "dish_station.initial_layout", LayoutTokens);
                if (scenario.Economy is { } economy)
                {
                    RequirePositive(economy.CompletedDishValue, "dish_station.economy.completed_dish_value");
                    RequirePositive(economy.LaborTicksPerWorkAction, "dish_station.economy.labor_ticks_per_work_action");
                    RequireNonNegative(economy.LaborCostPerTick, "dish_station.economy.labor_cost_per_tick");
                    RequireNonNegative(economy.StaffingCostPerEnabledTick, "dish_station.economy.staffing_cost_per_enabled_tick");
                    RequireNonNegative(economy.TrayReworkCost, "dish_station.economy.tray_rework_cost");
                    RequireNonNegative(economy.ServiceShortageDowntimeCost, "dish_station.economy.service_shortage_downtime_cost");
                    RequireNonNegative(economy.AutomationIncidentDowntimeCost, "dish_station.economy.automation_incident_downtime_cost");
                    RequireNonNegative(economy.FlowCellInvestmentCost, "dish_station.economy.flow_cell_investment_cost");
                }
            }
            if (item.TwoStationRouting is { } routing)
            {
                if (item.DishStation is null) Add("two_station_routing", "Two-station routing requires a dish_station base configuration.");
                RequirePositive(routing.TrialHorizonTicks, "two_station_routing.trial_horizon_ticks");
                if (routing.Stations is null || routing.Stations.Count != 2)
                    Add("two_station_routing.stations", "Exactly two routing stations are required.");
                else for (var index = 0; index < routing.Stations.Count; index++)
                {
                    var station = routing.Stations[index];
                    RequireToken(station.Id, $"two_station_routing.stations[{index}].id", RoutingStationTokens);
                    Require(station.DisplayName, $"two_station_routing.stations[{index}].display_name", "Routing station display name is required.");
                    RequireCounts(station.InitialDirty, $"two_station_routing.stations[{index}].initial_dirty");
                    RequireToken(station.DemandKind, $"two_station_routing.stations[{index}].demand_kind", RoutingDemandTokens);
                    RequireToken(station.InitialPolicy, $"two_station_routing.stations[{index}].initial_policy", RoutingPolicyTokens);
                }
                if (routing.Stations?.Select(station => station.Id).Where(id => id is not null).Distinct(StringComparer.Ordinal).Count() != routing.Stations?.Count)
                    Add("two_station_routing.stations", "Routing station IDs must be unique.");
            }
        });
        ValidateDefinitions(raw.Quests, "quests", "quest.", source, diagnostics, item =>
        {
            RequireId(item.Scenario, "scenario", "scenario.");
            RequireIds(item.Participants, "participants");
            Require(item.Objective, "objective", "Objective is required.");
            if (item.Completion is null) Add("completion", "Completion condition is required.");
            else
            {
                Require(item.Completion.Metric, "completion.metric", "Completion metric is required.");
                Require(item.Completion.Operator, "completion.operator", "Completion operator is required.");
                if (item.Completion.Value is null) Add("completion.value", "Completion value is required.");
            }
            if (item.Narrative is not null)
            {
                if (!StepIdPattern().IsMatch(item.Narrative.RuntimeId ?? "")) Add("narrative.runtime_id", "Runtime ID must use lowercase token syntax.");
                if (item.Narrative.Sequence is null or <= 0) Add("narrative.sequence", "Sequence must be a positive integer.");
                Require(item.Narrative.Title, "narrative.title", "Narrative title is required.");
                Require(item.Narrative.Situation, "narrative.situation", "Narrative situation is required.");
                Require(item.Narrative.Discovery, "narrative.discovery", "Narrative discovery is required.");
                Require(item.Narrative.UnlockRationale, "narrative.unlock_rationale", "Narrative unlock rationale is required.");
                if (item.Narrative.Reward is null) Add("narrative.reward", "Narrative reward is required.");
                else
                {
                    if (item.Narrative.Reward.Experience is null or <= 0) Add("narrative.reward.experience", "Experience reward must be a positive integer.");
                    RequireId(item.Narrative.Reward.Capability, "narrative.reward.capability", "capability.");
                }
                if (item.Narrative.Steps is null || item.Narrative.Steps.Count == 0) Add("narrative.steps", "At least one narrative step is required.");
                else
                {
                    for (var index = 0; index < item.Narrative.Steps.Count; index++)
                    {
                        var step = item.Narrative.Steps[index];
                        if (!StepIdPattern().IsMatch(step.Id ?? "")) Add($"narrative.steps[{index}].id", "Narrative step ID must use lowercase token syntax.");
                        Require(step.Text, $"narrative.steps[{index}].text", "Narrative step text is required.");
                        if (step.InputAction is not null && !StepIdPattern().IsMatch(step.InputAction))
                            Add($"narrative.steps[{index}].input_action", "Input action must use lowercase token syntax.");
                        var bindingCount = CountOccurrences(step.Text ?? "", "{binding}");
                        if (step.InputAction is null && bindingCount > 0)
                            Add($"narrative.steps[{index}].text", "The {binding} placeholder requires input_action.");
                        if (step.InputAction is not null && bindingCount != 1)
                            Add($"narrative.steps[{index}].text", "A step with input_action must contain exactly one {binding} placeholder.");
                    }
                    if (item.Narrative.Steps.Select(step => step.Id).Distinct(StringComparer.Ordinal).Count() != item.Narrative.Steps.Count)
                        Add("narrative.steps", "Narrative step IDs must be unique within a quest.");
                }
            }
        });
        ValidateDefinitions(raw.Characters, "characters", "character.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            Require(item.DisplayName, "display_name", "Display name is required.");
            RequireId(item.Role, "role", "role.");
            Require(item.Motivation, "motivation", "Motivation is required.");
            RequireIds(item.KnownFacts, "known_facts");
            RequireIds(item.BlindSpots, "blind_spots");
            RequireIds(item.Authority, "authority");
            RequireId(item.Presentation, "presentation", "presentation.character.");
            RequireId(item.PresentationFallback, "presentation_fallback", "presentation.fallback.");
            if (item.Relationships is not null)
            {
                for (var index = 0; index < item.Relationships.Count; index++)
                {
                    RequireId(item.Relationships[index].Character, $"relationships[{index}].character", "character.");
                    RequireId(item.Relationships[index].Kind, $"relationships[{index}].kind", "relationship.");
                }
                if (item.Relationships.Select(relationship => relationship.Character).Distinct(StringComparer.Ordinal).Count() != item.Relationships.Count)
                    Add("relationships", "Relationship targets must be unique.");
            }
            if (item.Barks is not null)
            {
                for (var index = 0; index < item.Barks.Count; index++)
                {
                    var bark = item.Barks[index];
                    RequireId(bark.Id, $"barks[{index}].id", "dialogue.");
                    RequireId(bark.Quest, $"barks[{index}].quest", "quest.");
                    RequireToken(bark.Trigger, $"barks[{index}].trigger", DialogueTriggerTokens);
                    RequireToken(bark.Priority, $"barks[{index}].priority", DialoguePriorityTokens);
                    RequireNonNegative(bark.CooldownTicks, $"barks[{index}].cooldown_ticks");
                    Require(bark.Line, $"barks[{index}].line", "Dialogue line is required.");
                    if ((bark.Line?.Length ?? 0) > 160) Add($"barks[{index}].line", "Dialogue line must be 160 characters or fewer.");
                }
                if (item.Barks.Select(bark => bark.Id).Distinct(StringComparer.Ordinal).Count() != item.Barks.Count)
                    Add("barks", "Dialogue bark IDs must be unique within a character.");
            }
        });
        var allBarkIds = (raw.Characters ?? []).SelectMany(character => (character.Barks ?? []).Select(bark => bark.Id)).Where(id => id is not null).ToArray();
        if (allBarkIds.Distinct(StringComparer.Ordinal).Count() != allBarkIds.Length)
            diagnostics.Add(new(source, "characters.barks", "Dialogue bark IDs must be globally unique."));
        ValidateDefinitions(raw.Incidents, "incidents", "incident.", source, diagnostics, item =>
        {
            RequireId(item.Industry, "industry", "industry.");
            Require(item.DisplayName, "display_name", "Display name is required.");
            RequireNonNegative(item.TriggerAtTick, "trigger_at_tick");
            RequireToken(item.Scope, "scope", IncidentScopeTokens);
            Require(item.Observable, "observable", "Immediate observable description is required.");
            Require(item.Evidence, "evidence", "Evidence description is required.");
            Require(item.Recovery, "recovery", "Recovery description is required.");
            var familyCount = new object?[]
            {
                item.ProcessDelay, item.CapacityLoss, item.BadSensor,
                item.BlockedResource, item.WorkerAbsence, item.DemandSpike,
            }.Count(value => value is not null);
            if (familyCount != 1) Add("effect", "Incident must declare exactly one effect family.");
            if (item.ProcessDelay is { } delay)
            {
                RequirePositive(delay.DurationTicks, "process_delay.duration_ticks");
                RequirePositive(delay.AddedCycleTicks, "process_delay.added_cycle_ticks");
            }
            if (item.CapacityLoss is { } capacity)
            {
                RequirePositive(capacity.DurationTicks, "capacity_loss.duration_ticks");
                RequirePositive(capacity.LostSlots, "capacity_loss.lost_slots");
            }
            if (item.BadSensor is { } sensor)
            {
                RequirePositive(sensor.DurationTicks, "bad_sensor.duration_ticks");
                RequireToken(sensor.Signal, "bad_sensor.signal", IncidentSensorTokens);
            }
            if (item.BlockedResource is { } blocked)
            {
                RequirePositive(blocked.DurationTicks, "blocked_resource.duration_ticks");
                RequireToken(blocked.Resource, "blocked_resource.resource", IncidentResourceTokens);
            }
            if (item.WorkerAbsence is { } absence)
            {
                RequirePositive(absence.DurationTicks, "worker_absence.duration_ticks");
                RequireToken(absence.Worker, "worker_absence.worker", IncidentWorkerTokens);
            }
            if (item.DemandSpike is { } demand)
            {
                RequirePositive(demand.DurationTicks, "demand_spike.duration_ticks");
                RequireToken(demand.DemandKind, "demand_spike.demand_kind", DishKindTokens);
                RequirePositive(demand.IntervalTicks, "demand_spike.interval_ticks");
            }
        });
        return;

        void ValidateDefinitions<T>(IReadOnlyList<T>? definitions, string collection, string prefix, string definitionSource,
            List<ContentDiagnostic> definitionDiagnostics, Action<T> validate) where T : RawDefinition
        {
            if (definitions is null)
            {
                definitionDiagnostics.Add(new(definitionSource, collection, "Definition collection must be a YAML sequence."));
                return;
            }
            for (var index = 0; index < definitions.Count; index++)
            {
                var currentPath = $"{collection}[{index}]";
                var item = definitions[index];
                if (!ContentId.IsValid(item.Id)) definitionDiagnostics.Add(new(definitionSource, $"{currentPath}.id", $"'{item.Id ?? "<missing>"}' is not a valid semantic content ID."));
                else if (!item.Id!.StartsWith(prefix, StringComparison.Ordinal)) definitionDiagnostics.Add(new(definitionSource, $"{currentPath}.id", $"Expected a '{prefix}' ID."));
                activePath = currentPath;
                validate(item);
            }
        }

        void Add(string path, string message) => diagnostics.Add(new(source, $"{activePath}.{path}", message));
        void Require(string? value, string path, string message) { if (string.IsNullOrWhiteSpace(value)) Add(path, message); }
        void RequireState(string? value, string path) { if (!StatePattern().IsMatch(value ?? "")) Add(path, "State must use lowercase semantic-token syntax."); }
        void RequirePositive(int? value, string path) { if (value is null or <= 0) Add(path, "Value must be a positive integer."); }
        void RequireNonNegative(int? value, string path) { if (value is null or < 0) Add(path, "Value must be a non-negative integer."); }
        void RequireCounts(RawDishCounts? value, string path)
        {
            if (value is null) { Add(path, "Dish counts are required."); return; }
            RequireNonNegative(value.Plates, $"{path}.plates");
            RequireNonNegative(value.Glasses, $"{path}.glasses");
            RequireNonNegative(value.Trays, $"{path}.trays");
        }
        void RequireToken(string? value, string path, IReadOnlySet<string> supported)
        {
            if (value is null || !supported.Contains(value))
                Add(path, $"Unsupported value '{value ?? "<missing>"}'; expected one of: {string.Join(", ", supported.Order())}.");
        }
        void RequireId(string? value, string path, string? prefix = null)
        {
            if (!ContentId.IsValid(value)) Add(path, $"'{value ?? "<missing>"}' is not a valid semantic content ID.");
            else if (prefix is not null && !value!.StartsWith(prefix, StringComparison.Ordinal)) Add(path, $"Expected a '{prefix}' reference.");
        }
        void RequireIds(IReadOnlyList<string>? values, string path)
        {
            if (values is null || values.Count == 0) { Add(path, "At least one reference is required."); return; }
            for (var index = 0; index < values.Count; index++) RequireId(values[index], $"{path}[{index}]");
            if (values.Distinct(StringComparer.Ordinal).Count() != values.Count) Add(path, "References must be unique.");
        }
    }

    private static void ValidateGraph(
        ImmutableArray<IndustryContentDefinition> industries,
        ImmutableArray<FacilityContentDefinition> facilities,
        ImmutableArray<ItemContentDefinition> items,
        ImmutableArray<WorkstationContentDefinition> workstations,
        ImmutableArray<ProcessContentDefinition> processes,
        ImmutableArray<ScenarioContentDefinition> scenarios,
        ImmutableArray<QuestContentDefinition> quests,
        ImmutableArray<CharacterContentDefinition> characters,
        ImmutableArray<IncidentContentDefinition> incidents,
        string source,
        List<ContentDiagnostic> diagnostics)
    {
        var kinds = new Dictionary<ContentId, ContentDefinitionKind>();
        AddAll(industries.Select(item => item.Id), ContentDefinitionKind.Industry, "industries");
        AddAll(facilities.Select(item => item.Id), ContentDefinitionKind.Facility, "facilities");
        AddAll(items.Select(item => item.Id), ContentDefinitionKind.Item, "items");
        AddAll(workstations.Select(item => item.Id), ContentDefinitionKind.Workstation, "workstations");
        AddAll(processes.Select(item => item.Id), ContentDefinitionKind.Process, "processes");
        AddAll(scenarios.Select(item => item.Id), ContentDefinitionKind.Scenario, "scenarios");
        AddAll(quests.Select(item => item.Id), ContentDefinitionKind.Quest, "quests");
        AddAll(characters.Select(item => item.Id), ContentDefinitionKind.Character, "characters");
        AddAll(incidents.Select(item => item.Id), ContentDefinitionKind.Incident, "incidents");

        foreach (var item in facilities)
        {
            Ref(item.Industry, ContentDefinitionKind.Industry, $"facility[{item.Id}].industry");
            foreach (var reference in item.Workstations) Ref(reference, ContentDefinitionKind.Workstation, $"facility[{item.Id}].workstations");
        }
        foreach (var item in items) Ref(item.Industry, ContentDefinitionKind.Industry, $"item[{item.Id}].industry");
        var itemsById = items.ToDictionary(item => item.Id);
        var workstationsById = workstations.ToDictionary(item => item.Id);
        foreach (var item in workstations)
        {
            Ref(item.Industry, ContentDefinitionKind.Industry, $"workstation[{item.Id}].industry");
            foreach (var reference in item.AcceptedItems)
            {
                Ref(reference, ContentDefinitionKind.Item, $"workstation[{item.Id}].accepted_items");
                if (itemsById.TryGetValue(reference, out var accepted))
                {
                    if (!accepted.States.Contains(item.InputState, StringComparer.Ordinal)) Error($"workstation[{item.Id}].input_state", $"State '{item.InputState}' does not exist on {reference}.");
                    if (!accepted.States.Contains(item.OutputState, StringComparer.Ordinal)) Error($"workstation[{item.Id}].output_state", $"State '{item.OutputState}' does not exist on {reference}.");
                }
            }
            ValidateBehaviorStates(item);
        }
        foreach (var item in processes)
        {
            Ref(item.Industry, ContentDefinitionKind.Industry, $"process[{item.Id}].industry");
            foreach (var step in item.Steps) Ref(step.Workstation, ContentDefinitionKind.Workstation, $"process[{item.Id}].steps[{step.Id}].workstation");
            var stepIds = item.Steps.Select(step => step.Id).ToHashSet(StringComparer.Ordinal);
            if (stepIds.Count != item.Steps.Length) Error($"process[{item.Id}].steps", "Process step IDs must be unique.");
            foreach (var route in item.Routes)
            {
                if (!stepIds.Contains(route.From)) Error($"process[{item.Id}].routes", $"Unknown source step '{route.From}'.");
                if (!stepIds.Contains(route.To)) Error($"process[{item.Id}].routes", $"Unknown destination step '{route.To}'.");
                if (!stepIds.Contains(route.From) || !stepIds.Contains(route.To)) continue;
                var fromStep = item.Steps.Single(step => step.Id == route.From);
                var toStep = item.Steps.Single(step => step.Id == route.To);
                if (!workstationsById.TryGetValue(fromStep.Workstation, out var fromWorkstation) ||
                    !workstationsById.TryGetValue(toStep.Workstation, out var toWorkstation)) continue;
                var commonItems = fromWorkstation.AcceptedItems.Intersect(toWorkstation.AcceptedItems).ToArray();
                if (commonItems.Length == 0)
                    Error($"process[{item.Id}].routes[{route.From}->{route.To}]", "Invalid transition: source and destination accept no common item.");
                else if (!string.Equals(fromWorkstation.OutputState, toWorkstation.InputState, StringComparison.Ordinal))
                    Error($"process[{item.Id}].routes[{route.From}->{route.To}]",
                        $"Invalid transition: output state '{fromWorkstation.OutputState}' does not match input state '{toWorkstation.InputState}'.");
            }
            if (!item.AllowCycles && HasCycle(stepIds, item.Routes)) Error($"process[{item.Id}].routes", "Process contains a cycle but allow_cycles is false.");
        }
        var charactersById = characters.ToDictionary(item => item.Id);
        var questsById = quests.ToDictionary(item => item.Id);
        foreach (var item in characters)
        {
            Ref(item.Industry, ContentDefinitionKind.Industry, $"character[{item.Id}].industry");
            foreach (var relationship in item.Relationships)
            {
                Ref(relationship.Character, ContentDefinitionKind.Character, $"character[{item.Id}].relationships[{relationship.Character}]");
                if (relationship.Character == item.Id)
                    Error($"character[{item.Id}].relationships[{relationship.Character}]", "Character cannot have a relationship to itself.");
                else if (charactersById.TryGetValue(relationship.Character, out var target) && target.Industry != item.Industry)
                    Error($"character[{item.Id}].relationships[{relationship.Character}]", "Relationship target must belong to the same industry.");
            }
            foreach (var bark in item.Barks)
            {
                Ref(bark.Quest, ContentDefinitionKind.Quest, $"character[{item.Id}].barks[{bark.Id}].quest");
                if (questsById.TryGetValue(bark.Quest, out var quest) && !quest.Participants.Contains(item.Id))
                    Error($"character[{item.Id}].barks[{bark.Id}].quest", $"Speaker '{item.Id}' is not a participant in quest '{quest.Id}'.");
            }
        }
        foreach (var item in incidents) Ref(item.Industry, ContentDefinitionKind.Industry, $"incident[{item.Id}].industry");
        foreach (var item in scenarios)
        {
            Ref(item.Industry, ContentDefinitionKind.Industry, $"scenario[{item.Id}].industry");
            Ref(item.Facility, ContentDefinitionKind.Facility, $"scenario[{item.Id}].facility");
            foreach (var reference in item.Processes) Ref(reference, ContentDefinitionKind.Process, $"scenario[{item.Id}].processes");
            foreach (var reference in item.Items) Ref(reference, ContentDefinitionKind.Item, $"scenario[{item.Id}].items");
            foreach (var reference in item.Characters) Ref(reference, ContentDefinitionKind.Character, $"scenario[{item.Id}].characters");
        }
        var scenariosById = scenarios.ToDictionary(item => item.Id);
        foreach (var item in quests)
        {
            Ref(item.Scenario, ContentDefinitionKind.Scenario, $"quest[{item.Id}].scenario");
            foreach (var participant in item.Participants)
            {
                Ref(participant, ContentDefinitionKind.Character, $"quest[{item.Id}].participants");
                if (scenariosById.TryGetValue(item.Scenario, out var scenario) && !scenario.Characters.Contains(participant))
                    Error($"quest[{item.Id}].participants", $"Participant '{participant}' is not in scenario '{scenario.Id}' character roster.");
            }
            if (!SupportedQuestMetrics.Contains(item.Completion.Metric)) Error($"quest[{item.Id}].completion.metric", $"Unknown metric '{item.Completion.Metric}'.");
            if (!SupportedQuestOperators.Contains(item.Completion.Operator)) Error($"quest[{item.Id}].completion.operator", $"Unknown operator '{item.Completion.Operator}'.");
        }
        return;

        void AddAll(IEnumerable<ContentId> ids, ContentDefinitionKind kind, string path)
        {
            foreach (var id in ids)
                if (!kinds.TryAdd(id, kind)) Error(path, $"Duplicate global content ID '{id}'.");
        }
        void Ref(ContentId id, ContentDefinitionKind expected, string path)
        {
            if (!kinds.TryGetValue(id, out var actual)) Error(path, $"Unknown {expected.ToString().ToLowerInvariant()} reference '{id}'.");
            else if (actual != expected) Error(path, $"Reference '{id}' has type {actual}, expected {expected}.");
        }
        void Error(string path, string message) => diagnostics.Add(new(source, path, message));
        void ValidateBehaviorStates(WorkstationContentDefinition workstation)
        {
            var expected = workstation.Behavior switch
            {
                ManualWorkstationBehaviorContentDefinition manual => manual.Action switch
                {
                    "scrape" => ("dirty", "scraped"),
                    "rack" => ("scraped", "racked"),
                    "unload" => ("washed_in_machine", "clean_wet"),
                    "dry-and-restock" => ("clean_wet", "available"),
                    _ => default,
                },
                BatchWorkstationBehaviorContentDefinition => ("racked", "washed_in_machine"),
                BufferWorkstationBehaviorContentDefinition => ("scraped", "racked"),
                InspectionWorkstationBehaviorContentDefinition => (workstation.InputState, workstation.InputState),
                ServiceWorkstationBehaviorContentDefinition => ("available", "dirty"),
                _ => default,
            };
            if (expected == default) return;
            if (workstation.InputState != expected.Item1 || workstation.OutputState != expected.Item2)
                Error($"workstation[{workstation.Id}].behavior",
                    $"{workstation.Behavior!.Family} behavior requires state transition '{expected.Item1}' -> '{expected.Item2}'.");
        }
    }

    private static bool HasCycle(HashSet<string> steps, ImmutableArray<ProcessRouteContentDefinition> routes)
    {
        var edges = steps.ToDictionary(step => step, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var route in routes)
            if (edges.TryGetValue(route.From, out var destinations) && edges.ContainsKey(route.To)) destinations.Add(route.To);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return steps.Any(Visit);

        bool Visit(string step)
        {
            if (visiting.Contains(step)) return true;
            if (!visited.Add(step)) return false;
            visiting.Add(step);
            foreach (var destination in edges[step]) if (Visit(destination)) return true;
            visiting.Remove(step);
            return false;
        }
    }

    private static string Hash(
        ImmutableArray<IndustryContentDefinition> industries,
        ImmutableArray<FacilityContentDefinition> facilities,
        ImmutableArray<ItemContentDefinition> items,
        ImmutableArray<WorkstationContentDefinition> workstations,
        ImmutableArray<ProcessContentDefinition> processes,
        ImmutableArray<ScenarioContentDefinition> scenarios,
        ImmutableArray<QuestContentDefinition> quests,
        ImmutableArray<CharacterContentDefinition> characters,
        ImmutableArray<IncidentContentDefinition> incidents)
    {
        var text = new StringBuilder().AppendLine("schema|1");
        foreach (var item in industries) text.AppendLine($"industry|{item.Id}|{Encode(item.DisplayName)}");
        foreach (var item in facilities) text.AppendLine($"facility|{item.Id}|{item.Industry}|{Encode(item.DisplayName)}|{Join(item.Workstations)}");
        foreach (var item in items) text.AppendLine($"item|{item.Id}|{item.Industry}|{Encode(item.DisplayName)}|{string.Join(',', item.States.Select(Encode))}");
        foreach (var item in workstations)
        {
            text.AppendLine($"workstation|{item.Id}|{item.Industry}|{Encode(item.DisplayName)}|{Join(item.AcceptedItems)}|{item.InputState}|{item.OutputState}|{item.Presentation}|{item.PresentationFallback}");
            if (item.Behavior is not null) text.AppendLine(Behavior(item.Behavior));
        }
        foreach (var item in processes)
        {
            text.AppendLine($"process|{item.Id}|{item.Industry}|{item.AllowCycles}");
            foreach (var step in item.Steps) text.AppendLine($"step|{step.Id}|{step.Workstation}");
            foreach (var route in item.Routes.OrderBy(route => route.From).ThenBy(route => route.To)) text.AppendLine($"route|{route.From}|{route.To}");
        }
        foreach (var item in scenarios)
        {
            text.AppendLine($"scenario|{item.Id}|{item.Industry}|{item.Facility}|{Join(item.Processes)}|{Join(item.Items)}|{Join(item.Characters)}|{Encode(item.Seed)}");
            if (item.Narrative is { } narrative)
            {
                text.AppendLine($"scenario-narrative|{Encode(narrative.ChapterTitle)}|{Encode(narrative.DebriefSummary)}");
                foreach (var page in narrative.Briefing) text.AppendLine($"briefing|{Encode(page.Title)}|{Encode(page.Body)}");
                foreach (var question in narrative.DebriefQuestions) text.AppendLine($"debrief-question|{Encode(question)}");
            }
            if (item.DishStation is { } scenario)
                text.AppendLine($"dish-station|{Counts(scenario.InitialDirty)}|{Counts(scenario.InitialAvailable)}|{scenario.ArrivalIntervalTicks}|{scenario.GlassEveryArrivals}|{scenario.RackCapacity}|{scenario.WasherCycleTicks}|{scenario.WorkerActionIntervalTicks}|{scenario.FlowCellWorkerActionIntervalTicks}|{scenario.StickyReadyFaultAfterAutomatedStarts}|{scenario.StickyReadyFaultPermillePerStart}|{DishKindToken(scenario.DemandKind)}|{scenario.DemandIntervalTicks}|{scenario.InitialRushEnabled}|{scenario.InitialNewHireEnabled}|{KnowledgeToken(scenario.InitialNewHireSpecification)}|{AutomationToken(scenario.InitialAutomationPolicy)}|{LayoutToken(scenario.InitialLayout)}|economy:{scenario.Economy.CompletedDishValue},{scenario.Economy.LaborTicksPerWorkAction},{scenario.Economy.LaborCostPerTick},{scenario.Economy.StaffingCostPerEnabledTick},{scenario.Economy.TrayReworkCost},{scenario.Economy.ServiceShortageDowntimeCost},{scenario.Economy.AutomationIncidentDowntimeCost},{scenario.Economy.FlowCellInvestmentCost}");
            if (item.TwoStationRouting is { } routing)
            {
                text.AppendLine($"two-station-routing|{routing.TrialHorizonTicks}");
                foreach (var station in routing.Stations.OrderBy(station => station.Id))
                    text.AppendLine($"routing-station|{RoutingStationToken(station.Id)}|{Encode(station.DisplayName)}|{Counts(station.InitialDirty)}|{DishKindToken(station.DemandKind)}|{RoutingPolicyToken(station.InitialPolicy)}");
            }
        }
        foreach (var item in quests)
        {
            text.AppendLine($"quest|{item.Id}|{item.Scenario}|{Join(item.Participants)}|{Encode(item.Objective)}|{item.Completion.Metric}|{item.Completion.Operator}|{item.Completion.Value.ToString("R", CultureInfo.InvariantCulture)}");
            if (item.Narrative is not { } narrative) continue;
            text.AppendLine($"narrative|{narrative.RuntimeId}|{narrative.Sequence}|{Encode(narrative.Title)}|{Encode(narrative.Situation)}|{Encode(narrative.Discovery)}|{Encode(narrative.UnlockRationale)}|{narrative.ExperienceReward}|{narrative.CapabilityReward}");
            foreach (var step in narrative.Steps)
                text.AppendLine($"narrative-step|{step.Id}|{Encode(step.Text)}|{step.InputAction ?? ""}");
        }
        foreach (var item in characters)
        {
            text.AppendLine($"character|{item.Id}|{item.Industry}|{Encode(item.DisplayName)}|{item.Role}|{Encode(item.Motivation)}|{Join(item.KnownFacts)}|{Join(item.BlindSpots)}|{Join(item.Authority)}|{item.Presentation}|{item.PresentationFallback}");
            foreach (var relationship in item.Relationships)
                text.AppendLine($"character-relationship|{item.Id}|{relationship.Character}|{relationship.Kind}");
            foreach (var bark in item.Barks)
                text.AppendLine($"character-bark|{item.Id}|{bark.Id}|{bark.Quest}|{DialogueTriggerToken(bark.Trigger)}|{DialoguePriorityToken(bark.Priority)}|{bark.CooldownTicks}|{Encode(bark.Line)}");
        }
        foreach (var item in incidents)
            text.AppendLine($"incident|{item.Id}|{item.Industry}|{Encode(item.DisplayName)}|{item.TriggerAt.Value}|{item.Scope}|{Encode(item.Observable)}|{Encode(item.Evidence)}|{Encode(item.Recovery)}|{IncidentEffect(item.Effect)}");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private static string Join(ImmutableArray<ContentId> ids) => string.Join(',', ids.OrderBy(id => id.Value).Select(id => id.Value));
    private static string Counts(DishCounts counts) => $"{counts.Plates},{counts.Glasses},{counts.Trays}";
    private static string Behavior(WorkstationBehaviorContentDefinition behavior) => behavior switch
    {
        ManualWorkstationBehaviorContentDefinition value => $"behavior|manual|{value.Action}",
        BatchWorkstationBehaviorContentDefinition value => $"behavior|batch|{value.Capacity}|{value.CycleTicks}",
        BufferWorkstationBehaviorContentDefinition value => $"behavior|buffer|{value.Capacity}|{value.Ordering}",
        InspectionWorkstationBehaviorContentDefinition value => $"behavior|inspection|{value.Observation}",
        ServiceWorkstationBehaviorContentDefinition value => $"behavior|service|{DishKindToken(value.DemandKind)}|{value.RequestIntervalTicks}",
        _ => throw new ArgumentOutOfRangeException(nameof(behavior)),
    };
    private static string IncidentEffect(DishStationIncidentEffect effect) => effect switch
    {
        ProcessDelayIncidentEffect value => $"process-delay|{value.DurationTicks}|{value.AddedCycleTicks}",
        CapacityLossIncidentEffect value => $"capacity-loss|{value.DurationTicks}|{value.LostSlots}",
        BadSensorIncidentEffect value => $"bad-sensor|{value.DurationTicks}|reported-ready-stuck-true",
        BlockedResourceIncidentEffect value => $"blocked-resource|{value.DurationTicks}|washer",
        WorkerAbsenceIncidentEffect value => $"worker-absence|{value.DurationTicks}|new-hire",
        DemandSpikeIncidentEffect value => $"demand-spike|{value.DurationTicks}|{DishKindToken(value.DemandKind)}|{value.IntervalTicks}",
        _ => throw new ArgumentOutOfRangeException(nameof(effect)),
    };
    private static string DishKindToken(DishKind value) => value switch
    {
        DishKind.Plate => "plate",
        DishKind.Glass => "glass",
        DishKind.Tray => "tray",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
    private static string KnowledgeToken(DishProcessSpecification value) => value switch
    {
        { FlowDocumented: false, RushGlassPriorityDocumented: false, RareTrayHandlingDocumented: false } => "none",
        { FlowDocumented: true, RushGlassPriorityDocumented: false, RareTrayHandlingDocumented: false } => "happy-path",
        { FlowDocumented: true, RushGlassPriorityDocumented: true, RareTrayHandlingDocumented: false } => "rush-aware",
        { FlowDocumented: true, RushGlassPriorityDocumented: true, RareTrayHandlingDocumented: true } => "fully-documented",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
    private static string AutomationToken(WasherAutomationPolicy value) => value switch
    {
        { Enabled: false } => "off",
        { Enabled: true, RequirePhysicalReady: false } => "reported-ready-only",
        { Enabled: true, RequirePhysicalReady: true } => "corroborated-ready",
    };
    private static string LayoutToken(DishStationLayout value) => value switch
    {
        DishStationLayout.Linear => "linear",
        DishStationLayout.UShapedCell => "u-shaped-cell",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
    private static DishStationNarrativeEventKind DialogueTrigger(string value) => value switch
    {
        "queue-pressure" => DishStationNarrativeEventKind.QueuePressure,
        "automation-incident" => DishStationNarrativeEventKind.AutomationIncident,
        "shift-succeeded" => DishStationNarrativeEventKind.ShiftSucceeded,
        _ => throw new UnreachableException(),
    };
    private static string DialogueTriggerToken(DishStationNarrativeEventKind value) => value switch
    {
        DishStationNarrativeEventKind.QueuePressure => "queue-pressure",
        DishStationNarrativeEventKind.AutomationIncident => "automation-incident",
        DishStationNarrativeEventKind.ShiftSucceeded => "shift-succeeded",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
    private static CharacterDialoguePriority DialoguePriority(string value) => value switch
    {
        "ambient" => CharacterDialoguePriority.Ambient,
        "important" => CharacterDialoguePriority.Important,
        "critical" => CharacterDialoguePriority.Critical,
        _ => throw new UnreachableException(),
    };
    private static string DialoguePriorityToken(CharacterDialoguePriority value) => value switch
    {
        CharacterDialoguePriority.Ambient => "ambient",
        CharacterDialoguePriority.Important => "important",
        CharacterDialoguePriority.Critical => "critical",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    private static int CountOccurrences(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
    private static ContentId Id(string? value) => new(value!);
    private static ImmutableArray<ContentId> Ids(IReadOnlyList<string>? values) =>
        values!.Select(Id).OrderBy(id => id.Value, StringComparer.Ordinal).ToImmutableArray();
    private static WorkstationBehaviorContentDefinition? CompileWorkstationBehavior(RawWorkstation workstation) =>
        workstation.Manual is { } manual ? new ManualWorkstationBehaviorContentDefinition(manual.Action!) :
        workstation.Batch is { } batch ? new BatchWorkstationBehaviorContentDefinition(batch.Capacity!.Value, batch.CycleTicks!.Value) :
        workstation.Buffer is { } buffer ? new BufferWorkstationBehaviorContentDefinition(buffer.Capacity!.Value, buffer.Ordering!) :
        workstation.Inspection is { } inspection ? new InspectionWorkstationBehaviorContentDefinition(inspection.Observation!) :
        workstation.Service is { } service ? new ServiceWorkstationBehaviorContentDefinition(
            service.DemandKind switch
            {
                "plate" => DishKind.Plate,
                "glass" => DishKind.Glass,
                "tray" => DishKind.Tray,
                _ => throw new UnreachableException(),
            }, service.RequestIntervalTicks!.Value) : null;
    private static IncidentContentDefinition CompileIncident(RawIncident incident) => new(
        Id(incident.Id),
        Id(incident.Industry),
        incident.DisplayName!,
        new(incident.TriggerAtTick!.Value),
        incident.Scope!,
        incident.Observable!,
        incident.Evidence!,
        incident.Recovery!,
        incident.ProcessDelay is { } delay ? new ProcessDelayIncidentEffect(delay.DurationTicks!.Value, delay.AddedCycleTicks!.Value) :
        incident.CapacityLoss is { } capacity ? new CapacityLossIncidentEffect(capacity.DurationTicks!.Value, capacity.LostSlots!.Value) :
        incident.BadSensor is { } sensor ? new BadSensorIncidentEffect(sensor.DurationTicks!.Value) :
        incident.BlockedResource is { } blocked ? new BlockedResourceIncidentEffect(blocked.DurationTicks!.Value) :
        incident.WorkerAbsence is { } absence ? new WorkerAbsenceIncidentEffect(absence.DurationTicks!.Value) :
        incident.DemandSpike is { } demand ? new DemandSpikeIncidentEffect(
            demand.DurationTicks!.Value,
            demand.DemandKind switch
            {
                "plate" => DishKind.Plate,
                "glass" => DishKind.Glass,
                "tray" => DishKind.Tray,
                _ => throw new UnreachableException(),
            },
            demand.IntervalTicks!.Value) : throw new UnreachableException());
    private static DishStationScenarioConfiguration CompileDishStation(RawDishStationScenario scenario) => new DishStationScenarioConfiguration
    {
        InitialDirty = new(scenario.InitialDirty!.Plates!.Value, scenario.InitialDirty.Glasses!.Value, scenario.InitialDirty.Trays!.Value),
        InitialAvailable = new(scenario.InitialAvailable!.Plates!.Value, scenario.InitialAvailable.Glasses!.Value, scenario.InitialAvailable.Trays!.Value),
        ArrivalIntervalTicks = scenario.ArrivalIntervalTicks!.Value,
        GlassEveryArrivals = scenario.GlassEveryArrivals!.Value,
        RackCapacity = scenario.RackCapacity!.Value,
        WasherCycleTicks = scenario.WasherCycleTicks!.Value,
        WorkerActionIntervalTicks = scenario.WorkerActionIntervalTicks!.Value,
        FlowCellWorkerActionIntervalTicks = scenario.FlowCellWorkerActionIntervalTicks!.Value,
        StickyReadyFaultAfterAutomatedStarts = scenario.StickyReadyFaultAfterAutomatedStarts!.Value,
        StickyReadyFaultPermillePerStart = scenario.StickyReadyFaultPermillePerStart!.Value,
        DemandKind = scenario.DemandKind switch { "plate" => DishKind.Plate, "glass" => DishKind.Glass, "tray" => DishKind.Tray, _ => throw new UnreachableException() },
        DemandIntervalTicks = scenario.DemandIntervalTicks!.Value,
        InitialRushEnabled = scenario.InitialRushEnabled!.Value,
        InitialNewHireEnabled = scenario.InitialNewHireEnabled!.Value,
        InitialNewHireSpecification = scenario.InitialNewHireKnowledge switch
        {
            "none" => default,
            "happy-path" => DishProcessSpecification.HappyPath,
            "rush-aware" => DishProcessSpecification.RushAware,
            "fully-documented" => DishProcessSpecification.FullyDocumented,
            _ => throw new UnreachableException(),
        },
        InitialAutomationPolicy = scenario.InitialAutomationPolicy switch
        {
            "off" => WasherAutomationPolicy.Off,
            "reported-ready-only" => WasherAutomationPolicy.ReportedReadyOnly,
            "corroborated-ready" => WasherAutomationPolicy.CorroboratedReady,
            _ => throw new UnreachableException(),
        },
        InitialLayout = scenario.InitialLayout switch
        {
            "linear" => DishStationLayout.Linear,
            "u-shaped-cell" => DishStationLayout.UShapedCell,
            _ => throw new UnreachableException(),
        },
        Economy = scenario.Economy is null
            ? DishStationEconomyConfiguration.Default
            : new(
                scenario.Economy.CompletedDishValue!.Value,
                scenario.Economy.LaborTicksPerWorkAction!.Value,
                scenario.Economy.LaborCostPerTick!.Value,
                scenario.Economy.StaffingCostPerEnabledTick!.Value,
                scenario.Economy.TrayReworkCost!.Value,
                scenario.Economy.ServiceShortageDowntimeCost!.Value,
                scenario.Economy.AutomationIncidentDowntimeCost!.Value,
                scenario.Economy.FlowCellInvestmentCost!.Value),
    }.Validate();

    private static TwoStationRoutingConfiguration CompileTwoStationRouting(
        RawTwoStationRouting routing,
        DishStationScenarioConfiguration baseScenario) => new TwoStationRoutingConfiguration(
        baseScenario,
        routing.Stations.Select(station => new DishRoutingStationProfile(
            RoutingStation(station.Id!),
            station.DisplayName!,
            new(station.InitialDirty!.Plates!.Value, station.InitialDirty.Glasses!.Value, station.InitialDirty.Trays!.Value),
            station.DemandKind switch
            {
                "plate" => DishKind.Plate,
                "glass" => DishKind.Glass,
                _ => throw new UnreachableException(),
            },
            RoutingPolicy(station.InitialPolicy!))).ToImmutableArray(),
        routing.TrialHorizonTicks!.Value).Validate();

    private static DishRoutingStationId RoutingStation(string token) => token switch
    {
        "main-dish-room" => DishRoutingStationId.MainDishRoom,
        "patio-service-station" => DishRoutingStationId.PatioServiceStation,
        _ => throw new UnreachableException(),
    };

    private static string RoutingStationToken(DishRoutingStationId station) => station switch
    {
        DishRoutingStationId.MainDishRoom => "main-dish-room",
        DishRoutingStationId.PatioServiceStation => "patio-service-station",
        _ => throw new UnreachableException(),
    };

    private static ProcessRoutingPolicy RoutingPolicy(string token) => token switch
    {
        "captured-order" => ProcessRoutingPolicy.CapturedOrder,
        "plates-first" => ProcessRoutingPolicy.PlatesFirst,
        "glasses-first" => ProcessRoutingPolicy.GlassesFirst,
        _ => throw new UnreachableException(),
    };

    private static string RoutingPolicyToken(ProcessRoutingPolicy policy) => policy switch
    {
        ProcessRoutingPolicy.CapturedOrder => "captured-order",
        ProcessRoutingPolicy.PlatesFirst => "plates-first",
        ProcessRoutingPolicy.GlassesFirst => "glasses-first",
        _ => throw new UnreachableException(),
    };
    private static void ThrowIfAny(List<ContentDiagnostic> diagnostics)
    {
        if (diagnostics.Count > 0) throw new ContentCompilationException(diagnostics);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StatePattern();
    [GeneratedRegex("^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StepIdPattern();

    private abstract class RawDefinition { public string? Id { get; set; } }
    private sealed class RawContentBundle
    {
        public int? SchemaVersion { get; set; }
        public List<RawIndustry> Industries { get; set; } = [];
        public List<RawFacility> Facilities { get; set; } = [];
        public List<RawItem> Items { get; set; } = [];
        public List<RawWorkstation> Workstations { get; set; } = [];
        public List<RawProcess> Processes { get; set; } = [];
        public List<RawScenario> Scenarios { get; set; } = [];
        public List<RawQuest> Quests { get; set; } = [];
        public List<RawCharacter> Characters { get; set; } = [];
        public List<RawIncident> Incidents { get; set; } = [];
    }
    private sealed class RawIndustry : RawDefinition { public string? DisplayName { get; set; } }
    private sealed class RawFacility : RawDefinition { public string? Industry { get; set; } public string? DisplayName { get; set; } public List<string> Workstations { get; set; } = []; }
    private sealed class RawItem : RawDefinition { public string? Industry { get; set; } public string? DisplayName { get; set; } public List<string> States { get; set; } = []; }
    private sealed class RawWorkstation : RawDefinition
    {
        public string? Industry { get; set; }
        public string? DisplayName { get; set; }
        public List<string> AcceptedItems { get; set; } = [];
        public string? InputState { get; set; }
        public string? OutputState { get; set; }
        public string? Presentation { get; set; }
        public string? PresentationFallback { get; set; }
        public RawManualBehavior? Manual { get; set; }
        public RawBatchBehavior? Batch { get; set; }
        public RawBufferBehavior? Buffer { get; set; }
        public RawInspectionBehavior? Inspection { get; set; }
        public RawServiceBehavior? Service { get; set; }
    }
    private sealed class RawManualBehavior { public string? Action { get; set; } }
    private sealed class RawBatchBehavior { public int? Capacity { get; set; } public int? CycleTicks { get; set; } }
    private sealed class RawBufferBehavior { public int? Capacity { get; set; } public string? Ordering { get; set; } }
    private sealed class RawInspectionBehavior { public string? Observation { get; set; } }
    private sealed class RawServiceBehavior { public string? DemandKind { get; set; } public int? RequestIntervalTicks { get; set; } }
    private sealed class RawProcess : RawDefinition
    {
        public string? Industry { get; set; }
        public List<RawProcessStep> Steps { get; set; } = [];
        public List<RawProcessRoute> Routes { get; set; } = [];
        public bool AllowCycles { get; set; }
    }
    private sealed class RawProcessStep { public string? Id { get; set; } public string? Workstation { get; set; } }
    private sealed class RawProcessRoute { public string? From { get; set; } public string? To { get; set; } }
    private sealed class RawScenario : RawDefinition
    {
        public string? Industry { get; set; }
        public string? Facility { get; set; }
        public List<string> Processes { get; set; } = [];
        public List<string> Items { get; set; } = [];
        public List<string> Characters { get; set; } = [];
        public string? Seed { get; set; }
        public RawScenarioNarrative? Narrative { get; set; }
        public RawDishStationScenario? DishStation { get; set; }
        public RawTwoStationRouting? TwoStationRouting { get; set; }
    }
    private sealed class RawScenarioNarrative
    {
        public string? ChapterTitle { get; set; }
        public List<RawBriefingPage> Briefing { get; set; } = [];
        public string? DebriefSummary { get; set; }
        public List<string> DebriefQuestions { get; set; } = [];
    }
    private sealed class RawBriefingPage { public string? Title { get; set; } public string? Body { get; set; } }
    private sealed class RawDishStationScenario
    {
        public RawDishCounts? InitialDirty { get; set; }
        public RawDishCounts? InitialAvailable { get; set; }
        public int? ArrivalIntervalTicks { get; set; }
        public int? GlassEveryArrivals { get; set; }
        public int? RackCapacity { get; set; }
        public int? WasherCycleTicks { get; set; }
        public int? WorkerActionIntervalTicks { get; set; }
        public int? FlowCellWorkerActionIntervalTicks { get; set; }
        public int? StickyReadyFaultAfterAutomatedStarts { get; set; }
        public int? StickyReadyFaultPermillePerStart { get; set; }
        public string? DemandKind { get; set; }
        public int? DemandIntervalTicks { get; set; }
        public bool? InitialRushEnabled { get; set; }
        public bool? InitialNewHireEnabled { get; set; }
        public string? InitialNewHireKnowledge { get; set; }
        public string? InitialAutomationPolicy { get; set; }
        public string? InitialLayout { get; set; }
        public RawDishStationEconomy? Economy { get; set; }
    }
    private sealed class RawDishStationEconomy
    {
        public int? CompletedDishValue { get; set; }
        public int? LaborTicksPerWorkAction { get; set; }
        public int? LaborCostPerTick { get; set; }
        public int? StaffingCostPerEnabledTick { get; set; }
        public int? TrayReworkCost { get; set; }
        public int? ServiceShortageDowntimeCost { get; set; }
        public int? AutomationIncidentDowntimeCost { get; set; }
        public int? FlowCellInvestmentCost { get; set; }
    }
    private sealed class RawTwoStationRouting
    {
        public int? TrialHorizonTicks { get; set; }
        public List<RawRoutingStation> Stations { get; set; } = [];
    }
    private sealed class RawRoutingStation
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public RawDishCounts? InitialDirty { get; set; }
        public string? DemandKind { get; set; }
        public string? InitialPolicy { get; set; }
    }
    private sealed class RawDishCounts { public int? Plates { get; set; } public int? Glasses { get; set; } public int? Trays { get; set; } }
    private sealed class RawQuest : RawDefinition
    {
        public string? Scenario { get; set; }
        public List<string> Participants { get; set; } = [];
        public string? Objective { get; set; }
        public RawQuestCompletion? Completion { get; set; }
        public RawQuestNarrative? Narrative { get; set; }
    }
    private sealed class RawQuestCompletion { public string? Metric { get; set; } public string? Operator { get; set; } public double? Value { get; set; } }
    private sealed class RawQuestNarrative
    {
        public string? RuntimeId { get; set; }
        public int? Sequence { get; set; }
        public string? Title { get; set; }
        public string? Situation { get; set; }
        public string? Discovery { get; set; }
        public string? UnlockRationale { get; set; }
        public RawQuestReward? Reward { get; set; }
        public List<RawQuestNarrativeStep> Steps { get; set; } = [];
    }
    private sealed class RawQuestReward { public int? Experience { get; set; } public string? Capability { get; set; } }
    private sealed class RawQuestNarrativeStep { public string? Id { get; set; } public string? Text { get; set; } public string? InputAction { get; set; } }
    private sealed class RawCharacter : RawDefinition
    {
        public string? Industry { get; set; }
        public string? DisplayName { get; set; }
        public string? Role { get; set; }
        public string? Motivation { get; set; }
        public List<string> KnownFacts { get; set; } = [];
        public List<string> BlindSpots { get; set; } = [];
        public List<string> Authority { get; set; } = [];
        public List<RawCharacterRelationship> Relationships { get; set; } = [];
        public List<RawCharacterBark> Barks { get; set; } = [];
        public string? Presentation { get; set; }
        public string? PresentationFallback { get; set; }
    }
    private sealed class RawCharacterRelationship { public string? Character { get; set; } public string? Kind { get; set; } }
    private sealed class RawCharacterBark
    {
        public string? Id { get; set; }
        public string? Quest { get; set; }
        public string? Trigger { get; set; }
        public string? Priority { get; set; }
        public int? CooldownTicks { get; set; }
        public string? Line { get; set; }
    }
    private sealed class RawIncident : RawDefinition
    {
        public string? Industry { get; set; }
        public string? DisplayName { get; set; }
        public int? TriggerAtTick { get; set; }
        public string? Scope { get; set; }
        public string? Observable { get; set; }
        public string? Evidence { get; set; }
        public string? Recovery { get; set; }
        public RawProcessDelayIncident? ProcessDelay { get; set; }
        public RawCapacityLossIncident? CapacityLoss { get; set; }
        public RawBadSensorIncident? BadSensor { get; set; }
        public RawBlockedResourceIncident? BlockedResource { get; set; }
        public RawWorkerAbsenceIncident? WorkerAbsence { get; set; }
        public RawDemandSpikeIncident? DemandSpike { get; set; }
    }
    private sealed class RawProcessDelayIncident { public int? DurationTicks { get; set; } public int? AddedCycleTicks { get; set; } }
    private sealed class RawCapacityLossIncident { public int? DurationTicks { get; set; } public int? LostSlots { get; set; } }
    private sealed class RawBadSensorIncident { public int? DurationTicks { get; set; } public string? Signal { get; set; } }
    private sealed class RawBlockedResourceIncident { public int? DurationTicks { get; set; } public string? Resource { get; set; } }
    private sealed class RawWorkerAbsenceIncident { public int? DurationTicks { get; set; } public string? Worker { get; set; } }
    private sealed class RawDemandSpikeIncident { public int? DurationTicks { get; set; } public string? DemandKind { get; set; } public int? IntervalTicks { get; set; } }
}
