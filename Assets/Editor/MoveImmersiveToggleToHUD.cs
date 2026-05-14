// Assets/Editor/MoveImmersiveToggleToHUD.cs
//
// Moves BtnImmersiveToggle out of MenuPanel and onto a dedicated persistent
// screen-space canvas so it is visible in every mode.
//
// Run: PeriodicAR → Move Immersive Toggle to HUD
//
// What it does
// ─────────────
//  1. Finds and destroys any BtnImmersiveToggle that lives inside MenuPanel
//     (the old placement from SetupImmersiveMode).
//
//  2. Finds or rebuilds "ImmersiveToggleCanvas" — a ScreenSpaceOverlay Canvas
//     at scene root with sortingOrder 190.  Because it is NOT a child of
//     MenuPanel, ARModeManager.HideAllPanels() never touches it.
//
//  3. Creates BtnImmersiveToggle on that canvas, centred at the bottom of
//     the screen (anchor bottom-centre, 20 px above the edge).
//
//  4. Wires ImmersiveModeManager.immersiveButtonLabel → the new button's
//     TMP_Text so RefreshButtonLabel() keeps the text current.
//
//  5. Wires the button's onClick → ImmersiveModeManager.ToggleImmersiveMode
//     as a single persistent listener (removes any stale listeners first).
//
//  6. Nulls out MenuUI.btnImmersiveToggle (it no longer lives there).
//
//  7. Marks scene dirty and saves.
//
// Safe to re-run — destroys and recreates ImmersiveToggleCanvas each time.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MoveImmersiveToggleToHUD
{
    [MenuItem("PeriodicAR/Move Immersive Toggle to HUD")]
    public static void Execute()
    {
        // ── Locate required components ────────────────────────────────────────────
        var imm = Object.FindFirstObjectByType<ImmersiveModeManager>(FindObjectsInactive.Include);
        if (imm == null)
        {
            Debug.LogError("[MoveImmersiveToggleToHUD] ImmersiveModeManager not found. " +
                           "Run PeriodicAR → Setup Immersive Mode first.");
            return;
        }

        var menuUI = Object.FindFirstObjectByType<MenuUI>(FindObjectsInactive.Include);

        // ── 1. Remove old BtnImmersiveToggle from MenuPanel ───────────────────────
        RemoveOldToggleButton(menuUI);

        // ── 2. Destroy previous ImmersiveToggleCanvas (idempotent) ───────────────
        var old = GameObject.Find("ImmersiveToggleCanvas");
        if (old != null)
        {
            Object.DestroyImmediate(old);
            Debug.Log("[MoveImmersiveToggleToHUD] Removed previous ImmersiveToggleCanvas.");
        }

        // ── 3. Create persistent canvas ───────────────────────────────────────────
        var canvasGO = new GameObject("ImmersiveToggleCanvas");
        // Scene root — not parented to MenuPanel or any panel GO, so HideAllPanels()
        // cannot reach it regardless of how the hierarchy changes in future.

        var canvas          = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;   // above panels (≤101), below close/scale HUDs

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 4. Create the button ──────────────────────────────────────────────────
        var btnGO   = new GameObject("BtnImmersiveToggle");
        btnGO.layer = LayerMask.NameToLayer("UI");
        btnGO.transform.SetParent(canvasGO.transform, false);
        btnGO.AddComponent<CanvasRenderer>();

        // Bottom-centre of the screen, 20 px above the edge.
        var rt           = btnGO.AddComponent<RectTransform>();
        rt.anchorMin     = new Vector2(0.5f, 0f);
        rt.anchorMax     = new Vector2(0.5f, 0f);
        rt.pivot         = new Vector2(0.5f, 0f);
        rt.sizeDelta     = new Vector2(220f, 60f);
        rt.anchoredPosition = new Vector2(0f, 20f);

        var img   = btnGO.AddComponent<Image>();
        img.color = new Color(0.12f, 0.38f, 0.68f, 0.92f);   // blue — "this is a mode toggle"

        var btn    = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.12f, 0.38f, 0.68f, 0.92f);
        colors.highlightedColor = new Color(0.18f, 0.52f, 0.88f, 1.00f);
        colors.pressedColor     = new Color(0.08f, 0.25f, 0.50f, 1.00f);
        colors.selectedColor    = colors.normalColor;
        btn.colors              = colors;

        // ── 5. Label ──────────────────────────────────────────────────────────────
        var labelGO   = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(btnGO.transform, false);
        labelGO.AddComponent<CanvasRenderer>();

        var labelRT        = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin  = Vector2.zero;
        labelRT.anchorMax  = Vector2.one;
        labelRT.offsetMin  = new Vector2(8f, 4f);
        labelRT.offsetMax  = new Vector2(-8f, -4f);

        var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = "Immersive: OFF";
        tmp.fontSize   = 20f;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.color      = Color.white;
        tmp.fontStyle  = FontStyles.Bold;

        // ── 6. Wire immersiveButtonLabel on ImmersiveModeManager ─────────────────
        var immSO     = new SerializedObject(imm);
        var labelProp = immSO.FindProperty("immersiveButtonLabel");
        if (labelProp != null)
        {
            labelProp.objectReferenceValue = tmp;
            immSO.ApplyModifiedProperties();
            Debug.Log("[MoveImmersiveToggleToHUD] Wired immersiveButtonLabel.");
        }
        else
        {
            Debug.LogWarning("[MoveImmersiveToggleToHUD] 'immersiveButtonLabel' property not found on " +
                             "ImmersiveModeManager — label text will not auto-update.");
        }

        // ── 7. Wire onClick → ToggleImmersiveMode (single persistent listener) ────
        btn.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(btn.onClick, imm.ToggleImmersiveMode);
        EditorUtility.SetDirty(btn);
        Debug.Log("[MoveImmersiveToggleToHUD] Wired onClick → ImmersiveModeManager.ToggleImmersiveMode.");

        // ── 8. Null out MenuUI.btnImmersiveToggle (no longer lives there) ─────────
        if (menuUI != null)
        {
            var menuSO  = new SerializedObject(menuUI);
            var btnProp = menuSO.FindProperty("btnImmersiveToggle");
            if (btnProp != null)
            {
                btnProp.objectReferenceValue = null;
                menuSO.ApplyModifiedProperties();
                Debug.Log("[MoveImmersiveToggleToHUD] Cleared MenuUI.btnImmersiveToggle reference.");
            }
        }

        // ── 9. Save ───────────────────────────────────────────────────────────────
        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[MoveImmersiveToggleToHUD] Done — BtnImmersiveToggle is now on " +
                  "ImmersiveToggleCanvas (sortingOrder 190, bottom-centre, always visible).");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches the MenuUI hierarchy (and the whole scene as fallback) for any
    /// GameObject named "BtnImmersiveToggle" and destroys it.
    /// </summary>
    private static void RemoveOldToggleButton(MenuUI menuUI)
    {
        // Primary: look inside MenuPanel hierarchy
        if (menuUI != null)
        {
            var found = FindDeepChild(menuUI.transform, "BtnImmersiveToggle");
            if (found != null)
            {
                Object.DestroyImmediate(found.gameObject);
                Debug.Log("[MoveImmersiveToggleToHUD] Removed BtnImmersiveToggle from MenuPanel.");
                return;
            }
        }

        // Fallback: scene-wide search (covers old placements in other parents)
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.IsValid()) continue;
            if (t.gameObject.name != "BtnImmersiveToggle") continue;
            // Skip if it is already on a canvas we just created
            if (t.GetComponentInParent<Canvas>()?.name == "ImmersiveToggleCanvas") continue;

            Object.DestroyImmediate(t.gameObject);
            Debug.Log("[MoveImmersiveToggleToHUD] Removed BtnImmersiveToggle from scene (fallback search).");
            return;
        }

        Debug.Log("[MoveImmersiveToggleToHUD] No old BtnImmersiveToggle found — nothing to remove.");
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (t != parent && t.name == childName) return t;
        }
        return null;
    }
}
