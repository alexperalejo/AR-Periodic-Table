using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Scale
{
    /// <summary>
    /// Self-bootstrapping. Adds "Scale" button to HUD (-40, -240).
    /// Toggles between atom-radius and mole-volume visualizer modes.
    /// </summary>
    public class ScaleController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ScaleController>() != null) return;
            var go = new GameObject("[ScaleController]");
            DontDestroyOnLoad(go);
            go.AddComponent<ScaleController>();
        }

        public static ScaleController Instance { get; private set; }

        private enum ScaleMode { None, Atom, Mole }
        private ScaleMode _mode = ScaleMode.None;

        private GameObject       _visualRoot;
        private AtomScaleVisualizer _atomVis;
        private MoleVisualizer      _moleVis;
        private GameObject          _modePanel;
        private bool                _panelOpen;
        private string              _currentSymbol;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (UI.AppShellController.Instance != null)
            {
                UI.AppShellController.Instance.RegisterWheelItem(new UI.WheelItem
                {
                    id    = "scale",
                    label = "Scale",
                    iconName = "⊚",
                    page  = 0,
                    onTap = ToggleModePanel,
                });
            }
            else
            {
                CreateHudButton();
            }
        }

        public void ToggleModePanel()
        {
            if (_modePanel == null)
            {
                var cv = FindFirstObjectByType<Canvas>();
                if (cv != null) CreateModePanel(cv);
            }
            TogglePanel();
        }

        private void Update()
        {
            // Refresh display when held element changes.
            var sym = Tutor.AppStateProvider.Instance?.heldElementSymbol;
            if (!string.IsNullOrEmpty(sym) && sym != _currentSymbol)
            {
                _currentSymbol = sym;
                if (_mode != ScaleMode.None) ShowCurrent();
            }
        }

        public void ShowAtomMode(string symbol = null)
        {
            _mode = ScaleMode.Atom;
            _currentSymbol = symbol ?? (Tutor.AppStateProvider.Instance?.heldElementSymbol ?? "H");
            EnsureVisualRoot();
            _atomVis.gameObject.SetActive(true);
            _moleVis.gameObject.SetActive(false);
            _atomVis.ShowElement(_currentSymbol);

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeScaleMode = "atom";
        }

        public void ShowMoleMode(string symbol = null)
        {
            _mode = ScaleMode.Mole;
            _currentSymbol = symbol ?? (Tutor.AppStateProvider.Instance?.heldElementSymbol ?? "H");
            EnsureVisualRoot();
            _moleVis.gameObject.SetActive(true);
            _atomVis.gameObject.SetActive(false);
            _moleVis.ShowElement(_currentSymbol);

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeScaleMode = "mole";
        }

        public void HideAll()
        {
            _mode = ScaleMode.None;
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.activeScaleMode = null;
        }

        private void ShowCurrent()
        {
            if (_mode == ScaleMode.Atom) ShowAtomMode(_currentSymbol);
            else if (_mode == ScaleMode.Mole) ShowMoleMode(_currentSymbol);
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot != null) { _visualRoot.SetActive(true); return; }

            _visualRoot = new GameObject("[ScaleVisual]");
            DontDestroyOnLoad(_visualRoot);

            var cam = Camera.main;
            _visualRoot.transform.position = cam != null
                ? cam.transform.position + cam.transform.forward * 0.6f
                : Vector3.forward * 0.6f;

            var atomGo = new GameObject("AtomVis");
            atomGo.transform.SetParent(_visualRoot.transform, false);
            _atomVis = atomGo.AddComponent<AtomScaleVisualizer>();

            var moleGo = new GameObject("MoleVis");
            moleGo.transform.SetParent(_visualRoot.transform, false);
            _moleVis = moleGo.AddComponent<MoleVisualizer>();

            _atomVis.gameObject.SetActive(false);
            _moleVis.gameObject.SetActive(false);
        }

        // ---- HUD button (row 2: y = -240) --------------------------------------

        private void CreateHudButton()
        {
            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null) return;

            var btnGo = MakeHudButton(hudCanvas, "ScaleButton", "Scale",
                new Color(0.10f, 0.14f, 0.08f, 0.85f), new Vector2(-40f, -240f));
            btnGo.GetComponent<Button>().onClick.AddListener(TogglePanel);

            CreateModePanel(hudCanvas);
        }

        private void CreateModePanel(Canvas parent)
        {
            _modePanel = new GameObject("ScaleModePanel", typeof(RectTransform));
            _modePanel.transform.SetParent(parent.transform, false);
            var rt = _modePanel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -430f);
            rt.sizeDelta = new Vector2(280f, 260f);
            _modePanel.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.08f, 0.95f);

            var vlg = _modePanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter; vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false; vlg.spacing = 8f;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            AddModeButton("Atom Size", () => { ShowAtomMode(); TogglePanel(); });
            AddModeButton("Mole Volume", () => { ShowMoleMode(); TogglePanel(); });
            AddModeButton("Hide", () => { HideAll(); TogglePanel(); }, new Color(0.4f, 0.1f, 0.1f, 1f));

            _modePanel.SetActive(false);
        }

        private void AddModeButton(string label, System.Action onClick, Color? bg = null)
        {
            var go = new GameObject(label + "Btn", typeof(RectTransform));
            go.transform.SetParent(_modePanel.transform, false);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 70f;
            go.AddComponent<Image>().color = bg ?? new Color(0.15f, 0.28f, 0.15f, 1f);
            var btn = go.AddComponent<Button>(); btn.onClick.AddListener(() => onClick());
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        }

        private void TogglePanel()
        {
            _panelOpen = !_panelOpen;
            if (_modePanel != null) _modePanel.SetActive(_panelOpen);
        }

        private static GameObject MakeHudButton(Canvas parent, string name, string label,
            Color bg, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(180f, 180f);
            go.AddComponent<Image>().color = bg;
            go.AddComponent<Button>();
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 30); lrt.offsetMax = new Vector2(-4, -4);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 36f; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            return go;
        }

        private static Canvas FindHudCanvas()
        {
            var hud = FindAnyObjectByType<UI.ARHudController>();
            Canvas c = hud != null ? hud.GetComponentInParent<Canvas>() : null;
            return c != null ? c : FindFirstObjectByType<Canvas>();
        }
    }
}
