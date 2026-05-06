// Assets/Editor/SetupObjectDetection.cs
using PeriodicAR.ObjectDetection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.EditorTools
{
    /// <summary>
    /// One-click setup for the object-detection scan feature. Adds a
    /// ScanController GameObject to the active scene and clones the existing
    /// Place button into a Scan button, wired to the controller. Mirrors the
    /// idempotent re-runnable style of BuildARHud — running again finds the
    /// existing ScanController and updates it rather than duplicating it.
    /// </summary>
    public static class SetupObjectDetection
    {
        [MenuItem("Tools/AR Periodic Table/Setup Object Detection")]
        public static void Setup()
        {
            // ---- 1. ScanController GameObject ------------------------------------
            var existing = Object.FindFirstObjectByType<ScanController>();
            ScanController controller;
            if (existing != null)
            {
                controller = existing;
                Debug.Log("[SetupObjectDetection] Found existing ScanController; updating in place.");
            }
            else
            {
                var go = new GameObject("ScanController");
                controller = go.AddComponent<ScanController>();
                Debug.Log("[SetupObjectDetection] Created new ScanController GameObject.");
            }

            // ---- 2. Try to clone the Place button into a Scan button -------------
            // Find the existing HUD canvas — your project standard is ARHudCanvas.
            var hudCanvas = GameObject.Find("ARHudCanvas");
            Button scanButton = null;
            if (hudCanvas == null)
            {
                Debug.LogWarning("[SetupObjectDetection] ARHudCanvas not in scene — Scan button NOT created. " +
                                 "Add the HUD prefab first via Tools > AR Periodic Table > Build AR HUD.");
            }
            else
            {
                // Re-use an existing ScanButton if present.
                Transform existingScanBtn = hudCanvas.transform.Find("ScanButton")
                    ?? FindDeepChild(hudCanvas.transform, "ScanButton");

                if (existingScanBtn != null)
                {
                    scanButton = existingScanBtn.GetComponent<Button>();
                    Debug.Log("[SetupObjectDetection] Reusing existing ScanButton.");
                }
                else
                {
                    // Find the Place button so we can clone its style.
                    Transform placeBtnTr = FindDeepChild(hudCanvas.transform, "PlaceButton")
                        ?? FindDeepChild(hudCanvas.transform, "Place Button");
                    if (placeBtnTr == null)
                    {
                        Debug.LogWarning("[SetupObjectDetection] Couldn't find PlaceButton to clone style from. " +
                                         "Add a button manually and drag it into ScanController.scanButton.");
                    }
                    else
                    {
                        var clone = Object.Instantiate(placeBtnTr.gameObject, placeBtnTr.parent);
                        clone.name = "ScanButton";
                        scanButton = clone.GetComponent<Button>();

                        // Offset the new button so it doesn't sit on top of Place.
                        var rt = clone.GetComponent<RectTransform>();
                        if (rt != null) rt.anchoredPosition += new Vector2(0f, -120f);

                        // Strip ARHudController-specific listeners that were copied
                        // along with the original button.
                        if (scanButton != null) scanButton.onClick.RemoveAllListeners();

                        // Update label text.
                        var lbl = clone.GetComponentInChildren<TMP_Text>(true);
                        if (lbl != null) lbl.text = "Scan";

                        Debug.Log("[SetupObjectDetection] Created ScanButton by cloning PlaceButton.");
                    }
                }
            }

            // ---- 3. Wire ScanController via SerializedObject so the Inspector saves it
            var so = new SerializedObject(controller);
            if (scanButton != null)
            {
                var prop = so.FindProperty("scanButton");
                if (prop != null) prop.objectReferenceValue = scanButton;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Mark the scene dirty so the user can save.
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = controller.gameObject;

            Debug.Log("[SetupObjectDetection] Done. Verify the ScanController Inspector — " +
                      "the serverUrl is pre-filled with https://tpu.gonzalezerik.com/detect. " +
                      "Save the scene to persist.");
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var hit = FindDeepChild(child, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
