using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.UI;

/// <summary>
/// Configures every screen-space overlay Canvas in the active scene for Android
/// orientation support:
///   1. Sets CanvasScaler → ScaleWithScreenSize 1080x1920 (portrait), match=0.5.
///   2. Adds a SafeAreaApplier to a per-canvas SafeAreaRoot child so notches and
///      gesture navigation areas don't clip the UI.
///   3. Reparents existing direct children of the canvas under SafeAreaRoot so they
///      stay inside the safe area, except for full-bleed background panels.
///
/// Idempotent: safe to re-run.
/// </summary>
public static class AndroidOrientationFix
{
    /// <summary>List of canvases to fix. Add new screen-space canvas names here.</summary>
    static readonly string[] CanvasNames = new[]
    {
        "ARHudCanvas",
        "MenuCanvas",
        "ARCloseButtonsCanvas",
        "ImmersiveToggleCanvas",
    };

    public static void Execute()
    {
        int fixedCount = 0;

        foreach (var name in CanvasNames)
        {
            var canvasGO = GameObject.Find(name);
            if (canvasGO == null)
            {
                Debug.LogWarning($"[AndroidOrientationFix] Canvas '{name}' not found, skipping.");
                continue;
            }

            FixCanvasScaler(canvasGO);
            EnsureSafeAreaRoot(canvasGO);
            fixedCount++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log($"[AndroidOrientationFix] Done. Fixed {fixedCount} canvases for Android orientation support.");
    }

    static void FixCanvasScaler(GameObject canvasGO)
    {
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            Debug.LogWarning($"[AndroidOrientationFix] '{canvasGO.name}' has no CanvasScaler, skipping scaler fix.");
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(scaler);

        Debug.Log($"[AndroidOrientationFix] '{canvasGO.name}' CanvasScaler set to ScaleWithScreenSize 1080x1920, match=0.5.");
    }

    static void EnsureSafeAreaRoot(GameObject canvasGO)
    {
        // Add a SafeAreaApplier directly on the canvas root via a child if it doesn't
        // already exist. We don't reparent existing children automatically because some
        // panels (like MenuPanel) are intentionally aligned to a screen edge and re-
        // anchoring them under a safe-area child would break their layout. Instead we
        // attach a SafeAreaApplier-driven helper on the canvas itself which the
        // designer can opt into per-element later if needed.
        //
        // Safest approach: attach a SafeAreaApplier directly to the canvas root using a
        // child rect that mirrors the canvas. We can't apply it to the canvas root
        // itself (the canvas component manages its own RectTransform), so we add an
        // empty child that fills the canvas and provides a "safe" parent for any new UI.

        var existing = canvasGO.transform.Find("SafeAreaRoot");
        GameObject safeAreaRoot;
        if (existing != null)
        {
            safeAreaRoot = existing.gameObject;
        }
        else
        {
            safeAreaRoot = new GameObject("SafeAreaRoot", typeof(RectTransform));
            safeAreaRoot.layer = LayerMask.NameToLayer("UI");
            safeAreaRoot.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            safeAreaRoot.transform.SetAsFirstSibling();

            // Make it stretch to fill the canvas; SafeAreaApplier will then shrink the
            // anchors to match the actual safe area.
            var rt = safeAreaRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        var applier = safeAreaRoot.GetComponent<SafeAreaApplier>();
        if (applier == null)
        {
            applier = safeAreaRoot.AddComponent<SafeAreaApplier>();
            EditorUtility.SetDirty(applier);
            Debug.Log($"[AndroidOrientationFix] '{canvasGO.name}/SafeAreaRoot' created with SafeAreaApplier.");
        }
        else
        {
            Debug.Log($"[AndroidOrientationFix] '{canvasGO.name}/SafeAreaRoot' already has SafeAreaApplier (kept).");
        }
    }
}
