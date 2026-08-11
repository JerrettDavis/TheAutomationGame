## Outcome

Describe the player/system outcome this change enables.

## Simulation and architecture

- [ ] Consequential changes enter through commands or application services.
- [ ] Domain and simulation code remain independent of Stride.
- [ ] New simulation primitives received ontology review, or none were added.
- [ ] Hidden conditions remain discoverable and causally explainable.

## Validation

- [ ] `dotnet build TheAutomationGame.sln -c Release`
- [ ] `dotnet test TheAutomationGame.sln -c Release --no-build`
- [ ] Headless representative scenario
- [ ] Native UI smoke test when presentation changed
- [ ] Human validation requirement documented when automation is insufficient

List the exact commands and relevant results:

## Documentation

- [ ] Updated the nearest authoritative document or ADR, or documentation was not affected.
