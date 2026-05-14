using UnityEngine;

namespace PeriodicAR.AcidBase
{
    public class AcidsController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<AcidsController>() != null) return;
            var go = new GameObject("[AcidsController]");
            DontDestroyOnLoad(go);
            go.AddComponent<AcidsController>();
        }

        public static AcidsController Instance { get; private set; }

        private GameObject _panel;
        private Beaker     _beaker;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "acids",
                label    = "pH Lab",
                iconName = "⚗",
                page     = 1,
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
                UI.AppShellController.Instance?.ShowStatus("Add a substance to see its pH", 3f);
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.beakerState = "neutral";
            }
            else
            {
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.beakerState = null;
            }
        }

        private void BuildPanel()
        {
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;
            _panel = AcidsPrefabFactory.BuildPanel(cv, out _beaker);
            _beaker.Init();
        }

        // ---- Tutor tool entry points -----------------------------------------------

        public void AddToBeaker(string substanceName)
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            var list = AcidBaseCatalog.GetSubstances();
            var sub = list.Find(s => s.name.Contains(substanceName) || s.formula == substanceName);
            if (sub != null) _beaker?.SetSubstance(sub);
        }

        public void EmptyBeaker()
        {
            _beaker?.SetPh(7f);
            UI.AppShellController.Instance?.ShowStatus("Beaker reset to neutral water", 2f);
        }

        public void SetBeakerPh(float ph)
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            _beaker?.SetPh(ph);
        }
    }
}
