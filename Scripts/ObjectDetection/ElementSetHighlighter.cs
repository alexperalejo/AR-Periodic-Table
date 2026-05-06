// Assets/Scripts/ObjectDetection/ElementSetHighlighter.cs
using System.Collections.Generic;
using PeriodicAR.AR;
using PeriodicAR.Data;
using UnityEngine;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Highlights an arbitrary *set of element symbols* on the spawned periodic
    /// table — the use case is "the detected object contains C, H, O, Si — pop
    /// those forward."
    ///
    /// Mirrors the caching + pop-out pattern of ElementCategoryHighlighter so
    /// the visual language stays consistent. Designed to coexist: this script
    /// only touches cubes when its own Highlight method is called, and clears
    /// itself when ClearHighlight is called or when ElementCategoryHighlighter
    /// activates a category filter.
    /// </summary>
    [DisallowMultipleComponent]
    public class ElementSetHighlighter : MonoBehaviour
    {
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Pop-out animation")]
        [Tooltip("How far in WORLD METERS matching elements pop forward.")]
        [SerializeField] private float popOutWorldMeters = 0.06f;
        [Tooltip("Direction of pop-out, in each element's LOCAL space.")]
        [SerializeField] private Vector3 popOutLocalDirection = new Vector3(0f, 0f, -1f);

        [Header("References")]
        [SerializeField] private ElementLoader elementLoader;

        private struct CubeEntry
        {
            public Renderer  renderer;
            public Transform tileRoot;
            public Vector3   originalLocalPosition;
            public Color     originalBase;
            public string    symbol;
        }

        private readonly List<CubeEntry> _cubes = new();
        private MaterialPropertyBlock _mpb;
        private bool _initialized;
        private readonly HashSet<string> _activeSymbols = new();

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (elementLoader == null) elementLoader = FindFirstObjectByType<ElementLoader>();
        }

        private void Start() => Initialize();

        private void Initialize()
        {
            if (_initialized) return;
            if (elementLoader == null || elementLoader.Table == null) return;

            foreach (Transform child in transform)
            {
                int atomicNum = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (atomicNum <= 0) continue;

                var rend = child.GetComponentInChildren<Renderer>();
                if (rend == null) continue;

                AtomElementData data = elementLoader.FindByNumber(atomicNum);
                if (data == null || string.IsNullOrEmpty(data.symbol)) continue;

                rend.GetPropertyBlock(_mpb);
                Color baseCol = _mpb.GetColor(s_BaseColorId);
                if (baseCol == default && rend.sharedMaterial != null
                    && rend.sharedMaterial.HasProperty(s_BaseColorId))
                {
                    baseCol = rend.sharedMaterial.GetColor(s_BaseColorId);
                }
                if (baseCol == default) baseCol = Color.white;

                _cubes.Add(new CubeEntry
                {
                    renderer              = rend,
                    tileRoot              = child,
                    originalLocalPosition = child.localPosition,
                    originalBase          = baseCol,
                    symbol                = data.symbol,
                });
            }

            _initialized = true;
        }

        /// <summary>
        /// Highlight every element whose symbol is in `symbols`. Symbols are
        /// compared case-sensitively in their canonical form ("Na", "C", "Mg").
        /// Calling again with a different set replaces the previous selection.
        /// </summary>
        public void Highlight(IEnumerable<string> symbols)
        {
            if (!_initialized) Initialize();
            if (!_initialized) return;

            _activeSymbols.Clear();
            if (symbols != null)
            {
                foreach (var s in symbols)
                    if (!string.IsNullOrEmpty(s)) _activeSymbols.Add(s);
            }

            float rootScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 1e-4f);
            float localPopDistance = popOutWorldMeters / rootScale;
            Vector3 popDir = popOutLocalDirection.sqrMagnitude > 1e-6f
                ? popOutLocalDirection.normalized
                : Vector3.forward;
            Vector3 popOffset = popDir * localPopDistance;

            foreach (var entry in _cubes)
            {
                bool match = _activeSymbols.Contains(entry.symbol);
                Vector3 newLocalPos = entry.originalLocalPosition;
                Color targetColor = entry.originalBase;

                if (match)
                {
                    Vector3 popInParent = entry.tileRoot != null
                        ? (entry.tileRoot.localRotation * popOffset)
                        : popOffset;
                    newLocalPos = entry.originalLocalPosition + popInParent;
                }
                else
                {
                    targetColor = new Color(
                        entry.originalBase.r * 0.30f,
                        entry.originalBase.g * 0.30f,
                        entry.originalBase.b * 0.30f,
                        entry.originalBase.a * 0.45f);
                }

                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, targetColor);
                entry.renderer.SetPropertyBlock(_mpb);

                if (entry.tileRoot != null) entry.tileRoot.localPosition = newLocalPos;
            }

            Debug.Log($"[ElementSetHighlighter] Highlighted {_activeSymbols.Count} symbols.");
        }

        public void ClearHighlight()
        {
            if (!_initialized) Initialize();
            if (!_initialized) return;

            _activeSymbols.Clear();

            foreach (var entry in _cubes)
            {
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, entry.originalBase);
                entry.renderer.SetPropertyBlock(_mpb);

                if (entry.tileRoot != null)
                    entry.tileRoot.localPosition = entry.originalLocalPosition;
            }
        }

        public bool HasActiveHighlight => _activeSymbols.Count > 0;
    }
}
