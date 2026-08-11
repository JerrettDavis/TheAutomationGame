# ADR-0001: Use Stride as Initial Client Engine

- Status: Accepted for prototype
- Date: 2026-08-10

## Context

The project needs a high-performance C#-friendly 2D/3D engine without becoming dependent on proprietary engine licensing. The simulation itself should remain portable.

## Decision

Use Stride 4.3 as the initial rendering/input/audio/editor client on .NET 10/C# 14.

## Consequences

Positive:

- native modern C#/.NET workflow;
- MIT-licensed engine source;
- 3D and 2D capabilities;
- Game Studio asset/scene tooling;
- standard C# solution structure.

Negative:

- smaller ecosystem than Unity/Godot;
- some tooling may need to be built in-house;
- platform support must be validated against project needs.

## Constraint

Simulation/domain libraries never reference Stride.

## Revisit when

- prototype performance blocks progress;
- required platform is unsupported;
- tooling limitations create disproportionate cost;
- project needs cannot be solved with a thin client adapter.
