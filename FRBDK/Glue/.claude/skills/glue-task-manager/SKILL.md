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

## A same-tier task queued early can still surface last

`GenerateElementCode` on a screen with N derived screens synchronously enqueues one `AddOrMoveToEnd`
task per derived screen the moment it runs - so any other `AddOrMoveToEnd` call made around the same
time (e.g. a plugin's own error-refresh reacting to the same property change) ends up dequeued after
all N of them, even though it was queued just as early. This is "tiers beat enqueue order" above,
just easy to miss because the delay looks like *your* call ran late, not like a same-tier burst beat
it. `RefreshCommands.RefreshErrorsFor` (`RefreshCommands.cs`) takes an optional `executionPreference`
for exactly this: pass `Fifo` (or anything but `AddOrMoveToEnd`) from a reaction that must show up
promptly, and - since `AddOrRunIfTasked`'s inline fast path excludes only `AddOrMoveToEnd` - if you're
already inside a task, it runs immediately instead of queueing at all. Conversely, a reaction that
must wait until codegen has actually landed still wants `AddOrMoveToEnd` (see above).

## The nested-task fast path checks `TaskManager.Self`, not the calling instance

`AddOrRunIfTasked`'s `IsInTask()` gate is hardcoded to `TaskManager.Self.SyncTaskThreadId`, not
`this.SyncTaskThreadId` — so a `TaskManager.Self.AddAsync(...)` call made from inside an already-running
task's callback (any preference except `AddOrMoveToEnd`) runs inline via `RunTask(...).Wait()` instead of
re-entering the priority queue, meaning its own `TaskExecutionPreference` argument is inert. In production
this is safe because `StartDoTaskManagerLoop` wraps the dedicated task thread in Nito.AsyncEx's
`AsyncContext.Run`, which pins every `await` continuation started within it back to that same thread — so
`IsInTask()` still sees the same thread ID after any number of nested `await`s. This can't be observed
under `GlueUnitTests`: `GlueTestBootstrap` forces `SynchronousMode = true` before `TaskManager.Self` is
ever constructed, which permanently fixes that one singleton instance's `SyncTaskThreadId` to whatever
thread first touched it — no later toggle of `TaskManager.SynchronousMode` (e.g. in
`TaskManagerSynchronousModeTests`) un-fixes it, so nested calls always look "in task" there regardless of
what the real threaded model would do.
