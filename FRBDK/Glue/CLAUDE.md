# FlatRedBall Editor (Glue) — Claude Context

This directory contains the FlatRedBall (FRB) Editor, also known as "Glue". It is a large, long-lived project that has grown organically over many years, so code organization is often inconsistent or outdated.

## Refactoring Approach

See [REFACTORING.md](REFACTORING.md) for the full refactoring philosophy, checklist, and progress notes.

Key principles:
- Preserve existing functionality — do not break things while improving them
- Verify changes with unit tests
- When starting a new feature, do an incremental refactor pass first to move the affected code in the right direction before adding new behavior
