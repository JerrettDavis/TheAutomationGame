# Product Vision

## Working title

**The Automation Game**

## One-sentence pitch

A simulation sandbox where the player starts by doing ordinary work, learns how real processes behave, and gradually gains the ability to define, delegate, mechanize, automate, program, and architect increasingly large systems without losing human ownership of what those systems are for.

## Product thesis

Most programming games begin with formal logic and teach the player how to command a machine. This game begins with reality and teaches the player how to **understand a system before trying to automate it**.

The player learns through consequences rather than lectures. A missing requirement may not fail immediately. It may produce a faster, cheaper system that quietly makes one rare but catastrophic mistake. A beautiful abstraction may make the next three implementations easier and the fourth almost impossible. Outsourcing may improve implementation quality while reducing organizational understanding. Excess efficiency may remove the resilience required to survive a disruption.

The game should make these dynamics visible, manipulable, and enjoyable.

## Player fantasy

The long-form fantasy is increasing leverage:

```text
One hour of labor
  -> one hour of output

One hour improving a process
  -> ten workers save an hour per day

One hour improving an automation
  -> one hundred facilities save an hour per day

One hour improving a platform
  -> thousands of processes inherit a capability
```

Leverage amplifies mistakes at the same time:

```text
manual mistake      -> one bad outcome
automated mistake   -> thousands of bad outcomes
platform mistake    -> many systems inherit the defect
organizational error -> the company optimizes the wrong goal
```

The player therefore earns greater power only by learning how to preserve intent, evidence, resilience, and accountability at greater scale.

## Intended audience

Primary:

- curious players who enjoy Factorio, Satisfactory, The Sims, Cities: Skylines, Zachtronics, management games, automation sandboxes, or programming-adjacent games;
- developers and technical professionals who want a systems-oriented sandbox;
- learners who are intimidated by traditional computer-science-first programming instruction.

Secondary:

- educators teaching systems thinking, software design, operations, or process analysis;
- organizations using scenarios for professional development;
- modders who want a programmable simulation platform.

## What the game teaches

The game should create intuitive familiarity with:

- observation and requirements discovery;
- process mapping;
- actors, states, events, decisions, effects, and outcomes;
- queues, bottlenecks, throughput, latency, and utilization;
- interfaces and contracts;
- bounded responsibility and decomposition;
- state machines;
- validation and testing;
- failure, retry, timeout, duplicate handling, reconciliation, and recovery;
- human-in-the-loop systems;
- design patterns as discovered solutions;
- distributed systems and organizational boundaries;
- maintainability, coupling, cohesion, and abstraction tradeoffs;
- telemetry, tracing, and debugging;
- outsourcing, delegation, and retained ownership;
- programming as an expressive representation of already-understood behavior.

## What the game is not

The game is not primarily:

- a syntax tutor;
- a LeetCode/algorithm puzzle collection;
- a logic-gate or microprocessor simulator;
- a factory game with educational labels pasted onto standard recipes;
- an ideological argument that automation is inherently good or bad;
- a single prescribed software methodology.

## Desired emotional arc

1. **Competence** — “I learned this job.”
2. **Curiosity** — “Why are we doing it this way?”
3. **Leverage** — “I made this easier.”
4. **Confidence** — “I can describe why it works.”
5. **Hubris** — “I can automate all of this.”
6. **Consequence** — “My model missed reality.”
7. **Understanding** — “I need to validate assumptions and preserve escape hatches.”
8. **Scale** — “I can compose systems.”
9. **Ownership** — “Others can do the work while I still understand what must remain true.”
10. **Stewardship** — “The hardest decision is what should be automated at all.”

## North-star experience

A player should eventually be able to click a production incident and traverse both directions:

```text
Observed failure
  -> runtime trace
    -> system interaction
      -> automation decision
        -> process rule
          -> documented assumption
            -> observed business need
```

and:

```text
Business need
  -> process
    -> interaction
      -> contract
        -> automation
          -> implementation
            -> runtime evidence
```

The game should make that continuity feel natural rather than academic.
