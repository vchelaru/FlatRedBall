---
name: worktree-cleanup
description: Remove a subagent's .claude/worktrees/agent-<id> dir after its PR merges. Triggers: git worktree remove, "Permission denied"/"Device or resource busy" on worktree cleanup, Windows file locks on bin/obj.
---

# Worktree Cleanup (Windows)

The Agent tool's `isolation: "worktree"` auto-creates `.claude/worktrees/agent-<id>/` and auto-removes it if the agent made no changes. Once an agent *did* change files and its PR has merged, the worktree is orphaned and needs manual cleanup — `git worktree remove` almost always fails with "Permission denied" on Windows because MSBuild's persistent build server and file watchers hold handles on `bin/`/`obj/` for a while after `dotnet build`/`dotnet test` exits. Don't bother expecting the plain command to succeed first try.

**Landmine — the orchestrator's own Bash cwd is the most common lock.** If you ever `cd`'d the Bash tool into a worktree path (or ran commands there) during the session, that shell holds a directory handle for as long as it stays parked there — `rm -rf` and even `Remove-Item` fail against it. Before any removal attempt, run `pwd` and confirm the Bash cwd is the primary checkout, not the worktree. `cd`ing away in a prior tool call doesn't guarantee it stuck — verify, don't assume.

Sequence, run from the primary checkout:

1. Confirm Bash cwd is NOT inside the worktree (`pwd`); `cd` out first if it is.
2. `git worktree remove --force .claude/worktrees/agent-<id>` — clears git's bookkeeping even when the underlying `rm` fails. Check `git worktree list` to confirm the entry is gone.
3. If the directory still exists on disk, `dotnet build-server shutdown` (releases MSBuild/Roslyn server handles), then retry deletion.
4. If it still won't delete, switch tools: PowerShell's `Remove-Item -Recurse -Force "<path>"` succeeds where bash `rm -rf` reports "Device or resource busy" — this is a real, repeatable difference in this environment, not a fluke. Prefer it as the fallback rather than retrying `rm`.
5. `git worktree prune -v`, then verify: `git worktree list` (entry absent) and list the `.claude/worktrees/` dir (no leftover folder).

If PowerShell's `Remove-Item` still fails: check for a Visual Studio instance (or its `ServiceHub.*.exe`/indexer) with that worktree open, or stray `dotnet`/`MSBuild`/`VBCSCompiler.exe` processes beyond what `build-server shutdown` released — kill or ask the user before force-killing anything unidentified.

**Don't trust the agent's self-report of what it created as a workaround.** An agent that hit a build dependency needing a specific sibling-directory name may describe its fix as a "symlink/junction," but actually run `git worktree add` at that path (a second real worktree, invisible in `git worktree list` under the original name). If `git worktree remove` on the original leaves a same-named directory elsewhere that a plain `rmdir`/`Remove-Item` refuses with "not empty" rather than deleting instantly (reparse points delete instantly regardless of contents), it's a real directory, not a link — check `(Get-Item $path).Attributes` for `ReparsePoint` before assuming, and if absent, treat it as an orphaned worktree copy: safe to delete outright once its PR has merged.
