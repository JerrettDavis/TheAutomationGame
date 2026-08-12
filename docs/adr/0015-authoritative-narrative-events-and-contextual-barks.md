# ADR-0015: Authoritative Narrative Events and Contextual Barks

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S027 must let the named restaurant cast react to actual station conditions. Binding authored lines directly to client-observed counters would make presentation infer gameplay truth; putting dialogue prose inside simulation notifications would keep characters and localization coupled to authoritative code. A broad event bus or conversation system is not justified by three concrete reactions.

## Decision

The dish-station simulation records a closed set of typed narrative events at authoritative transitions: first-shift queue pressure, unsafe automation incident, and successful reliability window. Each event contains only its tick, semantic kind, and authoritative quest identity. Events are deterministic snapshot/replay evidence and contain no dialogue or presentation data.

Character content owns optional bark definitions. A bark has a globally stable `dialogue.` ID, participating quest ID, trigger kind, priority, cooldown ticks, and short authored line. Compilation validates the trigger/priority family, quest reference, speaker participation, uniqueness, cooldown, and length.

An engine-neutral content router filters eligible barks for an event, applies per-bark cooldown, chooses highest priority, and uses stable bark ID as the tie-breaker. The client resolves the resulting stable speaker ID through the character catalog and displays name, role, and line for a bounded duration. Loading/resetting initializes the observer at current event history, so old reactions are not replayed as new UI.

## Consequences

### Positive

- character reactions are causally tied to authoritative world transitions;
- prose remains authored content and can change without simulation edits;
- speaker identity survives renaming and presentation fallback;
- priority/cooldown behavior is deterministic and headlessly testable;
- replay reconstructs the same narrative event evidence.

### Negative

- v1 exposes only three trigger kinds and one-line barks;
- cooldown state is presentation/content-routing state, not persisted gameplay state;
- no branching conversation, response choice, localization key, voice, or spatial speaker behavior exists yet.

## Validation

- trigger and resolve Tessa for queue pressure, Devon for the automation incident, and Avery for shift success;
- reject malformed trigger/priority/cooldown, duplicate IDs, unknown quests, and nonparticipant speakers;
- prove priority ordering and cooldown boundaries;
- prove replay-equivalent narrative event sequences;
- resolve stable speaker identity into client name/role presentation;
- emit the complete three-bark sequence byte-identically in headless mode.

## Revisit when

- S028 revises full first-shift narrative transitions;
- branching dialogue or player responses become a scheduled capability;
- localization replaces inline authored lines with string resources;
- NPC location/availability becomes authoritative and affects who can speak.
