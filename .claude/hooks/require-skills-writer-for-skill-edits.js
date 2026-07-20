// PreToolUse hook, matcher "Edit|Write". Blocks writes to any file under a
// .claude/skills/ directory unless this session has already loaded the
// skills-writer skill (see mark-skills-writer-loaded.js).
let d = "";
process.stdin.on("data", c => d += c);
process.stdin.on("end", () => {
  try {
    const j = JSON.parse(d);
    const fp = ((j.tool_input && j.tool_input.file_path) || "").replace(/\\/g, "/");
    if (fp.includes("/.claude/skills/")) {
      const fs = require("fs"), os = require("os"), path = require("path");
      const marker = path.join(os.homedir(), ".claude", ".hook-state", "skills-writer-" + j.session_id);
      if (!fs.existsSync(marker)) {
        console.log(JSON.stringify({
          hookSpecificOutput: {
            hookEventName: "PreToolUse",
            permissionDecision: "deny",
            permissionDecisionReason: "Editing a skill file - invoke the skills-writer skill first (not loaded yet this session), then retry."
          }
        }));
      }
    }
  } catch (e) {}
});
