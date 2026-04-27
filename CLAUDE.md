## Unity Editor Operations

Use `unity-cmd.ps1` to interact with Unity Editor:

### Basic commands
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "scene"}'
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "inspect", "path": "Player", "lens": "scripts"}'
powershell -ExecutionPolicy Bypass -File unity-cmd.ps1 '{"type": "refresh"}' -Timeout 120

### Workflow
1. `scene` to understand current state
2. `create` / `add-component` / `set` to build
3. `inspect` with lenses to verify
4. `save-scene` to persist
5. Write C# scripts, then `refresh` to compile and check errors

### Tips
- Use batch commands (JSON array) to group independent operations
- Use `scratch` for complex multi-step setup instead of many JSON commands
- Always `save-scene` before `new-scene` or `open-scene`
- For serialized Unity properties, use `m_` prefix: `m_LocalPosition`, `m_SizeDelta`
- Component names are short: `Image`, not `UnityEngine.UI.Image`
