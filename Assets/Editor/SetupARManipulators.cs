// Assets/Editor/SetupARManipulators.cs
//
// One-shot idempotent setup for AR world-object manipulation.
//
// Run: PeriodicAR → Setup AR Manipulators
//
// What it does
// ────────────
//  1. Finds BohrModelRoot and ElementInfoCard via ARModeManager's serialized fields.
//  2. Adds ARWorldObjectManipulator to each (skips if already present).
//  3. Adds a trigger SphereCollider to BohrModelRoot (radius 1.5 local units) so
//     physics raycasting can detect it for drag initiation.  A trigger is used so
//     it does not interfere with XR interactions.
//  4. Configures each manipulator:
//     BohrModelRoot  — billboard=false, hudCorner=(0,0), minScale=0.01, maxScale=0.15
//     ElementInfoCard — billboard=true,  hudCorner=(1,0), minScale=0.01, maxScale=0.20
//  5. Marks the scene dirty and saves.
//
// Dependencies
// ─────────────
//  • ARWorldObjectManipulator.cs   — the runtime script
//  • AtomGenerator.cs              — arWorldScale field + ApplyScaleOnly()
//  • ImmersiveModeManager.cs       — PlaceObjectsInitially() (initial placement only)

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupARManipulators
{
    [MenuItem("PeriodicAR/Setup AR Manipulators")]
    public static void Execute()
    {
        // ── Locate ARModeManager ──────────────────────────────────────────────────
        var arm = Object.FindFirstObjectByType<ARModeManager>(FindObjectsInactive.Include);
        if (arm == null)
        {
            Debug.LogError("[SetupARManipulators] ARModeManager not found in scene — aborting.");
            return;
        }

        // ── Read ARModeManager's serialized fields ────────────────────────────────
        var armSO       = new SerializedObject(arm);
        var bohrProp    = armSO.FindProperty("bohrModelRoot");
        var cardProp    = armSO.FindProperty("elementInfoCard");

        GameObject bohrRoot = bohrProp?.objectReferenceValue as GameObject;
        var cardComp        = cardProp?.objectReferenceValue as ElementInfoCard;
        GameObject cardRoot = cardComp != null ? cardComp.gameObject : null;

        if (bohrRoot == null)
            Debug.LogWarning("[SetupARManipulators] bohrModelRoot is not assigned in ARModeManager. " +
                             "Assign it manually and re-run.");
        if (cardRoot == null)
            Debug.LogWarning("[SetupARManipulators] elementInfoCard is not assigned in ARModeManager. " +
                             "Assign it manually and re-run.");

        // ── BohrModelRoot ─────────────────────────────────────────────────────────
        if (bohrRoot != null)
        {
            var m = GetOrAddManipulator(bohrRoot);

            // Configure via SerializedObject so undo works and Inspector updates.
            var so = new SerializedObject(m);
            SetBool  (so, "draggable",        true);
            SetBool  (so, "scalable",         true);
            SetFloat (so, "minScale",         0.01f);
            SetFloat (so, "maxScale",         0.15f);
            SetBool  (so, "billboard",        false);   // atom spins on its own
            SetBool  (so, "showScaleButtons", true);
            SetFloat (so, "scaleStep",        1.15f);
            SetString(so, "hudLabel",         "Atom Scale");
            SetVec2  (so, "hudCorner",        Vector2.zero);              // bottom-left
            SetVec2  (so, "hudMargin",        new Vector2(20f, 20f));
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(m);

            // Add a trigger SphereCollider as the physics drag target.
            // Radius 1.5 in local units — at arWorldScale 0.04 this is ~6 cm world radius.
            // isTrigger = true so it does not interfere with XR grab / physics.
            if (bohrRoot.GetComponent<SphereCollider>() == null)
            {
                var col      = bohrRoot.AddComponent<SphereCollider>();
                col.radius   = 1.5f;
                col.isTrigger = true;
                EditorUtility.SetDirty(bohrRoot);
                Debug.Log("[SetupARManipulators] Added SphereCollider (trigger, r=1.5) to BohrModelRoot.");
            }
            else
            {
                Debug.Log("[SetupARManipulators] SphereCollider already present on BohrModelRoot — skipped.");
            }

            // Ensure AtomGenerator.arWorldScale is 0.04 (can drift if set elsewhere).
            var atomGen = bohrRoot.GetComponent<AtomGenerator>()
                       ?? bohrRoot.GetComponentInChildren<AtomGenerator>(includeInactive: true);
            if (atomGen != null && Mathf.Approximately(atomGen.arWorldScale, 0f))
            {
                atomGen.arWorldScale = 0.04f;
                EditorUtility.SetDirty(atomGen);
                Debug.Log("[SetupARManipulators] Initialised AtomGenerator.arWorldScale = 0.04.");
            }
        }

        // ── ElementInfoCard ───────────────────────────────────────────────────────
        if (cardRoot != null)
        {
            var m = GetOrAddManipulator(cardRoot);

            var so = new SerializedObject(m);
            SetBool  (so, "draggable",        true);
            SetBool  (so, "scalable",         true);
            SetFloat (so, "minScale",         0.01f);
            SetFloat (so, "maxScale",         0.20f);
            SetBool  (so, "billboard",        true);    // card always faces camera
            SetBool  (so, "showScaleButtons", true);
            SetFloat (so, "scaleStep",        1.15f);
            SetString(so, "hudLabel",         "Info Scale");
            SetVec2  (so, "hudCorner",        new Vector2(1f, 0f));       // bottom-right
            SetVec2  (so, "hudMargin",        new Vector2(20f, 20f));
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(m);
        }

        // ── Save scene ────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupARManipulators] Done.\n" +
                  $"  BohrModelRoot   → {(bohrRoot != null ? bohrRoot.name   : "(unassigned)")}\n" +
                  $"  ElementInfoCard → {(cardRoot != null ? cardRoot.name   : "(unassigned)")}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static ARWorldObjectManipulator GetOrAddManipulator(GameObject go)
    {
        var m = go.GetComponent<ARWorldObjectManipulator>();
        if (m == null)
        {
            m = go.AddComponent<ARWorldObjectManipulator>();
            Debug.Log($"[SetupARManipulators] Added ARWorldObjectManipulator to '{go.name}'.");
        }
        else
        {
            Debug.Log($"[SetupARManipulators] ARWorldObjectManipulator already on '{go.name}' — updating settings.");
        }
        return m;
    }

    private static void SetBool  (SerializedObject so, string name, bool   v) { var p = so.FindProperty(name); if (p != null) p.boolValue   = v; }
    private static void SetFloat (SerializedObject so, string name, float  v) { var p = so.FindProperty(name); if (p != null) p.floatValue  = v; }
    private static void SetString(SerializedObject so, string name, string v) { var p = so.FindProperty(name); if (p != null) p.stringValue = v; }
    private static void SetVec2  (SerializedObject so, string name, Vector2 v)
    {
        var p = so.FindProperty(name);
        if (p != null) { p.vector2Value = v; }
    }
}
