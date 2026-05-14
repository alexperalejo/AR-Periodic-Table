using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PeriodicAR.Orbitals
{
    public class OrbitalController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<OrbitalController>() != null) return;
            var go = new GameObject("[OrbitalController]");
            DontDestroyOnLoad(go);
            go.AddComponent<OrbitalController>();
        }

        public static OrbitalController Instance { get; private set; }

        private GameObject       _panel;
        private TextMeshProUGUI  _configLabel;
        private TextMeshProUGUI  _elementLabel;
        private Button           _closeBtn;

        private GameObject       _viewerGo;
        private OrbitalRenderer  _orbRenderer;

        private ElementLoader _loader;
        private int           _currentZ;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "orbitals",
                label    = "Orbitals",
                iconName = "⊛",
                page     = 0,
                onTap    = ToggleOrbitalView,
            });
        }

        private void Update()
        {
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();
            if (_viewerGo == null || !_viewerGo.activeSelf) return;

            if (_loader == null) return;
            var sp = Tutor.AppStateProvider.Instance;
            if (sp == null) return;

            if (!string.IsNullOrEmpty(sp.heldElementSymbol))
            {
                var elem = FindElement(sp.heldElementSymbol);
                if (elem != null && elem.number != _currentZ)
                    ShowOrbital(elem.number, elem.symbol, elem.name);
            }
        }

        public void ToggleOrbitalView()
        {
            if (_panel == null) BuildPanel();
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);

            if (open)
            {
                SpawnViewer();
                var sp = Tutor.AppStateProvider.Instance;
                if (sp != null) { sp.orbitalViewActive = true; }
                UI.AppShellController.Instance?.ShowStatus("Grab an element cube to show its orbitals", 4f);
            }
            else
            {
                DespawnViewer();
                var sp = Tutor.AppStateProvider.Instance;
                if (sp != null) { sp.orbitalViewActive = false; sp.orbitalViewElement = null; }
            }
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;
            _panel = OrbitalPrefabFactory.BuildPanel(cv, out _configLabel, out _elementLabel, out _closeBtn);
            _closeBtn.onClick.AddListener(ToggleOrbitalView);
        }

        private void SpawnViewer()
        {
            if (_viewerGo != null) { _viewerGo.SetActive(true); return; }
            _viewerGo = new GameObject("[OrbitalViewer]");
            DontDestroyOnLoad(_viewerGo);
            var cam = Camera.main;
            if (cam != null)
                _viewerGo.transform.position = cam.transform.position + cam.transform.forward * 0.4f;
            _orbRenderer = _viewerGo.AddComponent<OrbitalRenderer>();
        }

        private void DespawnViewer()
        {
            if (_viewerGo != null) _viewerGo.SetActive(false);
            _orbRenderer?.Clear();
            _currentZ = 0;
        }

        public void ShowOrbital(int atomicNumber, string symbol, string name)
        {
            if (_viewerGo == null || !_viewerGo.activeSelf) SpawnViewer();
            _currentZ = atomicNumber;
            _orbRenderer?.Show(atomicNumber);

            string configStr = ElectronConfigCalculator.FormatConfig(atomicNumber);
            if (_elementLabel) _elementLabel.text = $"{name} ({symbol})  Orbitals";
            if (_configLabel)  _configLabel.text  = configStr;

            var sp = Tutor.AppStateProvider.Instance;
            if (sp != null) { sp.orbitalViewActive = true; sp.orbitalViewElement = symbol; }

            UI.AppShellController.Instance?.ShowStatus($"{symbol}: {configStr}", 3f);
        }

        // ---- Tutor tool entry points -----------------------------------------------

        public void ShowOrbitalsForSymbol(string symbol)
        {
            if (_loader == null) _loader = FindAnyObjectByType<ElementLoader>();
            if (_loader == null) return;
            var elem = FindElement(symbol);
            if (elem == null) return;
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            ShowOrbital(elem.number, elem.symbol, elem.name);
        }

        private Data.AtomElementData FindElement(string symbol)
        {
            if (_loader?.Table?.elements == null) return null;
            foreach (var e in _loader.Table.elements)
                if (e.symbol == symbol) return e;
            return null;
        }
    }
}
