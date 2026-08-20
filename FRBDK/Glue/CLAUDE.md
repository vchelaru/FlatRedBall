# FlatRedBall Editor (Glue) — Claude Context

This directory contains the FlatRedBall (FRB) Editor, also known as "Glue". It is a large, long-lived project that has grown organically over many years, so code organization is often inconsistent or outdated.

## User-facing strings are hardcoded

Glue is not localized and never will be. `FRBDK/Localization` (`L.Texts.*`, the `Texts*.resx` files) is
deprecated and being stripped out: write new user-facing text as a plain string literal, and when you
touch a line that reads `L.Texts.Something`, inline the English text instead of leaving it or adding a
new resource.

## Behavioral Fixes Need a Test, Not Just a Manual Retest

A fix to Glue behavior (RefreshManager, CodeGeneration, etc.) isn't done until a `GlueUnitTests` test
exercises it — one that fails against the old code and passes against the new. Don't substitute "it
compiles" plus asking the user to manually rebuild/retest for this.

## Refactoring Approach

See [REFACTORING.md](REFACTORING.md) for the full refactoring philosophy, checklist, and progress notes.

Key principles:
- Preserve existing functionality — do not break things while improving them
- Verify changes with unit tests
- When starting a new feature, do an incremental refactor pass first to move the affected code in the right direction before adding new behavior
