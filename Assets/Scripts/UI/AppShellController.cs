using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Central singleton for the v3 UI shell.
    /// Owns the action wheel, status pill, and Tutor button.
    /// All v1/v2/v3 feature controllers call RegisterWheelItem() in Start().
    /// Self-bootstrapping via AfterSceneLoad; Instance is set in Awake() so it's
    /// available to all other controllers' Start() methods.
    /// </summary>
    public class AppShellController : MonoBehaviour
    {
        // ---- Bootstrap ----------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<AppShellController>() != null) return;
            var go = new GameObject("[AppShellController]");
            DontDestroyOnLoad(go);
            go.AddComponent<AppShellController>();
        }

        public static AppShellController Instance { get; private set; }

        // ---- State --------------------------------------------------------------

        private ActionWheel  _wheel;
        private StatusPill   _pill;
        private Button       _tutorButton;
        private Image        _tutorBadge;

        private readonly List<WheelItem> _registeredItems = new();
        private string _defaultPinnedId = "scan";

        // ---- Lifecycle ----------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var shellGo = AppShellPrefabFactory.CreateShell(
                out _wheel, out _pill, out _tutorButton, out _tutorBadge);
            DontDestroyOnLoad(shellGo);

            _tutorButton.onClick.AddListener(OnTutorButtonPressed);

            // Register built-in items (Place).
            foreach (var item in WheelRegistry.GetBuiltinItems())
                RegisterWheelItem(item);
        }

        // ---- Shell API ----------------------------------------------------------

        public void ShowStatus(string text, float autoHideSec = 3f)
            => _pill?.Show(text, autoHideSec);

        public void ClearStatus() => _pill?.Clear();

        public void OpenWheel()  => _wheel?.Expand();
        public void CloseWheel() => _wheel?.Collapse();

        public void PinAsShortcut(string wheelItemId)
        {
            _defaultPinnedId = wheelItemId;
            _wheel?.SetPinnedId(wheelItemId);
        }

        public void SetTutorBadge(bool show)
        {
            if (_tutorBadge != null) _tutorBadge.gameObject.SetActive(show);
        }

        // ---- Wheel registration -------------------------------------------------

        public void RegisterWheelItem(WheelItem item)
        {
            _registeredItems.RemoveAll(x => x.id == item.id);
            _registeredItems.Add(item);
            if (_wheel != null) FlushItemsToWheel();
        }

        private void FlushItemsToWheel()
        {
            // Sort by page then order of registration.
            _registeredItems.Sort((a, b) =>
            {
                int cmp = a.page.CompareTo(b.page);
                return cmp != 0 ? cmp : _registeredItems.IndexOf(a).CompareTo(_registeredItems.IndexOf(b));
            });
            _wheel?.SetItems(new List<WheelItem>(_registeredItems));
            _wheel?.SetPinnedId(_defaultPinnedId);
        }

        // ---- Tutor button -------------------------------------------------------

        private void OnTutorButtonPressed()
        {
            var tutorCtrl = FindAnyObjectByType<Tutor.TutorController>();
            tutorCtrl?.TogglePanel();
        }
    }
}
