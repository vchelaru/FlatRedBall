// PostToolUse hook, matcher "Skill". Records that this session has loaded the
// skills-writer skill, so require-skills-writer-for-skill-edits.js can allow
// subsequent skill-file edits in the same session.
let d = "";
process.stdin.on("data", c => d += c);
process.stdin.on("end", () => {
  try {
    const j = JSON.parse(d);
    if (j.tool_input && j.tool_input.skill === "skills-writer" && j.session_id) {
      const fs = require("fs"), os = require("os"), path = require("path");
      const dir = path.join(os.homedir(), ".claude", ".hook-state");
      fs.mkdirSync(dir, { recursive: true });
      fs.writeFileSync(path.join(dir, "skills-writer-" + j.session_id), "");
    }
  } catch (e) {}
});
