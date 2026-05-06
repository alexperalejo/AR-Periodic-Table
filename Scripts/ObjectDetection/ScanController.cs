// Assets/Scripts/ObjectDetection/ScanController.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Drop this on a GameObject in your AR scene and wire the Scan button to
    /// OnScanButtonClicked. It will:
    ///
    ///   1. Capture a frame from the AR camera
    ///   2. Upload it to the Coral detection server
    ///   3. Spawn a world-space popup near the highest-confidence detection
    ///   4. On the popup's "Highlight on Table" button, push the element list
    ///      into ElementSetHighlighter (which must be attached to the spawned
    ///      pTableGroup — TapToPlace adds it automatically once you also wire
    ///      it through TapToPlace as you do for ElementCategoryHighlighter).
    ///
    /// Auto-discovers ARCameraManager and ElementSetHighlighter so it works
    /// even with no Inspector wiring beyond the serverUrl.
    /// </summary>
    public class ScanController : MonoBehaviour
    {
        [Header("Server")]
        [Tooltip("Full URL to your detection endpoint.")]
        [SerializeField] private string serverUrl = "https://tpu.gonzalezerik.com/detect";
        [Tooltip("Cloudflare Access service-token Client ID (optional). Leave blank if you don't gate the tunnel with Access.")]
        [SerializeField] private string cfAccessClientId = "";
        [Tooltip("Cloudflare Access service-token Client Secret (optional).")]
        [SerializeField] private string cfAccessClientSecret = "";
        [SerializeField, Range(0.05f, 0.95f)] private float scoreThreshold = 0.4f;
        [SerializeField, Range(1, 10)] private int maxResults = 5;

        [Header("UI")]
        [Tooltip("Optional: the Scan button. If unset, you can call OnScanButtonClicked yourself.")]
        [SerializeField] private Button scanButton;
        [Tooltip("Optional: a status label that shows progress / errors.")]
        [SerializeField] private TMP_Text statusLabel;
        [Tooltip("Optional: a hand-authored DetectionPopup prefab. If null, one is built procedurally.")]
        [SerializeField] private DetectionPopup popupPrefab;

        [Header("References (auto-discovered if empty)")]
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Camera arCamera;
        [SerializeField] private ElementSetHighlighter elementSetHighlighter;

        [Header("Behavior")]
        [Tooltip("Only spawn a popup for the single highest-confidence detection.")]
        [SerializeField] private bool topDetectionOnly = true;
        [Tooltip("Automatically push the detected elements into the table highlighter (no need to tap 'Highlight').")]
        [SerializeField] private bool autoHighlight = true;

        private ObjectElementsCatalog _catalog;
        private DetectionPopup _activePopup;
        private bool _scanInFlight;

        private void OnEnable()
        {
            if (cameraManager == null) cameraManager = FindAnyObjectByType<ARCameraManager>();
            if (arCamera == null && cameraManager != null) arCamera = cameraManager.GetComponent<Camera>();
            if (arCamera == null) arCamera = Camera.main;

            // ElementSetHighlighter lives on the spawned pTableGroup, which only
            // exists after the user places the table. We re-resolve on demand.
            TryResolveSetHighlighter();

            _catalog = ObjectElementsLoader.Load();

            if (scanButton != null) scanButton.onClick.AddListener(OnScanButtonClicked);
            SetStatus("Ready");
        }

        private void OnDisable()
        {
            if (scanButton != null) scanButton.onClick.RemoveListener(OnScanButtonClicked);
        }

        private void TryResolveSetHighlighter()
        {
            if (elementSetHighlighter != null) return;

            // First try a scene-wide search — handles the case where the user
            // hand-attached the component or it's already on the spawned table
            // from a previous scan.
            elementSetHighlighter = FindAnyObjectByType<ElementSetHighlighter>();
            if (elementSetHighlighter != null) return;

            // Fallback: mirror the lazy pattern used by CategoryFolderButton —
            // attach to the same root that already hosts ElementCategoryHighlighter,
            // since both target the same pTableGroup children.
            var existingCategory = FindAnyObjectByType<PeriodicAR.UI.ElementCategoryHighlighter>();
            if (existingCategory != null)
            {
                elementSetHighlighter = existingCategory.GetComponent<ElementSetHighlighter>()
                    ?? existingCategory.gameObject.AddComponent<ElementSetHighlighter>();
            }
        }

        public void OnScanButtonClicked()
        {
            if (_scanInFlight)
            {
                Debug.Log("[ScanController] Scan already in progress, ignoring tap.");
                return;
            }
            StartCoroutine(RunScan());
        }

        private IEnumerator RunScan()
        {
            _scanInFlight = true;
            if (scanButton != null) scanButton.interactable = false;
            SetStatus("Capturing…");

            // ---- 1. Grab a camera frame ------------------------------------------
            ARCameraFrameGrabber.GrabResult grab = ARCameraFrameGrabber.TryGrabJpeg(cameraManager);
            if (!grab.success)
            {
                Finish($"Capture failed: {grab.error}");
                yield break;
            }
            SetStatus($"Uploading {grab.jpegBytes.Length / 1024} KB…");

            // ---- 2. Send to server -----------------------------------------------
            var client = new DetectionServerClient
            {
                ServerUrl            = serverUrl,
                MaxResults           = maxResults,
                ScoreThreshold       = scoreThreshold,
                CfAccessClientId     = cfAccessClientId,
                CfAccessClientSecret = cfAccessClientSecret,
            };

            DetectionServerClient.Result serverResult = default;
            yield return client.Send(grab.jpegBytes, r => serverResult = r);

            if (!serverResult.success)
            {
                Finish($"Server error: {serverResult.error}");
                yield break;
            }

            DetectionResponse resp = serverResult.response;
            if (resp.detections == null || resp.detections.Length == 0)
            {
                Finish("Nothing recognized. Try moving closer or angling differently.");
                yield break;
            }

            // ---- 3. Pick detection(s) and spawn popups ---------------------------
            int spawnCount = topDetectionOnly ? 1 : resp.detections.Length;

            // Dispose any previous popup before spawning a new one.
            if (_activePopup != null) Destroy(_activePopup.gameObject);

            for (int i = 0; i < spawnCount && i < resp.detections.Length; i++)
            {
                var det = resp.detections[i];
                _catalog.TryGet(det.label, out var comp); // comp may be null

                DetectionPopup popup = popupPrefab != null
                    ? Instantiate(popupPrefab)
                    : ObjectDetectionPrefabFactory.CreatePopup();

                System.Action highlightAction = () => HighlightForComposition(comp);
                System.Action closeAction     = () => { _activePopup = null; };

                popup.Configure(arCamera, det, comp, highlightAction, closeAction);
                _activePopup = popup;

                if (autoHighlight) HighlightForComposition(comp);
            }

            Finish($"Found: {resp.detections[0].label} ({resp.detections[0].score * 100f:F0}%) · {resp.inference_ms:F0} ms");
        }

        private void HighlightForComposition(ObjectComposition comp)
        {
            if (comp == null) return;
            TryResolveSetHighlighter();
            if (elementSetHighlighter == null)
            {
                Debug.LogWarning("[ScanController] No ElementSetHighlighter in scene — place the table first.");
                return;
            }

            var symbols = new HashSet<string>();
            if (comp.primary != null) foreach (var s in comp.primary) symbols.Add(s);
            if (comp.trace   != null) foreach (var s in comp.trace)   symbols.Add(s);

            elementSetHighlighter.Highlight(symbols);
        }

        private void Finish(string status)
        {
            SetStatus(status);
            _scanInFlight = false;
            if (scanButton != null) scanButton.interactable = true;
        }

        private void SetStatus(string s)
        {
            if (statusLabel != null) statusLabel.text = s;
            Debug.Log($"[ScanController] {s}");
        }
    }
}
