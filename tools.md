# Tooling Notes

This file is intentionally short. Detailed tooling decisions live in ADRs and the implementation roadmap.

## Baseline

- C# 14 / .NET 10
- Stride 4.3 client
- xUnit or NUnit for unit/component tests; choose once project bootstrap is complete
- BenchmarkDotNet for microbenchmarks
- Git LFS for large binary source assets
- Blender for authored 3D assets
- Krita/Affinity/Photoshop-class raster tooling as available
- Audacity/Reaper-class audio tooling as available
- YAML or JSON for hand-authored game content, compiled/validated during build

## Tooling principle

No proprietary creation tool may become the only practical way to recover or modify a critical source asset. Retain original source files in documented, portable formats whenever possible.
