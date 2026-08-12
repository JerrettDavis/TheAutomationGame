# Content Definition Template

Use this as an authoring checklist. Exact schemas are defined by the implemented content compiler.

```yaml
schema_version: 1
id: <globally-stable-semantic-id>
display_name: "..."
tags: []
```

## References

List every stable ID this definition depends on and what type is expected.

## Parameters / authored values

Document gameplay-affecting parameters and units.

Avoid unlabeled magic numbers.

## Presentation

Reference a stable presentation ID, never a raw model path as domain identity.

## Determinism

If this definition can vary, state:

```yaml
template: template.<...>
template_version: 1
seed: <named explicit seed or scenario-derived seed>
```

List which fields may vary.

## Validation invariants

Examples:

- capacity > 0;
- referenced states exist;
- required process port exists;
- quest metric is registered;
- transition graph is valid;
- facility remains traversable.

## Authoring provenance

For generated content, preserve template/source provenance so diagnostics can point back to what the author actually wrote.
