// Assets/Editor/SetupARCloseButtons.cs
//
// One-shot idempotent setup for the AR close buttons.
//
// Run: PeriodicAR → Setup AR Close Buttons
//
// What it creates (only if not already present):
//
//   BohrCloseButton   (world-space Canvas)
//   └─ ButtonRoot     (RectTransform, Image, Button)
//      └─ Label       (TextMeshProUGUI)
//
//   InfoCloseButton   (world-space Canvas)
//   └─ ButtonRoot     (RectTransform, Image, Button)
//      └─ Label       (TextMeshProUGUI)
//
// Both GOs are parented to the ARModeManager's GameObject so they travel with it
// and are easy to find in the Hierarchy.  ARCloseButton.cs positions them relative
// to their anchor every frame, so parent-space position doesn't matter.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupARCloseButtons
{
    [MenuItem("PeriodicAR/Setup AR Close Buttons")]
    public static void Execute()
    {
        // ── Locate ARModeManager ──────────────────────────────────────────────────
        var arm = Object.FindFirstObjectByType<ARModeManager>(FindObjectsInactive.Include);
        if (arm == null)
        {
            Debug.LogError("[SetupARCloseButtons] ARModeManager not found — aborting.");
            return;
        }
        Transform armTransform = arm.transform;

        // ── Read ARModeManager's serialized fields via SerializedObject ───────────
        var armSO         = new SerializedObject(arm);
        var bohrRootProp  = armSO.FindProperty("bohrModelRoot");
        var infoCardProp  = armSO.FindProperty("elementInfoCard");

        GameObject bohrRoot = bohrRootProp?.objectReferenceValue as GameObject;
        // elementInfoCard is an ElementInfoCard component — get its GameObject
        var infoCardComp = infoCardProp?.objectReferenceValue as ElementInfoCard;
        GameObject infoCardGO = infoCardComp != null ? infoCardComp.gameObject : null;

        if (bohrRoot == null)
            Debug.LogWarning("[SetupARCloseButtons] bohrModelRoot is not assigned in ARModeManager. " +
                             "Assign it manually after running this tool.");
        if (infoCardGO == null)
            Debug.LogWarning("[SetupARCloseButtons] elementInfoCard is not assigned in ARModeManager. " +
                             "Assign it manually after running this tool.");

        // ── Create/update BohrModel close button ──────────────────────────────────
        var bohrClose = GetOrCreateCloseButton(
            parentTransform : armTransform,
            goName          : "BohrCloseButton",
            target          : ARCloseButton.CloseTarget.BohrModel,
            anchorGO        : bohrRoot,
            rightOffset     : 0.14f,
            upOffset        : 0.10f);

        // ── Create/update ElementInfo close button ────────────────────────────────
        var infoClose = GetOrCreateCloseButton(
            parentTransform : armTransform,
            goName          : "InfoCloseButton",
            target          : ARCloseButton.CloseTarget.ElementInfo,
            anchorGO        : infoCardGO,
            rightOffset     : 0.14f,
            upOffset        : 0.10f);

        // ── Dirty + save ──────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupARCloseButtons] Done.\n" +
                  $"  BohrCloseButton anchor  = {(bohrRoot   != null ? bohrRoot.name   : "(unassigned — set manually)")}\n" +
                  $"  InfoCloseButton anchor  = {(infoCardGO != null ? infoCardGO.name : "(unassigned — set manually)")}");
    }

    // ── Builder ───────────────────────────────────────────────────────────────────

    private static ARCloseButton GetOrCreateCloseButton(
        Transform                  parentTransform,
        string                     goName,
        ARCloseButton.CloseTarget  target,
        GameObject                 anchorGO,
        float                      rightOffset,
        float                      upOffset)
    {
        // Re-use if already present.
        Transform existing = parentTransform.Find(goName);
        ARCloseButton btn;

        if (existing != null)
        {
            btn = existing.GetComponent<ARCloseButton>();
            if (btn == null) btn = existing.gameObject.AddComponent<ARCloseButton>();
            Debug.Log($"[SetupARCloseButtons] Reusing existing '{goName}'.");
        }
        else
        {
            // ── Root Canvas GO ────────────────────────────────────────────────────
            var root = new GameObject(goName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(ARCloseButton));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parentTransform, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode        = RenderMode.WorldSpace;
            canvas.sortingOrder      = 110;  // above MenuCanvas (101)

            var rt = (RectTransform)root.transform;
            rt.sizeDelta   = new Vector2(160f, 48f);
            rt.localScale  = Vector3.one * 0.001f; // 1 canvas-unit = 1 mm
            rt.localPosition = Vector3.zero;

            btn = root.GetComponent<ARCloseButton>();

            // ── Button child ──────────────────────────────────────────────────────
            var btnGO = new GameObject("ButtonRoot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            btnGO.layer = LayerMask.NameToLayer("UI");
            btnGO.transform.SetParent(root.transform, false);

            var btnRT = (RectTransform)btnGO.transform;
            btnRT.anchorMin        = Vector2.zero;
            btnRT.anchorMax        = Vector2.one;
            btnRT.offsetMin        = Vector2.zero;
            btnRT.offsetMax        = Vector2.zero;

            // Dark semi-transparent background
            var img  = btnGO.GetComponent<Image>();
            img.color         = new Color(0.08f, 0.08f, 0.08f, 0.82f);
            img.raycastTarget = true;

            // Round corners via built-in sprite slicing (none here — flat rect is fine)
            var button = btnGO.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor      = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            colors.highlightedColor = new Color(0.85f, 0.25f, 0.20f, 0.90f); // red tint on hover
            colors.pressedColor     = new Color(0.65f, 0.10f, 0.08f, 1.00f);
            button.colors           = colors;

            // ── Label child ───────────────────────────────────────────────────────
            var labelGO = new GameObject("Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelGO.layer = LayerMask.NameToLayer("UI");
            labelGO.transform.SetParent(btnGO.transform, false);

            var labelRT = (RectTransform)labelGO.transform;
            labelRT.anchorMin  = Vector2.zero;
            labelRT.anchorMax  = Vector2.one;
            labelRT.offsetMin  = new Vector2(6f, 4f);
            labelRT.offsetMax  = new Vector2(-6f, -4f);

            var tmp         = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text        = target == ARCloseButton.CloseTarget.BohrModel
                ? "X  Close Atom" : "X  Close Info";
            tmp.fontSize    = 22f;
            tmp.alignment   = TMPro.TextAlignmentOptions.Center;
            tmp.color       = Color.white;
            tmp.fontStyle   = TMPro.FontStyles.Bold;

            // ── Wire button onClick → ARCloseButton.OnClose ───────────────────────
            button.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(button.onClick, btn.OnClose);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(btnGO);
            EditorUtility.SetDirty(labelGO);
            Debug.Log($"[SetupARCloseButtons] Created '{goName}'.");
        }

        // ── Apply settings (whether new or reused) ────────────────────────────────
        var btnSO = new SerializedObject(btn);

        var targetProp  = btnSO.FindProperty("_target");
        var anchorProp  = btnSO.FindProperty("anchor");
        var rightProp   = btnSO.FindProperty("_rightOffset");
        var upProp      = btnSO.FindProperty("_upOffset");
        var scaleProp   = btnSO.FindProperty("_canvasScale");

        if (targetProp  != null) targetProp.enumValueIndex  = (int)target;
        if (anchorProp  != null) anchorProp.objectReferenceValue = anchorGO != null
            ? (Object)anchorGO.transform
            : null;
        if (rightProp   != null) rightProp.floatValue  = rightOffset;
        if (upProp      != null) upProp.floatValue     = upOffset;
        if (scaleProp   != null) scaleProp.floatValue  = 0.001f;

        btnSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(btn);

        return btn;
    }
}
