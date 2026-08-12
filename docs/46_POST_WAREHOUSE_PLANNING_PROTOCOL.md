# Post-Warehouse Planning Protocol

> How to convert the broad N7–N17 roadmap into the next fixed session tranche after S043 without redoing this design work.

## Why the fixed backlog stops at S043

Restaurant is the first concrete domain. Warehouse is the second. Until both are playable, many proposed "generic" systems are only hypotheses.

S043 is intentionally a planning boundary. It should reveal:

- which domain primitives genuinely recur;
- which UI/editor abstractions survive a second industry;
- how much YAML/template authoring actually costs;
- what presentation variation is needed;
- how pattern evidence behaves across domains;
- where performance/content tooling really hurts.

The next tranche should therefore be generated from **implemented evidence**, while the campaign and product destination remain fixed by the roadmap.

## Inputs to the planning session

Read only:

- `33_PRODUCT_ROADMAP.md`;
- completed evidence from `35_SESSION_BACKLOG.md`;
- S043 reuse audit;
- `40_CAMPAIGN_STORY_CHARACTERS_PERSONAS.md`;
- `40A_CHARACTER_ROSTER_AND_QUESTLINE_MATRIX.md`;
- `41_PATTERN_LEARNING_AND_PATTERNKIT.md`;
- `41A_PATTERNKIT_COVERAGE_MATRIX.md`;
- current gap audit.

## Output

Append the next 20–40 fixed sessions to `35_SESSION_BACKLOG.md`, normally covering:

1. N7 Pattern Learning foundation completion;
2. N8 Retail complete chapter;
3. any platform/presentation work proven necessary by two industries;
4. bounded side-specialization sessions that now have real mechanics.

Do not schedule Factory/Logistics implementation sessions in the same tranche unless Retail architecture is already substantially proven. Keep the work horizon short enough that code evidence can reshape later implementation order.

## Session-generation rules

Each generated session must:

- correspond to one named quest beat, player tool, content capability, or concrete architecture proof;
- have immediate automated/playable proof;
- list prerequisites;
- list explicit exclusions;
- avoid generic "framework" sessions;
- preserve one chapter's manual baseline before its abstraction;
- include the pattern's counterexample as a separate session when substantial;
- insert human playtest gates before declaring a chapter complete.

## Product roadmap stays stable unless evidence invalidates it

Do not reopen:

- embodied-first play;
- YAML authoring;
- deterministic bounded generation;
- main-story GoF coverage;
- PatternKit metadata boundary;
- restricted automation IR before code;
- multiplayer deferral;

without new concrete evidence and a decision-log update.

## Expected post-S043 tranche shape

A likely structure, to be finalized from repo truth:

```text
PATTERN FOUNDATION
  Strategy cross-domain evidence
  Codex history/reinforcement
  PatternKit catalog import/coverage report

RETAIL BASELINE
  facility/cast/manual checkout
  transaction/economy additions

RETAIL PATTERN ARC
  Adapter
  Decorator
  Facade
  Proxy
  Memento

RETAIL STRESS TEST
  promotion pileup
  peak-day composition

RETAIL HUMAN GATE

PLATFORM HARDENING DISCOVERED FROM 3 INDUSTRIES
```

## Final rule

Heavy design work is already captured in the campaign and pattern matrices. The planning session should convert those designs into implementable contracts based on current repository seams, not invent a new product direction.
