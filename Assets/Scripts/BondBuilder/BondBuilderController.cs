using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.BondBuilder
{
    public class BondBuilderController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<BondBuilderController>() != null) return;
            var go = new GameObject("[BondBuilderController]");
            DontDestroyOnLoad(go);
            go.AddComponent<BondBuilderController>();
        }

        public static BondBuilderController Instance { get; private set; }

        private GameObject       _panel;
        private GameObject       _rendererGo;
        private MoleculeRenderer _renderer;
        private MoleculeRecipe   _current;

        private TextMeshProUGUI  _nameLabel;
        private TextMeshProUGUI  _geometryLabel;
        private TextMeshProUGUI  _storyText;
        private TextMeshProUGUI  _polarityLabel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "bonds",
                label    = "Bond Builder",
                iconName = "⬡",
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
                EnsureRenderer();
                _rendererGo.SetActive(true);
                ShowMolecule(0);
                UI.AppShellController.Instance?.ShowStatus("Tap a molecule to explore its bonds", 3f);
            }
            else
            {
                if (_rendererGo) _rendererGo.SetActive(false);
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.buildingMolecule = null;
            }
        }

        public void ShowMolecule(int idx)
        {
            var all = MoleculeRecipeCatalog.GetAll();
            if (idx >= all.Count) return;
            _current = all[idx];

            _renderer?.Render(_current);

            if (_nameLabel)     _nameLabel.text     = $"{_current.name}  ({_current.formula})";
            if (_geometryLabel) _geometryLabel.text = $"Geometry: {_current.geometry}  ·  Bond angle: {_current.bondAngle}°";
            if (_polarityLabel) _polarityLabel.text = $"Polarity: {_current.polarity}";
            if (_storyText)     _storyText.text     = _current.story;

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.buildingMolecule = _current.id;
        }

        public void ShowMoleculeById(string molId)
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            EnsureRenderer();
            _rendererGo.SetActive(true);

            int idx = MoleculeRecipeCatalog.GetAll().FindIndex(m => m.id == molId);
            if (idx >= 0) ShowMolecule(idx);
        }

        private void EnsureRenderer()
        {
            if (_rendererGo != null) return;
            _rendererGo = new GameObject("[MoleculeRenderer]");
            DontDestroyOnLoad(_rendererGo);
            var cam = Camera.main;
            if (cam != null)
                _rendererGo.transform.position = cam.transform.position + cam.transform.forward * 0.4f;
            _rendererGo.transform.localScale = Vector3.one * 0.8f;
            _renderer = _rendererGo.AddComponent<MoleculeRenderer>();
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            float bot = UI.SafeAreaResolver.BottomInset();
            _panel = new GameObject("BondBuilderPanel", typeof(RectTransform));
            _panel.transform.SetParent(cv.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rt.sizeDelta = new Vector2(310f, 400f);

            _panel.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 6f;
            _panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var hRow = NewRow(_panel.transform, 36f);
            AddLabel(hRow.transform, "Bond Builder", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            var closeBtn = AddButton(hRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);
            closeBtn.onClick.AddListener(TogglePanel);

            _nameLabel     = AddLabel(_panel.transform, "", UI.Theme.TypeSizeM, UI.Theme.Accent, bold: true, height: 30f);
            _geometryLabel = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 24f);
            _polarityLabel = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 22f);
            _storyText     = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 80f, wrap: true);

            // Molecule picker buttons
            var molRow = NewRow(_panel.transform, 44f);
            int navIdx = 0;
            var prevBtn = AddButton(molRow.transform, "◀", UI.Theme.SurfaceGlass, UI.Theme.OnSurface);
            var nextBtn = AddButton(molRow.transform, "▶", UI.Theme.SurfaceGlass, UI.Theme.OnSurface);
            prevBtn.onClick.AddListener(() => { navIdx = Mathf.Max(0, navIdx - 1); ShowMolecule(navIdx); });
            nextBtn.onClick.AddListener(() => { navIdx = Mathf.Min(MoleculeRecipeCatalog.GetAll().Count - 1, navIdx + 1); ShowMolecule(navIdx); });

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
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (wrap) tmp.enableWordWrapping = true;
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
