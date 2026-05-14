using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Safety
{
    public class SafetyController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<SafetyController>() != null) return;
            var go = new GameObject("[SafetyController]");
            DontDestroyOnLoad(go);
            go.AddComponent<SafetyController>();
        }

        public static SafetyController Instance { get; private set; }

        private GameObject      _panel;
        private int             _currentIdx;

        private Image            _hazardBar;
        private TextMeshProUGUI  _comboPairLabel;
        private TextMeshProUGUI  _hazardLabel;
        private TextMeshProUGUI  _reactionLabel;
        private TextMeshProUGUI  _effectsText;
        private TextMeshProUGUI  _scenarioText;
        private TextMeshProUGUI  _adviceText;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "safety",
                label    = "Safety",
                iconName = "⚠",
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
                ShowCard(_currentIdx);
                UI.AppShellController.Instance?.ShowStatus("⚠ Dangerous chemical combinations to avoid", 3f);
            }
        }

        public void ShowSafetyCard(string comboId)
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            int idx = SafetyCatalog.GetAll().FindIndex(c => c.id == comboId);
            if (idx >= 0) ShowCard(idx);
        }

        private void ShowCard(int idx)
        {
            var all = SafetyCatalog.GetAll();
            if (all.Count == 0) return;
            idx = Mathf.Clamp(idx, 0, all.Count - 1);
            _currentIdx = idx;
            var combo = all[idx];

            Color hazardColor = UI.Theme.Danger;
            ColorUtility.TryParseHtmlString(combo.hazardColor, out hazardColor);

            if (_hazardBar)      _hazardBar.color      = hazardColor;
            if (_comboPairLabel) _comboPairLabel.text  = $"{combo.ingredientA}  +  {combo.ingredientB}";
            if (_hazardLabel)
            {
                _hazardLabel.text  = combo.hazard;
                _hazardLabel.color = hazardColor;
            }
            if (_reactionLabel)  _reactionLabel.text   = combo.reaction;
            if (_effectsText)    _effectsText.text     = $"<b>Effects:</b> {combo.effects}";
            if (_scenarioText)   _scenarioText.text    = $"<b>When this happens:</b> {combo.commonScenario}";
            if (_adviceText)     _adviceText.text      = $"<b>✓ Do this instead:</b> {combo.advice}";
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            float bot = UI.SafeAreaResolver.BottomInset();
            _panel = new GameObject("SafetyPanel", typeof(RectTransform));
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

            // Hazard color bar
            var barGo = new GameObject("HazardBar", typeof(RectTransform));
            barGo.transform.SetParent(_panel.transform, false);
            barGo.AddComponent<LayoutElement>().preferredHeight = 8f;
            _hazardBar = barGo.AddComponent<Image>();
            _hazardBar.color = UI.Theme.Danger;

            // Header
            var hRow = NewRow(_panel.transform, 36f);
            AddLabel(hRow.transform, "⚠ Lab Safety", UI.Theme.TypeSizeM, UI.Theme.Danger, bold: true);
            var closeBtn = AddButton(hRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);
            closeBtn.onClick.AddListener(TogglePanel);

            _hazardLabel    = AddLabel(_panel.transform, "DEADLY", UI.Theme.TypeSizeM, UI.Theme.Danger, bold: true, height: 30f);
            _comboPairLabel = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS + 1f, UI.Theme.OnSurface, height: 36f, wrap: true);
            _reactionLabel  = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, new Color(0.8f, 0.8f, 0.5f), height: 24f);
            _effectsText    = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 70f, wrap: true);
            _scenarioText   = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 50f, wrap: true);
            _adviceText     = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.Success, height: 50f, wrap: true);

            // Navigation
            var navRow = NewRow(_panel.transform, 44f);
            var prevBtn = AddButton(navRow.transform, "◀ Prev", UI.Theme.SurfaceGlass, UI.Theme.OnSurface);
            var nextBtn = AddButton(navRow.transform, "Next ▶", UI.Theme.SurfaceGlass, UI.Theme.OnSurface);
            prevBtn.onClick.AddListener(() => ShowCard(_currentIdx - 1));
            nextBtn.onClick.AddListener(() => ShowCard(_currentIdx + 1));

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
