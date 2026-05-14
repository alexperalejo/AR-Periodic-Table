using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Equilibrium
{
    public class ReactionChamber : MonoBehaviour
    {
        private EquilibriumReaction _reaction;
        private ParticleSim         _sim;
        private ConcentrationGraph  _graph;

        private TextMeshProUGUI _equationLabel;
        private TextMeshProUGUI _contextLabel;
        private TextMeshProUGUI _perturbResult;
        private Dropdown        _reactionDropdown;
        private Dropdown        _perturbDropdown;
        private Button          _perturbButton;
        private Button          _closeButton;

        private float _reactantConc = 0.5f;
        private float _productConc  = 0.5f;

        public System.Action OnCloseRequested;

        public void Build(Canvas cv)
        {
            float bot = UI.SafeAreaResolver.BottomInset();

            var root = new GameObject("EquilibriumChamber", typeof(RectTransform));
            root.transform.SetParent(cv.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rootRt.sizeDelta = new Vector2(340f, 560f);

            root.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10); vlg.spacing = 6f;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var hRow = NewRow(root.transform, 36f);
            AddLabel(hRow.transform, "Equilibrium & Kinetics", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            _closeButton = AddButton(hRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);
            _closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());

            // Reaction dropdown
            _reactionDropdown = AddDropdown(root.transform, "Reaction", 44f);
            var names = new List<string>();
            foreach (var r in EquilibriumCatalog.GetAll()) names.Add(r.name);
            _reactionDropdown.AddOptions(names);
            _reactionDropdown.onValueChanged.AddListener(SelectReaction);

            // Equation label
            _equationLabel = AddLabel(root.transform, "", UI.Theme.TypeSizeM, UI.Theme.Accent, height: 30f, bold: true);

            // Particle sim container
            var simContainer = new GameObject("SimContainer", typeof(RectTransform));
            simContainer.transform.SetParent(root.transform, false);
            simContainer.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.8f);
            simContainer.AddComponent<LayoutElement>().preferredHeight = 120f;
            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(simContainer.transform, false);
            divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            var drt = divider.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0f); drt.anchorMax = new Vector2(0.5f, 1f);
            drt.pivot = new Vector2(0.5f, 0.5f); drt.sizeDelta = new Vector2(2f, 0f);

            _sim = simContainer.AddComponent<ParticleSim>();

            // Graph
            _graph = root.AddComponent<ConcentrationGraph>();
            _graph.Build(root.transform);

            // Perturb controls
            AddLabel(root.transform, "Perturb Equilibrium:", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 22f);
            var perturbRow = NewRow(root.transform, 44f);
            _perturbDropdown = AddDropdown(perturbRow.transform, "Perturbation", 44f);
            _perturbButton   = AddButton(perturbRow.transform, "Apply", UI.Theme.Accent, UI.Theme.OnSurface);
            _perturbButton.onClick.AddListener(ApplyPerturbation);

            _perturbResult = AddLabel(root.transform, "", UI.Theme.TypeSizeS, UI.Theme.Warning, height: 50f, wrap: true);

            // Context
            _contextLabel = AddLabel(root.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 60f, wrap: true);

            gameObject.AddComponent<RectTransform>().sizeDelta = Vector2.zero;
            transform.SetParent(cv.transform, false);

            SelectReaction(0);
        }

        private void SelectReaction(int idx)
        {
            var all = EquilibriumCatalog.GetAll();
            if (idx >= all.Count) return;
            _reaction = all[idx];

            if (_equationLabel) _equationLabel.text = _reaction.equation;
            if (_contextLabel)  _contextLabel.text  = _reaction.context;

            _reactantConc = 0.5f;
            _productConc  = 0.5f;

            Color rc = Color.blue, pc = Color.green;
            if (_reaction.reactants?.Count > 0) ColorUtility.TryParseHtmlString(_reaction.reactants[0].color, out rc);
            if (_reaction.products?.Count  > 0) ColorUtility.TryParseHtmlString(_reaction.products[0].color,  out pc);

            if (_sim != null)
            {
                var rt = _sim.GetComponent<RectTransform>();
                if (rt == null) rt = _sim.gameObject.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                _sim.Init(rt, rc, pc);
                _sim.SetCounts(40, 40);
            }

            _graph?.UpdateBars(0.5f, 0.5f);

            PopulatePerturbDropdown();

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeEquilibriumReaction = _reaction.id;
        }

        private void PopulatePerturbDropdown()
        {
            if (_perturbDropdown == null || _reaction?.leChatelier == null) return;
            _perturbDropdown.ClearOptions();
            var opts = new List<string>(_reaction.leChatelier.Keys);
            _perturbDropdown.AddOptions(opts);
        }

        private void ApplyPerturbation()
        {
            if (_reaction?.leChatelier == null) return;
            int idx = _perturbDropdown != null ? _perturbDropdown.value : 0;
            var keys = new List<string>(_reaction.leChatelier.Keys);
            if (idx >= keys.Count) return;

            string key = keys[idx];
            string note = _reaction.leChatelier[key];
            if (_perturbResult) _perturbResult.text = note;

            // Simulate equilibrium shift
            bool shiftsRight = note.Contains("RIGHT") || note.Contains("right");
            bool shiftsLeft  = note.Contains("LEFT")  || note.Contains("left");

            if (shiftsRight) { _reactantConc = Mathf.Max(0.1f, _reactantConc - 0.2f); _productConc  = Mathf.Min(0.9f, _productConc  + 0.2f); }
            else if (shiftsLeft) { _productConc  = Mathf.Max(0.1f, _productConc  - 0.2f); _reactantConc = Mathf.Min(0.9f, _reactantConc + 0.2f); }

            _sim?.SetCounts(Mathf.RoundToInt(_reactantConc * 80), Mathf.RoundToInt(_productConc * 80));
            _graph?.UpdateBars(_reactantConc, _productConc);
            UI.AppShellController.Instance?.ShowStatus(shiftsRight ? "→ Shifts right" : shiftsLeft ? "← Shifts left" : "No net shift", 2f);
        }

        // ---- helpers ---------------------------------------------------------------

        private static GameObject NewRow(Transform parent, float h)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false;
            hlg.spacing = 6f;
            go.AddComponent<LayoutElement>().preferredHeight = h;
            return go;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size, Color color,
            bool bold = false, float height = 28f, bool wrap = false)
        {
            var go = new GameObject("L", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (wrap) { tmp.enableWordWrapping = true; tmp.alignment = TextAlignmentOptions.TopLeft; }
            return tmp;
        }

        private static Dropdown AddDropdown(Transform parent, string name, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            go.AddComponent<LayoutElement>().preferredHeight = h;
            var dd = go.AddComponent<Dropdown>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var rt = lGo.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.color = UI.Theme.OnSurface; tmp.fontSize = UI.Theme.TypeSizeS; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return dd;
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
