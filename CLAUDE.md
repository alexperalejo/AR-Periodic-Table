## Unity Editor Integration

Two complementary bridges connect Claude Code to the Unity Editor:

### 1. MCP Unity (rich, real-time — primary)

The `com.gamelovers.mcp-unity` package adds a WebSocket server inside Unity. The Node.js
MCP server connects to it and exposes tools directly in Claude Code conversations.

**After first-time setup** (Node.js + Git installed, Unity opened, server built):
```
# Claude Code automatically connects via MCP — no manual commands needed.
# Tools appear as: unity_get_hierarchy, unity_execute_menu_item, etc.
```

Server location after setup:
`C:\Users\erikg\mcp-unity\Server~\build\index.js`

See `Tools > MCP Unity > Server Window` in the Unity Editor for the exact configured path.

---

### 2. File Bridge (unity-cmd.ps1 — fallback / batch use)

The `Assets/LLM/Editor/UnityBridge.cs` script polls `Assets/LLM/Bridge/request.json`
every Editor update tick and writes responses to `Assets/LLM/Bridge/response.md`.

Unity must be open with the project loaded. No play mode required.

#### Sending a command
```powershell
cd C:\Users\erikg\Documents\AR-Periodic-Table-dev
.\unity-cmd.ps1 '{"type": "help"}'
.\unity-cmd.ps1 '{"type": "scene"}'
.\unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120
.\unity-cmd.ps1 -File request.json          # for complex JSON
```

#### Full command reference

| type | params | description |
|------|--------|-------------|
| `help` | — | List all commands |
| `scene` | `depth` (int, default 3) | Full scene hierarchy |
| `inspect` | `path`, `lens` (all/scripts/components/transform) | Inspect a GameObject |
| `refresh` | — | AssetDatabase.Refresh() + recompile |
| `create` | `name`, `parent`?, `position`[x,y,z]?, `components`[]? | Create a GameObject |
| `add-component` | `path`, `component` | Add a component by type name |
| `set` | `path`, `component`, `property`, `value` | Set a serialized property |
| `find` | `name`?, `tag`?, `component`? | Find GameObjects |
| `delete` | `path` | Delete a GameObject (with Undo) |
| `save-scene` | — | Save active scene |
| `new-scene` | — | Create a new empty scene |
| `open-scene` | `path` | Open a scene file |
| `play` | `action` (enter/exit/toggle) | Control play mode |
| `console` | `count`? (default 30) | Last N console messages |
| `assets` | `filter`?, `path`? | List project assets |
| `run-menu` | `item` | Execute any Unity menu item |
| `select` | `path` | Select + ping a GameObject in the Editor |

**Serialized property names** use the `m_` prefix form: `m_LocalPosition`, `m_SizeDelta`, etc.

**Batch commands** — pass a JSON array to run several in one call:
```powershell
.\unity-cmd.ps1 '[{"type":"scene"},{"type":"console"}]'
```

#### Workflow
1. `scene` — understand current hierarchy
2. `create` / `add-component` / `set` — make changes
3. `inspect` with `lens` to verify
4. `save-scene` — persist
5. Write C# scripts, then `refresh` to compile and check errors
