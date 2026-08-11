# Risk Register

| Risk | Why it matters | Early mitigation |
|---|---|---|
| Simulation scope explosion | Attempting to model everything prevents a playable game | Fidelity tiers; ontology review; one dish-station vertical slice |
| Educational game feels like coursework | Players optimize for answers instead of systems | Conditions, not prescribed solutions; consequences; sandbox-first mechanics |
| Stride ecosystem gaps | Smaller ecosystem may require custom tooling | Keep client thin; spike risky features early; own core simulation |
| C# performance | Large simulation may create GC/cache pressure | Headless benchmarks; data-oriented stores; frequency scheduling; profile first |
| One entity per object | Renderer/client collapses at scale | instancing, pooling, LOD/aggregation |
| Determinism becomes too costly | Parallelism/physics/content can create drift | deterministic core only for gameplay-significant outcomes; client non-authoritative |
| Ontology too generic | Game becomes abstract and flavorless | industry-specific content and visuals; permit specialized capabilities when justified |
| Ontology too bespoke | Every industry becomes a new game | primitive review gate; second-industry reuse test |
| Automation moralizes | Game becomes anti-tech or pro-tech propaganda | reward outcomes/tradeoffs, not automation percentage |
| AI lesson becomes dated | "AI makes dumb mistakes" ages poorly | make AI capable; focus on assumptions, ownership, validation |
| Too much hidden information feels unfair | Players perceive random punishment | discoverable evidence; causal incident reconstruction |
| Progression feels like menus | Concept unlocks lack experiential meaning | unlock after experienced pain/problem |
| Code layer feels bolted on | Programming becomes separate minigame | shared automation IR; code represents existing models |
| Asset workload too high | Many industries need huge visual libraries | stylized modular kits; shared rigs/materials; asset reuse; phased fidelity |
| Licensing/provenance contamination | Commercial distribution becomes risky | asset manifest; source metadata; permissive/open tooling where possible |
| Modding security | arbitrary code introduces abuse/compat issues | data mods first; defer executable scripting decision |
| Save migrations become fragile | long-lived sandbox saves are valuable | explicit schema versions and migration tests from early builds |
| Feature creep from realism | accurate simulation can reduce fun/readability | simulate only variables that create meaningful decisions |

## Risk review cadence

Revisit at each roadmap phase exit. New technical dependencies require an ADR and corresponding risk entry when they materially change ownership or portability.
