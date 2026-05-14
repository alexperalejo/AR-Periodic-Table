// Assets/LLM/Editor/UnityBridge.cs
// File-based command bridge for Claude Code → Unity Editor integration.
// Polls Assets/LLM/Bridge/request.json, executes commands, writes response.md.
// Send commands via: .\unity-cmd.ps1 '{"type":"help"}'
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PeriodicAR.LLMBridge
{
    [InitializeOnLoad]
    public static class UnityBridge
    {
        static readonly string RequestFile;
        static readonly string ResponseFile;
        static DateTime _lastProcessed = DateTime.MinValue;

        static UnityBridge()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            RequestFile = Path.Combine(root, "Assets", "LLM", "Bridge", "request.json");
            ResponseFile = Path.Combine(root, "Assets", "LLM", "Bridge", "response.md");
            Directory.CreateDirectory(Path.GetDirectoryName(ResponseFile));
            EditorApplication.update += Poll;
            Debug.Log("[UnityBridge] Ready — polling Assets/LLM/Bridge/request.json");
        }

        // ── Polling ─────────────────────────────────────────────────────────────

        static void Poll()
        {
            if (!File.Exists(RequestFile)) return;
            var t = File.GetLastWriteTimeUtc(RequestFile);
            if (t <= _lastProcessed) return;
            _lastProcessed = t;

            string raw;
            try { raw = File.ReadAllText(RequestFile, Encoding.UTF8).Trim(); }
            catch { return; }
            if (string.IsNullOrEmpty(raw) || raw == "{}") return;

            var sb = new StringBuilder();
            try
            {
                if (raw.StartsWith("["))
                {
                    var arr = JArray.Parse(raw);
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (i > 0) sb.Append("\n\n---\n\n");
                        sb.Append(Dispatch(arr[i] as JObject));
                    }
                }
                else
                {
                    sb.Append(Dispatch(JObject.Parse(raw)));
                }
            }
            catch (Exception ex)
            {
                sb.Append($"**Bridge error:** {ex.Message}\n\n```\n{ex.StackTrace}\n```");
            }

            File.WriteAllText(ResponseFile, sb.ToString(), Encoding.UTF8);
        }

        static string Dispatch(JObject cmd)
        {
            if (cmd == null) return "**Error:** null command";
            var type = (string)cmd["type"] ?? "";
            try
            {
                return type switch
                {
                    "help"          => CmdHelp(),
                    "scene"         => CmdScene(cmd),
                    "inspect"       => CmdInspect(cmd),
                    "refresh"       => CmdRefresh(),
                    "create"        => CmdCreate(cmd),
                    "add-component" => CmdAddComponent(cmd),
                    "set"           => CmdSet(cmd),
                    "find"          => CmdFind(cmd),
                    "delete"        => CmdDelete(cmd),
                    "save-scene"    => CmdSaveScene(),
                    "new-scene"     => CmdNewScene(),
                    "open-scene"    => CmdOpenScene(cmd),
                    "play"          => CmdPlay(cmd),
                    "console"       => CmdConsole(),
                    "assets"        => CmdAssets(cmd),
                    "run-menu"      => CmdRunMenu(cmd),
                    "select"        => CmdSelect(cmd),
                    _               => $"Unknown command `{type}`. Use `{{\"type\":\"help\"}}` for available commands."
                };
            }
            catch (Exception ex)
            {
                return $"**Error in `{type}`:** {ex.Message}";
            }
        }

        // ── Commands ─────────────────────────────────────────────────────────────

        static string CmdHelp() => @"# Unity Bridge Commands

| type | required params | optional params | description |
|------|----------------|-----------------|-------------|
| `scene` | — | `depth` (int, default 3) | Full scene hierarchy |
| `inspect` | `path` | `lens` (all/scripts/components/transform) | Inspect a GameObject |
| `refresh` | — | — | Refresh & recompile assets |
| `create` | `name` | `parent`, `position`[x,y,z], `components`[] | Create a GameObject |
| `add-component` | `path`, `component` | — | Add a component |
| `set` | `path`, `component`, `property`, `value` | — | Set a serialized property |
| `find` | — | `name`, `tag`, `component` | Find GameObjects |
| `delete` | `path` | — | Delete a GameObject |
| `save-scene` | — | — | Save the active scene |
| `new-scene` | — | — | Create a new empty scene |
| `open-scene` | `path` | — | Open a scene by path |
| `play` | `action` (enter/exit/toggle) | — | Control play mode |
| `console` | — | `count` (int, default 30) | Recent console messages |
| `assets` | — | `filter`, `path` | List project assets |
| `run-menu` | `item` | — | Execute a Unity menu item |
| `select` | `path` | — | Select a GameObject in the Editor |
| `help` | — | — | This help text |

