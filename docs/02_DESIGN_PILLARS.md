# Design Pillars

These are non-negotiable design constraints. Features that conflict with them require an explicit ADR or design review.

## 1. Reality before automation

Every automation must be grounded in an observable process, resource, constraint, actor, or outcome. The player may automate prematurely, but the simulation must retain a richer underlying reality capable of exposing the consequences.

## 2. The model is not reality

Designed behavior and observed behavior can differ. Sensors lie, networks delay, humans improvise, machines degrade, vendors violate expectations, and rare combinations happen.

The game should routinely create situations where a formally correct automation is operationally wrong because the player's assumptions were incomplete.

## 3. The work may be a graph; the player's current problem should be narratable

Complex systems contain cross-cutting dependencies, but each active problem should be understandable as a bounded episode with an entry condition, interactions, decisions, and a terminal outcome.

Unknowns that prevent a coherent solution become explicit research, observation, or experimentation activities.

## 4. Experience before terminology

The player should encounter a problem before the game names the common solution.

Examples:

- varying algorithms -> Strategy;
- incompatible equipment -> Adapter;
- state-dependent behavior -> State;
- interested downstream systems -> Observer/Event;
- long-running multi-step recovery -> Saga;
- repeated transient failure -> Retry;
- cascading dependency failure -> Circuit Breaker/Bulkhead.

Pattern names belong in the codex after the player has experienced their utility.

## 5. Automation is a trade, not a score

There is no global "automation percentage" victory condition.

Good systems balance:

- throughput;
- cost;
- safety;
- quality;
- resilience;
- maintainability;
- flexibility;
- human experience;
- customer outcomes;
- organizational understanding.

## 6. Outsource doing; retain ownership of knowing

The player is encouraged to delegate implementation, operation, research, testing, and repetitive analysis. Delegation becomes dangerous when the player also delegates the definition of purpose, acceptance, invariants, and consequential tradeoffs without understanding them.

A system can be well implemented and still be wrong.

## 7. Humans are components, not defects

Humans bring judgment, ambiguity resolution, improvisation, context, empathy, and responsibility. Machines bring scale, speed, consistency, memory, and repeatability.

The game should reward appropriate allocation of responsibility rather than universal replacement of people.

## 8. Architecture must have consequences

Coupling, boundaries, retry policy, shared services, caches, queues, and contracts must affect gameplay through observable behavior: latency, blast radius, maintenance cost, coordination cost, recovery characteristics, and change difficulty.

## 9. Programming is an advanced representation

Visual and textual representations should converge on the same underlying concepts. Code should feel like a more compact and expressive interface to models the player already understands.

## 10. Debugging is narrative comparison

A bug or incident should expose:

```text
expected story
vs.
observed story
-> first meaningful divergence
```

Logs and traces exist to recover the story, not merely produce colored graphs.

## 11. Scale introduces new kinds of problems

The game should not merely increase numerical difficulty. Moving from workstation to department to facility to organization should introduce qualitatively new concerns: contracts, ownership, coordination, consistency, policy, governance, and platform tradeoffs.

## 12. Every abstraction has a cost

The player should experience both duplication debt and abstraction debt. Reuse is not automatically superior. Shared capabilities create coordination and blast-radius costs.

## 13. The simulation is authoritative

Presentation may simplify, interpolate, summarize, or hide detail. It must not become the source of truth for the simulated world.

## 14. Learning must survive sandbox play

Educational concepts must emerge from mechanics that remain interesting after the player already knows the terminology. The game should still be enjoyable as a systems sandbox without tutorials.

## 15. Failure is information

Failures should usually reveal something the player did not know, not simply deduct currency. Recovery should teach as much as construction.
