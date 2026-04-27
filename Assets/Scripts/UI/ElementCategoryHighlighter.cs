// Assets/Scripts/UI/ElementCategoryHighlighter.cs
using System.Collections.Generic;
using System.Globalization;
using PeriodicAR.AR;
using PeriodicAR.Data;
using UnityEngine;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Attached at runtime to the spawned pTableGroup instance. Tints each cube's
    /// renderer using MaterialPropertyBlock. Non-matching cubes are darkened to 25%
    /// gray (NOT transparent) so we do not depend on URP Surface Type being Transparent.
    ///
    /// cpk-hex is used as the "match" color. For the 10 elements missing cpk-hex,
    /// a per-button default color is used.
    /// </summary>
    [DisallowMultipleComponent]
    public class ElementCategoryHighlighter : MonoBehaviour
    {
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit / Simple Lit

        // Explicit halogens by atomic number (see prompt mapping).
        private static readonly HashSet<int> s_HalogenNumbers = new() { 9, 17, 35, 53, 85, 117 };

        // Default colors when an element has no cpk-hex.
        private static readonly Dictionary<string, Color> s_ButtonDefaultColor = new()
        {
            { "alkali",          ParseHex("#FF6666") },
            { "alkaline",        ParseHex("#FFDEAD") },
            { "lanthanoid",      ParseHex("#FFBFFF") },
            { "actinoid",        ParseHex("#FF99CC") },
            { "transition",      ParseHex("#FFC0C0") },
            { "posttransition",  ParseHex("#CCCCCC") },
            { "metalloid",       ParseHex("#CCCC99") },
            { "halogen",         ParseHex("#FFFF66") },
            { "noblegas",        ParseHex("#C0FFFF") },
            { "othernonmetal",   ParseHex("#A0FFA0") },
        };

        private static readonly Color s_DimmedColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        [SerializeField] private ElementLoader elementLoader; // assign in Inspector OR auto-find

        [Header("Pop-out animation")]
        [Tooltip("How far in WORLD METERS the matching elements pop forward. Auto-fit shrinks the table root, so we scale-compensate when we apply it — a value here of 0.05 means a 5cm pop regardless of how big or small the spawned table ended up.")]
        [SerializeField] private float popOutWorldMeters = 0.06f;
        [Tooltip("Direction of pop-out, in each element's LOCAL space (relative to its own root). -Z pops cubes toward the viewer for this prefab.")]
        [SerializeField] private Vector3 popOutLocalDirection = new Vector3(0f, 0f, -1f);

        // Per-cube caches so we can restore originals on "All".
        private struct CubeEntry
        {
            public Renderer  renderer;
            public Transform tileRoot;              // direct child of pTableGroup that holds the element
            public Vector3   originalLocalPosition; // tileRoot.localPosition before any pop-out
            public Color     originalBase;
            public int       atomicNumber;
            public string    jsonCategory;
            public string    symbol;
        }

        private readonly List<CubeEntry> _cubes = new();
        private MaterialPropertyBlock _mpb;
        private bool   _initialized;
        private string _activeButton;          // null = nothing highlighted

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (elementLoader == null) elementLoader = FindFirstObjectByType<ElementLoader>();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            if (elementLoader == null || elementLoader.Table == null)
            {
                Debug.LogWarning("[ElementCategoryHighlighter] ElementLoader or its table not ready yet; highlighter will init lazily on first Highlight call.");
                return;
            }

            // Walk every DIRECT child of pTableGroup. For each one, resolve its atomic
            // number via ElementTile if present, else fall back to GameObject name
            // matching ("H", "Li", "Be", … "Og"). This keeps the highlighter working
            // even if AutoSetupTiles was never run in the editor.
            int found = 0, skipped = 0;
            foreach (Transform child in transform)
            {
                int atomicNum = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (atomicNum <= 0) { skipped++; continue; }

                var rend = child.GetComponentInChildren<Renderer>();
                if (rend == null) { skipped++; continue; }

                AtomElementData data = elementLoader.FindByNumber(atomicNum);
                if (data == null) { skipped++; continue; }

                rend.GetPropertyBlock(_mpb);
                Color baseCol = _mpb.GetColor(s_BaseColorId);
                if (baseCol == default) baseCol = rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(s_BaseColorId)
                    ? rend.sharedMaterial.GetColor(s_BaseColorId)
                    : Color.white;

                _cubes.Add(new CubeEntry
                {
                    renderer              = rend,
                    tileRoot              = child,
                    originalLocalPosition = child.localPosition,
                    originalBase          = baseCol,
                    atomicNumber          = atomicNum,
                    jsonCategory          = data.category ?? string.Empty,
                    symbol                = data.symbol ?? string.Empty,
                });
                found++;
            }

            Debug.Log($"[ElementCategoryHighlighter] Cached {found} cubes; skipped {skipped} (no recognizable element).");
            _initialized = true;
        }

        /// <summary>
        /// Highlight a category. Pressing the same category twice toggles back to
        /// "no filter" (all elements restored). Matching elements get tinted with
        /// their CPK / category default color and pop forward; non-matching
        /// elements stay where they are and get dimmed.
        /// </summary>
        public void Highlight(string buttonKey)
        {
            if (!_initialized) Initialize();
            if (!_initialized) return;

            // Toggle: pressing the active button again clears.
            if (_activeButton == buttonKey)
            {
                ClearHighlight();
                return;
            }
            _activeButton = buttonKey;

            Vector3 popDir = popOutLocalDirection.sqrMagnitude > 1e-6f
                ? popOutLocalDirection.normalized
                : Vector3.forward;

            // Convert the desired WORLD-meters pop distance into pTableGroup local units
            // by dividing by the root's lossy scale. Auto-fit shrinks the root by ~400×,
            // so a 6cm world pop ends up as ~24 local units — large enough to see, small
            // enough not to throw the cube off the screen.
            float rootScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 1e-4f);
            float localPopDistance = popOutWorldMeters / rootScale;
            Vector3 popOffset = popDir * localPopDistance;
            Debug.Log($"[ElementCategoryHighlighter] Highlight('{buttonKey}'). rootScale={rootScale:F4}, localPopDistance={localPopDistance:F2}.");

            foreach (var entry in _cubes)
            {
                bool match = MatchesButton(buttonKey, entry.atomicNumber, entry.jsonCategory);
                Vector3 newLocalPos = entry.originalLocalPosition;
                bool changeColor = false;
                Color target = entry.originalBase;

                if (match)
                {
                    // Don't tint matching elements — keep their original color. Just pop them
                    // forward along the configured local direction.
                    Vector3 popInParent = entry.tileRoot != null
                        ? (entry.tileRoot.localRotation * popOffset)
                        : popOffset;
                    newLocalPos = entry.originalLocalPosition + popInParent;
                }
                else
                {
                    // Non-matching: dim a copy of the cube's own original color (darker +
                    // more transparent) so it visually recedes without looking like a flat
                    // gray override.
                    target = new Color(
                        entry.originalBase.r * 0.30f,
                        entry.originalBase.g * 0.30f,
                        entry.originalBase.b * 0.30f,
                        entry.originalBase.a * 0.45f);
                    changeColor = true;
                }

                if (changeColor)
                {
                    entry.renderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor(s_BaseColorId, target);
                    entry.renderer.SetPropertyBlock(_mpb);
                }
                else
                {
                    // Matching: explicitly restore originalBase in case a previous selection
                    // had dimmed this one. Without this, switching from one filter to another
                    // would leave previously-dimmed cubes still dim.
                    entry.renderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor(s_BaseColorId, entry.originalBase);
                    entry.renderer.SetPropertyBlock(_mpb);
                }

                if (entry.tileRoot != null) entry.tileRoot.localPosition = newLocalPos;
            }
        }

        public void ClearHighlight()
        {
            if (!_initialized) Initialize();
            if (!_initialized) return;

            _activeButton = null;

            foreach (var entry in _cubes)
            {
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, entry.originalBase);
                entry.renderer.SetPropertyBlock(_mpb);

                if (entry.tileRoot != null) entry.tileRoot.localPosition = entry.originalLocalPosition;
            }
        }

        public string ActiveButton => _activeButton;

        private static bool MatchesButton(string buttonKey, int atomicNumber, string category)
        {
            category = category ?? string.Empty;
            switch (buttonKey)
            {
                case "alkali":
                    return category == "alkali metal"
                        || category == "unknown, but predicted to be an alkali metal";
                case "alkaline":
                    return category == "alkaline earth metal";
                case "lanthanoid":
                    return category == "lanthanide";
                case "actinoid":
                    return category == "actinide";
                case "transition":
                    return category == "transition metal"
                        || category == "unknown, probably transition metal";
                case "posttransition":
                    return category == "post-transition metal"
                        || category == "unknown, probably post-transition metal";
                case "metalloid":
                    return category == "metalloid"
                        || category == "unknown, probably metalloid";
                case "noblegas":
                    return category == "noble gas"
                        || category == "unknown, predicted to be noble gas";
                case "halogen":
                    return s_HalogenNumbers.Contains(atomicNumber);
                case "othernonmetal":
                    // polyatomic nonmetal, OR diatomic nonmetal that is NOT a halogen.
                    return category == "polyatomic nonmetal"
                        || (category == "diatomic nonmetal" && !s_HalogenNumbers.Contains(atomicNumber));
                default:
                    return false;
            }
        }

        private static Color ParseHex(string hex)
        {
            TryParseHex(hex, out var c);
            return c;
        }

        private static bool TryParseHex(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(hex)) return false;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length != 6) return false;
            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)) return false;
            if (!byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)) return false;
            if (!byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)) return false;
            color = new Color(r / 255f, g / 255f, b / 255f, 1f);
            return true;
        }
    }
}
