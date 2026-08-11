using Automation.Content;
using Automation.Domain;
using Automation.Simulation;
using Automation.Tools;

var options = HeadlessOptions.Parse(args);
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
{
    world.Schedule(new PerformDishActionCommand(new(1), DishAction.Scrape, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(2), DishAction.Rack, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(3), DishAction.StartWasher, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(24), DishAction.Unload, DishKind.Plate));
    world.Schedule(new PerformDishActionCommand(new(25), DishAction.DryAndRestock, DishKind.Plate));
    world.Schedule(new SetRushCommand(new(26), true));
    world.Schedule(new InspectProcessCommand(new(31)));
    world.Schedule(new ConfirmBottleneckCommand(new(32), DishState.Dirty));
    world.Schedule(new ConfigureDishStationLayoutCommand(new(33), DishStationLayout.UShapedCell));
    world.Schedule(new PerformDishActionCommand(new(33), DishAction.Scrape, DishKind.Glass));
    world.Schedule(new PerformDishActionCommand(new(34), DishAction.Rack, DishKind.Glass));
    world.Schedule(new PerformDishActionCommand(new(35), DishAction.StartWasher, DishKind.Glass));
    world.Schedule(new PerformDishActionCommand(new(56), DishAction.Unload, DishKind.Glass));
    world.Schedule(new PerformDishActionCommand(new(57), DishAction.DryAndRestock, DishKind.Glass));
    world.Schedule(new SetNewHireEnabledCommand(new(61), true));
    world.Schedule(new TrainNewHireCommand(new(62), DishProcessSpecification.HappyPath));
    world.Schedule(new TrainNewHireCommand(new(76), DishProcessSpecification.RushAware));
    world.Schedule(new TrainNewHireCommand(new(158), DishProcessSpecification.FullyDocumented));
    world.Schedule(new ConfigureWasherAutomationCommand(new(200), WasherAutomationPolicy.ReportedReadyOnly));
    world.Schedule(new InspectAutomationIncidentCommand(new(241)));
    world.Schedule(new ReplayAutomationIncidentCommand(new(242)));
    world.Schedule(new ConfigureWasherAutomationCommand(new(243), WasherAutomationPolicy.CorroboratedReady));
    world.Schedule(new ReplayAutomationIncidentCommand(new(244)));
    world.Schedule(new ConfigureDishSupplyCommand(new(245), DishState.Available, DishKind.Glass, 3));
    world.Schedule(new StartShiftTrialCommand(new(245)));
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
foreach (var quest in snapshot.Progression.Quests)
    Console.WriteLine($"  quest={quest.Id,-22} complete={quest.Complete,-5} progress={quest.Percent,3}% start={quest.StartedAtTick,4} end={quest.CompletedAtTick,4} activeTicks={quest.ElapsedTicks,4}");
Console.WriteLine($"newHire enabled={snapshot.NewHire.Enabled} flowDocumented={snapshot.NewHire.Specification.FlowDocumented} glassPriority={snapshot.NewHire.Specification.RushGlassPriorityDocumented} trayKnowledge={snapshot.NewHire.Specification.RareTrayHandlingDocumented} actions={snapshot.NewHire.ActionsCompleted} plateActions={snapshot.NewHire.PlateActions} glassActions={snapshot.NewHire.GlassActions} trayActions={snapshot.NewHire.TrayActions} trayRework={snapshot.NewHire.TrayReworkIncidents}");
Console.WriteLine($"layout={snapshot.Layout.Layout} estimatedRoute={snapshot.Layout.EstimatedRouteSteps} sandboxWalked={snapshot.Layout.SandboxMovementSteps} playerCell={snapshot.Layout.PlayerCell.X},{snapshot.Layout.PlayerCell.Y} baselineRoute={snapshot.Layout.BaselineRouteSteps} validatedRoute={snapshot.Layout.ValidatedRouteSteps} playerSteps={snapshot.Layout.PlayerTravelSteps} newHireSteps={snapshot.Layout.NewHireTravelSteps}");
Console.WriteLine($"placements scrape={Cell(snapshot.Layout.Placements.Scrape)} rack={Cell(snapshot.Layout.Placements.Rack)} washer={Cell(snapshot.Layout.Placements.Washer)} unload={Cell(snapshot.Layout.Placements.Unload)} dry={Cell(snapshot.Layout.Placements.DryRestock)} service={Cell(snapshot.Layout.Placements.Service)}");
Console.WriteLine($"automation enabled={snapshot.Automation.Policy.Enabled} interlock={snapshot.Automation.Policy.RequirePhysicalReady} reportedReady={snapshot.Automation.ReportedReady} physicalReady={snapshot.Automation.PhysicalReady} starts={snapshot.Automation.AutomatedStarts} incidents={snapshot.Automation.Incidents} prevented={snapshot.Automation.PreventedUnsafeStarts}");
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

internal sealed record HeadlessOptions(
    int Seed,
    int Ticks,
    bool ScriptedDemo,
    bool SandboxDemo,
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
          --benchmark-actors N          run the synthetic actor benchmark instead
          --benchmark-ticks N           benchmark ticks (default 100)
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

    public static HeadlessOptions Parse(string[] args)
    {
        var seed = ReadInt(args, "--seed", 42);
        var ticks = ReadInt(args, "--ticks", 300);
        var sandboxDemo = args.Contains("--sandbox-demo", StringComparer.OrdinalIgnoreCase);
        var demo = !sandboxDemo && !args.Contains("--empty", StringComparer.OrdinalIgnoreCase);
        var knowledge = ReadString(args, "--knowledge", "none").ToLowerInvariant() switch
        {
            "happy" => DishProcessSpecification.HappyPath,
            "rush" => DishProcessSpecification.RushAware,
            "full" => DishProcessSpecification.FullyDocumented,
            "none" => default,
            var value => throw new ArgumentException($"Unknown knowledge profile '{value}'."),
        };
        var automation = ReadString(args, "--automation", "off").ToLowerInvariant() switch
        {
            "off" => WasherAutomationPolicy.Off,
            "reported" => WasherAutomationPolicy.ReportedReadyOnly,
            "safe" => WasherAutomationPolicy.CorroboratedReady,
            var value => throw new ArgumentException($"Unknown automation policy '{value}'."),
        };
        var layout = ReadString(args, "--layout", "linear").ToLowerInvariant() switch
        {
            "linear" => DishStationLayout.Linear,
            "cell" => DishStationLayout.UShapedCell,
            var value => throw new ArgumentException($"Unknown layout '{value}'."),
        };
        var scenario = new DishStationScenarioConfiguration
        {
            InitialDirty = new(
                ReadInt(args, "--initial-plates", 6),
                ReadInt(args, "--initial-glasses", 2),
                ReadInt(args, "--initial-trays", 0)),
            InitialAvailable = new(
                ReadInt(args, "--clean-plates", 0),
                ReadInt(args, "--clean-glasses", 0),
                ReadInt(args, "--clean-trays", 0)),
            ArrivalIntervalTicks = ReadInt(args, "--arrival-interval", 30),
            GlassEveryArrivals = ReadInt(args, "--glass-every", 3),
            RackCapacity = ReadInt(args, "--rack-capacity", 12),
            WasherCycleTicks = ReadInt(args, "--washer-cycle", 20),
            WorkerActionIntervalTicks = ReadInt(args, "--worker-interval", 5),
            FlowCellWorkerActionIntervalTicks = ReadInt(args, "--flow-worker-interval", 4),
            StickyReadyFaultAfterAutomatedStarts = ReadInt(args, "--sticky-after", 2),
            StickyReadyFaultPermillePerStart = ReadInt(args, "--fault-permille", 0),
            DemandKind = ReadEnum(args, "--demand-kind", DishKind.Glass),
            DemandIntervalTicks = ReadInt(args, "--demand-interval", 15),
            InitialRushEnabled = args.Contains("--rush", StringComparer.OrdinalIgnoreCase),
            InitialNewHireEnabled = args.Contains("--worker-enabled", StringComparer.OrdinalIgnoreCase),
            InitialNewHireSpecification = knowledge,
            InitialAutomationPolicy = automation,
            InitialLayout = layout,
        }.Validate();
        return new(
            seed,
            ticks,
            demo,
            sandboxDemo,
            args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase),
            ReadInt(args, "--benchmark-actors", 0),
            ReadInt(args, "--benchmark-ticks", 100),
            scenario);
    }

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
