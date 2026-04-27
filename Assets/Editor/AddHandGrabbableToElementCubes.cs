using UnityEditor;
using UnityEngine;
using PeriodicAR.AR.HandTracking;

namespace PeriodicAR.EditorTools
{
    public static class AddHandGrabbableToElementCubes
    {
        private const string CubesFolder = "Assets/Prefabs/cubes";

        [MenuItem("Tools/AR Periodic Table/Add HandGrabbable To Element Cubes")]
        public static void Run()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { CubesFolder });
            int scanned = 0, updated = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;
                scanned++;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.GetComponent<CubeHandGrabbable>() == null)
                    {
                        root.AddComponent<CubeHandGrabbable>();
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
            Debug.Log($"[AddHandGrabbableToElementCubes] Scanned {scanned} prefabs. Updated {updated}.");
        }
    }
}
