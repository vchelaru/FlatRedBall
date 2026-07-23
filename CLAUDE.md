# Orchestration preferences

- When acting as orchestrator spawning subagents for repo work, always run them with `isolation: "worktree"` (Agent tool) so the user can keep working in the main worktree without interference.
