using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Experiments
{
    public class ExperimentsController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ExperimentsController>() != null) return;
            var go = new GameObject("[ExperimentsController]");
            DontDestroyOnLoad(go);
            go.AddComponent<ExperimentsController>();
        }

        public static ExperimentsController Instance { get; private set; }

        private GameObject       _panel;
        private ExperimentData   _current;

        private TextMeshProUGUI  _nameLabel;
        private TextMeshProUGUI  _scientistLabel;
        private TextMeshProUGUI  _summaryText;
        private TextMeshProUGUI  _provedText;
        private TextMeshProUGUI  _surpriseText;
        private Image            _accentBar;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "experiments",
                label    = "Experiments",
                iconName = "⚑",
                page     = 2,
                onTap    = TogglePanel,
            });
        }

        public void TogglePanel()
        {
            if (_panel == null) BuildPanel();
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open)
            {
                ShowExperiment(0);
                UI.AppShellController.Instance?.ShowStatus("Famous chemistry experiments", 2f);
            }
            else
            {
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.activeExperiment = null;
            }
        }

        public void ShowExperiment(int index)
        {
            var all = ExperimentsCatalog.GetAll();
            if (index >= all.Count) return;
            _current = all[index];
            UpdateCard();
        }

        public void ShowExperimentById(string id)
        {
            var exp = ExperimentsCatalog.Find(id);
            if (exp == null) return;
            _current = exp;
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            UpdateCard();
        }

        private void UpdateCard()
        {
            if (_current == null) return;
            if (_nameLabel)     _nameLabel.text     = _current.name;
            if (_scientistLabel)_scientistLabel.text = $"{_current.scientist}  ·  {_current.year}";
            if (_summaryText)   _summaryText.text   = _current.summary;
            if (_provedText)    _provedText.text     = $"<b>What it proved:</b> {_current.whatItProved}";
            if (_surpriseText)  _surpriseText.text  = $"<b>The surprise:</b> {_current.surprise}";
            if (_accentBar)
            {
                Color c = UI.Theme.Accent;
                ColorUtility.TryParseHtmlString(_current.color, out c);
                _accentBar.color = c;
            }
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeExperiment = _current.id;

            // Highlight elements on the periodic table
            var sp = Tutor.AppStateProvider.Instance;
            if (sp != null && _current.elements?.Count > 0)
                sp.currentlyHighlighted = new List<string>(_current.elements);
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            float bot = UI.SafeAreaResolver.BottomInset();

            _panel = new GameObject("ExperimentsPanel", typeof(RectTransform));
            _panel.transform.SetParent(cv.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rt.sizeDelta = new Vector2(340f, 560f);

            _panel.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 6f;
            _panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Accent color bar
            var barGo = new GameObject("AccentBar", typeof(RectTransform));
            barGo.transform.SetParent(_panel.transform, false);
            barGo.AddComponent<LayoutElement>().preferredHeight = 6f;
            _accentBar = barGo.AddComponent<Image>();

            // Header row
            var hRow = NewRow(_panel.transform, 36f);
            _nameLabel = AddLabel(hRow.transform, "", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            var closeBtn = AddButton(hRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);
            closeBtn.onClick.AddListener(TogglePanel);

            _scientistLabel = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.Accent, height: 24f);
            _summaryText    = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 60f, wrap: true);
            _provedText     = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 70f, wrap: true);
            _surpriseText   = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 80f, wrap: true);

            // Nav arrows
            var navRow = NewRow(_panel.transform, 44f);
            var prevBtn = AddButton(navRow.transform, "◀ Prev", UI.Theme.SurfaceGlass, UI.Theme.OnSurface);
            var nextBtn = AddButton(navRow.transform, "Next ▶", UI.Theme.SurfaceGlass, UI.Theme.OnSurface);
            int idx = 0;
            prevBtn.onClick.AddListener(() => { idx = Mathf.Max(0, idx - 1); ShowExperiment(idx); });
            nextBtn.onClick.AddListener(() => { idx = Mathf.Min(ExperimentsCatalog.GetAll().Count - 1, idx + 1); ShowExperiment(idx); });

            _panel.SetActive(false);
        }

        // ---- helpers ---------------------------------------------------------------

        private static GameObject NewRow(Transform parent, float h)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false; hlg.spacing = 6f;
            go.AddComponent<LayoutElement>().preferredHeight = h;
            return go;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size, Color color,
            bool bold = false, float height = 28f, bool wrap = false)
        {
            var go = new GameObject("L", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = wrap ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Center;
            if (bold)  tmp.fontStyle     = FontStyles.Bold;
            if (wrap)  tmp.enableWordWrapping = true;
            return tmp;
        }

        private static Button AddButton(Transform parent, string label, Color bg, Color labelColor, float width = -1f)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width; else le.flexibleWidth = 1f;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = UI.Theme.TypeSizeS; tmp.alignment = TextAlignmentOptions.Center; tmp.color = labelColor;
            return btn;
        }
    }
}
