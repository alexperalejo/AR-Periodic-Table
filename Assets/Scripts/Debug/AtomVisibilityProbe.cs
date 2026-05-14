using UnityEngine;

namespace PeriodicAR.Debugging
{
    /// <summary>
    /// Periodically logs the world position, scale, renderer state and bounds of the
    /// AtomSystem so we can tell whether the atom is invisible because it's
    /// (a) deactivated, (b) scaled to zero, (c) placed offscreen, or (d) actually
    /// rendering correctly. Attach to BohrModelRoot or AtomSystem.
    ///
    /// Logs are tagged "[VIS PROBE]" for easy logcat filtering.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class AtomVisibilityProbe : MonoBehaviour
    {
        [Tooltip("Seconds between status logs.")]
        public float intervalSeconds = 1.5f;

        [Tooltip("If true, also logs every change in activeInHierarchy.")]
        public bool logActiveStateChanges = true;

        [Tooltip("If true, also reports cumulative world bounds of every renderer in this hierarchy.")]
        public bool logRendererBounds = true;

        private float _nextLogTime;
        private bool _lastActive;

        private void OnEnable()
        {
            _nextLogTime = 0f; // log immediately
            _lastActive = gameObject.activeInHierarchy;
        }

        private void Update()
        {
            bool nowActive = gameObject.activeInHierarchy;
            if (logActiveStateChanges && nowActive != _lastActive)
            {
                Debug.Log($"[VIS PROBE] {name} activeInHierarchy changed: {_lastActive} -> {nowActive}");
                _lastActive = nowActive;
                _nextLogTime = 0f;
            }

            if (Time.unscaledTime < _nextLogTime) return;
            _nextLogTime = Time.unscaledTime + intervalSeconds;

            Vector3 worldPos = transform.position;
            Vector3 lossyScale = transform.lossyScale;

            // Renderer summary
            int rendererCount = 0;
            int enabledRenderers = 0;
            Bounds combined = new Bounds(worldPos, Vector3.zero);
            bool boundsInitialized = false;

            if (logRendererBounds)
            {
                var rends = GetComponentsInChildren<Renderer>(true);
                rendererCount = rends.Length;
                foreach (var r in rends)
                {
                    if (r.enabled && r.gameObject.activeInHierarchy)
                    {
                        enabledRenderers++;
                        if (!boundsInitialized) { combined = r.bounds; boundsInitialized = true; }
                        else combined.Encapsulate(r.bounds);
                    }
                }
            }

            Camera cam = Camera.main;
            string camInfo = cam != null
                ? $"camPos={cam.transform.position}, camFwd={cam.transform.forward}"
                : "camera=null";

            // Compute distance from camera to atom and screen-space mapping
            string screenInfo = "(no cam)";
            if (cam != null)
            {
                float dist = Vector3.Distance(cam.transform.position, worldPos);
                Vector3 sp = cam.WorldToScreenPoint(worldPos);
                bool inFrontOfCamera = sp.z > 0;
                bool insideViewport = sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
                screenInfo = $"distFromCam={dist:F2}m screenPos=({sp.x:F0},{sp.y:F0}) z={sp.z:F2} " +
                             $"inFront={inFrontOfCamera} onScreen={insideViewport}";
            }

            Debug.Log(
                $"[VIS PROBE] {name} active={gameObject.activeInHierarchy} " +
                $"worldPos={worldPos} lossyScale={lossyScale.x:F4} " +
                $"renderers={enabledRenderers}/{rendererCount} " +
                $"bounds={(boundsInitialized ? combined.ToString() : "<none>")} " +
                $"{camInfo} {screenInfo}");
        }
    }
}
