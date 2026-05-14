using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PeriodicAR.UI;

/// <summary>
/// One-shot editor tool: PeriodicAR → Setup Resizable Panels
///
/// For each target UI panel this tool:
///   1. Removes the old DraggableUIPanel component (if present).
///   2. Adds ResizableDraggableUIPanel and wires dragHandle → Header child.
///   3. Creates (or updates) a "ResizeHandle" child in the bottom-right corner
///      with PanelResizeHandle, an Image, and a "◢" label.
///   4. Fixes anchors to canvas-centre (0.5 / 0.5) so centerOnEnable works.
///
/// Panels targeted:
///   - CompoundCalculatorPanel  (has Header)
///   - ElectronsPanel           (has Header)
///   - PropertiesPanel          (has Header)
///   - ElementInfoPanel         (flat, no header — whole surface draggable)
///
/// NOT targeted (3D world-space, not UI):
///   - ElementInfoCard   – AR world-space 3D object
///   - BohrModelRoot     – AR world-space 3D object
/// </summary>
public static class SetupResizablePanels
{
    // UI panels that have a "Header" drag handle
    static readonly string[] PanelsWithHeader =
    {
        "CompoundCalculatorPanel",
        "ElectronsPanel",
        "PropertiesPanel",
    };

    // Flat UI panels with no header — whole surface is the drag handle
    static readonly string[] FlatPanels =
    {
        "ElementInfoPanel",
    };

    // ── Resize handle visuals ─────────────────────────────────────────────────────
    const float HANDLE_SIZE   = 44f;
    static readonly Color HANDLE_COLOR  = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color HANDLE_HOVER  = new Color(1f, 1f, 1f, 0.30f);
    static readonly Color GRIP_COLOR    = new Color(1f, 1f, 1f, 0.70f);

    // ── Entry point ───────────────────────────────────────────────────────────────

