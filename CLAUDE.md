@AGENTS.md

## Claude Code notes

- `AGENTS.md` (imported above) is the single source of truth for project rules. Put shared agent guidance there, not here; keep this file for Claude-specific notes only.
- `Packages/manifest.json` includes `com.coplaydev.unity-mcp` (MCP for Unity). When the Unity Editor is open with that bridge running, it can be used to inspect scenes, read the Console, and enter Play Mode for verification.
- Scene (`.unity`) and prefab (`.prefab`) files are large Unity YAML. Prefer targeted searches (by GUID, component name, or field) over reading them whole, and avoid hand-editing them unless the change is small and well understood.
