---
name: refactor
description: This skill should be used when the user asks to refactor code, do a refactor pass, clean up code, extract a class or service, inject a dependency, or make incremental improvements to existing code structure.
version: 1.0.0
---

# Refactoring in Glue

See [REFACTORING.md](../../../REFACTORING.md) for the full philosophy, checklist, and status of ongoing refactoring efforts.

## Core principle

Incremental is good. A small step in the right direction — even if it doesn't finish the job — is valuable. You do not need to complete an entire refactor in one pass. Leave the code measurably better without rewriting everything at once.

## What this means in practice

- A partial extraction (e.g., moving 3 methods into a new service) is a valid, shippable change.
- A forwarding stub that preserves existing call sites while introducing a seam is a valid, shippable change.
- If a full fix requires touching too many things at once, find the smallest safe slice and do that.
- Zero behavior change is the goal for each individual refactoring step. New behavior comes after.
