---
name: maintenance-mode-workflow
description: Gate checklist before fixing any FlatRedBall1 (engine + Glue) issue. Triggers: new bug report, "fix this issue", TDD, testability, before editing production code.
---

# Maintenance-Mode Issue Workflow

FlatRedBall1 (engine + Glue) is maintenance-mode — no new features — but existing projects depend on it, so issues still get fixed. Every fix should also nudge the touched code toward testability, not just patch around it.

Work through these gates in order before writing a fix:

1. **Scan for skills.** Search `.claude/skills/` (repo-wide) and `FRBDK/Glue/.claude/skills/` for anything covering this area. Nothing found? Stop and propose a research task to produce a new skill file — written damped, per [skills-writer](../skills-writer/SKILL.md), not a full write-up.
2. **Read the skill, discuss.** Once one exists (or was just written), read it and raise open questions with the user before touching code.
3. **Testability gate — two sub-checks, in order.** (a) Can the fix be pinned with a real unit test as the code stands? (b) If not, that's expected — this codebase is missing DI/interfaces in places — so propose and land a scoped refactor task *before* touching the bug, then re-check (a). See [REFACTORING.md](../../../FRBDK/Glue/REFACTORING.md) for the incremental-refactor philosophy and the transitional-injection pattern (`Xyz.Self` defaulting field with an internal setter) used to unstick static-singleton coupling. Do not write the bug fix itself until both sub-checks pass. **Guard this concretely: before your first `Edit`/`Write` to the buggy source file, state which failing test you just watched fail. If you can't state one, stop and write/run it first** — a confident diagnosis is not a substitute for this checkpoint.

   **Loophole:** if pinning the test requires a workaround *inside the test* because a shared test double (a `Fake*`/mock in `TestSupport`) doesn't behave like the real thing — e.g. hand-rolling a substitute object because the shared fake always returns null/empty for the case you need — that IS sub-check (a) failing, not passing. Writing the local workaround instead of noticing this is exactly the shortcut this gate exists to block: it makes one test green without fixing the seam, so the next test hits the same wall. Route to (b) instead: fix the shared fake/seam in `TestSupport` (real behavior, not a wider `null`), then write the test straight against it. Worked example: issue #2016 / `FakeFindManager.TreeNodeByTag` — see REFACTORING.md's "`FakeFindManager.TreeNodeByTag` now resolves real tags" entry.

   **Second loophole — a nearby value stands in for the real one.** A red test that asserts on a config default, a constant, or a setting one hop upstream of the actual code path is not the same as pinning the reported behavior, even when it is genuinely red-then-green. The tell: the test's assertion doesn't mention the thing the user actually observed break (a drag, a render, a click) — it mentions a number or flag that merely influences it. That gap lets the real defect (state that never reaches the code path at all) survive the "fix" untouched. If the only thing reachable from a fast test is such a proxy, that's sub-check (a) failing — route to (b) and either build the seam to drive the real path, or say so explicitly and fall back to manual verification, rather than letting the proxy's green stand in for the report's green.
4. **Red/Green, heavy TDD — not one big pinning test plus one big implementation.** Decompose the fix into its smallest behaviors/branches; each gets its own small failing test before the code that satisfies it, refactoring between cycles. Repeat until the fix is fully covered.

   **You must actually watch it fail.** If the fix is already written when you think of the test, disable the fix (a temporary `&& false` on the guard) and re-run: a test that passes with the fix disabled proves nothing and is worse than no test, because it reads as coverage forever after. This is not hypothetical — an event-surface test written for a plugin crash passed with both guards disabled, because the event it drove never reached the plugin in a headless host.

   Reproducing the *reported symptom* is the bar, not exercising the same method the stack trace names. A crash reached from two entry points needs a test that fails for the entry point the user hit.
5. **Manual-test call-out.** If the tests don't cover the full user-facing path, say explicitly how to verify manually. If they do, say "no manual testing needed."

This process is still being refined.
