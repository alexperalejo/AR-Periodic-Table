using System.Collections.Generic;
using PeriodicAR.AR;
using PeriodicAR.Data;
using PeriodicAR.Heatmaps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Abundance
{
    /// <summary>
    /// Self-bootstrapping. Adds "Abundance" wheel item (page 0).
    /// Colors all periodic table cubes by log-scale abundance in a chosen context.
    /// Reuses the viridis gradient from HeatmapOverlayController.
    /// </summary>
    public class AbundanceOverlayController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<AbundanceOverlayController>() != null) return;
            var go = new GameObject("[AbundanceOverlayController]");
            DontDestroyOnLoad(go);
            go.AddComponent<AbundanceOverlayController>();
        }

        public static AbundanceOverlayController Instance { get; private set; }

        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color[] s_Viridis = new[]
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
            public Renderer        renderer;
            public Color           originalColor;
            public AtomElementData data;
        }

        private TapToPlace    _placer;
        private ElementLoader _loader;
        private GameObject    _hookedTable;
        private List<CubeEntry> _cubes = new();
        private MaterialPropertyBlock _mpb;
        private string _activeContext;

        private GameObject _selectorPanel;
        private bool       _selectorOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id    = "abundance",
                label = "Abundance",
                iconName = "◉",
                page  = 0,
                onTap = OpenSelector,
            });
        }

        private void Update()
        {
            if (_placer == null) _placer = FindAnyObjectByType<TapToPlace>();
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();

            var current = _placer?.CurrentInstance;
            if (current != _hookedTable)
            {
                _cubes.Clear();
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

        public void ApplyContext(string contextId)
        {
            if (_cubes.Count == 0) return;
            _activeContext = contextId;

            var (logMin, logMax) = AbundanceCatalog.GetLogRange(contextId);
            float range = Mathf.Max(logMax - logMin, 0.001f);

            foreach (var entry in _cubes)
            {
                float v = AbundanceCatalog.GetAbundance(contextId, entry.data.symbol);
                Color tint;
                if (v <= 0f)
                    tint = new Color(0.1f, 0.1f, 0.15f);
                else
                {
                    float norm = (Mathf.Log10(v) - logMin) / range;
                    tint = SampleViridis(Mathf.Clamp01(norm));
                }
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, tint);
                entry.renderer.SetPropertyBlock(_mpb);
            }

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeAbundanceContext = contextId;

            UI.AppShellController.Instance?.ShowStatus($"Abundance: {contextId}", 3f);
        }

        public void ClearAbundance()
        {
            _activeContext = null;
            foreach (var entry in _cubes)
            {
                entry.renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_BaseColorId, entry.originalColor);
                entry.renderer.SetPropertyBlock(_mpb);
            }
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeAbundanceContext = null;
        }

        // ---- Selector panel ------------------------------------------------------

        public void OpenSelector()
        {
            if (_selectorPanel == null) BuildSelectorPanel();
            _selectorOpen = !_selectorOpen;
            if (_selectorPanel != null) _selectorPanel.SetActive(_selectorOpen);
        }

        private void BuildSelectorPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            _selectorPanel = new GameObject("AbundanceSelector", typeof(RectTransform));
            _selectorPanel.transform.SetParent(cv.transform, false);
            var rt = _selectorPanel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            float bot = UI.SafeAreaResolver.BottomInset();
            rt.anchoredPosition = new Vector2(bot, bot + UI.Theme.FabSize + 60f);
            rt.sizeDelta = new Vector2(260f, 400f);

            _selectorPanel.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = _selectorPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8); vlg.spacing = 6f;
            _selectorPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddSelectorLabel("Choose Context");

            foreach (var id in AbundanceCatalog.GetContextIds())
            {
                string captured = id;
                AddSelectorButton(id, () => { ApplyContext(captured); OpenSelector(); });
            }
            AddSelectorButton("Clear", () => { ClearAbundance(); OpenSelector(); },
                new Color(0.4f, 0.1f, 0.1f));

            _selectorPanel.SetActive(false);
        }

        private void AddSelectorLabel(string text)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(_selectorPanel.transform, false);
            go.AddComponent<LayoutElement>().preferredHeight = 40f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = UI.Theme.TypeSizeS;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = UI.Theme.OnSurface;
            tmp.fontStyle = FontStyles.Bold;
        }

        private void AddSelectorButton(string label, System.Action onClick, Color? bg = null)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(_selectorPanel.transform, false);
            go.AddComponent<LayoutElement>().preferredHeight = 50f;
            go.AddComponent<Image>().color = bg ?? UI.Theme.SurfaceGlass;
            var btn = go.AddComponent<Button>(); btn.onClick.AddListener(() => onClick());
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = UI.Theme.TypeSizeS;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = UI.Theme.OnSurface;
        }

        private static Color SampleViridis(float t)
        {
            t = Mathf.Clamp01(t);
            float scaled = t * (s_Viridis.Length - 1);
            int   lo     = Mathf.FloorToInt(scaled);
            int   hi     = Mathf.Min(lo + 1, s_Viridis.Length - 1);
            return Color.Lerp(s_Viridis[lo], s_Viridis[hi], scaled - lo);
        }
    }
}
