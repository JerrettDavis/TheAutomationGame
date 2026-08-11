# ADR-0005: Data-Driven Scenario and Content Definitions

- Status: Accepted for prototype
- Date: 2026-08-10

## Decision

Industries, scenarios, quests, roles, process definitions, skills, abilities, incidents, and most configuration are authored as validated data, initially YAML, referencing stable registered capabilities.

## Constraint

Content cannot instantiate arbitrary CLR types by name.

## Benefits

- rapid iteration;
- diffable content;
- future modding path;
- headless validation;
- less recompilation for design changes.