**Batch commands:** Pass a JSON array to run multiple commands in one call.
**Serialized property names:** Use the `m_` prefix form (e.g. `m_LocalPosition`, `m_SizeDelta`).";

        static string CmdScene(JObject cmd)
        {
            int maxDepth = (int?)cmd["depth"] ?? 3;
            var scene = SceneManager.GetActiveScene();
            var sb = new StringBuilder();
            sb.AppendLine($"# Scene: **{scene.name}**");
            sb.AppendLine($"`{scene.path}`  |  Dirty: {scene.isDirty}  |  Play mode: {EditorApplication.isPlaying}");
            sb.AppendLine();
            sb.AppendLine("## Hierarchy");
            foreach (var root in scene.GetRootGameObjects())
                AppendGO(sb, root, 0, maxDepth);
            return sb.ToString();
        }

        static void AppendGO(StringBuilder sb, GameObject go, int depth, int maxDepth)
        {
            var indent = new string(' ', depth * 2);
            var inactive = go.activeSelf ? "" : " _(inactive)_";
            var comps = string.Join(", ", Array.ConvertAll(
                go.GetComponents<Component>(),
                c => c == null ? "Missing" : c.GetType().Name));
            sb.AppendLine($"{indent}- **{go.name}**{inactive} — {comps}");
            if (depth < maxDepth - 1)
                foreach (Transform child in go.transform)
                    AppendGO(sb, child.gameObject, depth + 1, maxDepth);
        }

        static string CmdInspect(JObject cmd)
        {
            var path = (string)cmd["path"] ?? "";
            var lens = (string)cmd["lens"] ?? "all";
            var go = FindByPath(path);
            if (go == null) return $"GameObject not found: `{path}`";

            var sb = new StringBuilder();
            sb.AppendLine($"# {go.name}");
            sb.AppendLine($"Active: {go.activeSelf}  |  Layer: {LayerMask.LayerToName(go.layer)}  |  Tag: {go.tag}");
            sb.AppendLine($"World position: {go.transform.position:F3}  |  Euler: {go.transform.eulerAngles:F2}  |  Scale: {go.transform.localScale:F3}");
            sb.AppendLine();

            var components = go.GetComponents<Component>();

            if (lens is "all" or "components")
            {
                sb.AppendLine("## Components");
                foreach (var c in components)
                    sb.AppendLine(c == null ? "- _(Missing Script)_" : $"- {c.GetType().FullName}");
                sb.AppendLine();
            }

            if (lens is "all" or "scripts")
            {
                sb.AppendLine("## Script Fields (MonoBehaviours)");
                foreach (var c in components)
                {
                    if (c is not MonoBehaviour mb) continue;
                    sb.AppendLine($"### {mb.GetType().Name}");
                    var so = new SerializedObject(mb);
                    var it = so.GetIterator();
                    it.NextVisible(true);
                    while (it.NextVisible(false))
                        sb.AppendLine($"- `{it.name}` ({it.propertyType}): {PropValue(it)}");
                    sb.AppendLine();
                }
            }

            if (lens is "transform")
            {
                sb.AppendLine("## Transform (serialized properties)");
                var so = new SerializedObject(go.transform);
                var it = so.GetIterator();
                it.NextVisible(true);
                while (it.NextVisible(false))
                    sb.AppendLine($"- `{it.name}`: {PropValue(it)}");
            }

            return sb.ToString();
        }

        static string CmdRefresh()
        {
            AssetDatabase.Refresh();
            return "Asset database refresh triggered. Compilation will complete in a few seconds.";
        }

        static string CmdCreate(JObject cmd)
        {
            var name = (string)cmd["name"] ?? "New GameObject";
            var go = new GameObject(name);

            if (cmd["parent"] is JToken parentToken)
            {
                var parent = FindByPath(parentToken.ToString());
                if (parent != null) go.transform.SetParent(parent.transform, false);
            }

            if (cmd["position"] is JArray pos && pos.Count == 3)
                go.transform.localPosition = new Vector3((float)pos[0], (float)pos[1], (float)pos[2]);

            if (cmd["components"] is JArray compList)
                foreach (var c in compList)
                {
                    var t = FindType(c.ToString());
                    if (t != null) go.AddComponent(t);
                    else Debug.LogWarning($"[UnityBridge] Component type not found: {c}");
                }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = go;
            return $"Created **{go.name}** at path `{GetPath(go.transform)}` (id: {go.GetInstanceID()})";
        }

        static string CmdAddComponent(JObject cmd)
        {
            var path = (string)cmd["path"] ?? "";
            var compName = (string)cmd["component"] ?? "";
            var go = FindByPath(path);
            if (go == null) return $"GameObject not found: `{path}`";
            var type = FindType(compName);
            if (type == null) return $"Component type not found: `{compName}`";
            Undo.AddComponent(go, type);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return $"Added **{type.Name}** to **{go.name}**";
        }

        static string CmdSet(JObject cmd)
        {
            var path      = (string)cmd["path"]      ?? "";
            var compName  = (string)cmd["component"] ?? "";
            var propName  = (string)cmd["property"]  ?? "";
            var value     = cmd["value"];

            var go = FindByPath(path);
            if (go == null) return $"GameObject not found: `{path}`";

            Component comp = null;
            foreach (var c in go.GetComponents<Component>())
                if (c != null && c.GetType().Name == compName) { comp = c; break; }
            if (comp == null) return $"Component `{compName}` not found on `{path}`";

            var so = new SerializedObject(comp);
            var prop = so.FindProperty(propName);
            if (prop == null) return $"Property `{propName}` not found on `{compName}`";

            ApplyValue(prop, value);
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return $"Set `{compName}.{propName}` = `{value}`";
        }

        static string CmdFind(JObject cmd)
        {
            var nameFilter = (string)cmd["name"];
            var tagFilter  = (string)cmd["tag"];
            var compFilter = (string)cmd["component"];
            Type compType  = compFilter != null ? FindType(compFilter) : null;

            var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var sb = new StringBuilder();
            sb.AppendLine("## Found GameObjects");
            int count = 0;
            foreach (var go in all)
            {
                if (nameFilter != null && !go.name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)) continue;
                if (tagFilter  != null && go.tag != tagFilter) continue;
                if (compType   != null && go.GetComponent(compType) == null) continue;
                sb.AppendLine($"- **{go.name}** (`{GetPath(go.transform)}`)");
                count++;
            }
            if (count == 0) sb.AppendLine("_(none found)_");
            return sb.ToString();
        }

        static string CmdDelete(JObject cmd)
        {
            var path = (string)cmd["path"] ?? "";
            var go = FindByPath(path);
            if (go == null) return $"GameObject not found: `{path}`";
            var name = go.name;
            Undo.DestroyObjectImmediate(go);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return $"Deleted **{name}**";
        }

        static string CmdSaveScene()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            return $"Saved: `{scene.path}`";
        }

        static string CmdNewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            return "New empty scene created. Use `save-scene` after naming it.";
        }

        static string CmdOpenScene(JObject cmd)
        {
            var path = (string)cmd["path"] ?? "";
            if (!File.Exists(path))
            {
                var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
                if (!File.Exists(full)) return $"Scene not found: `{path}`";
                path = full;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return $"Opened: `{path}`";
        }

        static string CmdPlay(JObject cmd)
        {
            var action = (string)cmd["action"] ?? "toggle";
            return action switch
            {
                "enter"  => SetPlay(true),
                "exit"   => SetPlay(false),
                "toggle" => SetPlay(!EditorApplication.isPlaying),
                _        => $"Unknown action `{action}`. Use enter/exit/toggle."
            };
        }

        static string SetPlay(bool play)
        {
            EditorApplication.isPlaying = play;
            return $"Play mode: **{(play ? "entering" : "exiting")}**";
        }

        static string CmdConsole()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Console (last 30 entries)");
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                var logEntryType   = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                if (logEntriesType == null || logEntryType == null)
                {
                    sb.AppendLine("_(LogEntries reflection unavailable in this Unity version)_");
                    return sb.ToString();
                }

                var startMethod    = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var endMethod      = logEntriesType.GetMethod("EndGettingEntries",   BindingFlags.Static | BindingFlags.Public);
                var getCountMethod = logEntriesType.GetMethod("GetCount",            BindingFlags.Static | BindingFlags.Public);
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal",    BindingFlags.Static | BindingFlags.Public);

                startMethod?.Invoke(null, null);
                int total = (int)(getCountMethod?.Invoke(null, null) ?? 0);
                int start = Math.Max(0, total - 30);
                var entry = Activator.CreateInstance(logEntryType);
                var msgField  = logEntryType.GetField("message");
                var modeField = logEntryType.GetField("mode");

                for (int i = start; i < total; i++)
                {
                    getEntryMethod?.Invoke(null, new object[] { i, entry });
                    var msg  = msgField?.GetValue(entry)?.ToString() ?? "";
                    var mode = modeField?.GetValue(entry)?.ToString() ?? "";
                    var icon = mode.Contains("Error") || mode.Contains("Exception") ? "ERROR  " :
                               mode.Contains("Warning") ? "WARNING" : "LOG    ";
                    sb.AppendLine($"`{icon}` {msg.Split('\n')[0]}");
                }
                endMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"_(Error reading console: {ex.Message})_");
            }
            return sb.ToString();
        }

        static string CmdAssets(JObject cmd)
        {
            var filter     = (string)cmd["filter"] ?? "";
            var searchPath = (string)cmd["path"]   ?? "Assets";
            var guids      = AssetDatabase.FindAssets(filter, new[] { searchPath });
            var sb = new StringBuilder();
            int limit = Math.Min(guids.Length, 60);
            sb.AppendLine($"## Assets — {guids.Length} found (showing {limit})");
            for (int i = 0; i < limit; i++)
                sb.AppendLine($"- `{AssetDatabase.GUIDToAssetPath(guids[i])}`");
            if (guids.Length > 60) sb.AppendLine($"_(truncated — use `filter` to narrow results)_");
            return sb.ToString();
        }

        static string CmdRunMenu(JObject cmd)
        {
            var item = (string)cmd["item"] ?? "";
            if (string.IsNullOrEmpty(item)) return "Specify `item` e.g. `\"Tools/AR Periodic Table/Setup Object Detection\"`";
            EditorApplication.ExecuteMenuItem(item);
            return $"Executed menu: **{item}**";
        }

        static string CmdSelect(JObject cmd)
        {
            var path = (string)cmd["path"] ?? "";
            var go = FindByPath(path);
            if (go == null) return $"GameObject not found: `{path}`";
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return $"Selected **{go.name}**";
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var direct = GameObject.Find(path);
            if (direct != null) return direct;

            var parts = path.Split('/');
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                if (parts.Length == 1) return root;
                var child = root.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                if (child != null) return child.gameObject;
            }
            return null;
        }

        static string GetPath(Transform t)
        {
            var parts = new List<string> { t.name };
            var p = t.parent;
            while (p != null) { parts.Insert(0, p.name); p = p.parent; }
            return string.Join("/", parts);
        }

        static Type FindType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var t in asm.GetTypes())
                    if (t.Name == name && typeof(Component).IsAssignableFrom(t))
                        return t;
            return null;
        }

        static string PropValue(SerializedProperty p) => p.propertyType switch
        {
            SerializedPropertyType.Boolean         => p.boolValue.ToString(),
            SerializedPropertyType.Float           => p.floatValue.ToString("F4"),
            SerializedPropertyType.Integer         => p.intValue.ToString(),
            SerializedPropertyType.String          => $"\"{p.stringValue}\"",
            SerializedPropertyType.Vector2         => p.vector2Value.ToString(),
            SerializedPropertyType.Vector3         => p.vector3Value.ToString(),
            SerializedPropertyType.Vector4         => p.vector4Value.ToString(),
            SerializedPropertyType.Color           => p.colorValue.ToString(),
            SerializedPropertyType.Enum            => p.enumDisplayNames.Length > p.enumValueIndex
                                                          ? p.enumDisplayNames[p.enumValueIndex]
                                                          : p.enumValueIndex.ToString(),
            SerializedPropertyType.ObjectReference => p.objectReferenceValue != null
                                                          ? $"{p.objectReferenceValue.name} ({p.objectReferenceValue.GetType().Name})"
                                                          : "null",
            SerializedPropertyType.ArraySize       => p.intValue.ToString(),
            _                                      => p.type
        };

        static void ApplyValue(SerializedProperty prop, JToken value)
        {
            if (value == null) return;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Boolean:   prop.boolValue   = (bool)value;    break;
                case SerializedPropertyType.Float:     prop.floatValue  = (float)value;   break;
                case SerializedPropertyType.Integer:   prop.intValue    = (int)value;     break;
                case SerializedPropertyType.String:    prop.stringValue = (string)value;  break;
                case SerializedPropertyType.Enum:      prop.enumValueIndex = (int)value;  break;
                case SerializedPropertyType.Vector3 when value is JArray a3:
                    prop.vector3Value = new Vector3((float)a3[0], (float)a3[1], (float)a3[2]); break;
                case SerializedPropertyType.Vector2 when value is JArray a2:
                    prop.vector2Value = new Vector2((float)a2[0], (float)a2[1]); break;
                case SerializedPropertyType.Vector4 when value is JArray a4:
                    prop.vector4Value = new Vector4((float)a4[0], (float)a4[1], (float)a4[2], (float)a4[3]); break;
                case SerializedPropertyType.Color when value is JArray ac:
                    prop.colorValue = new Color((float)ac[0], (float)ac[1], (float)ac[2],
                        ac.Count > 3 ? (float)ac[3] : 1f); break;
            }
        }
    }
}
