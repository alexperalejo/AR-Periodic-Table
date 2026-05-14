using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Eliminates the duplicate "Close Atom" / "Close Info" buttons in the scene.
///
/// Background:
///   • <see cref="ARHudController"/> creates a runtime canvas '[ARObjectsHUD]' with
///     six buttons (Close Atom, Atom +, Atom -, Close Info, Info +, Info -). Those
///     buttons are anchored top-left, are visibility-gated to BohrModelRoot /
///     ElementInfoCard.activeSelf, and they include the scale +/- controls.
///
///   • <see cref="ARCloseButtonsHUD"/> attaches to the persistent
///     'ARCloseButtonsCanvas' GameObject and toggles two pre-made scene buttons
///     'BtnCloseAtom' and 'BtnCloseInfo' anchored top-right.
///
/// Both systems toggle visibility on the same triggers, which means when the user
/// enters Bohr mode TWO Close-Atom buttons appear simultaneously.
///
/// Resolution: keep the runtime '[ARObjectsHUD]' set (it has scale buttons too), and
/// permanently disable the scene-level 'BtnCloseAtom' / 'BtnCloseInfo' children of
/// 'ARCloseButtonsCanvas'. Their controlling component <see cref="ARCloseButtonsHUD"/>
/// is also disabled so it stops calling SetActive on them every frame.
///
/// Also re-positions the BtnImmersiveToggle button to a non-overlapping location
/// (top-center) since the runtime ARObjectsHUD owns the top-left.
///
/// Idempotent: safe to re-run.
/// </summary>
public static class FixDuplicateAtomButtons
{
    public static void Execute()
    {
        DisableLegacyCloseButtons();
        ReanchorImmersiveToggle();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[FixDuplicateAtomButtons] Done. Duplicate Atom/Info close buttons disabled.");
    }

    static void DisableLegacyCloseButtons()
    {
        var canvasGO = GameObject.Find("ARCloseButtonsCanvas");
        if (canvasGO == null)
        {
            Debug.LogWarning("[FixDuplicateAtomButtons] ARCloseButtonsCanvas not found; nothing to disable.");
            return;
        }

        // Disable the controller component so it stops re-enabling the buttons every
        // frame from its Update loop.
        var hud = canvasGO.GetComponent<ARCloseButtonsHUD>();
        if (hud != null)
        {
            if (hud.enabled)
            {
                hud.enabled = false;
                EditorUtility.SetDirty(hud);
                Debug.Log("[FixDuplicateAtomButtons] Disabled ARCloseButtonsHUD on ARCloseButtonsCanvas.");
            }
        }

        // Disable both legacy buttons. Note: they are stored as children but loaded
        // inactive in the scene already; we explicitly SetActive(false) here so future
        // edits or other scripts can't quietly re-enable them.
        DisableButton(canvasGO, "BtnCloseAtom");
        DisableButton(canvasGO, "BtnCloseInfo");
    }

    static void DisableButton(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        if (t == null)
        {
            Debug.LogWarning($"[FixDuplicateAtomButtons] '{name}' not found under {parent.name}.");
            return;
        }

        var go = t.gameObject;
        if (go.activeSelf)
        {
            go.SetActive(false);
            EditorUtility.SetDirty(go);
        }

        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn != null && btn.interactable)
        {
            btn.interactable = false;
            EditorUtility.SetDirty(btn);
        }

        Debug.Log($"[FixDuplicateAtomButtons] '{name}' disabled (was a duplicate of [ARObjectsHUD]/{name.Substring(3)}).");
    }

    static void ReanchorImmersiveToggle()
    {
        var btn = GameObject.Find("ImmersiveToggleCanvas/BtnImmersiveToggle");
        if (btn == null)
        {
            Debug.LogWarning("[FixDuplicateAtomButtons] BtnImmersiveToggle not found; skipping reanchor.");
            return;
        }

        var rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;

        // Anchor to top-center, well away from the [ARObjectsHUD] top-left buttons and
        // from the PlaceButton/LockButton in the top-right area of ARHudCanvas.
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(260f, 80f);
        EditorUtility.SetDirty(rt);

        Debug.Log("[FixDuplicateAtomButtons] BtnImmersiveToggle reanchored to top-center to avoid overlap.");
    }
}
