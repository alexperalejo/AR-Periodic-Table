using PeriodicAR.Data;
using TMPro;
using UnityEngine;

namespace PeriodicAR.Scale
{
    /// <summary>
    /// Shows a to-scale mole-volume cube or sphere for a given element.
    /// Volume = molar_mass / density cm³. Capped at 1 m³.
    /// For gases: uses 22,400 cm³ (STP molar volume). 1 cm³ = 1e-6 m³.
    /// </summary>
    public class MoleVisualizer : MonoBehaviour
    {
        private static readonly float MaxVolumeCm3 = 1e6f; // 1 m³

        private GameObject  _shape;
        private TextMeshPro _nameLabel;
        private TextMeshPro _volumeLabel;
        private TextMeshPro _noteLabel;
        private ElementLoader _loader;

        private void Awake() => _loader = FindAnyObjectByType<ElementLoader>();

        public void ShowElement(string symbol)
        {
            if (_loader?.Table == null) return;
            AtomElementData data = null;
            foreach (var el in _loader.Table.elements)
                if (el.symbol == symbol) { data = el; break; }
            if (data == null) return;

            bool isGas       = data.boil > 0f && data.boil < 373f; // boils below 100°C
            float volumeCm3  = isGas ? 22400f
                             : (data.density > 0f ? data.atomic_mass / data.density : 0f);
            volumeCm3 = Mathf.Min(volumeCm3, MaxVolumeCm3);

            float volumeM3   = volumeCm3 * 1e-6f;
            float sideM      = Mathf.Pow(volumeM3, 1f / 3f); // cube side in metres

            // Clamp to AR-visible range 2 cm … 30 cm.
            float displayScale = sideM;
            if (displayScale < 0.02f) displayScale = 0.02f;
            if (displayScale > 0.30f) displayScale = 0.30f;

            if (_shape != null) Destroy(_shape);

            if (isGas)
            {
                _shape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(_shape.GetComponent<Collider>());
                // Sphere diameter = cube side for rough equivalence.
                _shape.transform.localScale = Vector3.one * displayScale;
                var rend = _shape.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_BaseColor", new Color(0.7f, 0.9f, 1.0f, 0.5f));
                    rend.SetPropertyBlock(mpb);
                }
            }
            else
            {
                _shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(_shape.GetComponent<Collider>());
                _shape.transform.localScale = Vector3.one * displayScale;
                var rend = _shape.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_BaseColor", new Color(0.8f, 0.7f, 0.3f, 0.85f));
                    rend.SetPropertyBlock(mpb);
                }
            }

            _shape.transform.SetParent(transform, false);
            _shape.transform.localPosition = Vector3.zero;

            BuildLabels();

            string name = data.name;
            string volStr = volumeCm3 >= 1e5f ? $"{volumeCm3 / 1e6f:F4} m³"
                          : volumeCm3 >= 1000f ? $"{volumeCm3 / 1000f:F3} L"
                          : $"{volumeCm3:F1} cm³";

            string shapeNote = isGas ? "gas at STP (22.4 L/mol)"
                             : data.density > 0f ? $"density {data.density:F3} g/cm³"
                             : "density unknown";

            if (_nameLabel  != null) _nameLabel.text  = $"1 mol of {name} ({symbol})";
            if (_volumeLabel!= null) _volumeLabel.text = $"Volume: {volStr}  —  {shapeNote}";
            if (_noteLabel  != null) _noteLabel.text   = isGas
                ? "Sphere shown for gas (actual shape is vapour)"
                : volumeM3 >= 0.1f ? "Scale capped at 0.3 m — actual volume is larger"
                : "";
        }

        private void BuildLabels()
        {
            if (_nameLabel != null) return;

            var n = new GameObject("NameLabel"); n.transform.SetParent(transform, false);
            n.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            _nameLabel = n.AddComponent<TextMeshPro>();
            _nameLabel.fontSize = 0.05f; _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.color = Color.white;

            var v = new GameObject("VolLabel"); v.transform.SetParent(transform, false);
            v.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            _volumeLabel = v.AddComponent<TextMeshPro>();
            _volumeLabel.fontSize = 0.04f; _volumeLabel.alignment = TextAlignmentOptions.Center;
            _volumeLabel.color = new Color(0.8f, 0.8f, 0.8f);

            var nt = new GameObject("NoteLabel"); nt.transform.SetParent(transform, false);
            nt.transform.localPosition = new Vector3(0f, -0.26f, 0f);
            _noteLabel = nt.AddComponent<TextMeshPro>();
            _noteLabel.fontSize = 0.035f; _noteLabel.alignment = TextAlignmentOptions.Center;
            _noteLabel.color = new Color(0.6f, 0.8f, 0.6f);
        }
    }
}
