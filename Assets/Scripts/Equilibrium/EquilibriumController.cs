using UnityEngine;

namespace PeriodicAR.Equilibrium
{
    public class EquilibriumController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<EquilibriumController>() != null) return;
            var go = new GameObject("[EquilibriumController]");
            DontDestroyOnLoad(go);
            go.AddComponent<EquilibriumController>();
        }

        public static EquilibriumController Instance { get; private set; }

        private ReactionChamber _chamber;
        private bool _open;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id       = "equilibrium",
                label    = "Equilibrium",
                iconName = "⇌",
                page     = 2,
                onTap    = ToggleChamber,
            });
        }

        public void ToggleChamber()
        {
            _open = !_open;
            if (_open)
            {
                EnsureBuilt();
                _chamber.gameObject.SetActive(true);
                UI.AppShellController.Instance?.ShowStatus("Apply a perturbation to shift equilibrium", 4f);
            }
            else
            {
                if (_chamber != null) _chamber.gameObject.SetActive(false);
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.activeEquilibriumReaction = null;
            }
        }

        private void EnsureBuilt()
        {
            if (_chamber != null) return;
            var cv = FindFirstObjectByType<Canvas>();
            if (cv == null) return;

            var go = new GameObject("[ReactionChamber]");
            DontDestroyOnLoad(go);
            _chamber = go.AddComponent<ReactionChamber>();
            _chamber.Build(cv);
            _chamber.OnCloseRequested = ToggleChamber;
        }

        // ---- Tutor tool entry points -----------------------------------------------

        public void ShowReaction(string reactionId)
        {
            EnsureBuilt();
            _open = true;
            _chamber.gameObject.SetActive(true);
            // The chamber's own dropdown handles selection; we just open it here
            // Deeper wiring (pre-selecting by id) would require a public method on chamber
            UI.AppShellController.Instance?.ShowStatus($"Showing equilibrium: {reactionId}", 3f);
        }
    }
}
