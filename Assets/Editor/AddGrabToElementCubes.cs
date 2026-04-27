// Assets/Editor/AddGrabToElementCubes.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace PeriodicAR.EditorTools
{
    /// <summary>
    /// One-shot editor tool. Iterates every prefab under Assets/Prefabs/cubes/ and
    /// ensures each has the components needed for Phase 2 phone-AR grab:
    ///   - BoxCollider  (sized to the renderer AABB at identity scale)
    ///   - Rigidbody    (kinematic, gravityless)
    ///   - XRGrabInteractable  (Interactables namespace; positional-only drag)
    ///   - ARTransformer       (min 0.5x, max 2.0x; Horizontal is XRI's default alignment)
    ///
    /// Menu: Tools > AR Periodic Table > Add Grab To Element Cubes
    /// </summary>
    public static class AddGrabToElementCubes
    {
        private const string CubesFolder = "Assets/Prefabs/cubes";

        [MenuItem("Tools/AR Periodic Table/Add Grab To Element Cubes")]
        public static void Run()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { CubesFolder });
            int scanned = 0, updated = 0, skippedNoRenderer = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;
                scanned++;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = EnsureComponents(root, path, ref skippedNoRenderer);
                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        updated++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AddGrabToElementCubes] Scanned {scanned} prefabs. Updated {updated}. Skipped (no renderer) {skippedNoRenderer}.");
        }

        private static bool EnsureComponents(GameObject root, string path, ref int skippedNoRenderer)
        {
            bool modified = false;

            // 1. BoxCollider sized to the renderer AABB.
            if (root.GetComponent<Collider>() == null)
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogWarning($"[AddGrabToElementCubes] {path} has no Renderer; skipping collider.");
                    skippedNoRenderer++;
                }
                else
                {
                    Bounds worldBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);

                    var box = root.AddComponent<BoxCollider>();
                    // The prefab-contents root sits at world origin at identity scale,
                    // so world-space AABB == local-space AABB for this body.
                    box.center = root.transform.InverseTransformPoint(worldBounds.center);
                    box.size   = worldBounds.size;
                    modified = true;
                }
            }

            // 2. Kinematic Rigidbody. XRGrabInteractable needs one; we want phone AR,
            //    not physics drag, so it's kinematic with no gravity.
            var rb = root.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = root.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                modified = true;
            }

            // 3. XRGrabInteractable — the XRI 3.x Interactables namespace version.
            var grab = root.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                grab = root.AddComponent<XRGrabInteractable>();
                grab.trackPosition   = true;
                grab.trackRotation   = false;
                grab.trackScale      = false;
                grab.movementType    = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach   = false;
                grab.useDynamicAttach = true;
                modified = true;
            }

            // 4. ARTransformer — phone-AR plane-snapping transformer. Default
            //    requiredPlaneAlignment in XRI 3.3 is Horizontal, which is what we want.
            var tx = root.GetComponent<ARTransformer>();
            if (tx == null)
            {
                tx = root.AddComponent<ARTransformer>();
                tx.minScale = 0.5f;
                tx.maxScale = 2.0f;
                modified = true;
            }

            return modified;
        }
    }
}
