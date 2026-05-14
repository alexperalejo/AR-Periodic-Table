using System.Collections;
using PeriodicAR.Data;
using TMPro;
using UnityEngine;

namespace PeriodicAR.Scale
{
    /// <summary>
    /// Displays a to-scale atom radius sphere in world space.
    /// 1 pm = 0.0005 m (0.5 mm). Cycles through reference atoms on tap.
    /// </summary>
    public class AtomScaleVisualizer : MonoBehaviour
    {
        private static readonly (string symbol, float radiusPm, string note)[] s_References =
        {
            ("H",   53f,  "Smallest atom — just 1 proton"),
            ("C",   77f,  "Life's backbone element"),
            ("Fe", 126f,  "Iron — the heart of blood"),
            ("Cs", 298f,  "Largest naturally occurring atom"),
        };

        // Van der Waals radii in pm for common elements (fallback 150 pm).
        private static readonly System.Collections.Generic.Dictionary<string, float> s_Radii =
            new System.Collections.Generic.Dictionary<string, float>
        {
            {"H",53f},{"He",31f},{"Li",167f},{"Be",112f},{"B",87f},{"C",77f},{"N",75f},{"O",73f},{"F",71f},{"Ne",38f},
            {"Na",190f},{"Mg",145f},{"Al",118f},{"Si",111f},{"P",98f},{"S",103f},{"Cl",99f},{"Ar",71f},
            {"K",243f},{"Ca",194f},{"Fe",126f},{"Cu",128f},{"Zn",122f},{"Br",114f},{"Kr",88f},
            {"Rb",265f},{"Sr",219f},{"Ag",165f},{"I",133f},{"Xe",108f},
            {"Cs",298f},{"Ba",253f},{"Au",174f},{"Hg",171f},{"Pb",202f},
        };

        private const float PmToMeters = 0.0005f; // 1 pm = 0.5 mm

        private GameObject _sphere;
        private TextMeshPro _nameLabel;
        private TextMeshPro _sizeLabel;
        private TextMeshPro _noteLabel;
        private int         _refIndex = -1; // -1 = showing user element
        private string      _currentSymbol;
        private ElementLoader _loader;

        private void Awake()
        {
            _loader = FindAnyObjectByType<ElementLoader>();
        }

        public void ShowElement(string symbol)
        {
            _currentSymbol = symbol;
            _refIndex = -1;
            UpdateDisplay(symbol);
        }

        public void CycleReference()
        {
            _refIndex = (_refIndex + 1) % s_References.Length;
            var (sym, _, _) = s_References[_refIndex];
            UpdateDisplay(sym);
        }

        private void UpdateDisplay(string symbol)
        {
            float radiusPm = GetRadius(symbol);
            float worldR   = radiusPm * PmToMeters;

            if (_sphere == null) BuildScene();

            _sphere.transform.localScale = Vector3.one * (worldR * 2f);

            // Color by element type.
            var rend = _sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", GetElementColor(symbol));
                rend.SetPropertyBlock(mpb);
            }

            string displayName = GetDisplayName(symbol);
            if (_nameLabel != null)
                _nameLabel.text = $"{symbol} — {displayName}";
            if (_sizeLabel != null)
                _sizeLabel.text = $"Radius: {radiusPm:F0} pm  ({worldR * 100f:F2} cm at this scale)";

            // Reference note.
            string note = "";
            if (_refIndex >= 0) note = s_References[_refIndex].note;
            if (_noteLabel != null) _noteLabel.text = note;
        }

        private void BuildScene()
        {
            // Sphere
            _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_sphere.GetComponent<Collider>());
            _sphere.transform.SetParent(transform, false);

            // Slowly rotate.
            _sphere.AddComponent<ConstantRotator>();

            // Name label
            var nameLabelGo = new GameObject("NameLabel");
            nameLabelGo.transform.SetParent(transform, false);
            nameLabelGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            _nameLabel = nameLabelGo.AddComponent<TextMeshPro>();
            _nameLabel.fontSize   = 0.06f;
            _nameLabel.alignment  = TextAlignmentOptions.Center;
            _nameLabel.color      = Color.white;

            // Size label
            var sizeLabelGo = new GameObject("SizeLabel");
            sizeLabelGo.transform.SetParent(transform, false);
            sizeLabelGo.transform.localPosition = new Vector3(0f, -0.14f, 0f);
            _sizeLabel = sizeLabelGo.AddComponent<TextMeshPro>();
            _sizeLabel.fontSize  = 0.04f;
            _sizeLabel.alignment = TextAlignmentOptions.Center;
            _sizeLabel.color     = new Color(0.8f, 0.8f, 0.8f);

            // Note label
            var noteLabelGo = new GameObject("NoteLabel");
            noteLabelGo.transform.SetParent(transform, false);
            noteLabelGo.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            _noteLabel = noteLabelGo.AddComponent<TextMeshPro>();
            _noteLabel.fontSize  = 0.04f;
            _noteLabel.alignment = TextAlignmentOptions.Center;
            _noteLabel.color     = new Color(0.7f, 0.9f, 0.7f);

            // Tap-to-cycle hint.
            var hintGo = new GameObject("HintLabel");
            hintGo.transform.SetParent(transform, false);
            hintGo.transform.localPosition = new Vector3(0f, -0.26f, 0f);
            var hintTmp = hintGo.AddComponent<TextMeshPro>();
            hintTmp.text     = "Tap sphere to cycle reference atoms";
            hintTmp.fontSize = 0.035f;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color    = new Color(0.6f, 0.6f, 0.6f);
        }

        private static float GetRadius(string symbol)
        {
            if (s_Radii.TryGetValue(symbol, out float r)) return r;
            return 150f;
        }

        private string GetDisplayName(string symbol)
        {
            if (_loader?.Table == null) return symbol;
            foreach (var el in _loader.Table.elements)
                if (el.symbol == symbol) return el.name;
            return symbol;
        }

        private static Color GetElementColor(string symbol) => symbol switch
        {
            "H"  => new Color(0.9f, 0.9f, 0.9f),
            "C"  => new Color(0.2f, 0.2f, 0.2f),
            "N"  => new Color(0.2f, 0.4f, 0.9f),
            "O"  => new Color(0.9f, 0.1f, 0.1f),
            "Fe" => new Color(0.6f, 0.3f, 0.1f),
            "Au" => new Color(1.0f, 0.8f, 0.0f),
            "Cs" => new Color(0.4f, 0.2f, 0.8f),
            _    => new Color(0.4f, 0.7f, 0.9f),
        };

        private class ConstantRotator : MonoBehaviour
        {
            private void Update() => transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        }
    }
}
