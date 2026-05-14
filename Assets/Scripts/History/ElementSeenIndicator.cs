using PeriodicAR.AR;
using UnityEngine;

namespace PeriodicAR.History
{
    /// <summary>
    /// Manages the "seen" visual indicator on element cubes.
    /// Self-bootstrapping — watches for the table to spawn, then adds a small
    /// gold dot to each cube that has ever been encountered in a scan.
    /// </summary>
    public class ElementSeenIndicator : MonoBehaviour
    {
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Dot appearance.
        private static readonly Color DotColor     = new Color(1f, 0.82f, 0.18f, 0.92f); // gold
        private const float DotLocalScale           = 0.12f;   // relative to cube's local scale
        private const float DotLocalOffsetX         =  0.45f;  // corner of cube face
        private const float DotLocalOffsetY         =  0.45f;
        private const float DotLocalOffsetZ         = -0.55f;  // in front of the cube face

        // ---- Bootstrap ----------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ElementSeenIndicator>() != null) return;
            var go = new GameObject("[ElementSeenIndicator]");
            DontDestroyOnLoad(go);
            go.AddComponent<ElementSeenIndicator>();
        }

        // ---- State --------------------------------------------------------------

        private TapToPlace  _placer;
        private ElementLoader _loader;
        private GameObject  _hookedTable;

        private void Start()
        {
            ScanHistory.OnScanAdded += OnScanAdded;
        }

        private void OnDestroy()
        {
            ScanHistory.OnScanAdded -= OnScanAdded;
        }

        private void Update()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();

            var current = _placer != null ? _placer.CurrentInstance : null;
            if (current != _hookedTable)
            {
                _hookedTable = current;
                if (current != null)
                    ApplyAllIndicators(current.transform);
            }
        }

        // Called when the table spawns (or re-spawns).
        private void ApplyAllIndicators(Transform tableRoot)
        {
            var seen = ScanHistory.AllSeenElementSymbols();
            foreach (Transform child in tableRoot)
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (z <= 0) continue;
                if (_loader == null) continue;
                var data = _loader.FindByNumber(z);
                if (data == null) continue;

                bool isSeen = seen.Contains(data.symbol);
                SetDot(child, isSeen);
            }
        }

        // Called in real time when a new scan is logged.
        private void OnScanAdded(ScanRecord record)
        {
            if (_hookedTable == null || record.elementSymbols == null) return;

            var newSymbols = new System.Collections.Generic.HashSet<string>(
                record.elementSymbols,
                System.StringComparer.OrdinalIgnoreCase);

            foreach (Transform child in _hookedTable.transform)
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (z <= 0) continue;
                if (_loader == null) continue;
                var data = _loader.FindByNumber(z);
                if (data == null || !newSymbols.Contains(data.symbol)) continue;

                SetDot(child, true);
            }
        }

        // ---- Dot management -----------------------------------------------------

        private const string DotName = "__SeenDot__";

        private static void SetDot(Transform cubeRoot, bool visible)
        {
            // Find existing dot or create one.
            var existing = cubeRoot.Find(DotName);
            if (!visible && existing == null) return; // nothing to do

            if (existing == null)
            {
                if (!visible) return;
                existing = CreateDot(cubeRoot).transform;
            }

            existing.gameObject.SetActive(visible);
        }

        private static GameObject CreateDot(Transform cubeRoot)
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = DotName;
            dot.transform.SetParent(cubeRoot, false);
            dot.transform.localPosition = new Vector3(DotLocalOffsetX, DotLocalOffsetY, DotLocalOffsetZ);
            dot.transform.localScale    = Vector3.one * DotLocalScale;

            // Remove the collider — this is a purely visual overlay.
            var col = dot.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Tint gold via MaterialPropertyBlock so we don't modify shared materials.
            var rend = dot.GetComponent<Renderer>();
            if (rend != null)
            {
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb);
                mpb.SetColor(s_BaseColorId, DotColor);
                rend.SetPropertyBlock(mpb);
            }

            return dot;
        }
    }
}
