using UnityEngine;

namespace PeriodicAR.Offline
{
    /// <summary>
    /// Exposes offline mode toggle in the wheel and manages the user-visible offline state.
    /// When offline mode is active (forced or detected), LLM calls in TutorController
    /// should fail gracefully — TutorController already checks AppStateProvider.offlineMode.
    /// </summary>
    public class OfflineModeController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<OfflineModeController>() != null) return;
            var go = new GameObject("[OfflineModeController]");
            DontDestroyOnLoad(go);
            go.AddComponent<OfflineModeController>();
        }

        public static OfflineModeController Instance { get; private set; }

        private bool _forcedOffline;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UI.AppShellController.Instance?.RegisterWheelItem(new UI.WheelItem
            {
                id          = "offline",
                label       = "Offline Mode",
                iconName    = "✈",
                page        = 2,
                onTap       = ToggleOffline,
                isAvailable = () => true,
            });
        }

        public void ToggleOffline()
        {
            _forcedOffline = !_forcedOffline;
            if (Tutor.AppStateProvider.Instance != null)
                Tutor.AppStateProvider.Instance.offlineMode = _forcedOffline;

            string msg = _forcedOffline
                ? "Offline mode ON — all features work, AI tutor disabled"
                : "Offline mode OFF — checking connection...";
            UI.AppShellController.Instance?.ShowStatus(msg, 4f);

            if (!_forcedOffline) NetworkSentinel.Instance?.CheckNow();
        }

        public bool IsOffline => _forcedOffline || (Tutor.AppStateProvider.Instance?.offlineMode ?? false);
    }
}
