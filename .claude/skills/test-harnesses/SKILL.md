---
name: test-harnesses
description: Index of FRB1's existing test harnesses (fake game socket, gold project load+build, real game launch, embedded-code tests) — check here before saying a scenario can't be tested. Triggers: TDD, "how would we test", end-to-end, LiveGameProcess, GoldProject, FakeGameSide, BuildSmoke, LiveGame.
---

# Test Harnesses

Before calling a scenario untestable, or proposing a new harness, check this table and `ls` the
`TestSupport` folders. Details live in [[glue-live-game-testing]] (real game process) and
[[glue-unit-test-bootstrap]] (headless Glue, gold projects).

| Need | Harness | Category |
|---|---|---|
| Glue-side socket behavior with no game | `FRBDK\Glue\Tests\GlueUnitTests\GameCommunicationPlugin\FakeGameSide.cs` | default |
| Real project loaded headlessly in Glue, codegen run, built, live-edit code embedded | `TestSupport\GoldProject.cs` | `BuildSmoke` |
| Built game launched and driven over the real live-edit socket | `TestSupport\LiveGameProcess.cs` | `LiveGame` |
| Embedded (game-side) logic compiled and run in a scratch game project | pattern explained in `GlueControl\VariableAssignmentLogicEnumConversionTests.cs` | `BuildSmoke` |
| `#if` version gates in embedded code, no build | `GlueControl\EmbeddedVersionGateTests.cs` (Roslyn) | default |
| Embedded files that compile without a game, run in-process | `Tests\EngineUnitTests` links them in with `<Compile Include ... Link>` | engine tests |

## Gotchas

- **A race doesn't need to happen for real to be tested.** Put the files and timestamps into the state the race leaves behind, then launch.
- **Extending a harness beats working around it.** If `LiveGameProcess.StartAsync` or `GoldProject` lacks a hook for the step you need, add the hook there.
