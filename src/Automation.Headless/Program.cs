using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;
using Automation.Tools;

var expandTemplateIndex = Array.FindIndex(args, argument => string.Equals(argument, "--expand-template", StringComparison.OrdinalIgnoreCase));
if (expandTemplateIndex >= 0)
{
    if (expandTemplateIndex + 1 >= args.Length)
    {
        Console.Error.WriteLine("--expand-template requires a template YAML file path.");
        Environment.ExitCode = 2;
        return;
    }
    var namedSeedIndex = Array.FindIndex(args, argument => string.Equals(argument, "--named-seed", StringComparison.OrdinalIgnoreCase));
    var namedSeed = namedSeedIndex >= 0 && namedSeedIndex + 1 < args.Length ? args[namedSeedIndex + 1] : null;
    var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var index = 0; index < args.Length; index++)
    {
        if (!string.Equals(args[index], "--parameter", StringComparison.OrdinalIgnoreCase)) continue;
        if (index + 1 >= args.Length || args[index + 1].IndexOf('=') is <= 0)
        {
            Console.Error.WriteLine("--parameter requires NAME=VALUE.");
            Environment.ExitCode = 2;
            return;
        }
        var separator = args[index + 1].IndexOf('=');
        var name = args[index + 1][..separator];
        if (!parameters.TryAdd(name, args[index + 1][(separator + 1)..]))
        {
            Console.Error.WriteLine($"--parameter '{name}' was supplied more than once.");
            Environment.ExitCode = 2;
            return;
        }
        index++;
    }
    try
    {
        var template = ContentTemplateCompilerV1.CompileFile(args[expandTemplateIndex + 1]);
        var expansion = template.Expand(parameters, namedSeed);
        var selections = expansion.Provenance.VariantSelections.Count == 0
            ? "none"
            : string.Join(',', expansion.Provenance.VariantSelections.Select(pair => $"{pair.Key}={pair.Value}"));
        Console.WriteLine($"Template schema v{ContentTemplateCompilerV1.TemplateSchemaVersion} | id={template.Id} templateVersion={template.Version} seed={expansion.Provenance.NamedSeed ?? "none"} parameters={expansion.Provenance.Parameters.Count} variants={selections} definitions={expansion.Catalog.Manifest.DefinitionCount} contentSha256={expansion.Catalog.Manifest.Sha256} expansionSha256={expansion.ExpansionSha256}");
        if (args.Contains("--run-incident", StringComparer.OrdinalIgnoreCase))
        {
            if (expansion.Catalog.Incidents.Length == 0)
                throw new InvalidDataException("Expanded template contains no incident to run.");
            var ticksIndex = Array.FindIndex(args, argument => string.Equals(argument, "--ticks", StringComparison.OrdinalIgnoreCase));
            var ticks = ticksIndex >= 0 && ticksIndex + 1 < args.Length && int.TryParse(args[ticksIndex + 1], out var parsedTicks) && parsedTicks >= 0
                ? parsedTicks
                : 20;
            var incidentWorld = new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration);
            foreach (var definition in expansion.Catalog.Incidents)
            {
                var schedule = DishStationIncidentContentAdapter.ToSchedule(definition).Validate();
                incidentWorld.Schedule(new TriggerDishStationIncidentCommand(schedule.TriggerAt, schedule.Incident));
            }
            for (var tick = 0; tick < ticks; tick++) incidentWorld.Advance();
            foreach (var entry in incidentWorld.Snapshot().Incidents.Trace)
                Console.WriteLine($"incident t={entry.Tick.Value} id={entry.Id} kind={entry.Kind} phase={entry.Phase} observation={entry.Observation} evidence={entry.Evidence}");
            Console.WriteLine($"incident-run ticks={ticks} active={incidentWorld.Snapshot().Incidents.Active.Count} trace={incidentWorld.Snapshot().Incidents.Trace.Count}");
        }
    }
    catch (ContentCompilationException exception)
    {
        foreach (var diagnostic in exception.Diagnostics) Console.Error.WriteLine(diagnostic);
        Environment.ExitCode = 1;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
    {
        Console.Error.WriteLine($"{args[expandTemplateIndex + 1]}: $: {exception.Message}");
        Environment.ExitCode = 1;
    }
    return;
}

var compileContentIndex = Array.FindIndex(args, argument => string.Equals(argument, "--compile-content", StringComparison.OrdinalIgnoreCase));
if (compileContentIndex >= 0)
{
    if (compileContentIndex + 1 >= args.Length)
    {
        Console.Error.WriteLine("--compile-content requires a YAML file path.");
        Environment.ExitCode = 2;
        return;
    }
    try
    {
        var catalog = ContentCompilerV1.CompileFile(args[compileContentIndex + 1]);
        var counts = string.Join(',', catalog.Manifest.Counts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
        Console.WriteLine($"Content schema v{catalog.Manifest.SchemaVersion} | definitions={catalog.Manifest.DefinitionCount} {counts} sha256={catalog.Manifest.Sha256}");
    }
    catch (ContentCompilationException exception)
    {
        foreach (var diagnostic in exception.Diagnostics) Console.Error.WriteLine(diagnostic);
        Environment.ExitCode = 1;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"{args[compileContentIndex + 1]}: $: {exception.Message}");
        Environment.ExitCode = 1;
    }
    return;
}

if (args.Contains("--character-roster-demo", StringComparer.OrdinalIgnoreCase))
{
    var catalog = DishStationFirstHoursContent.Catalog;
    foreach (var character in catalog.Characters.OrderBy(character => character.Id.Value, StringComparer.Ordinal))
        Console.WriteLine($"character id={character.Id} name={character.DisplayName} role={character.Role} known={character.KnownFacts.Length} blind={character.BlindSpots.Length} authority={character.Authority.Length} relationships={character.Relationships.Length} presentation={character.Presentation} fallback={character.PresentationFallback}");
    foreach (var quest in DishStationFirstHoursContent.Quests.OrderBy(quest => quest.Sequence))
    {
        var participants = string.Join('|', quest.Participants.Select(participant => $"{participant}:{DishStationFirstHoursContent.Character(participant).DisplayName}"));
        Console.WriteLine($"quest sequence={quest.Sequence} id={quest.ContentId} participants={participants}");
    }
    return;
}

