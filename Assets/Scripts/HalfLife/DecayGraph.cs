using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.HalfLife
{
    /// <summary>
    /// Draws a log-scale graph of remaining parent atoms vs simulated time.
    /// Rendered procedurally as a series of UI Images (line segments).
    /// </summary>
    public class DecayGraph : MonoBehaviour
    {
        private RectTransform _plotArea;
        private TextMeshProUGUI _xLabel;
        private TextMeshProUGUI _yLabel;
        private readonly List<GameObject> _lineSegments = new();
        private readonly List<Vector2>    _points       = new();

        private double _halfLifeSeconds;
        private string _halfLifeDisplay;

        private const int   MaxPoints    = 200;
        private const float PlotW        = 240f;
        private const float PlotH        = 120f;

        public void Init(double halfLifeSeconds, string halfLifeDisplay)
        {
            _halfLifeSeconds = halfLifeSeconds;
            _halfLifeDisplay = halfLifeDisplay;
            _points.Clear();
            ClearLines();
            if (_xLabel != null) _xLabel.text = $"Time (×{halfLifeDisplay})";
        }

        public void AddPoint(float parentFraction, double simulatedSeconds)
        {
            if (_halfLifeSeconds <= 0) return;
            float xNorm = (float)(simulatedSeconds / (_halfLifeSeconds * 3.32)); // 3.32 ≈ log2(10) half-lives to reach ~10%
            xNorm = Mathf.Clamp01(xNorm);
            float yNorm = Mathf.Clamp01(parentFraction);

            _points.Add(new Vector2(xNorm * PlotW, yNorm * PlotH));
            if (_points.Count > MaxPoints) _points.RemoveAt(0);

            RedrawLines();
        }

        public void Clear()
        {
            _points.Clear();
            ClearLines();
        }

        public void Build(Transform parent)
        {
            var go = new GameObject("DecayGraph", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(PlotW + 40f, PlotH + 40f);

            // Background
            go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            // Plot area
            var plotGo = new GameObject("PlotArea", typeof(RectTransform));
            plotGo.transform.SetParent(go.transform, false);
            _plotArea = plotGo.GetComponent<RectTransform>();
            _plotArea.anchorMin = Vector2.zero;
            _plotArea.anchorMax = Vector2.zero;
            _plotArea.pivot     = Vector2.zero;
            _plotArea.anchoredPosition = new Vector2(30f, 20f);
            _plotArea.sizeDelta = new Vector2(PlotW, PlotH);

            // Axes lines
            DrawAxisLine(go.transform, new Vector2(30f, 20f), new Vector2(30f + PlotW, 20f));  // X axis
            DrawAxisLine(go.transform, new Vector2(30f, 20f), new Vector2(30f, 20f + PlotH)); // Y axis

            // Y label
            var yGo = new GameObject("YLabel", typeof(RectTransform));
            yGo.transform.SetParent(go.transform, false);
            var yrt = yGo.GetComponent<RectTransform>();
            yrt.anchorMin = yrt.anchorMax = new Vector2(0f, 0.5f);
            yrt.sizeDelta = new Vector2(28f, 80f);
            yrt.anchoredPosition = new Vector2(14f, 0f);
            _yLabel = yGo.AddComponent<TextMeshProUGUI>();
            _yLabel.text = "% parent";
            _yLabel.fontSize = 9f;
            _yLabel.color = UI.Theme.OnSurfaceDim;
            _yLabel.alignment = TextAlignmentOptions.Center;

            // X label
            var xGo = new GameObject("XLabel", typeof(RectTransform));
            xGo.transform.SetParent(go.transform, false);
            var xrt = xGo.GetComponent<RectTransform>();
            xrt.anchorMin = xrt.anchorMax = new Vector2(0.5f, 0f);
            xrt.sizeDelta = new Vector2(220f, 18f);
            xrt.anchoredPosition = new Vector2(15f, 5f);
            _xLabel = xGo.AddComponent<TextMeshProUGUI>();
            _xLabel.text = "Time";
            _xLabel.fontSize = 9f;
            _xLabel.color = UI.Theme.OnSurfaceDim;
            _xLabel.alignment = TextAlignmentOptions.Center;

            // Theoretical curve (faint dotted guide)
            DrawTheoreticalCurve();

            transform.SetParent(parent, false);
            var selfRt = GetComponent<RectTransform>();
            if (selfRt == null) return;
        }

        private void DrawAxisLine(Transform parent, Vector2 from, Vector2 to)
        {
            var seg = new GameObject("Axis", typeof(RectTransform));
            seg.transform.SetParent(parent, false);
            var img = seg.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.3f);
            var rt = seg.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            PositionLineSegment(rt, from, to);
        }

        private void DrawTheoreticalCurve()
        {
            // Draw the ideal exponential decay as faint dashes
            const int steps = 40;
            for (int i = 0; i < steps - 1; i++)
            {
                float t0 = (float)i / (steps - 1);
                float t1 = (float)(i + 1) / (steps - 1);
                float y0 = Mathf.Pow(2f, -t0 * 3.32f);
                float y1 = Mathf.Pow(2f, -t1 * 3.32f);
                if (i % 3 == 0) continue; // skip every 3rd → dashes

                var seg = new GameObject("Guide", typeof(RectTransform));
                seg.transform.SetParent(_plotArea, false);
                var img = seg.AddComponent<Image>();
                img.color = new Color(1f, 1f, 0f, 0.15f);
                var rt = seg.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0f, 0.5f);
                PositionLineSegment(rt,
                    new Vector2(t0 * PlotW, y0 * PlotH),
                    new Vector2(t1 * PlotW, y1 * PlotH));
            }
        }

        private void RedrawLines()
        {
            ClearLines();
            if (_plotArea == null || _points.Count < 2) return;

            for (int i = 0; i < _points.Count - 1; i++)
            {
                var seg = new GameObject("L", typeof(RectTransform));
                seg.transform.SetParent(_plotArea, false);
                var img = seg.AddComponent<Image>();
                img.color = UI.Theme.Accent;
                var rt = seg.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0f, 0.5f);
                PositionLineSegment(rt, _points[i], _points[i + 1]);
                _lineSegments.Add(seg);
            }
        }

        private static void PositionLineSegment(RectTransform rt, Vector2 from, Vector2 to)
        {
            Vector2 dir    = to - from;
            float   length = dir.magnitude;
            float   angle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.anchoredPosition = from;
            rt.sizeDelta        = new Vector2(length, 2f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
        }

        private void ClearLines()
        {
            foreach (var seg in _lineSegments)
                if (seg != null) Destroy(seg);
            _lineSegments.Clear();
        }
    }
}
