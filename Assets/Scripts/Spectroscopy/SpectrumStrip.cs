using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeriodicAR.Spectroscopy
{
    public enum SpectrumMode { Emission, Absorption }

    [RequireComponent(typeof(SpectrumStripBindings))]
    public class SpectrumStrip : MonoBehaviour
    {
        private SpectrumStripBindings _b;
        private SpectrumMode          _mode = SpectrumMode.Emission;
        private readonly List<GameObject> _lineObjs = new();
        private readonly List<string>     _activeSymbols = new();

        private const float MinNm = 400f;
        private const float MaxNm = 700f;
        private const float StripW = 480f;
        private const float StripH = 60f;
        private const float LineW  = 2.5f;

        private void Awake()
        {
            _b = GetComponent<SpectrumStripBindings>();
        }

        public void Init()
        {
            if (_b.emissionBtn)   _b.emissionBtn.onClick.AddListener(SetEmission);
            if (_b.absorptionBtn) _b.absorptionBtn.onClick.AddListener(SetAbsorption);
            if (_b.starsBtn)      _b.starsBtn.onClick.AddListener(ToggleStars);
            if (_b.popupClose)    _b.popupClose.onClick.AddListener(HidePopup);
            SetEmission();
        }

        public void AddElement(string symbol)
        {
            if (_activeSymbols.Contains(symbol)) return;
            _activeSymbols.Add(symbol);
            Redraw();
        }

        public void RemoveElement(string symbol)
        {
            _activeSymbols.Remove(symbol);
            Redraw();
        }

        public void ClearElements()
        {
            _activeSymbols.Clear();
            Redraw();
        }

        public void SetMode(SpectrumMode mode)
        {
            _mode = mode;
            Redraw();
            if (_b.modeLabel) _b.modeLabel.text = mode == SpectrumMode.Emission ? "Emission" : "Absorption";
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.spectrumMode = mode == SpectrumMode.Emission ? "emission" : "absorption";
        }

        private void SetEmission()   => SetMode(SpectrumMode.Emission);
        private void SetAbsorption() => SetMode(SpectrumMode.Absorption);

        private void Redraw()
        {
            foreach (var go in _lineObjs) if (go) Destroy(go);
            _lineObjs.Clear();

            if (_b.background)
                _b.background.color = _mode == SpectrumMode.Emission
                    ? new Color(0.02f, 0.02f, 0.04f, 0.95f)
                    : new Color(0.96f, 0.96f, 0.92f, 0.95f);

            if (_mode == SpectrumMode.Absorption) DrawRainbowBackground();

            foreach (var sym in _activeSymbols) DrawLinesFor(sym);
        }

        private void DrawRainbowBackground()
        {
            const int steps = 60;
            for (int i = 0; i < steps; i++)
            {
                float t  = (float)i / steps;
                float nm = Mathf.Lerp(MinNm, MaxNm, t);
                var seg = MakeLineGo($"Rainbow_{i}");
                var img = seg.AddComponent<Image>();
                img.color = EmissionLineCatalog.WavelengthToColor(nm);
                var rt  = seg.GetComponent<RectTransform>();
                float x = t * StripW;
                float w = StripW / steps + 1f;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(w, StripH);
                _lineObjs.Add(seg);
            }
        }

        private void DrawLinesFor(string symbol)
        {
            var lines = EmissionLineCatalog.GetLines(symbol);
            if (lines == null || lines.Count == 0)
            {
                DrawOutOfRangeMarker(symbol);
                return;
            }

            bool anyVisible = false;
            foreach (var line in lines)
            {
                if (line.wavelength < MinNm || line.wavelength > MaxNm) continue;
                anyVisible = true;

                float xNorm = (line.wavelength - MinNm) / (MaxNm - MinNm);
                float x     = xNorm * StripW;
                float h     = _mode == SpectrumMode.Emission
                    ? StripH * line.intensity
                    : StripH;

                var seg = MakeLineGo($"Line_{symbol}_{line.wavelength}");
                var img = seg.AddComponent<Image>();
                Color c = EmissionLineCatalog.WavelengthToColor(line.wavelength);
                img.color = _mode == SpectrumMode.Emission
                    ? new Color(c.r, c.g, c.b, line.intensity)
                    : new Color(0f, 0f, 0f, 0.7f * line.intensity);

                var rt = seg.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(x - LineW * 0.5f, 0f);
                rt.sizeDelta = new Vector2(LineW, h);
                _lineObjs.Add(seg);

                // Make it tappable
                var trig = seg.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                var captured = line;
                var capturedSym = symbol;
                entry.callback.AddListener((_) => ShowLinePopup(capturedSym, captured));
                trig.triggers.Add(entry);
            }

            if (!anyVisible) DrawOutOfRangeMarker(symbol);
        }

        private void DrawOutOfRangeMarker(string symbol)
        {
            // small UV/IR edge notch
            var seg = MakeLineGo($"OOR_{symbol}");
            var img = seg.AddComponent<Image>();
            img.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            var rt = seg.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(2f, StripH * 0.5f);
            rt.sizeDelta = new Vector2(4f, StripH * 0.4f);
            _lineObjs.Add(seg);
        }

        private GameObject MakeLineGo(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_b.linesContainer, false);
            return go;
        }

        private void ShowLinePopup(string symbol, EmissionLine line)
        {
            if (_b.linePopup == null) return;
            _b.linePopup.gameObject.SetActive(true);
            if (_b.popupText)
                _b.popupText.text = $"<b>{symbol}  {line.wavelength:F1} nm</b>\n{line.transition}\nRelative intensity: {line.intensity:P0}";
        }

        private void HidePopup()
        {
            if (_b.linePopup) _b.linePopup.gameObject.SetActive(false);
        }

        private bool _starsVisible;
        private void ToggleStars()
        {
            _starsVisible = !_starsVisible;
            if (_b.starOverlay) _b.starOverlay.gameObject.SetActive(_starsVisible);
        }
    }
}
