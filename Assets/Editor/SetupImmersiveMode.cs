// Assets/Editor/SetupImmersiveMode.cs
//
// One-shot idempotent scene setup for the Immersive Mode feature.
//
// Run: PeriodicAR → Setup Immersive Mode
//
// What it does:
//   1. Finds the ARModeManager GameObject and adds ImmersiveModeManager to it (if missing).
//   2. Wires _bohrModelRoot on ImmersiveModeManager to the scene's BohrModelRoot object.
//   3. Finds MenuPanel → ButtonRow (or MenuPanel root) and adds a "BtnImmersiveToggle" Button.
//   4. Wires btnImmersiveToggle on MenuUI to the new button.
//   5. Wires the button's onClick to ImmersiveModeManager.ToggleImmersiveMode via a
//      persistent UnityEvent (Inspector-style, zero runtime allocation).
//
// Safe to run multiple times — checks before adding duplicates.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupImmersiveMode
{
    [MenuItem("PeriodicAR/Setup Immersive Mode")]
    public static void Execute()
    {
        // ── 1. Find ARModeManager GO ──────────────────────────────────────────────
        var armGO = FindInactiveByName("ARModeManager");
        if (armGO == null)
        {
            // Fallback: search by component type
            var arm = Object.FindFirstObjectByType<ARModeManager>(FindObjectsInactive.Include);
            if (arm != null) armGO = arm.gameObject;
        }
        if (armGO == null)
        {
            Debug.LogError("[SetupImmersiveMode] Could not find ARModeManager in the scene. Aborting.");
            return;
        }
        Debug.Log($"[SetupImmersiveMode] Found ARModeManager on '{armGO.name}'.");

        // ── 2. Add ImmersiveModeManager if missing ────────────────────────────────
        var imm = armGO.GetComponent<ImmersiveModeManager>();
        if (imm == null)
        {
            imm = armGO.AddComponent<ImmersiveModeManager>();
            Debug.Log("[SetupImmersiveMode] Added ImmersiveModeManager to ARModeManager GO.");
        }
        else
        {
            Debug.Log("[SetupImmersiveMode] ImmersiveModeManager already present — skipping add.");
        }

        // ── 3. Wire _bohrModelRoot ────────────────────────────────────────────────
        // Look for a child or sibling named "BohrModelRoot".
        var bohrRT = FindInactiveByName("BohrModelRoot");
        if (bohrRT == null) bohrRT = FindInactiveByName("BohrModel");
        if (bohrRT == null) bohrRT = FindInactiveByName("AtomGenerator");
        if (bohrRT != null)
        {
            var so = new SerializedObject(imm);
            var bohrProp = so.FindProperty("_bohrModelRoot");
            if (bohrProp != null)
            {
                bohrProp.objectReferenceValue = bohrRT;
                so.ApplyModifiedProperties();
                Debug.Log($"[SetupImmersiveMode] Wired _bohrModelRoot → '{bohrRT.name}'.");
            }
        }
        else
        {
            Debug.LogWarning("[SetupImmersiveMode] Could not find BohrModelRoot/BohrModel/AtomGenerator in the scene. " +
                             "Please assign _bohrModelRoot manually on ImmersiveModeManager.");
        }

        // ── 4. Find MenuUI ────────────────────────────────────────────────────────
        var menuUI = Object.FindFirstObjectByType<MenuUI>(FindObjectsInactive.Include);
        if (menuUI == null)
        {
            Debug.LogError("[SetupImmersiveMode] Could not find MenuUI in the scene. Aborting.");
            return;
        }
        var menuGO = menuUI.gameObject;
        Debug.Log($"[SetupImmersiveMode] Found MenuUI on '{menuGO.name}'.");

        // ── 5. Find or create the Immersive toggle button ─────────────────────────
        // Look for an existing button with the right name first.
        Button toggleBtn = null;
        var existingBtnGO = FindChildByName(menuGO.transform, "BtnImmersiveToggle");

        if (existingBtnGO != null)
        {
            toggleBtn = existingBtnGO.GetComponent<Button>();
            Debug.Log("[SetupImmersiveMode] BtnImmersiveToggle already exists — reusing.");
        }
        else
        {
            // Find a good parent inside MenuPanel.  Prefer "ButtonRow" (the HLG row),
            // then "Body", then the MenuUI root itself.
            Transform btnParent = FindChildByName(menuGO.transform, "ButtonRow")?.transform
                               ?? FindChildByName(menuGO.transform, "Body")?.transform
                               ?? menuGO.transform;

            var btnGO = new GameObject("BtnImmersiveToggle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGO.layer = LayerMask.NameToLayer("UI");
            btnGO.transform.SetParent(btnParent, false);

            // Size and anchor — bottom-left of parent, fixed width so it doesn't
            // crowd the existing buttons.
            var rt = (RectTransform)btnGO.transform;
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(8f, 8f);
            rt.sizeDelta        = new Vector2(200f, 60f);

            // Background image
            var img = btnGO.GetComponent<Image>();
            img.color = new Color(0.15f, 0.45f, 0.75f, 0.90f); // blue tint → "interactive"

            toggleBtn = btnGO.GetComponent<Button>();

            // ── Label child ───────────────────────────────────────────────────────
            var labelGO = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.layer = LayerMask.NameToLayer("UI");
            labelGO.transform.SetParent(btnGO.transform, false);

            var labelRT = (RectTransform)labelGO.transform;
            labelRT.anchorMin        = Vector2.zero;
            labelRT.anchorMax        = Vector2.one;
            labelRT.offsetMin        = Vector2.zero;
            labelRT.offsetMax        = Vector2.zero;

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text      = "Immersive: OFF";
            tmp.fontSize  = 18f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            // Wire the label to ImmersiveModeManager.immersiveButtonLabel
            var immSO    = new SerializedObject(imm);
            var labelProp = immSO.FindProperty("immersiveButtonLabel");
            if (labelProp != null)
            {
                labelProp.objectReferenceValue = tmp;
                immSO.ApplyModifiedProperties();
                Debug.Log("[SetupImmersiveMode] Wired immersiveButtonLabel on ImmersiveModeManager.");
            }

            EditorUtility.SetDirty(btnGO);
            EditorUtility.SetDirty(labelGO);
            Debug.Log($"[SetupImmersiveMode] Created BtnImmersiveToggle under '{btnParent.name}'.");
        }

        // ── 6. Wire btnImmersiveToggle on MenuUI ──────────────────────────────────
        var menuSO   = new SerializedObject(menuUI);
        var btnProp  = menuSO.FindProperty("btnImmersiveToggle");
        if (btnProp != null)
        {
            btnProp.objectReferenceValue = toggleBtn;
            menuSO.ApplyModifiedProperties();
            Debug.Log("[SetupImmersiveMode] Wired btnImmersiveToggle on MenuUI.");
        }

        // ── 7. Wire button onClick → ImmersiveModeManager.ToggleImmersiveMode ────
        // Use UnityEventTools.AddPersistentListener so it shows up in the Inspector
        // and survives serialization without needing runtime lambda allocation.
        toggleBtn.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(
            toggleBtn.onClick,
            imm.ToggleImmersiveMode);
        EditorUtility.SetDirty(toggleBtn);
        Debug.Log("[SetupImmersiveMode] Wired button onClick → ImmersiveModeManager.ToggleImmersiveMode.");

        // ── 8. Save scene ─────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SetupImmersiveMode] Done — scene saved. Press Play and test with the Immersive button.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// Finds ANY GameObject in the active scene by name, including inactive ones.
    private static GameObject FindInactiveByName(string objName)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var rt in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!rt.gameObject.scene.IsValid()) continue;
            if (rt.gameObject.scene != scene) continue;
            if (rt.gameObject.name == objName) return rt.gameObject;
        }
        return null;
    }

    /// Finds a direct or deep child Transform by name. Returns null if not found.
    private static GameObject FindChildByName(Transform parent, string childName)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (t != parent && t.gameObject.name == childName)
                return t.gameObject;
        }
        return null;
    }
}
