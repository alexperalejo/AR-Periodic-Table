using UnityEngine;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Re-anchors a RectTransform every frame so its edges stay inside the device's
    /// safe area (Screen.safeArea). Critical on Android phones with cutouts/notches and
    /// gesture navigation bars, and required when the device rotates between portrait
    /// and landscape because the safe-area rect changes shape.
    ///
    /// Attach this component to the immediate child of a screen-space Canvas (NOT the
    /// canvas itself — the helper relies on a parent rect at fullscreen size to compute
    /// safe-area anchors). One typical setup:
    ///
    ///     RootCanvas (ScreenSpaceOverlay, ScaleWithScreenSize)
    ///       └ SafeAreaContainer  ← this component goes here
    ///           ├ ButtonA
    ///           ├ ButtonB
    ///           └ ...
    ///
    /// Or alternatively, attach it directly to each top-level UI element that needs to
    /// respect the safe area independently (e.g. PlaceButton, LockButton). Both modes
    /// are supported — the script just re-anchors `transform` to Screen.safeArea each
    /// time the safe-area or screen size changes.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class SafeAreaApplier : MonoBehaviour
    {
        [Tooltip("Verbose logging when the safe area changes (e.g. on orientation flip).")]
        [SerializeField] private bool verboseLogging = false;

        private RectTransform _rt;
        private Rect _lastSafeArea = Rect.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
        private Vector2Int _lastResolution = Vector2Int.zero;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            ApplySafeArea(force: true);
        }

        private void OnEnable()
        {
            ApplySafeArea(force: true);
        }

        private void Update()
        {
            // Cheap polling. Rotation/safe-area changes happen rarely so the cost is
            // a couple of struct comparisons per frame.
            var orient = Screen.orientation;
            var res = new Vector2Int(Screen.width, Screen.height);
            var safe = Screen.safeArea;

            if (safe != _lastSafeArea || orient != _lastOrientation || res != _lastResolution)
                ApplySafeArea(force: false);
        }

        private void ApplySafeArea(bool force)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null) return;

            Rect safe = Screen.safeArea;
            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0) return;

            // Convert Screen.safeArea (in pixels) into anchor coords on a full-screen rect.
            Vector2 anchorMin = new Vector2(safe.x / w, safe.y / h);
            Vector2 anchorMax = new Vector2((safe.x + safe.width)  / w,
                                            (safe.y + safe.height) / h);

            // Stretch this rect to its parent and let the safe-area shrink the anchors.
            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;

            _lastSafeArea = safe;
            _lastOrientation = Screen.orientation;
            _lastResolution = new Vector2Int(w, h);

            if (verboseLogging)
                Debug.Log($"[SafeAreaApplier] {name} layout refresh: orient={Screen.orientation} res={w}x{h} safe={safe} anchorMin={anchorMin} anchorMax={anchorMax}");
        }
    }
}
