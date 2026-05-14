using System.Collections.Generic;
using System.Globalization;
using PeriodicAR.AR;
using PeriodicAR.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Heatmaps
{
    /// <summary>
    /// Self-bootstrapping. Adds "Trends" button to HUD (-840, -40).
    /// Tints all periodic table cubes with a viridis gradient based on a chosen property.
    /// Uses MaterialPropertyBlock — no material duplication.
    /// </summary>
    public class HeatmapOverlayController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<HeatmapOverlayController>() != null) return;
            var go = new GameObject("[HeatmapOverlayController]");
            DontDestroyOnLoad(go);
            go.AddComponent<HeatmapOverlayController>();
        }

        public static HeatmapOverlayController Instance { get; private set; }

        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Viridis gradient: 8 stops from 0 (min) to 1 (max).
        private static readonly Color[] s_Viridis =
        {
            new Color(0.267f, 0.005f, 0.329f),
            new Color(0.282f, 0.141f, 0.458f),
            new Color(0.231f, 0.322f, 0.545f),
            new Color(0.173f, 0.499f, 0.558f),
            new Color(0.128f, 0.566f, 0.551f),
            new Color(0.208f, 0.718f, 0.474f),
            new Color(0.706f, 0.867f, 0.173f),
            new Color(0.993f, 0.906f, 0.145f),
        };

        private struct CubeEntry
        {
            public Renderer         renderer;
            public Color            originalColor;
            public AtomElementData  data;
        }

        private TapToPlace    _placer;
        private ElementLoader _loader;
        private GameObject    _hookedTable;
        private List<CubeEntry> _cubes = new();
        private MaterialPropertyBlock _mpb;
        private ElementPropertyResolver.Property? _activeProperty;

        private Button      _trendsButton;
        private GameObject  _selectorPanel;
        private bool        _selectorOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (UI.AppShellController.Instance != null)
            {
                UI.AppShellController.Instance.RegisterWheelItem(new UI.WheelItem
                {
                    id    = "trends",
                    label = "Trends",
                    iconName = "≋",
                    page  = 0,
                    onTap = OpenSelectorViaShell,
                });
            }
            else
            {
                CreateHudButton();
            }
        }

        public void OpenSelectorViaShell()
        {
            // Lazily create the selector panel on a transient canvas if needed.
            if (_selectorPanel == null)
            {
                var cv = FindFirstObjectByType<Canvas>();
                if (cv != null) CreateSelectorPanel(cv);
            }
            ToggleSelector();
        }

        private void Update()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();

            var current = _placer != null ? _placer.CurrentInstance : null;
            if (current != _hookedTable)
            {
                _cubes.Clear();
                _activeProperty = null;
                _hookedTable = current;
                if (current != null && _loader?.Table != null) CacheTable(current);
            }
        }

        private void CacheTable(GameObject table)
        {
            foreach (Transform child in table.transform)
            {
                int z = PeriodicTableLayout.ResolveAtomicNumber(child);
                if (z <= 0) continue;
                var rend = child.GetComponentInChildren<Renderer>();
                if (rend == null) continue;
                var data = _loader.FindByNumber(z);
                if (data == null) continue;

                rend.GetPropertyBlock(_mpb);
                Color c = _mpb.GetColor(s_BaseColorId);
                if (c == default && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(s_BaseColorId))
                    c = rend.sharedMaterial.GetColor(s_BaseColorId);

                _cubes.Add(new CubeEntry { renderer = rend, originalColor = c, data = data });
            }
        }

        public void ApplyHeatmap(ElementPropertyResolver.Property prop)
        {
            if (_cubes.Count == 0) return;
            _activeProperty = prop;

            var elements = new AtomElementData[_cubes.Count];
            for (int i = 0; i < _cubes.Count; i++) elements[i] = _cubes[i].data;
            var (mn, mx) = ElementPropertyResolver.RangeFor(prop, elements);
            float range = Mathf.Max(mx - mn, 1e-4f);

            foreach (var entry in _cubes)
            {
                float v = ElementPropertyResolver.GetValue(prop, entry.data);
                Color tint = v <= 0f
                    ? new Color(0.25f, 0.25f, 0.25f)
                    : SampleViridis((v - mn) / range);

                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, tint);
                entry.renderer.SetPropertyBlock(_mpb);
            }

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeHeatmap = ElementPropertyResolver.DisplayNameFor(prop);
        }

        public void ClearHeatmap()
        {
            _activeProperty = null;
            foreach (var entry in _cubes)
            {
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, entry.originalColor);
                entry.renderer.SetPropertyBlock(_mpb);
            }
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeHeatmap = null;
        }

        // ---- HUD button --------------------------------------------------------

        private void CreateHudButton()
        {
            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null) return;

            var btnGo = new GameObject("TrendsButton", typeof(RectTransform));
            btnGo.transform.SetParent(hudCanvas.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-840f, -40f);
            rt.sizeDelta        = new Vector2(180f, 180f);
            btnGo.AddComponent<Image>().color = new Color(0.06f, 0.14f, 0.22f, 0.85f);
            _trendsButton = btnGo.AddComponent<Button>();
            _trendsButton.onClick.AddListener(ToggleSelector);
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(btnGo.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 30); lrt.offsetMax = new Vector2(-4, -4);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Trends"; tmp.fontSize = 36f; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;

            CreateSelectorPanel(hudCanvas);
        }

        private void CreateSelectorPanel(Canvas parent)
        {
            _selectorPanel = new GameObject("HeatmapSelector", typeof(RectTransform));
            _selectorPanel.transform.SetParent(parent.transform, false);
            var rt = _selectorPanel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-840f, -230f);
            rt.sizeDelta = new Vector2(360f, 580f);
            _selectorPanel.AddComponent<Image>().color = new Color(0.06f, 0.10f, 0.16f, 0.95f);

            var vlg = _selectorPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment        = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight= false;
            vlg.padding  = new RectOffset(10, 10, 10, 10);
            vlg.spacing  = 8f;

            var csf = _selectorPanel.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            AddPanelLabel(_selectorPanel.transform, "Choose Property", 30f, FontStyles.Bold);

            foreach (var prop in ElementPropertyResolver.AllProperties())
            {
                var captured = prop;
                AddPropertyButton(_selectorPanel.transform, ElementPropertyResolver.DisplayNameFor(prop),
                    () => { ApplyHeatmap(captured); ToggleSelector(); });
            }

            // Clear button
            AddPropertyButton(_selectorPanel.transform, "Clear", ClearHeatmap,
                new Color(0.50f, 0.12f, 0.12f, 1f));

            _selectorPanel.SetActive(false);
        }

        private void ToggleSelector()
        {
            _selectorOpen = !_selectorOpen;
            if (_selectorPanel != null) _selectorPanel.SetActive(_selectorOpen);
        }

        private static void AddPanelLabel(Transform parent, string text, float size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 50f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        }

        private static void AddPropertyButton(Transform parent, string label, System.Action onClick,
            Color? bg = null)
        {
            var go = new GameObject(label + "Btn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 72f;
            go.AddComponent<Image>().color = bg ?? new Color(0.15f, 0.25f, 0.40f, 1f);
            var btn = go.AddComponent<Button>(); btn.onClick.AddListener(() => onClick());
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var rt = lGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 30f;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        }

        private static Color SampleViridis(float t)
        {
            t = Mathf.Clamp01(t);
            float f = t * (s_Viridis.Length - 1);
            int lo = Mathf.FloorToInt(f);
            int hi = Mathf.Min(lo + 1, s_Viridis.Length - 1);
            return Color.Lerp(s_Viridis[lo], s_Viridis[hi], f - lo);
        }

        private static Canvas FindHudCanvas()
        {
            var hudController = FindAnyObjectByType<UI.ARHudController>();
            Canvas c = hudController != null ? hudController.GetComponentInParent<Canvas>() : null;
            return c != null ? c : FindFirstObjectByType<Canvas>();
        }
    }
}
