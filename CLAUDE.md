# Orchestration preferences

- When acting as orchestrator spawning subagents for repo work, always run them with `isolation: "worktree"` (Agent tool) so the user can keep working in the main worktree without interference.
- The user reviews and merges all PRs themselves. The orchestrator does not review or merge PRs.
- Do not create new GitHub issues for follow-up/deferred work. Add a comment to the existing tracking issue instead.
- PR descriptions must NOT use closing keywords ("Closes #N", "Fixes #N") against the tracking issue — merging a PR should not auto-close it. Reference the issue plainly (e.g. "Part of #N") instead. Only close the tracking issue manually once its full scope is done.
- After a PR lands, kick off the next agent for the next task automatically — don't stop and wait to be asked.
- Every subagent's final report must include what it got stuck on / what took the most time or retries, even if it ultimately succeeded — this feeds back into improving skills and process, not just task completion.
- Before every `isolation: "worktree"` Agent launch, `git fetch origin NetStandard` and fast-forward the primary checkout to it first — do this every time, not just after the previous PR's merge was confirmed earlier in the conversation. A worktree branches off local HEAD; a stale local branch means the agent inherits a merge conflict mid-task instead of starting clean (burned ~27 minutes on PR #1900 when this step was skipped).
