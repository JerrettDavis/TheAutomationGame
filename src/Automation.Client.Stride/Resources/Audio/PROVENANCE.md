# S013 procedural audio cues

- Creator: project-authored deterministic synthesis in `tools/generate-audio-assets.ps1`
- Source: no third-party recording or sample source
- License: project MIT license
- Imported assets: `Assets/Audio/*.sdsnd`
- Runtime URLs: `Audio/DishRoomAmbience`, `Audio/Work`, `Audio/WasherStart`, `Audio/WasherComplete`, `Audio/Blocked`, `Audio/Failure`, `Audio/QuestSuccess`
- Format: mono PCM WAV, 22,050 Hz, 16-bit; compiled by Stride as non-spatialized in-memory sounds
- Shipping status: temporary production candidate
- Modified: generated directly at the listed parameters; rerun `tools/generate-audio-assets.ps1` to reproduce all source WAV files
- Notes: simple synthesized tones/hum deliberately avoid third-party licensing uncertainty. They establish routing, mix, accessibility, and replacement seams; later authored recordings may replace them without changing simulation identity.
