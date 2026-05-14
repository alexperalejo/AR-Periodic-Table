using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PeriodicAR.Spectroscopy
{
    /// <summary>
    /// World-space flame target. When an element cube enters its trigger zone,
    /// it notifies SpectroscopyController to add emission lines for that element.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FlameTarget : MonoBehaviour
    {
        public System.Action<string> OnElementAdded;
        public System.Action<string> OnElementRemoved;

        private Renderer _flameRenderer;
        private static readonly int s_ColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _mpb;
        private Color _defaultColor = new Color(1f, 0.6f, 0.1f, 0.8f);

        private void Awake()
        {
            _flameRenderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            string sym = ExtractSymbol(other.transform);
            if (string.IsNullOrEmpty(sym)) return;
            OnElementAdded?.Invoke(sym);
            AnimateFlame(sym);
        }

        private void OnTriggerExit(Collider other)
        {
            string sym = ExtractSymbol(other.transform);
            if (string.IsNullOrEmpty(sym)) return;
            OnElementRemoved?.Invoke(sym);
            ResetFlame();
        }

        private static string ExtractSymbol(Transform t)
        {
            int z = AR.PeriodicTableLayout.ResolveAtomicNumber(t);
            if (z <= 0) return null;
            var loader = FindAnyObjectByType<ElementLoader>();
            return loader?.FindByNumber(z)?.symbol;
        }

        private void AnimateFlame(string symbol)
        {
            var lines = EmissionLineCatalog.GetLines(symbol);
            Color flameColor = _defaultColor;
            if (lines != null && lines.Count > 0)
            {
                float topWavelength = lines[0].wavelength;
                flameColor = EmissionLineCatalog.WavelengthToColor(topWavelength);
                flameColor.a = 0.9f;
            }
            SetFlameColor(flameColor);
            StopAllCoroutines();
            StartCoroutine(FlickerFlame(flameColor));
        }

        private void ResetFlame()
        {
            StopAllCoroutines();
            SetFlameColor(_defaultColor);
        }

        private IEnumerator FlickerFlame(Color baseColor)
        {
            while (true)
            {
                float flicker = Random.Range(0.85f, 1.0f);
                SetFlameColor(baseColor * flicker);
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void SetFlameColor(Color c)
        {
            if (_flameRenderer == null) return;
            _flameRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(s_ColorId, c);
            _flameRenderer.SetPropertyBlock(_mpb);
        }
    }
}
