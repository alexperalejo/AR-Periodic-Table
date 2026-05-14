using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PeriodicAR → Fix Panel Child Anchors
///
/// Switches the four resizable panels from VerticalLayoutGroup root layout to
/// proper RectTransform anchor-based layout.  This makes Header / Body / ButtonRow
/// auto-reposition and auto-resize whenever ResizableDraggableUIPanel changes the
/// panel's sizeDelta — no per-frame layout rebuild required.
///
/// Layout produced:
///   Header     – top-stretch, fixed height (anchorMin/Max = (0,1)/(1,1))
///   Body       – full-stretch with insets for header + button row
///   ButtonRow  – bottom-stretch, fixed height (anchorMin/Max = (0,0)/(1,0))
///   ResizeHandle – bottom-right corner (unchanged)
///   Buttons inside ButtonRow – driven by HorizontalLayoutGroup, stretch with row
///
/// Idempotent — safe to run multiple times.
/// </summary>
public static class FixPanelChildAnchors
{
    // Must match the constants used when the panels were built in RebuildModePanels.
    const float HEADER_HEIGHT     = 70f;
    const float BUTTON_ROW_TOTAL  = 86f;  // BUTTON_ROW_HEIGHT (60) + padding (26)

    static readonly string[] AllPanels =
    {
        "CompoundCalculatorPanel",
        "ElectronsPanel",
        "PropertiesPanel",
        "ElementInfoPanel",
    };

    [MenuItem("PeriodicAR/Fix Panel Child Anchors")]
    public static void Execute()
    {
        int count = 0;
        foreach (string panelName in AllPanels)
        {
            var go = FindInactiveByName(panelName);
            if (go == null)
            {
                Debug.LogWarning($"[FixPanelChildAnchors] '{panelName}' not found in active scene — skipped.");
                continue;
            }
            FixPanel(go);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[FixPanelChildAnchors] Done — fixed {count} panel(s).");
    }

    // ── Per-panel ─────────────────────────────────────────────────────────────────

    static void FixPanel(GameObject panel)
    {
        // 1. Remove VerticalLayoutGroup from the panel root.
        //    With anchor-based children, a root VLG fights the anchors and
        //    re-stacks children from the top, preventing ButtonRow from staying
        //    at the bottom when the panel is resized.
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            Object.DestroyImmediate(vlg);
            Debug.Log($"[FixPanelChildAnchors] Removed root VerticalLayoutGroup from '{panel.name}'.");
        }

        // 2. Header — top-stretch, fixed height ──────────────────────────────────
        //    Pivot at top so anchoredPosition = (0,0) pins it to the panel top.
        var header = panel.transform.Find("Header") as RectTransform;
        if (header != null)
        {
            header.anchorMin        = new Vector2(0f, 1f);
            header.anchorMax        = new Vector2(1f, 1f);
            header.pivot            = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta        = new Vector2(0f, HEADER_HEIGHT);
            EditorUtility.SetDirty(header);
        }

        // 3. Body — full-stretch with insets ─────────────────────────────────────
        //    offsetMin / offsetMax directly control the four edges:
        //      offsetMin.y = BUTTON_ROW_TOTAL  → bottom edge sits above button row
        //      offsetMax.y = -HEADER_HEIGHT     → top edge sits below header
        var body = panel.transform.Find("Body") as RectTransform;
        if (body != null)
        {
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.pivot     = new Vector2(0.5f, 0.5f);
            body.offsetMin = new Vector2(0f,  BUTTON_ROW_TOTAL);
            body.offsetMax = new Vector2(0f, -HEADER_HEIGHT);
            EditorUtility.SetDirty(body);
        }

        // 4. ButtonRow — bottom-stretch, fixed height ────────────────────────────
        //    Pivot at bottom so anchoredPosition = (0,0) pins it to the panel bottom.
        var btnRow = panel.transform.Find("ButtonRow") as RectTransform;
        if (btnRow != null)
        {
            btnRow.anchorMin        = new Vector2(0f, 0f);
            btnRow.anchorMax        = new Vector2(1f, 0f);
            btnRow.pivot            = new Vector2(0.5f, 0f);
            btnRow.anchoredPosition = Vector2.zero;
            btnRow.sizeDelta        = new Vector2(0f, BUTTON_ROW_TOTAL);

            // Ensure HorizontalLayoutGroup exists and stretches buttons across the row.
            var hlg = btnRow.GetComponent<HorizontalLayoutGroup>()
                      ?? btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding             = new RectOffset(20, 20, 10, 16);
            hlg.spacing             = 16f;
            hlg.childAlignment      = TextAnchor.MiddleCenter;
            hlg.childControlWidth   = true;
            hlg.childControlHeight  = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            EditorUtility.SetDirty(btnRow);
        }

        // 5. ResizeHandle — re-confirm bottom-right corner ───────────────────────
        //    SetupResizablePanels already places it correctly, but enforce it here
        //    so this tool is self-contained.
        var resizeHandle = panel.transform.Find("ResizeHandle") as RectTransform;
        if (resizeHandle != null)
        {
            resizeHandle.anchorMin        = new Vector2(1f, 0f);
            resizeHandle.anchorMax        = new Vector2(1f, 0f);
            resizeHandle.pivot            = new Vector2(1f, 0f);
            resizeHandle.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(resizeHandle);
        }

        EditorUtility.SetDirty(panel);
        Debug.Log($"[FixPanelChildAnchors] Fixed '{panel.name}'.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// Finds a scene object by exact name including inactive objects, skipping prefab assets.
    static GameObject FindInactiveByName(string objName)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (!rt.gameObject.scene.IsValid()) continue;
            if (rt.gameObject.scene != activeScene) continue;
            if (rt.gameObject.name == objName) return rt.gameObject;
        }
        return null;
    }
}
