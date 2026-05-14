using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.CompoundScanner
{
    /// <summary>
    /// Compound Scanner (Part 11). Parses typed or detected text for chemical formulas
    /// and looks them up in the local database. Falls back to the LLM for unrecognized formulas.
    /// OCR integration reuses the existing object-detection camera frame pipeline.
    /// </summary>
    public class CompoundScannerController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<CompoundScannerController>() != null) return;
            var go = new GameObject("[CompoundScannerController]");
            DontDestroyOnLoad(go);
            go.AddComponent<CompoundScannerController>();
        }

        public static CompoundScannerController Instance { get; private set; }

        private GameObject      _panel;
        private TMP_InputField  _inputField;
        private TextMeshProUGUI _resultName;
        private TextMeshProUGUI _resultElements;
        private TextMeshProUGUI _resultCommonIn;
        private TextMeshProUGUI _statusText;
        private Button          _searchButton;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "compound",
                label    = "Compound Scan",
                iconName = "🔬",
                page     = 2,
                onTap    = TogglePanel,
            });
        }

        public void TogglePanel()
        {
            if (_panel == null) BuildPanel();
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open) UI.AppShellController.Instance?.ShowStatus("Type a formula or ingredient list to identify elements", 4f);
            else if (Tutor.AppStateProvider.Instance != null) Tutor.AppStateProvider.Instance.lastIdentifiedCompound = null;
        }

        public void IdentifyCompound(string text)
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            if (_inputField) _inputField.text = text;
            RunSearch(text);
        }

        private void RunSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("Enter a formula or ingredient name");
                return;
            }

            // Try exact formula match first
            var compound = CompoundDatabase.Find(text.Trim());
            if (compound != null) { ShowResult(compound); return; }

            // Extract formula-like tokens (capital letter + optional lowercase + digits)
            var tokens = ExtractFormulas(text);
            foreach (var token in tokens)
            {
                compound = CompoundDatabase.Find(token);
                if (compound != null) { ShowResult(compound); return; }
            }

            SetStatus($"'{text}' not in local database. Tap Tutor to ask about it.");
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.lastIdentifiedCompound = text;
        }

        private void ShowResult(CompoundEntry c)
        {
            if (_resultName)     _resultName.text     = $"{c.name}  ({c.formula})";
            if (_resultElements) _resultElements.text = $"Elements: {string.Join(", ", c.elements)}";
            if (_resultCommonIn) _resultCommonIn.text = $"Found in: {c.commonIn}";
            SetStatus("");

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.lastIdentifiedCompound = c.formula;

            // Highlight elements on the table
            var sp = Tutor.AppStateProvider.Instance;
            if (sp != null && c.elements?.Count > 0)
                sp.currentlyHighlighted = new List<string>(c.elements);

            UI.AppShellController.Instance?.ShowStatus($"Identified: {c.name}", 3f);
        }

        private void SetStatus(string msg)
        {
            if (_statusText) _statusText.text = msg;
            if (_resultName) _resultName.text = "";
            if (_resultElements) _resultElements.text = "";
            if (_resultCommonIn) _resultCommonIn.text = "";
        }

        private static List<string> ExtractFormulas(string text)
        {
            var result = new List<string>();
            var matches = Regex.Matches(text, @"\b[A-Z][a-z]?(?:\d*[A-Z][a-z]?)*\d*\b");
            foreach (Match m in matches) result.Add(m.Value);
            return result;
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            float bot = UI.SafeAreaResolver.BottomInset();
            _panel = new GameObject("CompoundScannerPanel", typeof(RectTransform));
            _panel.transform.SetParent(cv.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rt.sizeDelta = new Vector2(320f, 360f);

            _panel.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 6f;
            _panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var hRow = NewRow(_panel.transform, 36f);
            AddLabel(hRow.transform, "Compound Scanner", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            var closeBtn = AddButton(hRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);
            closeBtn.onClick.AddListener(TogglePanel);

            // Input row
            var inputRow = NewRow(_panel.transform, 44f);
            var inputGo = new GameObject("Input", typeof(RectTransform));
            inputGo.transform.SetParent(inputRow.transform, false);
            inputGo.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            inputGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _inputField = inputGo.AddComponent<TMP_InputField>();
            var inputText = new GameObject("Text", typeof(RectTransform));
            inputText.transform.SetParent(inputGo.transform, false);
            var inputTrt = inputText.GetComponent<RectTransform>();
            inputTrt.anchorMin = Vector2.zero; inputTrt.anchorMax = Vector2.one; inputTrt.offsetMin = new Vector2(8, 4); inputTrt.offsetMax = new Vector2(-8, -4);
            var inputTmp = inputText.AddComponent<TextMeshProUGUI>();
            inputTmp.color = UI.Theme.OnSurface; inputTmp.fontSize = UI.Theme.TypeSizeS;
            _inputField.textComponent = inputTmp;
            _inputField.placeholder = null;

            _searchButton = AddButton(inputRow.transform, "Search", UI.Theme.Accent, UI.Theme.OnSurface, 70f);
            _searchButton.onClick.AddListener(() => RunSearch(_inputField?.text ?? ""));

            _statusText     = AddLabel(_panel.transform, "Enter a formula (e.g. H2O) or ingredient name", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 30f);
            _resultName     = AddLabel(_panel.transform, "", UI.Theme.TypeSizeM, UI.Theme.Accent, bold: true, height: 32f);
            _resultElements = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 28f);
            _resultCommonIn = AddLabel(_panel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 60f, wrap: true);

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
