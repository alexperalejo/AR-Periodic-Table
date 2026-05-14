using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.HalfLife
{
    public class HalfLifeController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<HalfLifeController>() != null) return;
            var go = new GameObject("[HalfLifeController]");
            DontDestroyOnLoad(go);
            go.AddComponent<HalfLifeController>();
        }

        public static HalfLifeController Instance { get; private set; }

        private GameObject         _bench;
        private AtomCloud          _cloud;
        private DecayGraph         _graph;
        private TextMeshProUGUI    _statsLabel;
        private TextMeshProUGUI    _storyLabel;
        private Dropdown           _isotopeDropdown;
        private Button             _runButton;
        private Button             _resetButton;
        private Dropdown           _speedDropdown;

        private List<IsotopeData>  _isotopes;
        private IsotopeData        _current;
        private bool               _running;

        private static readonly float[] SpeedMultipliers = { 1f, 100f, 1000f, float.MaxValue };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "halflife",
                label    = "Half-Life",
                iconName = "☢",
                page     = 2,
                onTap    = ToggleBench,
            });
        }

        public void ToggleBench()
        {
            if (_bench == null) BuildBench();
            bool open = !_bench.activeSelf;
            _bench.SetActive(open);
            if (open) SelectIsotope(0);
        }

        private void BuildBench()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            _bench = HalfLifePrefabFactory.BuildBench(cv,
                out _cloud, out _graph, out _statsLabel, out _storyLabel,
                out _isotopeDropdown, out _runButton, out _resetButton, out _speedDropdown);

            // Populate isotope dropdown
            _isotopes = IsotopeCatalog.GetAll();
            var names = new List<string>();
            foreach (var iso in _isotopes) names.Add(iso.id);
            _isotopeDropdown.ClearOptions();
            _isotopeDropdown.AddOptions(names);
            _isotopeDropdown.onValueChanged.AddListener(SelectIsotope);

            _speedDropdown.onValueChanged.AddListener(OnSpeedChanged);

            _runButton.onClick.AddListener(OnRunPressed);
            _resetButton.onClick.AddListener(OnResetPressed);

            // Build atom cloud as world-space object
            var cloudGo = new GameObject("[AtomCloud]");
            DontDestroyOnLoad(cloudGo);
            cloudGo.transform.position = Vector3.zero;
            _cloud = cloudGo.AddComponent<AtomCloud>();
            _cloud.OnStatsChanged = OnStatsChanged;

            // Init graph
            _graph.Build(_bench.transform);
        }

        private void SelectIsotope(int index)
        {
            if (_isotopes == null || index >= _isotopes.Count) return;
            _current = _isotopes[index];

            _cloud?.Init(_current, GetSpeedMultiplier());
            _cloud?.ResetSimulation();
            _graph?.Init(_current.halfLifeSeconds, _current.halfLifeDisplay);
            _graph?.Clear();

            if (_storyLabel != null)
                _storyLabel.text = $"<b>{_current.use}</b>\n{_current.story}";

            if (_statsLabel != null)
                _statsLabel.text = FormatStats(1000, 0, 0);

            UpdateRunButtonLabel();

            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.halfLifeIsotope = _current.id;

            UI.AppShellController.Instance?.ShowStatus($"Isotope: {_current.id} — t½ = {_current.halfLifeDisplay}", 3f);
        }

        private void OnRunPressed()
        {
            if (_cloud == null) return;

            // "To Completion" mode
            int speedIdx = _speedDropdown != null ? _speedDropdown.value : 0;
            if (speedIdx == 3)
            {
                _cloud.JumpToFraction(0.5f);
                _graph?.AddPoint(0.5f, _current?.halfLifeSeconds ?? 1e10);
                return;
            }

            _running = !_running;
            if (_running)
                _cloud.StartSimulation();
            else
                _cloud.PauseSimulation();

            UpdateRunButtonLabel();
        }

        private void OnResetPressed()
        {
            _running = false;
            _cloud?.ResetSimulation();
            _graph?.Init(_current?.halfLifeSeconds ?? 1e10, _current?.halfLifeDisplay ?? "");
            _graph?.Clear();
            UpdateRunButtonLabel();
        }

        private void OnSpeedChanged(int idx)
        {
            _cloud?.SetTimeMultiplier(GetSpeedMultiplier());
        }

        private float GetSpeedMultiplier()
        {
            int idx = _speedDropdown != null ? _speedDropdown.value : 0;
            if (idx < 0 || idx >= 3) return 1f;
            return SpeedMultipliers[idx];
        }

        private void OnStatsChanged(int parent, int decayed, double simSeconds)
        {
            if (_statsLabel != null)
                _statsLabel.text = FormatStats(parent, decayed, simSeconds);

            float fraction = parent / 1000f;
            _graph?.AddPoint(fraction, simSeconds);
        }

        private void UpdateRunButtonLabel()
        {
            if (_runButton == null) return;
            var tmp = _runButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = _running ? "⏸  Pause" : "▶  Run";
        }

        private static string FormatStats(int parent, int decayed, double simSeconds)
        {
            string timeStr = FormatTime(simSeconds);
            return $"Original: {parent}   Decayed: {decayed}   Time: {timeStr}";
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 60)        return $"{seconds:F0}s";
            if (seconds < 3600)      return $"{seconds / 60:F1} min";
            if (seconds < 86400)     return $"{seconds / 3600:F1} hr";
            if (seconds < 3.156e7)   return $"{seconds / 86400:F1} days";
            if (seconds < 3.156e9)   return $"{seconds / 3.156e7:F1} yr";
            if (seconds < 3.156e12)  return $"{seconds / 3.156e9:F2} kyr";
            if (seconds < 3.156e15)  return $"{seconds / 3.156e12:F2} Myr";
            return $"{seconds / 3.156e16:F2} Gyr";
        }

        // ---- Tutor tool entry points -----------------------------------------------

        public void SimulateIsotope(string isotopeId)
        {
            if (_bench == null) BuildBench();
            _bench.SetActive(true);

            int idx = _isotopes?.FindIndex(x => x.id == isotopeId) ?? -1;
            if (idx < 0) return;
            if (_isotopeDropdown != null) _isotopeDropdown.value = idx;
            SelectIsotope(idx);
        }

        public void DateSample(string isotopeId, float fractionRemaining)
        {
            SimulateIsotope(isotopeId);
            _cloud?.JumpToFraction(fractionRemaining);
            if (_current != null)
            {
                double halfLives = -Mathf.Log(fractionRemaining) / Mathf.Log(2f);
                double ageSeconds = halfLives * _current.halfLifeSeconds;
                string ageStr = FormatTime(ageSeconds);
                UI.AppShellController.Instance?.ShowStatus($"Sample age ≈ {ageStr} ({halfLives:F1} half-lives)", 5f);
                _graph?.AddPoint(fractionRemaining, ageSeconds);
            }
        }
    }
}
