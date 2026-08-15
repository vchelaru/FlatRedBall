---
name: glue-task-manager
description: TaskManager's task-scheduling model — priority tiers, not FIFO, and why AddOrMoveToEnd coalescing can silently miss. Triggers: TaskExecutionPreference, AddOrMoveToEnd, TaskManager.Self.Add/AddAsync, GenerateElementCode, AddOrRunIfTasked, EffectiveId, out-of-order regeneration.
---

# Glue TaskManager Scheduling

`FRBDK/Glue/Glue/Tasks/TaskManager.cs` runs almost all project-mutating work through one dedicated
thread and a priority queue (`taskQueue`), not a FIFO queue. `glue-unit-test-bootstrap`'s "Sibling seams"
section covers the `SynchronousMode` test seam that collapses this to inline sequential execution —
which is why ordering bugs here are invisible under that mode and need a real (non-synchronous) queue
to pin.

## Tiers beat enqueue order

`TaskExecutionPreference` (`Asap` / `Fifo` / `AddOrMoveToEnd`) are fixed numeric tiers far apart
(`0`, `ulong.MaxValue/3`, `2*ulong.MaxValue/3`); `taskoffset` only breaks ties *within* a tier. A `Fifo`
task queued after an `AddOrMoveToEnd` task still runs first, unconditionally. `GenerateElementCode`
(`GenerateCodeCommands.cs`) deliberately queues its file-write + `.csproj`-registration work at
`AddOrMoveToEnd` to keep the UI responsive — so any operation that depends on that codegen having
already landed (moving or deleting the files it wrote) must also be `AddOrMoveToEnd`, or a default-`Fifo`
call queued later can still run first.

## Coalescing keys off DisplayInfo, which can be stale

`AddOrMoveToEnd`'s de-dup cancels an older pending task sharing the same `EffectiveId`
(`CustomId ?? DisplayInfo`). A `DisplayInfo` built from mutable state (e.g. `element.ToString()` →
`Name`) gives two logically-identical requests different ids across a mutation, so they never coalesce.
Also: `AddOrRunIfTasked`'s "already in a task, run inline" fast path explicitly excludes
`AddOrMoveToEnd` — it always goes through the real queue.