    [MenuItem("PeriodicAR/Setup Resizable Panels")]
    public static void Execute()
    {
        int count = 0;

        foreach (string name in PanelsWithHeader)
        {
            var go = FindInactiveByName(name);
            if (go == null) { Debug.LogWarning($"[SetupResizablePanels] '{name}' not found in scene."); continue; }
            ProcessPanel(go, hasHeader: true);
            count++;
        }

        foreach (string name in FlatPanels)
        {
            var go = FindInactiveByName(name);
            if (go == null) { Debug.LogWarning($"[SetupResizablePanels] '{name}' not found in scene."); continue; }
            ProcessPanel(go, hasHeader: false);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[SetupResizablePanels] Done — configured {count} panel(s).");
    }

    /// <summary>
    /// Finds a scene GameObject by exact name, including inactive and nested objects.
    /// Uses Resources.FindObjectsOfTypeAll so it works even when the object is disabled.
    /// Skips prefab assets — only returns objects that live in the active scene.
    /// </summary>
    static GameObject FindInactiveByName(string panelName)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            // Skip prefab assets (not in any scene)
            if (!rt.gameObject.scene.IsValid()) continue;
            if (rt.gameObject.scene != activeScene) continue;
            if (rt.gameObject.name == panelName)
                return rt.gameObject;
        }
        return null;
    }

    // ── Per-panel logic ───────────────────────────────────────────────────────────

    static void ProcessPanel(GameObject panel, bool hasHeader)
    {
        var rt = panel.GetComponent<RectTransform>();
        if (rt == null) { Debug.LogWarning($"[SetupResizablePanels] '{panel.name}' has no RectTransform — skipped."); return; }

        // 1. Fix anchors to canvas centre so centerOnEnable works
        FixAnchors(rt);

        // 2. Remove legacy DraggableUIPanel
        var old = panel.GetComponent<DraggableUIPanel>();
        if (old != null)
        {
            Object.DestroyImmediate(old);
            Debug.Log($"[SetupResizablePanels] Removed DraggableUIPanel from '{panel.name}'.");
        }

        // 3. Add (or get) ResizableDraggableUIPanel
        var rdp = panel.GetComponent<ResizableDraggableUIPanel>()
                  ?? panel.AddComponent<ResizableDraggableUIPanel>();

        rdp.clampToCanvas  = true;
        rdp.centerOnEnable = true;
        rdp.minSize = new Vector2(280f, 200f);
        rdp.maxSize = new Vector2(1200f, 1600f);

        // 4. Wire drag handle
        if (hasHeader)
        {
            var headerT = panel.transform.Find("Header") as RectTransform;
            if (headerT != null)
            {
                rdp.dragHandle = headerT;
                // Make the header Image raycast-transparent so clicks pass through to the panel drag handler
                // (the header itself needs to be a raycast target to receive pointer events)
                var headerImg = headerT.GetComponent<Image>();
                if (headerImg != null) headerImg.raycastTarget = true;
            }
            else
            {
                Debug.LogWarning($"[SetupResizablePanels] '{panel.name}' has no Header child — whole panel will be draggable.");
                rdp.dragHandle = null;
            }
        }
        else
        {
            rdp.dragHandle = null; // whole surface drags
        }

        // 5. Build or update the resize handle child
        var handleGO = BuildResizeHandle(panel);
        rdp.resizeHandle = handleGO.GetComponent<RectTransform>();

        EditorUtility.SetDirty(panel);
        Debug.Log($"[SetupResizablePanels] Configured '{panel.name}'.");
    }

    // ── Resize handle child ───────────────────────────────────────────────────────

    static GameObject BuildResizeHandle(GameObject panel)
    {
        // Reuse existing child if present (idempotent)
        var existing = panel.transform.Find("ResizeHandle");
        GameObject go = existing != null ? existing.gameObject : null;

        if (go == null)
        {
            go = new GameObject("ResizeHandle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(panel.transform, false);
        }

        // Remove any broken "Missing Script" components left from when PanelResizeHandle
        // was incorrectly defined inside ResizableDraggableUIPanel.cs
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        // Anchor to bottom-right corner, pivot at bottom-right
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(HANDLE_SIZE, HANDLE_SIZE);

        // Ignore VerticalLayoutGroup (panels use VLG on the root)
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        // Background image — subtle tinted square
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color         = HANDLE_COLOR;
        img.type          = Image.Type.Simple;
        img.raycastTarget = true; // must be true to receive pointer events

        // "◢" grip indicator text
        var existing_label = go.transform.Find("GripLabel");
        GameObject labelGO = existing_label != null
            ? existing_label.gameObject
            : new GameObject("GripLabel", typeof(RectTransform), typeof(CanvasRenderer));

        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(go.transform, false);

        var labelRT = (RectTransform)labelGO.transform;
        labelRT.anchorMin        = Vector2.zero;
        labelRT.anchorMax        = Vector2.one;
        labelRT.offsetMin        = Vector2.zero;
        labelRT.offsetMax        = Vector2.zero;

        var tmp = labelGO.GetComponent<TextMeshProUGUI>()
                  ?? labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = "◢";
        tmp.fontSize      = 22f;
        tmp.alignment     = TextAlignmentOptions.BottomRight;
        tmp.color         = GRIP_COLOR;
        tmp.raycastTarget = false; // let clicks pass through to the Image above

        // PanelResizeHandle behaviour
        var resizeHandle = go.GetComponent<PanelResizeHandle>();
        if (resizeHandle == null)
        {
            resizeHandle = go.AddComponent<PanelResizeHandle>();
        }

        EditorUtility.SetDirty(go);
        return go;
    }

    // ── Anchor fix ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the panel to centre-anchored (0.5 / 0.5) so that
    /// <c>anchoredPosition = Vector2.zero</c> reliably centres it on the canvas.
    /// The panel's world position is preserved during conversion.
    /// </summary>
    static void FixAnchors(RectTransform rt)
    {
        if (rt.anchorMin == new Vector2(0.5f, 0.5f) &&
            rt.anchorMax == new Vector2(0.5f, 0.5f))
            return; // already centred

        // Preserve the current world corners before changing anchors
        var parentRT = rt.parent as RectTransform;
        if (parentRT == null) return;

        // Calculate what anchoredPosition would be with centred anchors
        // by measuring the offset from the parent's centre
        Vector3[] worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);
        Vector3 worldCentre = (worldCorners[0] + worldCorners[2]) * 0.5f;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        // Convert world centre back to parent-local anchored position
        Vector2 localCentre;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT,
            RectTransformUtility.WorldToScreenPoint(null, worldCentre),
            null,
            out localCentre);

        rt.anchoredPosition = localCentre;
        EditorUtility.SetDirty(rt);
    }
}
