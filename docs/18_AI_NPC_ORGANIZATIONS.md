# AI, NPCs, and Organizations

## NPC goal

NPCs must feel like participants in systems rather than decorative pathfinding agents.

They need enough behavior to:

- perform roles;
- make bounded decisions;
- wait/queue;
- communicate;
- improvise;
- learn local habits;
- transfer knowledge;
- react to bad systems;
- create workarounds.

## Behavior architecture

Start with utility/state-machine/task-planning hybrids rather than heavyweight generative AI for moment-to-moment simulation.

A worker chooses from available actions based on:

- assigned role;
- active work;
- capability;
- priority;
- local knowledge;
- policy;
- state;
- nearby resources;
- fatigue/pressure;
- exceptional conditions.

## Improvisation

Humans can handle situations not captured by the formal process. Improvisation can:

- save an outcome;
- create hidden tribal knowledge;
- violate intended policy;
- mask a broken automation;
- become a discoverable process improvement.

## Organizations

Organizations model:

- teams;
- roles;
- ownership;
- communication paths;
- policies;
- budgets;
- shared capabilities;
- review authority;
- vendor dependencies.

## Coordination cost

Features crossing many ownership boundaries incur delay/communication cost. This gives architecture/organizational structure mechanical consequences.

## AI assistant/contractor

A late-game AI assistant is intentionally competent.

It can produce:

- research summaries;
- proposed process models;
- automation rules;
- code;
- tests;
- architecture suggestions;
- incident hypotheses.

The player can request:

- assumptions;
- confidence;
- alternatives;
- evidence plan;
- failure modes.

Blind acceptance accelerates delivery but may create unowned assumptions.

## LLM integration

The shipped game must not require external LLM APIs for core simulation or authored campaign completion. Optional generative integrations can be explored later behind stable interfaces and offline fallbacks.
