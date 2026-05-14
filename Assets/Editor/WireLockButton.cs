using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PeriodicAR.AR;
using PeriodicAR.UI;

/// <summary>
/// Attaches LockButtonController to ARHudCanvas/LockButton if it isn't there yet,
/// and wires its TapToPlace reference. This makes the LockButton actually do
/// something when tapped (toggling TapToPlace.LockTable / UnlockTable). Idempotent.
/// </summary>
public static class WireLockButton
{
    public static void Execute()
    {
        var lockBtn = GameObject.Find("ARHudCanvas/LockButton");
        if (lockBtn == null)
        {
            Debug.LogError("[WireLockButton] ARHudCanvas/LockButton not found in scene.");
            return;
        }

        // Find TapToPlace (it's on the XR Origin GameObject)
        var tapToPlace = Object.FindFirstObjectByType<TapToPlace>(FindObjectsInactive.Include);
        if (tapToPlace == null)
        {
            Debug.LogError("[WireLockButton] No TapToPlace component found in scene.");
            return;
        }

        // Add the controller if it isn't already attached.
        var ctrl = lockBtn.GetComponent<LockButtonController>();
        if (ctrl == null)
        {
            ctrl = lockBtn.AddComponent<LockButtonController>();
            Debug.Log("[WireLockButton] LockButtonController added to ARHudCanvas/LockButton.");
        }
        else
        {
            Debug.Log("[WireLockButton] LockButtonController already present — re-wiring references.");
        }

        // Use SerializedObject to assign the private SerializeField references.
        var so = new SerializedObject(ctrl);
        so.Update();

        var tapProp = so.FindProperty("tapToPlace");
        if (tapProp != null) tapProp.objectReferenceValue = tapToPlace;

        var labelProp = so.FindProperty("label");
        if (labelProp != null && labelProp.objectReferenceValue == null)
        {
            var tmp = lockBtn.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null) labelProp.objectReferenceValue = tmp;
        }

        var imgProp = so.FindProperty("buttonImage");
        if (imgProp != null && imgProp.objectReferenceValue == null)
        {
            var img = lockBtn.GetComponent<UnityEngine.UI.Image>();
            if (img != null) imgProp.objectReferenceValue = img;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[WireLockButton] Done. LockButton now toggles TapToPlace.LockTable/UnlockTable.");
    }
}
