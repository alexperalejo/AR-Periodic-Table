/// <summary>
/// One-shot setup: Tools → PeriodicAR → Add UX Fix Buttons
///
/// Adds three new buttons required by the 5-bug UX fix:
///   • LockButton      — in ARHudCanvas, wired to ARHudController.lockButton
///   • CancelButton    — in ARHudCanvas, wired to ARHudController.cancelPlacementButton
///   • BackspaceButton — in CompoundCalculatorPanel, wired to CompoundCalculatorUI.btnBackspace
///
/// Safe to run multiple times: skips creation if a button with that name already exists.
/// </summary>
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class AddUXFixButtons
{
    [MenuItem("Tools/PeriodicAR/Add UX Fix Buttons (Bugs 1, 4, 5)")]
    static void Run()
    {
        int changed = 0;

        // ── 1. ARHudCanvas prefab: Lock button + Cancel button ────────────────────
        string prefabPath = "Assets/Prefabs/UI/ARHudCanvas.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[AddUXFixButtons] Could not load ARHudCanvas prefab at: " + prefabPath);
        }
        else
        {
            // Find the ARHudController MonoBehaviour
            var hud = prefabRoot.GetComponentInChildren<PeriodicAR.UI.ARHudController>(true);
            // Find the PlaceButton (the canvas-space button with that name)
            Transform placeBtn = prefabRoot.transform.Find("PlaceButton");

            if (hud == null)
            {
                Debug.LogError("[AddUXFixButtons] ARHudController not found in ARHudCanvas prefab.");
            }
            else
            {
                // ── Lock Button (placed to the LEFT of PlaceButton) ───────────────
                var soHud = new SerializedObject(hud);

                if (prefabRoot.transform.Find("LockButton") != null)
                {
                    Debug.Log("[AddUXFixButtons] LockButton already exists — skipping.");
                }
                else
                {
                    GameObject lockBtn = CreateHudButton(
                        prefabRoot.transform,
                        name: "LockButton",
                        label: "Lock",
                        // Mirror PlaceButton's top-right placement but shift 200px left
                        anchorMin: new Vector2(1, 1),
                        anchorMax: new Vector2(1, 1),
                        anchoredPos: new Vector2(-240, -40),
                        size: new Vector2(180, 180),
                        pivot: new Vector2(1, 1),
                        bgColor: new Color(0.1f, 0.35f, 0.1f, 0.85f)   // dark green
                    );

                    var lockBtnComp = lockBtn.GetComponent<Button>();
                    var lockLabelComp = lockBtn.GetComponentInChildren<TextMeshProUGUI>();

                    soHud.Update();
                    soHud.FindProperty("lockButton").objectReferenceValue      = lockBtnComp;
                    soHud.FindProperty("lockButtonLabel").objectReferenceValue = lockLabelComp;
                    soHud.ApplyModifiedPropertiesWithoutUndo();

                    Debug.Log("[AddUXFixButtons] LockButton created and wired.");
                    changed++;
                }

                // ── Cancel Placement Button (bottom-centre, shown when Armed) ─────
                if (prefabRoot.transform.Find("CancelPlacementButton") != null)
                {
                    Debug.Log("[AddUXFixButtons] CancelPlacementButton already exists — skipping.");
                }
                else
                {
                    GameObject cancelBtn = CreateHudButton(
                        prefabRoot.transform,
                        name: "CancelPlacementButton",
                        label: "Cancel",
                        anchorMin: new Vector2(0.5f, 0),
                        anchorMax: new Vector2(0.5f, 0),
                        anchoredPos: new Vector2(0, 60),
                        size: new Vector2(240, 90),
                        pivot: new Vector2(0.5f, 0),
                        bgColor: new Color(0.6f, 0.1f, 0.1f, 0.85f)    // dark red
                    );
                    cancelBtn.SetActive(false); // hidden by default; ARHudController shows it when Armed

                    var cancelBtnComp = cancelBtn.GetComponent<Button>();

                    soHud.Update();
                    soHud.FindProperty("cancelPlacementRoot").objectReferenceValue    = cancelBtn;
                    soHud.FindProperty("cancelPlacementButton").objectReferenceValue  = cancelBtnComp;
                    soHud.ApplyModifiedPropertiesWithoutUndo();

                    Debug.Log("[AddUXFixButtons] CancelPlacementButton created and wired.");
                    changed++;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("[AddUXFixButtons] ARHudCanvas prefab saved.");
        }

        // ── 2. Scene: CompoundCalculatorPanel — Backspace button ─────────────────
        var calcUI = Object.FindAnyObjectByType<CompoundCalculatorUI>(FindObjectsInactive.Include);
        if (calcUI == null)
        {
            Debug.LogWarning("[AddUXFixButtons] CompoundCalculatorUI not found in open scene. Open MainARScene and run again.");
        }
        else
        {
            if (calcUI.btnBackspace != null)
            {
                Debug.Log("[AddUXFixButtons] btnBackspace already assigned — skipping.");
            }
            else
            {
                // Look for an existing BackspaceButton child first
                Transform existing = calcUI.transform.Find("BackspaceButton");
                Button bsBtn;
                if (existing != null)
                {
                    bsBtn = existing.GetComponent<Button>();
                    Debug.Log("[AddUXFixButtons] Found existing BackspaceButton.");
                }
                else
                {
                    // Find btnClear's transform so we can place Backspace next to it
                    Transform clearParent = calcUI.btnClear != null
                        ? calcUI.btnClear.transform.parent
                        : calcUI.transform;

                    GameObject bsGo = CreateHudButton(
                        clearParent,
                        name: "BackspaceButton",
                        label: "⌫",
                        anchorMin: new Vector2(0.5f, 0),
                        anchorMax: new Vector2(0.5f, 0),
                        anchoredPos: new Vector2(130, 10),
                        size: new Vector2(110, 60),
                        pivot: new Vector2(0, 0),
                        bgColor: new Color(0.45f, 0.2f, 0.05f, 0.9f)   // amber
                    );
                    bsBtn = bsGo.GetComponent<Button>();
                    Debug.Log("[AddUXFixButtons] BackspaceButton created in scene.");
                    changed++;
                }

                var so = new SerializedObject(calcUI);
                so.Update();
                so.FindProperty("btnBackspace").objectReferenceValue = bsBtn;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(calcUI.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    calcUI.gameObject.scene);
                Debug.Log("[AddUXFixButtons] BackspaceButton wired to CompoundCalculatorUI.");
            }
        }

        if (changed > 0)
            Debug.Log($"[AddUXFixButtons] Done — {changed} button(s) created. Save the scene (Ctrl+S) to persist.");
        else
            Debug.Log("[AddUXFixButtons] No changes needed — all buttons already present.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>Creates a canvas button with a TextMeshProUGUI label child.</summary>
    static GameObject CreateHudButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        Vector2 pivot,
        Color bgColor)
    {
        // Root button GO
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, worldPositionStays: false);
        go.layer = parent.gameObject.layer;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        rt.pivot            = pivot;

        var img = go.GetComponent<Image>();
        img.color = bgColor;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.disabledColor    = new Color(1f, 1f, 1f, 0.35f);
        btn.colors = colors;
        btn.targetGraphic = img;

        // Label child
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, worldPositionStays: false);
        labelGo.layer = go.layer;

        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin        = Vector2.zero;
        lrt.anchorMax        = Vector2.one;
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta        = new Vector2(-12, -12);
        lrt.pivot            = new Vector2(0.5f, 0.5f);

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text                    = label;
        tmp.color                   = Color.white;
        tmp.alignment               = TextAlignmentOptions.Center;
        tmp.enableAutoSizing        = true;
        tmp.fontSizeMin             = 14;
        tmp.fontSizeMax             = 56;

        return go;
    }
}
