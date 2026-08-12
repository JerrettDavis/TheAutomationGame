# Automation, Outsourcing, and Ownership

## Central principle

> Delegate the doing. Own the knowing.

The game should never teach that all work must be personally performed. The goal is leverage. The player should be able to outsource almost everything while retaining intellectual ownership of purpose, constraints, acceptance, assumptions, and consequential decisions.

## Delegation model

Delegation can include:

- employee assignment;
- specialist consultation;
- contractor implementation;
- vendor product purchase;
- internal team allocation;
- AI-assisted research;
- AI-generated implementation;
- automated operations.

## Definition quality

Outsourced quality depends partly on what the player provides.

Inputs may include:

- desired outcome;
- observed current process;
- examples;
- invariants;
- boundary conditions;
- known unknowns;
- interface constraints;
- acceptance evidence.

A highly capable implementer should be able to produce an excellent implementation of an incorrectly defined problem.

## Assumption injection

When required information is missing, an outsourced actor can:

- ask the player;
- research;
- use organizational defaults;
- infer an assumption;
- proceed with uncertainty.

The player can trade speed for understanding.

Repeatedly selecting "use recommended default" should create hidden assumptions, not an arbitrary debuff.

## Organizational understanding

Track whether important behavior is understood by:

- individual workers;
- player;
- documentation/specification;
- team;
- vendor only;
- automation only through opaque implementation.

Understanding affects incident response and safe change.

## Automation debt

Automation debt exists when responsibility has been automated faster than the organization has learned, specified, validated, or retained ownership of that responsibility.

Symptoms:

- nobody knows why a rule exists;
- behavior cannot be safely changed;
- tests prove implementation but not intent;
- vendor departure creates paralysis;
- operators use workarounds nobody modeled;
- a generated system cannot be regenerated confidently.

## Repaying automation debt

Activities:

- observe current reality;
- interview veteran staff;
- capture runtime traces;
- characterize behavior;
- identify invariants;
- reconstruct decisions;
- write/repair specifications;
- add evidence;
- replace opaque dependencies deliberately.

## AI implementation

AI should be genuinely useful. The lesson is not "AI makes mistakes." The lesson is that capable execution does not remove the need for human ownership.

A strong AI assistant can:

- generate implementation;
- suggest architecture;
- create tests;
- analyze traces;
- optimize schedules;
- identify patterns.

Expert use means interrogating assumptions and validating results against owned definitions.

## Authorship

The game treats authorship as intellectual ownership rather than keystroke count.

A player can own code they did not type if they can explain:

- why it exists;
- what outcome it serves;
- what must remain true;
- what assumptions it depends on;
- how failure is handled;
- what evidence shows it works;
- when it should be changed.

## Current restaurant implementation

S033 implements one bounded **Buy the Box** side arc after Strategy is named. Sam Rivera offers a mature routing/monitoring package, while the player may also keep the work in house. Three authored proposal bundles make different commitments about sourcing, integration boundary, support response, trace visibility, manual fallback, recurring/setup/maintenance cost, and who understands the boundary.

Every trial receives the same eight-request horizon and the same rare-tray mismatch: the restaurant emits `exception`, while the package contract expects `special`. The vendor behaves according to its defined interface; Sam is not made arbitrarily incompetent. `VendorOutsourcingWorld` reports normal cost/net, handled and missed requests, shortage and fallback costs, incident net, support timing, and a causal trace. In-house, managed-vendor, and observable-adapter choices all remain positive-net, but move downtime, labor, cost, and organizational understanding differently.

The client comparison board sends only explicit select/run commands and presents authoritative snapshots. At least two completed proposals establish the side-arc outcome, with no universally correct contract. Career schema 3 persists the command journal and exact traces; schema-2 careers begin the optional arc empty.
