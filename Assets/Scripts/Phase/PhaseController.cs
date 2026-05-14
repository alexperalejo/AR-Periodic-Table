using UnityEngine;

namespace PeriodicAR.Phase
{
    public class PhaseController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<PhaseController>() != null) return;
            var go = new GameObject("[PhaseController]");
            DontDestroyOnLoad(go);
            go.AddComponent<PhaseController>();
        }

        public static PhaseController Instance { get; private set; }

        private GameObject  _panel;
        private PhaseChamber _chamber;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "phase",
                label    = "Phase",
                iconName = "⬡",
                page     = 1,
                onTap    = TogglePanel,
            });
        }

        public void TogglePanel()
        {
            if (_panel == null) BuildPanel();
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open) UI.AppShellController.Instance?.ShowStatus("Drag sliders to change temperature and pressure", 4f);
            else
            {
                if (Tutor.AppStateProvider.Instance != null)
                {
                    Tutor.AppStateProvider.Instance.activePhaseSubstance = null;
                    Tutor.AppStateProvider.Instance.currentPhase         = null;
                }
            }
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;
            _panel = PhasePrefabFactory.BuildChamber(cv, out _chamber);
            if (_chamber.GetComponent<PhaseChamberBindings>()?.closeButton != null)
                _chamber.GetComponent<PhaseChamberBindings>().closeButton.onClick.AddListener(TogglePanel);
            _chamber.Init();
        }

        // ---- Tutor tool entry points -----------------------------------------------

        public void ShowPhase(string substanceKey, float tempK, float pressureAtm)
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            _chamber?.SetSubstance(substanceKey);
            _chamber?.SetConditions(tempK, pressureAtm);
        }
    }
}
