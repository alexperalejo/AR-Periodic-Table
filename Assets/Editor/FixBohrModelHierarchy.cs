using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Fixes a long-standing scene-wiring mistake: BohrModelRoot was an empty container
/// at (0,0,2) while AtomSystem (the GameObject with AtomGenerator + ElementLoader +
/// the actual atom particles as children) was a root-level sibling. This caused:
///
///   • ARModeManager.SetActive(BohrModelRoot) toggled an empty container, so the
///     atom never visibly hid or showed in sync with mode changes.
///   • ImmersiveModeManager.PlaceArObjectsNow() moved/scaled BohrModelRoot, but the
///     atom geometry under AtomSystem stayed at its authored transform — looking
///     like the atom "doesn't stay" when an element is tapped.
///   • Tap-to-update an existing atom appeared to work because LoadElement()
///     correctly wrote into AtomGenerator on AtomSystem; but visibility toggles in
///     SelectMode/HideBohrModel only operated on the empty BohrModelRoot.
///
/// Fix: re-parent AtomSystem under BohrModelRoot and zero its local transform so
/// AtomSystem's (proton/neutron/electron) children become grandchildren of
/// BohrModelRoot. After this, every reference to bohrModelRoot in ARModeManager,
/// ImmersiveModeManager, and ARHudController controls the real atom.
///
/// Idempotent: if AtomSystem is already a child of BohrModelRoot, this is a no-op.
/// </summary>
public static class FixBohrModelHierarchy
{
    public static void Execute()
    {
        var bohrRoot = GameObject.Find("BohrModelRoot");
        var atomSys  = GameObject.Find("AtomSystem");

        if (bohrRoot == null) { Debug.LogError("[FixBohrModelHierarchy] BohrModelRoot not found."); return; }
        if (atomSys == null)  { Debug.LogError("[FixBohrModelHierarchy] AtomSystem not found.");  return; }

        if (atomSys.transform.parent == bohrRoot.transform)
        {
            Debug.Log("[FixBohrModelHierarchy] AtomSystem is already a child of BohrModelRoot. Nothing to do.");
            return;
        }

        // Capture AtomSystem's authored world transform so we don't visually shift the
        // atom particles after re-parenting.
        Vector3 worldPos     = atomSys.transform.position;
        Quaternion worldRot  = atomSys.transform.rotation;
        Vector3 worldScale   = atomSys.transform.lossyScale;

        // Move BohrModelRoot to where AtomSystem currently lives so that the atom
        // visuals stay in place after re-parenting. After this, BohrModelRoot owns
        // the world transform; AtomSystem becomes (0,0,0) local.
        bohrRoot.transform.SetPositionAndRotation(worldPos, worldRot);
        bohrRoot.transform.localScale = Vector3.one;
        Debug.Log($"[FixBohrModelHierarchy] BohrModelRoot moved to AtomSystem's world transform " +
                  $"({worldPos}, scale={worldScale}).");

        // Re-parent AtomSystem under BohrModelRoot, preserving its world transform.
        // worldPositionStays:true keeps the visuals identical; we then zero the local
        // transform so future PlaceArObjectsNow calls on bohrRoot directly affect the
        // atom.
        atomSys.transform.SetParent(bohrRoot.transform, worldPositionStays: true);

        // OPTION: zero AtomSystem's local transform inside BohrModelRoot so they share
        // a position. This prevents the atom from appearing offset from the InfoCard
        // when both are placed near the table.
        atomSys.transform.localPosition = Vector3.zero;
        atomSys.transform.localRotation = Quaternion.identity;
        atomSys.transform.localScale    = Vector3.one;
        Debug.Log("[FixBohrModelHierarchy] AtomSystem re-parented under BohrModelRoot " +
                  "with localPos=0, localRot=identity, localScale=1.");

        EditorUtility.SetDirty(bohrRoot);
        EditorUtility.SetDirty(atomSys);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[FixBohrModelHierarchy] Done. Toggling BohrModelRoot now hides/shows the entire atom.");
    }
}
