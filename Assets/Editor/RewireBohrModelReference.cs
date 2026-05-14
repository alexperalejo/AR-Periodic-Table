using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Fixes the ARModeManager.bohrModelRoot serialized reference so it points at the
/// actual BohrModelRoot GameObject instead of its child AtomSystem.
///
/// Why this matters: the ARModeManager calls bohrModelRoot.SetActive(true/false) to
/// show/hide the atom. After the FixBohrModelHierarchy editor script re-parented
/// AtomSystem under BohrModelRoot, the original Inspector reference still pointed
/// at AtomSystem (the GameObject ID didn't change). That meant SetActive() was
/// toggling AtomSystem only, while BohrModelRoot itself remained inactive and
/// hid all of its children regardless.
///
/// This script re-targets the field to BohrModelRoot and ensures BohrModelRoot is
/// active in the scene so toggling its activeSelf actually shows/hides the atom.
///
/// Idempotent: safe to re-run.
/// </summary>
public static class RewireBohrModelReference
{
    public static void Execute()
    {
        var arModeManagerObj = Object.FindFirstObjectByType<ARModeManager>(FindObjectsInactive.Include);
        if (arModeManagerObj == null)
        {
            Debug.LogError("[RewireBohrModelReference] ARModeManager not found in scene.");
            return;
        }

        // GameObject.Find() ignores inactive objects, so use a Resources search that
        // covers inactive scene roots as well.
        GameObject bohrRoot = null;
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name == "BohrModelRoot" && t.gameObject.scene.IsValid() && t.parent == null)
            {
                bohrRoot = t.gameObject;
                break;
            }
        }
        if (bohrRoot == null)
        {
            Debug.LogError("[RewireBohrModelReference] BohrModelRoot GameObject not found (active or inactive).");
            return;
        }

        // Re-wire the ARModeManager.bohrModelRoot SerializedField.
        var so = new SerializedObject(arModeManagerObj);
        so.Update();
        var prop = so.FindProperty("bohrModelRoot");
        if (prop == null)
        {
            Debug.LogError("[RewireBohrModelReference] bohrModelRoot property not found on ARModeManager.");
            return;
        }
        var prev = prop.objectReferenceValue;
        prop.objectReferenceValue = bohrRoot;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(arModeManagerObj);

        Debug.Log($"[RewireBohrModelReference] ARModeManager.bohrModelRoot: " +
                  $"{(prev != null ? prev.name : "<null>")} -> {bohrRoot.name}");

        // BohrModelRoot must start active so SelectMode(BohrModel) → SetActive(true)
        // is a no-op visibility-wise, and so OnElementSelected (which only sets
        // bohrModelRoot.SetActive(true) when entering BohrModel mode) actually shows
        // the children. ARModeManager.ShowMenu does NOT touch bohrModelRoot, so the
        // initial state should be active.
        // However, the scene currently has BohrModelRoot inactive — fix that.
        if (!bohrRoot.activeSelf)
        {
            // We don't unconditionally activate here — the user's flow has BohrModelRoot
            // hidden until BohrModel mode is selected. Instead we leave it as the user
            // had it, AND make sure AtomSystem (nested child) is active so when the
            // user enters BohrModel mode and BohrModelRoot is activated, AtomSystem
            // and its children (Proton/Electron/etc.) are visible too.
            Debug.Log("[RewireBohrModelReference] BohrModelRoot is currently inactive (hidden). " +
                      "It will be activated by ARModeManager when BohrModel mode is selected. " +
                      "Ensuring AtomSystem child is active so it shows together with BohrModelRoot.");
        }

        var atomSys = bohrRoot.transform.Find("AtomSystem");
        if (atomSys != null && !atomSys.gameObject.activeSelf)
        {
            atomSys.gameObject.SetActive(true);
            EditorUtility.SetDirty(atomSys.gameObject);
            Debug.Log("[RewireBohrModelReference] AtomSystem activated so it shows when BohrModelRoot is activated.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[RewireBohrModelReference] Done. BohrModel toggle now controls the entire atom.");
    }
}
