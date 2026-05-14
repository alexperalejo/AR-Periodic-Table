using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Two surgical fixes:
///
/// 1. Stop the atom and InfoCard from being accidentally dragged when the user taps
///    a periodic-table tile. ARWorldObjectManipulator.alwaysManipulable was true
///    on both, which made every screen tap try to BeginDrag on whichever world
///    object was under the pointer. Setting alwaysManipulable=false constrains
///    drag to Immersive Mode only. The existing on-screen Atom +/- and Close
///    buttons (built by ARHudController at runtime) keep working since they don't
///    rely on the manipulator.
///
/// 2. Add a "Back to Menu" Button to ElementInfoCard/CardCanvas. Clicking it calls
///    ARModeManager.HideInfoCard() and ARModeManager.ShowMenu() — same handler the
///    runtime ARHudController "Close Info" button uses. Idempotent: if the button
///    already exists, just re-wires it.
/// </summary>
public static class FixAtomTapAndAddInfoBack
{
    public static void Execute()
    {
        DisableAlwaysManipulable();
        AddBackButtonToInfoCard();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[FixAtomTapAndAddInfoBack] Done.");
    }

    static void DisableAlwaysManipulable()
    {
        // BohrModelRoot/AtomSystem
        var atomSys = FindByPath("BohrModelRoot/AtomSystem");
        if (atomSys != null)
        {
            var manip = atomSys.GetComponent<ARWorldObjectManipulator>();
            if (manip != null && manip.alwaysManipulable)
            {
                manip.alwaysManipulable = false;
                EditorUtility.SetDirty(manip);
                Debug.Log("[FixAtomTapAndAddInfoBack] BohrModelRoot/AtomSystem.ARWorldObjectManipulator.alwaysManipulable -> false");
            }
        }

        // ElementInfoCard root
        var info = FindRootByName("ElementInfoCard");
        if (info != null)
        {
            var manip = info.GetComponent<ARWorldObjectManipulator>();
            if (manip != null && manip.alwaysManipulable)
            {
                manip.alwaysManipulable = false;
                EditorUtility.SetDirty(manip);
                Debug.Log("[FixAtomTapAndAddInfoBack] ElementInfoCard.ARWorldObjectManipulator.alwaysManipulable -> false");
            }
        }
    }

    static void AddBackButtonToInfoCard()
    {
        var cardCanvas = FindByPath("ElementInfoCard/CardCanvas");
        if (cardCanvas == null)
        {
            Debug.LogWarning("[FixAtomTapAndAddInfoBack] ElementInfoCard/CardCanvas not found. Skipping back button.");
            return;
        }

        // Look for an existing back button so we don't add a second one.
        var existing = cardCanvas.transform.Find("BtnBackToMenu");
        GameObject btnGO;
        if (existing != null)
        {
            btnGO = existing.gameObject;
            Debug.Log("[FixAtomTapAndAddInfoBack] BtnBackToMenu already exists — re-wiring.");
        }
        else
        {
            btnGO = new GameObject("BtnBackToMenu",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGO.layer = LayerMask.NameToLayer("UI");
            btnGO.transform.SetParent(cardCanvas.transform, worldPositionStays: false);
            Debug.Log("[FixAtomTapAndAddInfoBack] BtnBackToMenu created under ElementInfoCard/CardCanvas.");
        }

        // Anchor to top-right of the card so it doesn't hide important text.
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(220f, 70f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        var img = btnGO.GetComponent<Image>();
        img.color = new Color(0.50f, 0.10f, 0.40f, 0.95f);
        img.raycastTarget = true;

        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor     = new Color(0.70f, 0.70f, 0.70f, 1f);
        btn.colors = colors;

        // Label
        var label = btnGO.transform.Find("Label");
        GameObject labelGO;
        if (label == null)
        {
            labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelGO.layer = LayerMask.NameToLayer("UI");
            labelGO.transform.SetParent(btnGO.transform, worldPositionStays: false);
        }
        else
        {
            labelGO = label.gameObject;
        }
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        lrt.localScale = Vector3.one;

        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Back to Menu";
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontSize = 32f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 40f;
        tmp.raycastTarget = false;

        // Wire onClick — clear all listeners first to ensure the button only does this.
        // We use a persistent listener so it survives serialization and works with
        // Unity's Inspector display.
        var so = new SerializedObject(btn);
        var onClick = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        onClick.ClearArray();

        // Find ARModeManager so we can wire HideInfoCard + ShowMenu as persistent listeners.
        var arModeManagerObj = Object.FindFirstObjectByType<ARModeManager>(FindObjectsInactive.Include);
        if (arModeManagerObj == null)
        {
            Debug.LogWarning("[FixAtomTapAndAddInfoBack] ARModeManager not found — wiring runtime listener instead.");
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (ARModeManager.Instance != null)
                {
                    ARModeManager.Instance.HideInfoCard();
                    ARModeManager.Instance.ShowMenu();
                }
            });
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(btn);
            EditorUtility.SetDirty(btnGO);
            return;
        }

        // Add persistent calls: HideInfoCard, then ShowMenu.
        AppendPersistentCall(onClick, arModeManagerObj, "HideInfoCard");
        AppendPersistentCall(onClick, arModeManagerObj, "ShowMenu");
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(btn);
        EditorUtility.SetDirty(btnGO);
        Debug.Log("[FixAtomTapAndAddInfoBack] BtnBackToMenu wired to ARModeManager.HideInfoCard + ShowMenu.");
    }

    static void AppendPersistentCall(SerializedProperty calls, Object target, string methodName)
    {
        int i = calls.arraySize;
        calls.arraySize++;
        var call = calls.GetArrayElementAtIndex(i);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        // 0 = EventDefined, but we want a void no-arg call → use Void mode (0).
        call.FindPropertyRelative("m_Mode").intValue = 1; // Void
        call.FindPropertyRelative("m_CallState").intValue = (int)UnityEngine.Events.UnityEventCallState.RuntimeOnly;
    }

    static GameObject FindByPath(string path)
    {
        var t = GameObject.Find(path);
        if (t != null) return t;

        // Search inactive too (Resources scan).
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var tr in allTransforms)
        {
            if (!tr.gameObject.scene.IsValid()) continue;
            // Build path bottom-up
            string p = tr.name;
            var cur = tr.parent;
            while (cur != null) { p = cur.name + "/" + p; cur = cur.parent; }
            if (p == path) return tr.gameObject;
        }
        return null;
    }

    static GameObject FindRootByName(string name)
    {
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.parent == null && t.name == name && t.gameObject.scene.IsValid())
                return t.gameObject;
        }
        return null;
    }
}
