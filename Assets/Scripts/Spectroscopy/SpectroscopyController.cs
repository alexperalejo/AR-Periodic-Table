using System.Collections.Generic;
using UnityEngine;

namespace PeriodicAR.Spectroscopy
{
    public class SpectroscopyController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<SpectroscopyController>() != null) return;
            var go = new GameObject("[SpectroscopyController]");
            DontDestroyOnLoad(go);
            go.AddComponent<SpectroscopyController>();
        }

        public static SpectroscopyController Instance { get; private set; }

        private GameObject   _stripGo;
        private SpectrumStrip _strip;
        private GameObject   _flameGo;
        private FlameTarget  _flame;
        private bool         _active;

        private readonly List<string> _activeElements = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "spectroscopy",
                label    = "Spectrum",
                iconName = "〜",
                page     = 1,
                onTap    = ToggleSpectroscopy,
            });
        }

        public void ToggleSpectroscopy()
        {
            _active = !_active;

            if (_active)
            {
                EnsureBuilt();
                _stripGo.SetActive(true);
                PositionFlame();
                _flameGo.SetActive(true);
                UI.AppShellController.Instance?.ShowStatus("Drop element cubes into the flame", 4f);
            }
            else
            {
                if (_stripGo) _stripGo.SetActive(false);
                if (_flameGo) _flameGo.SetActive(false);
                _strip?.ClearElements();
                _activeElements.Clear();
                UpdateAppState();
            }
        }

        private void EnsureBuilt()
        {
            if (_stripGo != null) return;

            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            _stripGo = SpectroscopyPrefabFactory.BuildStrip(cv, out _strip);
            _strip.Init();

            _flameGo = SpectroscopyPrefabFactory.BuildFlame(out _flame);
            DontDestroyOnLoad(_flameGo);

            _flame.OnElementAdded   = OnElementAdded;
            _flame.OnElementRemoved = OnElementRemoved;
        }

        private void PositionFlame()
        {
            if (_flameGo == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            _flameGo.transform.position = cam.transform.position + cam.transform.forward * 0.5f
                                          + Vector3.down * 0.1f;
            _flameGo.transform.LookAt(cam.transform);
        }

        private void OnElementAdded(string symbol)
        {
            if (!_activeElements.Contains(symbol)) _activeElements.Add(symbol);
            _strip?.AddElement(symbol);
            UpdateAppState();
            UI.AppShellController.Instance?.ShowStatus($"{symbol} added to flame", 2f);
        }

        private void OnElementRemoved(string symbol)
        {
            _activeElements.Remove(symbol);
            _strip?.RemoveElement(symbol);
            UpdateAppState();
        }

        private void UpdateAppState()
        {
            if (Tutor.AppStateProvider.Instance == null) return;
            var sp = Tutor.AppStateProvider.Instance;
            sp.activeFlameElements.Clear();
            sp.activeFlameElements.AddRange(_activeElements);
        }

        // ---- Tutor tool entry points -----------------------------------------------

        public void IgniteElement(string symbol)
        {
            if (!_active) ToggleSpectroscopy();
            OnElementAdded(symbol);
        }

        public void ShowStellarSpectrum(string star)
        {
            if (!_active) ToggleSpectroscopy();
            if (_strip != null && _stripGo != null)
            {
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.spectrumMode = "stellar:" + star;
                _strip.SetMode(SpectrumMode.Absorption);
            }
            UI.AppShellController.Instance?.ShowStatus($"Showing {star} absorption spectrum", 3f);
        }
    }
}
