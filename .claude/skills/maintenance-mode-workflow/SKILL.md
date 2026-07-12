---
name: maintenance-mode-workflow
description: Gate checklist before fixing any FlatRedBall1 (engine + Glue) issue. Triggers: new bug report, "fix this issue", TDD, testability, before editing production code.
---

# Maintenance-Mode Issue Workflow

FlatRedBall1 (engine + Glue) is maintenance-mode — no new features — but existing projects depend on it, so issues still get fixed. Every fix should also nudge the touched code toward testability, not just patch around it.

Work through these gates in order before writing a fix:

1. **Scan for skills.** Search `.claude/skills/` (repo-wide) and `FRBDK/Glue/.claude/skills/` for anything covering this area. Nothing found? Stop and propose a research task to produce a new skill file — written damped, per [skills-writer](../skills-writer/SKILL.md), not a full write-up.
2. **Read the skill, discuss.** Once one exists (or was just written), read it and raise open questions with the user before touching code.
3. **Testability gate.** Can the fix be pinned with a real unit test as it stands? If not, stop and propose a scoped refactor task first — see [REFACTORING.md](../../../FRBDK/Glue/REFACTORING.md) for the incremental-refactor philosophy and the transitional-injection pattern (`Xyz.Self` defaulting field with an internal setter) used to unstick static-singleton coupling. Do not write the fix until this gate passes.
4. **Red.** Write a failing test for the fix.
5. **Green.** Implement the fix.
6. **Manual-test call-out.** If the tests don't cover the full user-facing path, say explicitly how to verify manually. If they do, say "no manual testing needed."

This process is still being refined.
