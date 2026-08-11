# Content Authoring

## Goal

Designers should be able to create industries, scenarios, quests, process definitions, incidents, skills, and educational discoveries without recompiling the core simulation for ordinary content changes.

## Content types

```text
content/
  industries/
  organizations/
  facilities/
  roles/
  jobs/
  processes/
  scenarios/
  quests/
  incidents/
  abilities/
  skills/
  patterns/
  assets/
```

## Format

Start with YAML for hand-authored definitions because it is readable and diff-friendly. Compile to validated runtime structures during build/content import.

JSON remains a supported interchange/debug format.

## Example quest

```yaml
id: restaurant.dishstation.glass-shortage
scenario: restaurant.dishstation.01
kind: improvement
condition:
  metric: clean_glasses_available
  during: dinner_rush
  below: 12
  sustained_for: 120s
objective:
  description: Keep clean glasses available during dinner service.
constraints:
  - customer_wait_time_must_not_increase
teaches:
  - queues
  - bottlenecks
  - measurement
```

## Behavior hooks

Content definitions reference registered capabilities rather than arbitrary type names.

```yaml
decision: inventory.route_by_temperature
```

A content registry resolves stable IDs to implementation behavior.

Avoid allowing content files to instantiate arbitrary CLR types.

## Validation

Content compilation should fail on:

- duplicate IDs;
- missing references;
- impossible state transitions;
- unknown capabilities;
- missing localization keys;
- invalid asset references;
- contradictory prerequisites;
- progression cycles where forbidden.

## Scenario authoring workflow

1. Write the real-world story.
2. Identify learning target.
3. Define baseline process.
4. Define hidden conditions/unknowns.
5. Define measurable consequences.
6. Define fair discovery paths.
7. Define optional player solutions, not one mandatory solution.
8. Define validation criteria.
9. Run headless simulations across seeds.
10. Playtest without revealing lesson title.

## Content must not become code-shaped requirements

A quest should describe a condition such as “refunds above the employee threshold require approval,” not “implement `RefundGuard`.” Implementation concepts may be discovered later.

## Versioning

Every content definition has:

- stable ID;
- schema version where needed;
- optional migration logic;
- source/provenance metadata.

Saved games reference stable IDs and must tolerate content evolution through migration rules.
