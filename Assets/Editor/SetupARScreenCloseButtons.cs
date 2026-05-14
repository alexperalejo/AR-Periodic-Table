// Assets/Editor/SetupARScreenCloseButtons.cs
//
// One-shot idempotent setup for screen-space AR close buttons.
//
// Run: PeriodicAR → Setup AR Screen Close Buttons
//
// What it does
// ─────────────
//  1. REMOVES the old world-space BohrCloseButton and InfoCloseButton GOs
//     (parented to ARModeManager) if they exist, so there are no duplicates.
//
//  2. Creates "ARCloseButtonsCanvas" — a ScreenSpaceOverlay Canvas at the
//     scene root with sortingOrder 200.
//
//  3. Inside the canvas creates:
//       BtnCloseAtom  (top-right corner, y = -20 from top)
//       BtnCloseInfo  (top-right corner, y = -80 from top)
//     Both start hidden (SetActive false); ARCloseButtonsHUD drives visibility.
//
//  4. Adds ARCloseButtonsHUD to the canvas root GO and wires:
//       • bohrModelRoot   → from ARModeManager serialized field
//       • elementInfoCard → from ARModeManager serialized field
//       • btnCloseAtom    → BtnCloseAtom GO
//       • btnCloseInfo    → BtnCloseInfo GO
//
//  5. Wires each Button.onClick → ARCloseButtonsHUD.CloseAtom / CloseInfo
//     as persistent listeners.
//
//  6. Marks scene dirty and saves.
//
// Re-running this tool is safe: if the canvas already exists it is destroyed
// and rebuilt cleanly (idempotent).

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupARScreenCloseButtons
{
    // ── Button visual settings ────────────────────────────────────────────────────

    private const float BtnWidth    = 160f;
    private const float BtnHeight   =  52f;
    private const float BtnMarginX  =  16f;   // px from right edge
    private const float BtnSpacingY =  64f;   // px between button tops
    // HandOverlayToggle sits top-right at (-40,-40), 380×110 px → bottom edge at 150 px from top.
    // Start close buttons at 170 px so they clear it with a comfortable gap.
    private const float BtnTopY     = 170f;

    // ── Entry point ───────────────────────────────────────────────────────────────

    [MenuItem("PeriodicAR/Setup AR Screen Close Buttons")]
    public static void Execute()
    {
        // ── Locate ARModeManager ──────────────────────────────────────────────────
        var arm = Object.FindFirstObjectByType<ARModeManager>(FindObjectsInactive.Include);
        if (arm == null)
        {
            Debug.LogError("[SetupARScreenCloseButtons] ARModeManager not found — aborting.");
            return;
        }

        // ── Read ARModeManager serialized fields ──────────────────────────────────
        var armSO        = new SerializedObject(arm);
        var bohrProp     = armSO.FindProperty("bohrModelRoot");
        var cardProp     = armSO.FindProperty("elementInfoCard");

        GameObject bohrRoot   = bohrProp?.objectReferenceValue as GameObject;
        var        cardComp   = cardProp?.objectReferenceValue as ElementInfoCard;
        GameObject cardRoot   = cardComp != null ? cardComp.gameObject : null;

        if (bohrRoot == null)
            Debug.LogWarning("[SetupARScreenCloseButtons] bohrModelRoot not assigned in ARModeManager — " +
                             "assign it and re-run.");
        if (cardRoot == null)
            Debug.LogWarning("[SetupARScreenCloseButtons] elementInfoCard not assigned in ARModeManager — " +
                             "assign it and re-run.");

        // ── 1. Remove old world-space close button GOs ────────────────────────────
        RemoveOldWorldSpaceButtons(arm.transform);

        // ── 2. Destroy any existing screen-space canvas we created before ─────────
        var existing = GameObject.Find("ARCloseButtonsCanvas");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[SetupARScreenCloseButtons] Removed old ARCloseButtonsCanvas.");
        }

        // ── 3. Create the screen-space Canvas ────────────────────────────────────
        var canvasGO = new GameObject("ARCloseButtonsCanvas");
        // Leave at scene root — ScreenSpaceOverlay rendering is independent of
        // transform hierarchy, but scene-root placement avoids accidental scale
        // or active-state inheritance from a parent.

        var canvas            = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder   = 195;   // above MenuCanvas (≤101), below scale HUD (210)

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 4. Create buttons ─────────────────────────────────────────────────────
        var hudComp = canvasGO.AddComponent<ARCloseButtonsHUD>();

        GameObject atomBtn = CreateCloseButton(canvasGO.transform, "BtnCloseAtom",
            "Close Atom", BtnTopY);
        GameObject infoBtn = CreateCloseButton(canvasGO.transform, "BtnCloseInfo",
            "Close Info", BtnTopY + BtnSpacingY);

        // Start hidden — ARCloseButtonsHUD.Update() drives visibility.
        atomBtn.SetActive(false);
        infoBtn.SetActive(false);

        // ── 5. Wire ARCloseButtonsHUD ─────────────────────────────────────────────
        var hudSO = new SerializedObject(hudComp);
        SetObj(hudSO, "bohrModelRoot",   bohrRoot);
        SetObj(hudSO, "elementInfoCard", cardRoot);
        SetObj(hudSO, "btnCloseAtom",    atomBtn);
        SetObj(hudSO, "btnCloseInfo",    infoBtn);
        hudSO.ApplyModifiedProperties();

        // ── 6. Wire persistent onClick listeners ──────────────────────────────────
        WireButton(atomBtn, hudComp, nameof(ARCloseButtonsHUD.CloseAtom));
        WireButton(infoBtn, hudComp, nameof(ARCloseButtonsHUD.CloseInfo));

        // ── 7. Dirty + save ───────────────────────────────────────────────────────
        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupARScreenCloseButtons] Done.\n" +
                  $"  bohrModelRoot   = {(bohrRoot != null ? bohrRoot.name   : "(unassigned)")}\n" +
                  $"  elementInfoCard = {(cardRoot != null ? cardRoot.name   : "(unassigned)")}\n" +
                  $"  Canvas sortingOrder = {canvas.sortingOrder}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds and destroys BohrCloseButton and InfoCloseButton children of the
    /// ARModeManager transform — the old world-space close button GOs.
    /// </summary>
    private static void RemoveOldWorldSpaceButtons(Transform armTransform)
    {
        string[] oldNames = { "BohrCloseButton", "InfoCloseButton" };
        foreach (string n in oldNames)
        {
            Transform old = armTransform.Find(n);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
                Debug.Log($"[SetupARScreenCloseButtons] Removed world-space '{n}'.");
            }
        }
    }

    /// <summary>
    /// Creates a button at the top-right of the screen.
    /// <paramref name="topOffset"/> is the distance down from the top edge in pixels.
    /// </summary>
    private static GameObject CreateCloseButton(Transform parent, string goName,
                                                string label, float topOffset)
    {
        // ── Root ──────────────────────────────────────────────────────────────────
        var root   = new GameObject(goName);
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(parent, false);

        var rt         = root.AddComponent<RectTransform>();
        rt.anchorMin   = new Vector2(1f, 1f);   // top-right anchor
        rt.anchorMax   = new Vector2(1f, 1f);
        rt.pivot       = new Vector2(1f, 1f);
        rt.sizeDelta   = new Vector2(BtnWidth, BtnHeight);
        // anchoredPosition: negative X = move left from right edge,
        //                   negative Y = move down from top edge.
        rt.anchoredPosition = new Vector2(-BtnMarginX, -topOffset);

        root.AddComponent<CanvasRenderer>();

        var img   = root.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        img.raycastTarget = true;

        var btn    = root.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.10f, 0.10f, 0.10f, 0.88f);
        colors.highlightedColor = new Color(0.80f, 0.20f, 0.18f, 0.95f);  // red on hover
        colors.pressedColor     = new Color(0.55f, 0.08f, 0.06f, 1.00f);
        colors.selectedColor    = colors.normalColor;
        btn.colors              = colors;

        // ── Label ─────────────────────────────────────────────────────────────────
        var labelGO   = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.AddComponent<CanvasRenderer>();

        var labelRT        = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin  = Vector2.zero;
        labelRT.anchorMax  = Vector2.one;
        labelRT.offsetMin  = new Vector2(8f,  4f);
        labelRT.offsetMax  = new Vector2(-8f, -4f);

        var tmp        = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = label;
        tmp.fontSize   = 20f;
        tmp.alignment  = TMPro.TextAlignmentOptions.Center;
        tmp.color      = Color.white;
        tmp.fontStyle  = TMPro.FontStyles.Bold;

        EditorUtility.SetDirty(root);
        return root;
    }

    private static void WireButton(GameObject btnGO, ARCloseButtonsHUD hud, string methodName)
    {
        var btn = btnGO.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();

        // Use reflection to get the MethodInfo and let UnityEventTools wire a
        // persistent listener — this appears in the Inspector and survives recompile.
        var method = typeof(ARCloseButtonsHUD).GetMethod(methodName);
        if (method == null)
        {
            Debug.LogWarning($"[SetupARScreenCloseButtons] Could not find method '{methodName}' on ARCloseButtonsHUD.");
            return;
        }

        var del = (UnityEngine.Events.UnityAction)
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), hud, method);

        UnityEventTools.AddPersistentListener(btn.onClick, del);
        EditorUtility.SetDirty(btn);
    }

    private static void SetObj(SerializedObject so, string prop, Object val)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = val;
    }
}