if (args.Contains("--automation-ir-demo", StringComparer.OrdinalIgnoreCase))
{
    foreach (var (name, policy) in new[]
    {
        ("reported", WasherAutomationPolicy.ReportedReadyOnly),
        ("corroborated", WasherAutomationPolicy.CorroboratedReady),
    })
    {
        var result = DishStationAutomationRules.Evaluate(policy, rackCount: 1, reportedReady: true, physicalReady: false);
        Console.WriteLine($"automation-ir policy={name} rule={result.Trace.RuleId} enabled={result.Trace.Enabled} matched={result.ConditionMatched} effects={result.SelectedEffects.Length}");
        foreach (var observed in result.Trace.ObservedValues)
            Console.WriteLine($"  observed path={observed.Path} ref={observed.Reference} kind={observed.Value.Kind} value={observed.Value}");
        foreach (var predicate in result.Trace.Predicates)
            Console.WriteLine($"  predicate path={predicate.Path} expression={predicate.Expression} result={predicate.Result}");
        foreach (var selected in result.Trace.SelectedEffects)
            Console.WriteLine($"  effect order={selected.Order} type={selected.Effect.GetType().Name}");
    }
    return;
}

if (args.Contains("--automation-editor-demo", StringComparer.OrdinalIgnoreCase))
{
    var scenario = DishStationFirstHoursContent.ScenarioConfiguration with
    {
        InitialDirty = new(4, 0, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        WasherCycleTicks = 2,
        StickyReadyFaultAfterAutomatedStarts = 1,
        StickyReadyFaultPermillePerStart = 0,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
    };
    var demo = new DishStationWorld(42, scenario);
    demo.ExecuteNow(new PerformDishActionCommand(demo.Tick, DishAction.Scrape, DishKind.Plate));
    demo.ExecuteNow(new PerformDishActionCommand(demo.Tick, DishAction.Rack, DishKind.Plate));
    demo.ExecuteNow(new BeginAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new SetAutomationRuleEnabledCommand(demo.Tick, true));
    Console.WriteLine($"automation-editor draft enabled={demo.Snapshot().Automation.ActiveEdit!.Enabled} conditions={string.Join(',', demo.Snapshot().Automation.ActiveEdit!.Conditions)} action={demo.Snapshot().Automation.ActiveEdit!.Action} valid={demo.Snapshot().Automation.ActiveEdit!.Diagnostics.Length == 0}");
    demo.ExecuteNow(new ApplyAutomationRuleEditCommand(demo.Tick));
    demo.Advance();
    demo.ExecuteNow(new PerformDishActionCommand(demo.Tick, DishAction.Scrape, DishKind.Plate));
    demo.ExecuteNow(new PerformDishActionCommand(demo.Tick, DishAction.Rack, DishKind.Plate));
    demo.Advance();
    demo.ExecuteNow(new ReplayAutomationIncidentCommand(demo.Tick));
    Console.WriteLine($"automation-editor unsafe rule={demo.Snapshot().Automation.ActiveRule.Id} matched={demo.Snapshot().Automation.Incident.LastReplayWouldStart} incident={demo.Snapshot().Automation.Incident.Recorded}");
    demo.ExecuteNow(new BeginAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new ToggleAutomationRuleConditionCommand(demo.Tick, AutomationObservable.PhysicalReady));
    Console.WriteLine($"automation-editor refined conditions={string.Join(',', demo.Snapshot().Automation.ActiveEdit!.Conditions)} valid={demo.Snapshot().Automation.ActiveEdit!.Diagnostics.Length == 0}");
    demo.ExecuteNow(new ApplyAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new ReplayAutomationIncidentCommand(demo.Tick));
    var final = demo.Snapshot().Automation;
    Console.WriteLine($"automation-editor safe rule={final.ActiveRule.Id} matched={final.Incident.LastReplayWouldStart} policy={final.Policy} trace={final.RuleTrace.Count}");
    foreach (var predicate in final.RuleTrace[^1].Evaluation.Predicates)
        Console.WriteLine($"  predicate path={predicate.Path} expression={predicate.Expression} result={predicate.Result}");
    return;
}

if (args.Contains("--automation-compare-demo", StringComparer.OrdinalIgnoreCase))
{
    var scenario = DishStationFirstHoursContent.ScenarioConfiguration with
    {
        InitialDirty = new(6, 2, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        WasherCycleTicks = 2,
        DemandIntervalTicks = 2,
        StickyReadyFaultAfterAutomatedStarts = 1,
        StickyReadyFaultPermillePerStart = 0,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
        InitialNewHireEnabled = false,
    };
    var demo = new DishStationWorld(42, scenario);
    demo.ExecuteNow(new BeginAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new SetAutomationRuleEnabledCommand(demo.Tick, true));
    demo.ExecuteNow(new ApplyAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new SaveAutomationRulePresetCommand(demo.Tick, AutomationPresetSlot.Baseline));
    demo.ExecuteNow(new BeginAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new ToggleAutomationRuleConditionCommand(demo.Tick, AutomationObservable.PhysicalReady));
    demo.ExecuteNow(new ApplyAutomationRuleEditCommand(demo.Tick));
    demo.ExecuteNow(new SaveAutomationRulePresetCommand(demo.Tick, AutomationPresetSlot.Variant));
    demo.ExecuteNow(new RunAutomationRuleComparisonCommand(demo.Tick, 16));
    var demoComparison = demo.Snapshot().Automation.Comparison.LatestResult!;
    Console.WriteLine($"automation-compare seed={demoComparison.Baseline.Seed} horizon={demoComparison.Baseline.HorizonTicks} sameScenario={demoComparison.Baseline.Scenario == demoComparison.Variant.Scenario} verdict={demoComparison.Verdict}");
    Console.WriteLine($"  baseline completed={demoComparison.Baseline.Metrics.Completed} shortages={demoComparison.Baseline.Metrics.ServiceShortages} starts={demoComparison.Baseline.Metrics.AutomatedStarts} incidents={demoComparison.Baseline.Metrics.UnsafeIncidents} prevented={demoComparison.Baseline.Metrics.PreventedUnsafeStarts} matched={demoComparison.Baseline.FirstReadinessDivergence!.Evaluation.ConditionMatched}");
    Console.WriteLine($"  variant  completed={demoComparison.Variant.Metrics.Completed} shortages={demoComparison.Variant.Metrics.ServiceShortages} starts={demoComparison.Variant.Metrics.AutomatedStarts} incidents={demoComparison.Variant.Metrics.UnsafeIncidents} prevented={demoComparison.Variant.Metrics.PreventedUnsafeStarts} matched={demoComparison.Variant.FirstReadinessDivergence!.Evaluation.ConditionMatched}");
    foreach (var predicate in demoComparison.Variant.FirstReadinessDivergence.Evaluation.Predicates)
        Console.WriteLine($"  variant-predicate path={predicate.Path} expression={predicate.Expression} result={predicate.Result}");
    return;
}

if (args.Contains("--economy-compare-demo", StringComparer.OrdinalIgnoreCase))
{
    var seedIndex = Array.FindIndex(args, argument => string.Equals(argument, "--seed", StringComparison.OrdinalIgnoreCase));
    var seed = seedIndex >= 0 && seedIndex + 1 < args.Length && int.TryParse(args[seedIndex + 1], out var parsedSeed)
        ? parsedSeed
        : 42;
    var economyComparison = DishStationEconomyComparison.Run(seed, DishStationFirstHoursContent.ScenarioConfiguration);
    Console.WriteLine($"economy-compare seed={economyComparison.LinearStation.Seed} horizon={economyComparison.LinearStation.HorizonTicks} sameSeed={economyComparison.SameSeed} sameScenario={economyComparison.Scenario == DishStationFirstHoursContent.ScenarioConfiguration} differentProfile={economyComparison.DifferentProfile}");
    PrintEconomyChoice(economyComparison.LinearStation);
    PrintEconomyChoice(economyComparison.FlowCell);
    return;
}

if (args.Contains("--two-station-demo", StringComparer.OrdinalIgnoreCase))
{
    var seedIndex = Array.FindIndex(args, argument => string.Equals(argument, "--seed", StringComparison.OrdinalIgnoreCase));
    var seed = seedIndex >= 0 && seedIndex + 1 < args.Length && int.TryParse(args[seedIndex + 1], out var parsedSeed)
        ? parsedSeed
        : 42;
    var routing = new TwoStationRoutingWorld(seed, DishStationTwoStationsContent.Configuration);
    routing.ExecuteNow(new CopyRoutingStationPolicyCommand(routing.Tick,
        DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
    routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
    var copied = routing.Snapshot().LatestTrial!;
    Console.WriteLine($"two-station quest={DishStationTwoStationsContent.Quest.Narrative!.Title} seed={copied.Seed} horizon={copied.HorizonTicks} copyCount={routing.Snapshot().CopyCount}");
    PrintRoutingTrial("copied", copied);

    routing.ExecuteNow(new SetRoutingStationPolicyCommand(routing.Tick,
        DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
    routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
    var fitted = routing.Snapshot().LatestTrial!;
    PrintRoutingTrial("fitted", fitted);
    var replay = TwoStationRoutingWorld.Restore(routing.CreateReplaySave()).Snapshot();
    Console.WriteLine($"two-station outcome improved={fitted.TotalShortages < copied.TotalShortages} bothSupplied={fitted.TotalShortages == 0} sameDecisionSlot=True replayTrials={replay.Trials.Count} discovery={DishStationTwoStationsContent.Quest.Narrative.Discovery}");
    return;
}

if (args.Contains("--pattern-knowledge-demo", StringComparer.OrdinalIgnoreCase))
{
    var seedIndex = Array.FindIndex(args, argument => string.Equals(argument, "--seed", StringComparison.OrdinalIgnoreCase));
    var seed = seedIndex >= 0 && seedIndex + 1 < args.Length && int.TryParse(args[seedIndex + 1], out var parsedSeed)
        ? parsedSeed
        : 42;
    var routing = new TwoStationRoutingWorld(seed, DishStationTwoStationsContent.Configuration);
    var profile = PatternKnowledgeProfile.Empty;
    routing.ExecuteNow(new CopyRoutingStationPolicyCommand(routing.Tick,
        DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
    routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
    profile = RestaurantPatternEvidenceRecognizer.Recognize(profile, routing.Snapshot(), DishStationPatternContent.Strategy);
    routing.ExecuteNow(new SetRoutingStationPolicyCommand(routing.Tick,
        DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
    routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
    profile = RestaurantPatternEvidenceRecognizer.Recognize(profile, routing.Snapshot(), DishStationPatternContent.Strategy);
    var knowledge = profile.For(DishStationPatternContent.Strategy.PatternId);
    Console.WriteLine($"pattern-codex id={knowledge.Pattern} title={DishStationPatternContent.Strategy.PreNameTitle} nameStatus={(knowledge.Has(PatternKnowledgeMilestone.Named) ? "named" : "not-recorded")} evidence={knowledge.Evidence.Length} milestones={string.Join(',', knowledge.Milestones)}");
    foreach (var evidence in knowledge.Evidence)
        Console.WriteLine($"  evidence={evidence.Id} milestone={evidence.Milestone} place={evidence.Place} consequence={evidence.Consequence} replay={evidence.ReplayReference}");
    var restored = AutomationCareerSaveStore.Deserialize(AutomationCareerSaveStore.Serialize(new(
        new DishStationWorld(seed, DishStationFirstHoursContent.ScenarioConfiguration), routing, profile)),
        seed, DishStationTwoStationsContent.Configuration);
    var restoredKnowledge = restored.PatternKnowledge.For(DishStationPatternContent.Strategy.PatternId);
    Console.WriteLine($"pattern-codex outcome recognized={restoredKnowledge.Has(PatternKnowledgeMilestone.Recognized)} named={restoredKnowledge.Has(PatternKnowledgeMilestone.Named)} persistedEvidence={restoredKnowledge.Evidence.Length} replayTrials={restored.TwoStationRouting.Snapshot().Trials.Count}");
    return;
}

if (args.Contains("--pattern-naming-demo", StringComparer.OrdinalIgnoreCase))
{
    var routing = new TwoStationRoutingWorld(42, DishStationTwoStationsContent.Configuration);
    routing.ExecuteNow(new CopyRoutingStationPolicyCommand(routing.Tick,
        DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
    routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
    routing.ExecuteNow(new SetRoutingStationPolicyCommand(routing.Tick,
        DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
    routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
    var recognized = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
        routing.Snapshot(), DishStationPatternContent.Strategy);
    var before = recognized.For(DishStationPatternContent.Strategy.PatternId);
    Console.WriteLine($"pattern-reveal start recognized={before.Has(PatternKnowledgeMilestone.Recognized)} named={before.Has(PatternKnowledgeMilestone.Named)} evidence={before.Evidence.Length}");
    Console.WriteLine($"reflection={DishStationPatternContent.Strategy.Naming.ReflectionPrompt}");
    var named = PatternNamingService.RecordReflection(recognized, DishStationPatternContent.Strategy);
    var restored = AutomationCareerSaveStore.Deserialize(AutomationCareerSaveStore.Serialize(new(
        new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration), routing, named)),
        42, DishStationTwoStationsContent.Configuration);
    var knowledge = restored.PatternKnowledge.For(DishStationPatternContent.Strategy.PatternId);
    var naming = DishStationPatternContent.Strategy.Naming;
    Console.WriteLine($"pattern-reveal name={naming.DisplayTitle} category={DishStationPatternContent.Strategy.Category.ToUpperInvariant()} intent={naming.Intent}");
    foreach (var item in naming.Structure) Console.WriteLine($"  structure={item}");
    foreach (var item in naming.Benefits) Console.WriteLine($"  benefit={item}");
    foreach (var item in naming.Costs) Console.WriteLine($"  cost={item}");
    var conclusion = knowledge.Conclusions.Single(item => item.Milestone == PatternKnowledgeMilestone.Named);
    Console.WriteLine($"pattern-reveal outcome recognized={knowledge.Has(PatternKnowledgeMilestone.Recognized)} named={knowledge.Has(PatternKnowledgeMilestone.Named)} basis={conclusion.Basis} persistedEvidence={knowledge.Evidence.Length} replayTrials={restored.TwoStationRouting.Snapshot().Trials.Count}");
    return;
}

if (args.Contains("--vendor-demo", StringComparer.OrdinalIgnoreCase))
{
    var vendor = new VendorOutsourcingWorld(DishStationVendorContent.Configuration);
    Console.WriteLine($"vendor-episode title={DishStationVendorContent.Quest.Narrative!.Title} localCode={vendor.Configuration.LocalRareTrayCode} vendorCode={vendor.Configuration.VendorRareTrayCode} horizon={vendor.Configuration.TrialHorizonTicks} incidentAt={vendor.Configuration.IncidentAtTick}");
    foreach (var proposal in Enum.GetValues<VendorProposalId>())
    {
        vendor.ExecuteNow(new SelectVendorProposalCommand(vendor.Tick, proposal));
        vendor.ExecuteNow(new RunVendorProposalTrialCommand(vendor.Tick));
        var trial = vendor.Snapshot().LatestTrial!;
        Console.WriteLine($"proposal={trial.Proposal} sourcing={trial.Sourcing} boundary={trial.Boundary} knowledge={trial.KnowledgeOwner} response={trial.SupportResponseTicks} trace={trial.TraceAvailable} fallback={trial.ManualFallbackAvailable} normalCost={trial.NormalCost} normalNet={trial.NormalNetValue} handled={trial.RequestsHandled} missed={trial.RequestsMissed} fallbackRequests={trial.FallbackRequests} incidentCost={trial.IncidentTotalCost} incidentNet={trial.IncidentNetValue} viable={trial.Viable}");
        foreach (var entry in trial.Trace)
            Console.WriteLine($"  trace tick={entry.Tick} phase={entry.Phase} owner={entry.KnowledgeOwner} observable={entry.Observable}");
    }
    var restored = VendorOutsourcingWorld.Restore(vendor.CreateReplaySave());
    var trials = restored.Snapshot().Trials;
    Console.WriteLine($"vendor-outcome compared={restored.Snapshot().ComparedProposalCount} viable={trials.Count(trial => trial.Viable)} distinctRisks={trials.Select(trial => (trial.RequestsMissed, trial.KnowledgeOwner, trial.IncidentTotalCost)).Distinct().Count()} replayTrials={trials.Count}");
    return;
}

var options = HeadlessOptions.Parse(args, DishStationFirstHoursContent.ScenarioConfiguration);
if (options.ShowHelp)
{
    Console.WriteLine(HeadlessOptions.HelpText);
    return;
}

if (options.BenchmarkActors > 0)
{
    var result = SyntheticWorkBenchmark.Run(options.BenchmarkActors, options.BenchmarkTicks);
    Console.WriteLine($"Synthetic work | actors={result.ActorCount} ticks={result.Ticks} transitions={result.Transitions} checksum={result.Checksum:X16} elapsedMs={result.Elapsed.TotalMilliseconds:F2} rate={result.Transitions / Math.Max(result.Elapsed.TotalSeconds, 0.000001):F0}/s representatives={result.RepresentativeStates.Length}");
    return;
}

var world = new DishStationWorld(options.Seed, options.Scenario);
world.ExecuteNow(new CompleteIntroCommand(world.Tick, GuidanceMode.Contextual));

if (options.ScriptedDemo)
    DishStationFirstShiftReferenceRun.Schedule(world);

if (options.CaptureDemo)
{
    world.Schedule(new StartProcessCaptureCommand(new(1), "Restore a plate"));
    world.Schedule(new PerformDishActionCommand(new(2), DishAction.Scrape, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(3), DishAction.Rack, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(4), DishAction.StartWasher, DishKind.Plate));
    var unloadAt = 5L + options.Scenario.WasherCycleTicks;
    world.Schedule(new PerformDishActionCommand(new(unloadAt), DishAction.Unload, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(unloadAt + 1), DishAction.DryAndRestock, DishKind.Plate));
    world.Schedule(new CompleteProcessCaptureCommand(new(unloadAt + 2)));
    if (options.ProcessEditorDemo)
    {
        var editAt = unloadAt + 3;
        world.Schedule(new BeginProcessEditCommand(new(editAt), new(1)));
        world.Schedule(new MoveProcessStepCommand(new(editAt + 1), new(2), 1));
        world.Schedule(new ApplyProcessEditCommand(new(editAt + 2)));
        world.Schedule(new MoveProcessStepCommand(new(editAt + 3), new(2), -1));
        for (var step = 1; step <= 5; step++)
            world.Schedule(new AssignProcessStepCommand(new(editAt + 3 + step), new(step), new(1)));
        world.Schedule(new SetProcessRoutingPolicyCommand(new(editAt + 9), ProcessRoutingPolicy.GlassesFirst));
        world.Schedule(new ApplyProcessEditCommand(new(editAt + 10)));
        world.Schedule(new ConfigureDishSupplyCommand(new(editAt + 12), DishState.Dirty, DishKind.Plate, 1));
        world.Schedule(new ConfigureDishSupplyCommand(new(editAt + 12), DishState.Dirty, DishKind.Glass, 1));
        world.Schedule(new ConfigureDishSupplyCommand(new(editAt + 12), DishState.Available, DishKind.Plate, 0));
        world.Schedule(new ConfigureDishSupplyCommand(new(editAt + 12), DishState.Available, DishKind.Glass, 0));
        world.Schedule(new SetNewHireEnabledCommand(new(editAt + 12), true));
        world.Schedule(new SetRushCommand(new(editAt + 12), true));
    }
}

if (options.SandboxDemo)
{
    var compact = new DishStationPlacements(new(0, 4), new(1, 4), new(2, 4), new(3, 4), new(4, 4), new(5, 4));
    var tick = 1L;
    foreach (var fixture in Enum.GetValues<DishStationFixture>())
        world.Schedule(new PlaceDishStationFixtureCommand(new(tick++), fixture, compact.At(fixture)));
    world.Schedule(new MovePlayerCommand(new(tick), new FloorCell(3, 5)));
}

for (var i = 0; i < options.Ticks; i++)
{
    world.Advance();
}

var snapshot = world.Snapshot();
if (options.NarrativeDemo)
{
    var narrative = DishStationFirstHoursContent.Narrative;
    Console.WriteLine($"chapter title={narrative.Chapter.ChapterTitle}");
    for (var index = 0; index < narrative.Chapter.Briefing.Length; index++)
        Console.WriteLine($"briefing page={index + 1} title={narrative.Chapter.Briefing[index].Title} body={narrative.Chapter.Briefing[index].Body}");
    foreach (var quest in narrative.Quests)
    {
        var people = string.Join('|', quest.Participants.Select(participant => DishStationFirstHoursContent.Character(participant).DisplayName));
        Console.WriteLine($"quest sequence={quest.Sequence} title={quest.Title} people={people} situation={quest.Situation} discovery={quest.Discovery}");
    }
    var dialogue = new CharacterDialogueRouter(DishStationFirstHoursContent.Catalog);
    foreach (var narrativeEvent in snapshot.NarrativeEvents)
        if (dialogue.Resolve(narrativeEvent) is { } bark)
            Console.WriteLine($"character-beat t={bark.Tick.Value} speaker={DishStationFirstHoursContent.Character(bark.Speaker).DisplayName} line={bark.Line}");
    Console.WriteLine($"debrief summary={narrative.Chapter.DebriefSummary}");
    for (var index = 0; index < narrative.Chapter.DebriefQuestions.Length; index++)
        Console.WriteLine($"debrief question={index + 1} text={narrative.Chapter.DebriefQuestions[index]}");
    var developerKinds = new HashSet<RecordedCommandKind>
    {
        RecordedCommandKind.AddDirtyDishes,
        RecordedCommandKind.ConfigureDishSupply,
        RecordedCommandKind.ResetDishStation,
        RecordedCommandKind.InjectStickyReadyFault,
        RecordedCommandKind.ConfigureWasherAutomation,
    };
    var developerCommands = world.CreateReplaySave().CommandInvocations.Count(invocation => developerKinds.Contains(invocation.Command.CommandKind));
    Console.WriteLine($"completion tick={snapshot.ShiftReport.CompletedAtTick} quests={snapshot.Progression.Quests.Count(quest => quest.Complete)}/{snapshot.Progression.Quests.Count} shift={snapshot.ShiftTrial.Status} checks={snapshot.ShiftTrial.SuccessfulDemandChecks}/{snapshot.ShiftTrial.TargetDemandChecks} developerCommands={developerCommands}");
    return;
}
if (options.DialogueDemo)
{
    var dialogue = new CharacterDialogueRouter(DishStationFirstHoursContent.Catalog);
    foreach (var narrativeEvent in snapshot.NarrativeEvents)
    {
        if (dialogue.Resolve(narrativeEvent) is not { } bark) continue;
        var speaker = DishStationFirstHoursContent.Character(bark.Speaker);
        Console.WriteLine($"dialogue t={bark.Tick.Value} event={bark.Trigger} quest={bark.Quest} priority={bark.Priority} speaker={bark.Speaker}:{speaker.DisplayName} bark={bark.Id} line={bark.Line}");
    }
    return;
}
Console.WriteLine($"Dish station | episode={DishStationEpisodeDefinition.FirstPlayable.Id} seed={options.Seed} tick={snapshot.Tick.Value}");
Console.WriteLine($"scenario arrivals={options.Scenario.ArrivalIntervalTicks} glassEvery={options.Scenario.GlassEveryArrivals} rackCapacity={options.Scenario.RackCapacity} washerCycle={options.Scenario.WasherCycleTicks} worker={options.Scenario.WorkerActionIntervalTicks}/{options.Scenario.FlowCellWorkerActionIntervalTicks} demand={options.Scenario.DemandKind}/{options.Scenario.DemandIntervalTicks} stickyAfter={options.Scenario.StickyReadyFaultAfterAutomatedStarts} faultPermille={options.Scenario.StickyReadyFaultPermillePerStart}");
foreach (var state in Enum.GetValues<DishState>())
{
    var count = snapshot.At(state);
    var metric = snapshot.MetricAt(state);
    Console.WriteLine($"{state,-16} plates={count.Plates,3} glasses={count.Glasses,3} trays={count.Trays,2} pressure={metric.TotalItemTicks,6} maxQueue={metric.MaxQueueDepth,3} glassOldest={metric.OldestGlassAge,3} glassAvg={metric.AverageResidenceTicks(DishKind.Glass),3}");
}

Console.WriteLine($"completed={snapshot.Completed} shortages={snapshot.ServiceShortages} washerRunning={snapshot.WasherRunning} pressureLeader={snapshot.Bottleneck?.ToString() ?? "none"} tutorial={snapshot.TutorialStage}");
Console.WriteLine($"career intro={snapshot.Onboarding.Complete}/{snapshot.Onboarding.GuidanceMode} level={snapshot.Progression.Level} xp={snapshot.Progression.Experience} activeQuest={snapshot.Progression.ActiveQuest?.ToString() ?? "complete"} quests={snapshot.Progression.Quests.Count(quest => quest.Complete)}/{snapshot.Progression.Quests.Count}");
Console.WriteLine($"shiftTrial status={snapshot.ShiftTrial.Status} checks={snapshot.ShiftTrial.SuccessfulDemandChecks}/{snapshot.ShiftTrial.TargetDemandChecks} attempts={snapshot.ShiftTrial.Attempts} start={snapshot.ShiftTrial.StartedAtTick} end={snapshot.ShiftTrial.CompletedAtTick}");
Console.WriteLine($"shiftReport available={snapshot.ShiftReport.Available} tick={snapshot.ShiftReport.CompletedAtTick} completed={snapshot.ShiftReport.CompletedDishes} shortages={snapshot.ShiftReport.ServiceShortages} route={snapshot.ShiftReport.BaselineRouteSteps}->{snapshot.ShiftReport.ValidatedRouteSteps}/{snapshot.ShiftReport.FinalRouteSteps} worker={snapshot.ShiftReport.WorkerActions} rework={snapshot.ShiftReport.TrayReworkIncidents} automation={snapshot.ShiftReport.AutomatedStarts}/{snapshot.ShiftReport.AutomationIncidents}/{snapshot.ShiftReport.PreventedUnsafeStarts}");
Console.WriteLine($"economy value={snapshot.Economy.ThroughputValue} labor={snapshot.Economy.LaborTicks}/{snapshot.Economy.LaborCost} staffing={snapshot.Economy.StaffedTicks}/{snapshot.Economy.StaffingCost} waste={snapshot.Economy.ReworkIncidents}/{snapshot.Economy.WasteCost} shortageDowntime={snapshot.Economy.ServiceShortages}/{snapshot.Economy.ShortageDowntimeCost} incidentDowntime={snapshot.Economy.AutomationIncidents}/{snapshot.Economy.IncidentDowntimeCost} investment={snapshot.Economy.FlowCellInvested}/{snapshot.Economy.InvestmentCost} total={snapshot.Economy.TotalCost} net={snapshot.Economy.NetValue}");
if (snapshot.ShiftReport.Available)
    Console.WriteLine($"scorecard value={snapshot.ShiftReport.Economy.ThroughputValue} total={snapshot.ShiftReport.Economy.TotalCost} net={snapshot.ShiftReport.Economy.NetValue}");
foreach (var quest in snapshot.Progression.Quests)
    Console.WriteLine($"  quest={quest.Id,-22} complete={quest.Complete,-5} progress={quest.Percent,3}% start={quest.StartedAtTick,4} end={quest.CompletedAtTick,4} activeTicks={quest.ElapsedTicks,4}");
Console.WriteLine($"newHire enabled={snapshot.NewHire.Enabled} flowDocumented={snapshot.NewHire.Specification.FlowDocumented} glassPriority={snapshot.NewHire.Specification.RushGlassPriorityDocumented} trayKnowledge={snapshot.NewHire.Specification.RareTrayHandlingDocumented} actions={snapshot.NewHire.ActionsCompleted} plateActions={snapshot.NewHire.PlateActions} glassActions={snapshot.NewHire.GlassActions} trayActions={snapshot.NewHire.TrayActions} trayRework={snapshot.NewHire.TrayReworkIncidents}");
Console.WriteLine($"layout={snapshot.Layout.Layout} estimatedRoute={snapshot.Layout.EstimatedRouteSteps} sandboxWalked={snapshot.Layout.SandboxMovementSteps} playerCell={snapshot.Layout.PlayerCell.X},{snapshot.Layout.PlayerCell.Y} baselineRoute={snapshot.Layout.BaselineRouteSteps} validatedRoute={snapshot.Layout.ValidatedRouteSteps} playerSteps={snapshot.Layout.PlayerTravelSteps} newHireSteps={snapshot.Layout.NewHireTravelSteps}");
Console.WriteLine($"placements scrape={Cell(snapshot.Layout.Placements.Scrape)} rack={Cell(snapshot.Layout.Placements.Rack)} washer={Cell(snapshot.Layout.Placements.Washer)} unload={Cell(snapshot.Layout.Placements.Unload)} dry={Cell(snapshot.Layout.Placements.DryRestock)} service={Cell(snapshot.Layout.Placements.Service)}");
Console.WriteLine($"automation enabled={snapshot.Automation.Policy.Enabled} interlock={snapshot.Automation.Policy.RequirePhysicalReady} reportedReady={snapshot.Automation.ReportedReady} physicalReady={snapshot.Automation.PhysicalReady} starts={snapshot.Automation.AutomatedStarts} incidents={snapshot.Automation.Incidents} prevented={snapshot.Automation.PreventedUnsafeStarts}");
Console.WriteLine($"automationRule id={snapshot.Automation.ActiveRule.Id} enabled={snapshot.Automation.ActiveRule.Enabled} draftOpen={snapshot.Automation.ActiveEdit is not null} evaluations={snapshot.Automation.RuleTrace.Count}");
if (snapshot.Automation.Comparison.LatestResult is { } comparison)
    Console.WriteLine($"automationCompare verdict={comparison.Verdict} seed={comparison.Baseline.Seed} horizon={comparison.Baseline.HorizonTicks} completed={comparison.Baseline.Metrics.Completed}->{comparison.Variant.Metrics.Completed} shortages={comparison.Baseline.Metrics.ServiceShortages}->{comparison.Variant.Metrics.ServiceShortages} incidents={comparison.Baseline.Metrics.UnsafeIncidents}->{comparison.Variant.Metrics.UnsafeIncidents} prevented={comparison.Baseline.Metrics.PreventedUnsafeStarts}->{comparison.Variant.Metrics.PreventedUnsafeStarts}");
foreach (var artifact in snapshot.ProcessCapture.Artifacts)
{
    Console.WriteLine($"process id={artifact.Id.Value} owner={artifact.Owner.Value} name={artifact.Name} baseline=v{artifact.Baseline.Version} current=v{artifact.Current.Version} applied={snapshot.ProcessCapture.AppliedArtifactId?.Value.ToString() ?? "no"} routing={artifact.Current.RoutingPolicy} source={artifact.Current.Provenance.Source} seed={artifact.Current.Provenance.WorldSeed} started={artifact.Current.Provenance.StartedAt.Value} completed={artifact.Current.Provenance.CompletedAt.Value} steps={artifact.Current.Steps.Length}");
    foreach (var step in artifact.Current.Steps)
        Console.WriteLine($"  step={step.Sequence} id={step.Id.Value} t={step.ObservedAt.Value} observedActor={step.Actor.Value} assignedActor={step.AssignedActor.Value} workstation={step.Workstation} action={step.Action} item={step.ItemKind} transition={step.InputState}->{step.OutputState}");
}
Console.WriteLine($"incident recorded={snapshot.Automation.Incident.Recorded} at={snapshot.Automation.Incident.OccurredAt.Value} replays={snapshot.Automation.Incident.ReplayCount} lastWouldStart={snapshot.Automation.Incident.LastReplayWouldStart} regression={snapshot.Automation.Incident.RegressionPassed}");
Console.WriteLine("Automation trace:");
foreach (var entry in snapshot.Automation.Trace)
{
    Console.WriteLine($"  t{entry.Tick.Value,3} {entry.Outcome,-20} policy={(entry.Policy.RequirePhysicalReady ? "safe" : entry.Policy.Enabled ? "reported" : "off"),-8} reported={entry.ReportedReady} physical={entry.PhysicalReady}");
}
Console.WriteLine("Notifications:");
foreach (var notification in world.Notifications)
{
    Console.WriteLine($"  t{notification.Tick.Value,3} {notification.Title}: {notification.Message}");
}

static string Cell(FloorCell cell) => $"{cell.X},{cell.Y}";

static void PrintEconomyChoice(DishStationEconomyChoiceResult choice) =>
    Console.WriteLine($"  choice={choice.Choice} viable={choice.Viable} layout={choice.Layout} completed={choice.CompletedDishes} shortages={choice.ServiceShortages} workerActions={choice.Economy.WorkerActions} workerTravel={choice.WorkerTravelSteps} value={choice.Economy.ThroughputValue} labor={choice.Economy.LaborCost} staffing={choice.Economy.StaffingCost} waste={choice.Economy.WasteCost} downtime={choice.Economy.DowntimeCost} investment={choice.Economy.InvestmentCost} total={choice.Economy.TotalCost} net={choice.Economy.NetValue}");

static void PrintRoutingTrial(string label, TwoStationRoutingTrialResult trial)
{
    Console.WriteLine($"  trial={label} sequence={trial.Sequence} completed={trial.TotalCompleted} shortages={trial.TotalShortages} net={trial.TotalNetValue}");
    foreach (var station in trial.Stations)
        Console.WriteLine($"    station={station.Station} name={station.DisplayName} demand={station.DemandKind} policy={station.Policy} completed={station.CompletedDishes} shortages={station.ServiceShortages} actions={station.WorkerActions} travel={station.WorkerTravelSteps} value={station.ThroughputValue} cost={station.TotalCost} net={station.NetValue}");
}

internal sealed record HeadlessOptions(
    int Seed,
    int Ticks,
    bool ScriptedDemo,
    bool SandboxDemo,
    bool CaptureDemo,
    bool ProcessEditorDemo,
    bool DialogueDemo,
    bool NarrativeDemo,
    bool ShowHelp,
    int BenchmarkActors,
    int BenchmarkTicks,
    DishStationScenarioConfiguration Scenario)
{
    public const string HelpText = """
        Automation.Headless options:
          --ticks N                     ticks to simulate (default 300)
          --seed N                      deterministic seed (default 42)
          --empty                       do not schedule the tutorial demo
          --sandbox-demo                place a compact custom floor and move the player headlessly
          --capture-demo                capture one manual plate workflow as a versioned process artifact
          --process-editor-demo         capture, validate, edit, apply, and rerun a glass-first process
          --benchmark-actors N          run the synthetic actor benchmark instead
          --benchmark-ticks N           benchmark ticks (default 100)
          --compile-content PATH        validate/normalize a schema-v1 YAML bundle and print its manifest
          --character-roster-demo       print the named first-shift cast and quest participant mappings
          --dialogue-demo               run the first shift and print resolved contextual character barks
          --narrative-demo              print the complete authored first-shift narrative and outcome proof
          --expand-template PATH        expand/compile a template-v1 YAML file and print provenance hashes
          --automation-ir-demo          evaluate and print reported/safe washer-rule traces headlessly
          --automation-editor-demo      create, apply, trace, refine, and replay one player washer rule
          --automation-compare-demo     compare baseline/variant rules in identical authoritative trials
          --economy-compare-demo        compare staffed linear/flow-cell choices over the same 120-tick shift
          --two-station-demo            copy, refit, and compare routing policies across two restaurant stations
          --pattern-knowledge-demo      recognize, persist, and print the pre-name restaurant Codex evidence
          --pattern-naming-demo         reflect, name Strategy, and print its structure/tradeoffs after resume
          --vendor-demo                 compare build/managed/observable sourcing under one boundary incident
          --named-seed NAME             required by templates with declared variable fields
          --parameter NAME=VALUE        supply a template parameter; repeat for additional parameters
          --initial-plates N            initial dirty plates
          --initial-glasses N           initial dirty glasses
          --initial-trays N             initial dirty trays
          --clean-plates N              initial available plates
          --clean-glasses N             initial available glasses
          --clean-trays N               initial available trays
          --arrival-interval N          ticks between dirty arrivals
          --glass-every N               one glass per N arrivals
          --rack-capacity N             maximum staged dishes
          --washer-cycle N              washer cycle ticks
          --worker-interval N           linear-layout worker action interval
          --flow-worker-interval N      U-cell worker action interval
          --sticky-after N              sticky ready after N automated starts; 0 disables
          --fault-permille N            deterministic sticky-ready risk per start, 0..1000
          --demand-kind Plate|Glass|Tray
          --demand-interval N           ticks between rush requests
          --rush                        begin with demand enabled
          --worker-enabled              begin with the new hire enabled
          --knowledge none|happy|rush|full
          --automation off|reported|safe
          --layout linear|cell

        Use --empty when changing timings for a free-running scenario; the scripted demo is timed for defaults.
        """;

    public static HeadlessOptions Parse(string[] args, DishStationScenarioConfiguration authoredScenario)
    {
        ArgumentNullException.ThrowIfNull(authoredScenario);
        var seed = ReadInt(args, "--seed", 42);
        var ticks = ReadInt(args, "--ticks", 300);
        var sandboxDemo = args.Contains("--sandbox-demo", StringComparer.OrdinalIgnoreCase);
        var processEditorDemo = args.Contains("--process-editor-demo", StringComparer.OrdinalIgnoreCase);
        var captureDemo = processEditorDemo || args.Contains("--capture-demo", StringComparer.OrdinalIgnoreCase);
        var dialogueDemo = args.Contains("--dialogue-demo", StringComparer.OrdinalIgnoreCase);
        var narrativeDemo = args.Contains("--narrative-demo", StringComparer.OrdinalIgnoreCase);
        var demo = !sandboxDemo && !captureDemo && !args.Contains("--empty", StringComparer.OrdinalIgnoreCase);
        var knowledge = ReadString(args, "--knowledge", KnowledgeToken(authoredScenario.InitialNewHireSpecification)).ToLowerInvariant() switch
        {
            "happy" => DishProcessSpecification.HappyPath,
            "rush" => DishProcessSpecification.RushAware,
            "full" => DishProcessSpecification.FullyDocumented,
            "none" => default,
            var value => throw new ArgumentException($"Unknown knowledge profile '{value}'."),
        };
        var automation = ReadString(args, "--automation", AutomationToken(authoredScenario.InitialAutomationPolicy)).ToLowerInvariant() switch
        {
            "off" => WasherAutomationPolicy.Off,
            "reported" => WasherAutomationPolicy.ReportedReadyOnly,
            "safe" => WasherAutomationPolicy.CorroboratedReady,
            var value => throw new ArgumentException($"Unknown automation policy '{value}'."),
        };
        var layout = ReadString(args, "--layout", LayoutToken(authoredScenario.InitialLayout)).ToLowerInvariant() switch
        {
            "linear" => DishStationLayout.Linear,
            "cell" => DishStationLayout.UShapedCell,
            var value => throw new ArgumentException($"Unknown layout '{value}'."),
        };
        var scenario = (authoredScenario with
        {
            InitialDirty = new(
                ReadInt(args, "--initial-plates", authoredScenario.InitialDirty.Plates),
                ReadInt(args, "--initial-glasses", authoredScenario.InitialDirty.Glasses),
                ReadInt(args, "--initial-trays", authoredScenario.InitialDirty.Trays)),
            InitialAvailable = new(
                ReadInt(args, "--clean-plates", authoredScenario.InitialAvailable.Plates),
                ReadInt(args, "--clean-glasses", authoredScenario.InitialAvailable.Glasses),
                ReadInt(args, "--clean-trays", authoredScenario.InitialAvailable.Trays)),
            ArrivalIntervalTicks = ReadInt(args, "--arrival-interval", authoredScenario.ArrivalIntervalTicks),
            GlassEveryArrivals = ReadInt(args, "--glass-every", authoredScenario.GlassEveryArrivals),
            RackCapacity = ReadInt(args, "--rack-capacity", authoredScenario.RackCapacity),
            WasherCycleTicks = ReadInt(args, "--washer-cycle", authoredScenario.WasherCycleTicks),
            WorkerActionIntervalTicks = ReadInt(args, "--worker-interval", authoredScenario.WorkerActionIntervalTicks),
            FlowCellWorkerActionIntervalTicks = ReadInt(args, "--flow-worker-interval", authoredScenario.FlowCellWorkerActionIntervalTicks),
            StickyReadyFaultAfterAutomatedStarts = ReadInt(args, "--sticky-after", authoredScenario.StickyReadyFaultAfterAutomatedStarts),
            StickyReadyFaultPermillePerStart = ReadInt(args, "--fault-permille", authoredScenario.StickyReadyFaultPermillePerStart),
            DemandKind = ReadEnum(args, "--demand-kind", authoredScenario.DemandKind),
            DemandIntervalTicks = ReadInt(args, "--demand-interval", authoredScenario.DemandIntervalTicks),
            InitialRushEnabled = authoredScenario.InitialRushEnabled || args.Contains("--rush", StringComparer.OrdinalIgnoreCase),
            InitialNewHireEnabled = authoredScenario.InitialNewHireEnabled || args.Contains("--worker-enabled", StringComparer.OrdinalIgnoreCase),
            InitialNewHireSpecification = knowledge,
            InitialAutomationPolicy = automation,
            InitialLayout = layout,
        }).Validate();
        return new(
            seed,
            ticks,
            demo,
            sandboxDemo,
            captureDemo,
            processEditorDemo,
            dialogueDemo,
            narrativeDemo,
            args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase),
            ReadInt(args, "--benchmark-actors", 0),
            ReadInt(args, "--benchmark-ticks", 100),
            scenario);
    }

    private static string KnowledgeToken(DishProcessSpecification value) => value switch
    {
        { FlowDocumented: false, RushGlassPriorityDocumented: false, RareTrayHandlingDocumented: false } => "none",
        { FlowDocumented: true, RushGlassPriorityDocumented: false, RareTrayHandlingDocumented: false } => "happy",
        { FlowDocumented: true, RushGlassPriorityDocumented: true, RareTrayHandlingDocumented: false } => "rush",
        { FlowDocumented: true, RushGlassPriorityDocumented: true, RareTrayHandlingDocumented: true } => "full",
        _ => throw new ArgumentException("Authored new-hire knowledge does not map to a supported CLI profile."),
    };

    private static string AutomationToken(WasherAutomationPolicy value) => value switch
    {
        { Enabled: false } => "off",
        { Enabled: true, RequirePhysicalReady: false } => "reported",
        { Enabled: true, RequirePhysicalReady: true } => "safe",
    };

    private static string LayoutToken(DishStationLayout value) => value switch
    {
        DishStationLayout.Linear => "linear",
        DishStationLayout.UShapedCell => "cell",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static int ReadInt(string[] args, string name, int fallback)
    {
        var index = Array.FindIndex(args, value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value)
            ? value
            : fallback;
    }

    private static string ReadString(string[] args, string name, string fallback)
    {
        var index = Array.FindIndex(args, value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }

    private static T ReadEnum<T>(string[] args, string name, T fallback) where T : struct, Enum
    {
        var value = ReadString(args, name, fallback.ToString());
        return Enum.TryParse<T>(value, true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {name} value '{value}'.");
    }
}
