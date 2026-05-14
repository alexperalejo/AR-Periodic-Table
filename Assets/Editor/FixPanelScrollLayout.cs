using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PeriodicAR → Fix Panel Scroll Layout  (one-shot, idempotent)
///
/// For each resizable panel this tool guarantees the complete hierarchy:
///
///   PanelRoot  (center anchor, sizeDelta driven by ResizableDraggableUIPanel)
///   ├── Header      anchorMin=(0,1) anchorMax=(1,1)  pivot=(0.5,1)  h=70
///   ├── Body        anchorMin=(0,0) anchorMax=(1,1)  full stretch with insets
///   │    └── ScrollView   full-stretch inside Body
///   │         └── Viewport full-stretch, Mask
///   │              └── Content  anchorMin=(0,1) anchorMax=(1,1)  top-stretch
///   │                   ├── VLG  childControlWidth/Height=true, forceExpandWidth=true
///   │                   ├── ContentSizeFitter  verticalFit=PreferredSize
///   │                   └── [text rows]  enableAutoSizing, word-wrap, no fixed size
///   ├── ButtonRow   anchorMin=(0,0) anchorMax=(1,0)  pivot=(0.5,0)  h=86
///   └── ResizeHandle  anchorMin=(1,0) anchorMax=(1,0)  pivot=(1,0)
///
/// Existing text children of Body that are NOT yet inside a ScrollView are
/// moved into Content automatically — TMP_Text references on UI scripts stay
/// valid because only the parent changes, not the GameObject itself.
///
/// Run: PeriodicAR → Fix Panel Scroll Layout
/// </summary>
public static class FixPanelScrollLayout
{
    // ── Constants (must match RebuildModePanels) ──────────────────────────────────
    const float HEADER_HEIGHT    = 70f;
    const float BTN_ROW_HEIGHT   = 86f;   // 60 + 26 (top+bottom padding)
    const float CONTENT_PADDING  = 20f;
    const float CONTENT_SPACING  = 12f;

    const float TMP_FONT_SIZE_MIN = 12f;
    const float TMP_FONT_SIZE_MAX = 24f;  // floor; actual max is max(existing, this)

    static readonly string[] Panels =
    {
        "CompoundCalculatorPanel",
        "ElectronsPanel",
        "PropertiesPanel",
        "ElementInfoPanel",
    };

    // ── Entry point ───────────────────────────────────────────────────────────────

