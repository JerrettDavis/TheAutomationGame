# Contributing

Thank you for helping build The Automation Game. The project teaches systems thinking through observable simulated consequences, so contributions should preserve that learning model as well as technical correctness.

## Before changing code

Read [AGENTS.md](AGENTS.md), the [architecture](docs/06_ARCHITECTURE.md), and the nearest authoritative design document. New architectural decisions belong in `docs/adr/`; update an existing authoritative document instead of creating a disconnected note.

## Development setup

Requirements:

- Git with Git LFS;
- the .NET SDK selected by `global.json`;
- Windows for the Stride client and native UI smoke test.

```powershell
git lfs install
dotnet restore TheAutomationGame.sln
dotnet build TheAutomationGame.sln -c Release
dotnet test TheAutomationGame.sln -c Release --no-build
```

For presentation changes, also run:

```powershell
.\tools\ui-smoke.ps1 -AllowDesktopInput
```

The native smoke test drives the real Windows client and should not be substituted with a narrower renderer-only assertion. It takes exclusive control of the shared cursor and must run only while the desktop is idle; the explicit switch prevents accidental execution.

## Pull requests

- Keep the simulation authoritative and Stride-free in `Automation.Domain` and `Automation.Simulation`.
- Route consequential player actions through explicit commands or application services.
- Add a headless validation path for major simulation behavior.
- Preserve discovery: authored scenarios describe observable outcomes, not required button sequences.
- Include tests and update the nearest documentation or ADR.
- State what was validated and call out any human-playtest requirement that automation cannot prove.

Keep pull requests focused. Do not include generated build output, local saves, playtest artifacts, credentials, or unrelated formatting changes.
