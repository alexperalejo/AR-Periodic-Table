using System.Collections.Generic;
using PeriodicAR.AR;
using PeriodicAR.Data;
using UnityEngine;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Highlights an arbitrary set of element symbols on the spawned periodic table.
    /// Matching cubes pop forward and receive a gold tint; non-matching cubes dim.
    /// Self-bootstrapping singleton — no Inspector wiring needed.
    /// </summary>
    public class ElementSetHighlighter : MonoBehaviour
    {
        // ---- bootstrap ----
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ElementSetHighlighter>() != null) return;
            var go = new GameObject("[ElementSetHighlighter]");
            DontDestroyOnLoad(go);
            go.AddComponent<ElementSetHighlighter>();
        }

        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        private struct CubeEntry
        {
            public Renderer  renderer;
            public Transform tileRoot;
            public Vector3   originalLocalPosition;
            public Color     originalColor;
            public string    symbol;
        }

        public float popOutWorldMeters = 0.06f;
        public Color highlightTint     = new Color(1f, 0.85f, 0.2f, 1f);

        private TapToPlace       _placer;
        private ElementLoader    _loader;
        private GameObject       _hookedTable;
        private List<CubeEntry>  _cubes       = new();
        private HashSet<string>  _highlighted = new(System.StringComparer.OrdinalIgnoreCase);
        private MaterialPropertyBlock _mpb;
        private bool             _initialized;

        private void Awake() => _mpb = new MaterialPropertyBlock();

        private void Update()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();

            var current = _placer != null ? _placer.CurrentInstance : null;
            if (current != _hookedTable)
            {
                _cubes.Clear();
                _highlighted.Clear();
                _initialized = false;
                _hookedTable = current;
                if (current != null) CacheTable(current.transform);
            }
        }

        private void CacheTable(Transform tableRoot)
        {
            if (_loader == null || _loader.Table == null) return;
            foreach (Transform child in tableRoot)
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (z <= 0) continue;

                var rend = child.GetComponentInChildren<Renderer>();
                if (rend == null) continue;

                AtomElementData data = _loader.FindByNumber(z);
                if (data == null) continue;

                rend.GetPropertyBlock(_mpb);
                Color baseCol = _mpb.GetColor(s_BaseColorId);
                if (baseCol == default && rend.sharedMaterial != null
                    && rend.sharedMaterial.HasProperty(s_BaseColorId))
                    baseCol = rend.sharedMaterial.GetColor(s_BaseColorId);
                if (baseCol == default) baseCol = Color.white;

                _cubes.Add(new CubeEntry
                {
                    renderer              = rend,
                    tileRoot              = child,
                    originalLocalPosition = child.localPosition,
                    originalColor         = baseCol,
                    symbol                = data.symbol ?? string.Empty,
                });
            }
            _initialized = true;
            Debug.Log($"[ElementSetHighlighter] Cached {_cubes.Count} cubes.");
        }

        public void HighlightElements(IEnumerable<string> symbols)
        {
            if (!_initialized) return;

            // Restore all cubes first, then apply the new set.
            RestoreAll();
            _highlighted.Clear();

            var matchSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (symbols != null)
                foreach (var s in symbols) { matchSet.Add(s); _highlighted.Add(s); }

            float rootScale    = _hookedTable != null
                ? Mathf.Max(Mathf.Abs(_hookedTable.transform.lossyScale.x), 1e-4f)
                : 1f;
            float localPop = popOutWorldMeters / rootScale;

            foreach (var entry in _cubes)
            {
                bool match = matchSet.Contains(entry.symbol);
                entry.renderer.GetPropertyBlock(_mpb);

                if (match)
                {
                    _mpb.SetColor(s_BaseColorId, highlightTint);
                    entry.renderer.SetPropertyBlock(_mpb);
                    // Pop toward viewer in local space (same convention as ElementCategoryHighlighter: -Z is out).
                    entry.tileRoot.localPosition = entry.originalLocalPosition
                        + entry.tileRoot.localRotation * (Vector3.back * localPop);
                }
                else
                {
                    var dim = new Color(
                        entry.originalColor.r * 0.30f,
                        entry.originalColor.g * 0.30f,
                        entry.originalColor.b * 0.30f,
                        entry.originalColor.a * 0.45f);
                    _mpb.SetColor(s_BaseColorId, dim);
                    entry.renderer.SetPropertyBlock(_mpb);
                }
            }

            // Sync state to AppStateProvider.
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.currentlyHighlighted = new List<string>(_highlighted);
        }

        public void ClearHighlight()
        {
            if (!_initialized) return;
            RestoreAll();
            _highlighted.Clear();
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.currentlyHighlighted.Clear();
        }

        private void RestoreAll()
        {
            foreach (var entry in _cubes)
            {
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, entry.originalColor);
                entry.renderer.SetPropertyBlock(_mpb);
                entry.tileRoot.localPosition = entry.originalLocalPosition;
            }
        }

        // Static accessor so other systems (ScanController, TutorToolRegistry) can find it.
        public static ElementSetHighlighter Instance => FindFirstObjectByType<ElementSetHighlighter>();
    }
}