    [MenuItem("PeriodicAR/Fix Panel Scroll Layout")]
    public static void Execute()
    {
        int fixed_ = 0;
        foreach (string name in Panels)
        {
            var go = FindInactiveByName(name);
            if (go == null)
            {
                Debug.LogWarning($"[FixPanelScrollLayout] '{name}' not found — skipped.");
                continue;
            }
            FixPanel(go);
            fixed_++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[FixPanelScrollLayout] Done — fixed {fixed_} panel(s).");
    }

    // ── Per-panel ─────────────────────────────────────────────────────────────────

    static void FixPanel(GameObject panel)
    {
        Debug.Log($"[FixPanelScrollLayout] Fixing '{panel.name}'…");

        // 1. Remove stale root VerticalLayoutGroup — children use anchor-based layout.
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Object.DestroyImmediate(vlg);

        // 2. Fix outer layer anchors.
        FixOuterAnchors(panel);

        // 3. Build / fix the ScrollRect hierarchy inside Body.
        var body = panel.transform.Find("Body") as RectTransform;
        if (body == null)
        {
            Debug.LogWarning($"[FixPanelScrollLayout] '{panel.name}' has no Body child — skipped inner layout.");
            return;
        }

        // Remove Body's LayoutElement — it was for the old root VLG and is now dead weight.
        var bodyLE = body.GetComponent<LayoutElement>();
        if (bodyLE != null) Object.DestroyImmediate(bodyLE);

        // Remove Body's VerticalLayoutGroup — Body used to hold text rows directly, so it
        // had a VLG.  Now Body only contains the ScrollView (full-stretch anchors) and the
        // VLG fights that: VLG stacks children top-down and doesn't control height when
        // childControlHeight=false, but VLG still overrides the child's Y position and
        // reports 0 preferred height for the ScrollRect, making the scroll area invisible.
        var bodyVLG = body.GetComponent<VerticalLayoutGroup>();
        if (bodyVLG != null) Object.DestroyImmediate(bodyVLG);

        var content = EnsureScrollRectInBody(body);
        if (content == null) return;

        // 4. Fix the VLG / ContentSizeFitter on Content.
        FixContentLayout(content);

        // 5. Fix every TMP_Text inside Content.
        FixAllTMP(content);

        EditorUtility.SetDirty(panel);
        Debug.Log($"[FixPanelScrollLayout] '{panel.name}' done.");
    }

    // ── Outer anchor layer ────────────────────────────────────────────────────────

    static void FixOuterAnchors(GameObject panel)
    {
        var header     = panel.transform.Find("Header")      as RectTransform;
        var body       = panel.transform.Find("Body")        as RectTransform;
        var btnRow     = panel.transform.Find("ButtonRow")   as RectTransform;
        var resizeHandle = panel.transform.Find("ResizeHandle") as RectTransform;

        // Header — top-stretch, fixed height
        if (header != null)
        {
            header.anchorMin        = new Vector2(0f, 1f);
            header.anchorMax        = new Vector2(1f, 1f);
            header.pivot            = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta        = new Vector2(0f, HEADER_HEIGHT);
            EditorUtility.SetDirty(header);
        }

        // Body — full stretch, with insets that leave room for Header at top
        // and ButtonRow at bottom.
        if (body != null)
        {
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.pivot     = new Vector2(0.5f, 0.5f);
            body.offsetMin = new Vector2(0f, BTN_ROW_HEIGHT);
            body.offsetMax = new Vector2(0f, -HEADER_HEIGHT);
            EditorUtility.SetDirty(body);
        }

        // ButtonRow — bottom-stretch, fixed height
        if (btnRow != null)
        {
            btnRow.anchorMin        = new Vector2(0f, 0f);
            btnRow.anchorMax        = new Vector2(1f, 0f);
            btnRow.pivot            = new Vector2(0.5f, 0f);
            btnRow.anchoredPosition = Vector2.zero;
            btnRow.sizeDelta        = new Vector2(0f, BTN_ROW_HEIGHT);

            // Ensure HorizontalLayoutGroup is configured to fill the row.
            var hlg = btnRow.GetComponent<HorizontalLayoutGroup>()
                      ?? btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth     = true;
            hlg.childControlHeight    = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            EditorUtility.SetDirty(btnRow);
        }

        // ResizeHandle — bottom-right corner
        if (resizeHandle != null)
        {
            resizeHandle.anchorMin        = new Vector2(1f, 0f);
            resizeHandle.anchorMax        = new Vector2(1f, 0f);
            resizeHandle.pivot            = new Vector2(1f, 0f);
            resizeHandle.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(resizeHandle);
        }
    }

    // ── ScrollRect hierarchy inside Body ─────────────────────────────────────────

    /// <summary>
    /// Guarantees Body → ScrollView → Viewport → Content exists.
    /// If there are existing direct text/layout children in Body (legacy layout),
    /// they are moved into Content so TMP_Text references on UI scripts stay valid.
    /// Returns the Content GameObject.
    /// </summary>
    static RectTransform EnsureScrollRectInBody(RectTransform body)
    {
        // ── Case A: ScrollView already exists — do a full re-verification ───────────
        var existingSV = body.Find("ScrollView");
        if (existingSV != null)
        {
            var svRT = existingSV as RectTransform;
            var svGO = existingSV.gameObject;
            svGO.SetActive(true);
            EditorUtility.SetDirty(svGO);
            FixScrollViewAnchors(svRT);

            // Ensure ScrollRect is configured.
            var svRect = svGO.GetComponent<ScrollRect>() ?? svGO.AddComponent<ScrollRect>();
            svRect.horizontal        = false;
            svRect.vertical          = true;
            svRect.movementType      = ScrollRect.MovementType.Clamped;
            svRect.inertia           = true;
            svRect.decelerationRate  = 0.135f;
            svRect.scrollSensitivity = 40f;

            var svImg = svGO.GetComponent<Image>() ?? svGO.AddComponent<Image>();
            svImg.color         = Color.clear;
            svImg.raycastTarget = true;
            EditorUtility.SetDirty(svGO);

            // ── Viewport ──────────────────────────────────────────────────────────
            var vpT = existingSV.Find("Viewport");
            if (vpT == null)
            {
                Debug.LogWarning($"[FixPanelScrollLayout] No Viewport under ScrollView in '{body.name}' — leaving as-is.");
                return null;
            }
            var aVpRT = vpT as RectTransform;
            var aVpGO = vpT.gameObject;
            aVpGO.SetActive(true);
            FixViewportAnchors(aVpRT);

            // Replace legacy Mask+Image with RectMask2D — simpler, no stencil issues.
            var oldMask = aVpGO.GetComponent<Mask>();
            if (oldMask != null) Object.DestroyImmediate(oldMask);
            var oldImg  = aVpGO.GetComponent<Image>();
            if (oldImg  != null) Object.DestroyImmediate(oldImg);
            if (aVpGO.GetComponent<RectMask2D>() == null)
                aVpGO.AddComponent<RectMask2D>();
            EditorUtility.SetDirty(aVpGO);

            // ── Content ───────────────────────────────────────────────────────────
            var ctT = vpT.Find("Content");
            if (ctT == null)
            {
                Debug.LogWarning($"[FixPanelScrollLayout] No Content under Viewport in '{body.name}' — leaving as-is.");
                return null;
            }
            var aCtRT = ctT as RectTransform;
            var aCtGO = ctT.gameObject;
            aCtGO.SetActive(true);
            EditorUtility.SetDirty(aCtGO);

            // Wire ScrollRect → Viewport and Content.
            svRect.viewport = aVpRT;
            svRect.content  = aCtRT;
            EditorUtility.SetDirty(svRect);

            Debug.Log($"[FixPanelScrollLayout] Case A complete for '{body.name}'. Viewport='{aVpGO.name}' Content='{aCtGO.name}'.");
            return aCtRT;
        }

        // ── Case B: No ScrollView yet — collect existing children, then build ──────
        // Gather all direct children that should become content rows.
        // Skip any LayoutElement-only helper objects that should stay on Body.
        var toMove = new List<Transform>();
        for (int i = 0; i < body.childCount; i++)
        {
            var child = body.GetChild(i);
            // Don't move the scroll hierarchy itself (just built) or resize handle.
            if (child.name == "ScrollView" || child.name == "ResizeHandle") continue;
            toMove.Add(child);
        }

        // ── Build ScrollView ──────────────────────────────────────────────────────
        var scrollGO = new GameObject("ScrollView",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrollGO.layer = LayerMask.NameToLayer("UI");
        scrollGO.transform.SetParent(body, false);
        FixScrollViewAnchors((RectTransform)scrollGO.transform);

        var scrollImg = scrollGO.GetComponent<Image>();
        scrollImg.color         = Color.clear;
        scrollImg.raycastTarget = true; // ScrollRect needs a raycast-target background

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal       = false;
        sr.vertical         = true;
        sr.movementType     = ScrollRect.MovementType.Clamped;
        sr.inertia          = true;
        sr.decelerationRate = 0.135f;
        sr.scrollSensitivity = 40f;

        // ── Build Viewport ────────────────────────────────────────────────────────
        var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = LayerMask.NameToLayer("UI");
        vpGO.transform.SetParent(scrollGO.transform, false);
        FixViewportAnchors((RectTransform)vpGO.transform);

        // ── Build Content ─────────────────────────────────────────────────────────
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.layer = LayerMask.NameToLayer("UI");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = Vector2.zero;

        // ── Wire ScrollRect ───────────────────────────────────────────────────────
        sr.viewport = (RectTransform)vpGO.transform;
        sr.content  = contentRT;

        // ── Move legacy children into Content ─────────────────────────────────────
        foreach (var child in toMove)
        {
            child.SetParent(contentGO.transform, false);
            // Anchors will be fixed by FixAllTMP / FixContentLayout below.
        }

        EditorUtility.SetDirty(scrollGO);
        EditorUtility.SetDirty(vpGO);
        EditorUtility.SetDirty(contentGO);
        return contentRT;
    }

    // ── Anchor helpers ────────────────────────────────────────────────────────────

    static void FixScrollViewAnchors(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);
    }

    static void FixViewportAnchors(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0f, 1f); // top-left required by ScrollRect
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);
    }

    // ── Content VLG + ContentSizeFitter ──────────────────────────────────────────

    static void FixContentLayout(RectTransform contentRT)
    {
        var go = contentRT.gameObject;

        // Ensure anchors are top-stretch (Content must be as wide as Viewport).
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = Vector2.zero; // height set by ContentSizeFitter

        // VLG: sets each row's WIDTH and HEIGHT from TMP's preferredHeight.
        // childControlHeight=true is the key — it asks TMP for GetPreferredHeight()
        // AFTER the width has been committed, so word-wrap is baked in correctly.
        var vlg = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(
            (int)CONTENT_PADDING, (int)CONTENT_PADDING,
            (int)CONTENT_PADDING, (int)CONTENT_PADDING);
        vlg.spacing             = CONTENT_SPACING;
        vlg.childAlignment      = TextAnchor.UpperLeft;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = true;  // critical: height from TMP preferred height
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter makes Content as tall as its children.
        var csf = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // width from anchors
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        EditorUtility.SetDirty(go);
    }

    // ── TMP text fixes ────────────────────────────────────────────────────────────

    /// <summary>
    /// Recursively visits every TMP_Text component inside <paramref name="content"/>
    /// and applies correct settings + removes conflicting LayoutElement overrides.
    /// </summary>
    static void FixAllTMP(RectTransform content)
    {
        foreach (var tmp in content.GetComponentsInChildren<TMP_Text>(includeInactive: true))
        {
            var go = tmp.gameObject;

            // ── TMP settings ──────────────────────────────────────────────────────
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = TMP_FONT_SIZE_MIN;
            // Keep existing fontSizeMax if it's higher (designer intention).
            tmp.fontSizeMax      = Mathf.Max(tmp.fontSizeMax, TMP_FONT_SIZE_MAX);

            tmp.textWrappingMode = TextWrappingModes.Normal;   // always wrap
            tmp.overflowMode     = TextOverflowModes.Overflow; // report full preferred height

            // ── RectTransform anchors ─────────────────────────────────────────────
            // Inside a VLG with childControlWidth/Height=true, the VLG drives position
            // and size; anchors don't affect layout behaviour.  We still set them to
            // top-stretch so the intent is readable and there are no stale fixed values.
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);

            // ── LayoutElement — remove fixed preferredHeight ──────────────────────
            // A LayoutElement.preferredHeight freezes the row at that pixel count and
            // prevents TMP's natural word-wrap height from being used.  Remove the
            // LayoutElement entirely; use SetLayoutElement (via RebuildModePanels) only
            // for rows that genuinely need a minimum height (e.g. summary / description).
            var le = go.GetComponent<LayoutElement>();
            if (le != null)
            {
                // If the LayoutElement was only carrying a preferredHeight / flexibleHeight,
                // removing it is safe.  If it carries a useful minHeight (> 0), keep it but
                // clear the preferredHeight override so TMP can breathe.
                if (le.minHeight > 0f)
                {
                    le.preferredHeight = -1f;  // clear fixed override, keep minimum
                    le.flexibleHeight  = -1f;
                }
                else
                {
                    Object.DestroyImmediate(le);
                }
            }

            EditorUtility.SetDirty(go);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

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
