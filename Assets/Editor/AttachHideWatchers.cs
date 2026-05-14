using PeriodicAR.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Attaches HideWatcher to BohrModelRoot, BohrModelRoot/AtomSystem, and
/// ElementInfoCard so every OnDisable / OnEnable on those objects emits a
/// "[HIDE TRACE]" log including a full stack trace. Use to identify which
/// script is hiding the atom or info card after an element is selected.
/// Idempotent.
/// </summary>
public static class AttachHideWatchers
{
    public static void Execute()
    {
        Attach("BohrModelRoot");
        Attach("BohrModelRoot/AtomSystem");
        Attach("ElementInfoCard");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[AttachHideWatchers] Done.");
    }

    static void Attach(string path)
    {
        var go = FindGameObjectIncludingInactive(path);
        if (go == null)
        {
            Debug.LogWarning($"[AttachHideWatchers] '{path}' not found in scene.");
            return;
        }

        var watcher = go.GetComponent<HideWatcher>();
        if (watcher == null)
        {
            watcher = go.AddComponent<HideWatcher>();
            EditorUtility.SetDirty(go);
            Debug.Log($"[AttachHideWatchers] HideWatcher added to '{path}'.");
        }
        else
        {
            Debug.Log($"[AttachHideWatchers] HideWatcher already on '{path}'.");
        }
    }

    /// <summary>Find a scene GameObject by '/'-separated path, including inactive ones.</summary>
    static GameObject FindGameObjectIncludingInactive(string path)
    {
        // Try the active-only fast path first.
        var found = GameObject.Find(path);
        if (found != null) return found;

        // Slow path: walk every Transform in loaded scenes (covers inactive).
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (!t.gameObject.scene.IsValid()) continue;
            string p = t.name;
            var cur = t.parent;
            while (cur != null)
            {
                p = cur.name + "/" + p;
                cur = cur.parent;
            }
            if (p == path) return t.gameObject;
        }
        return null;
    }
}
