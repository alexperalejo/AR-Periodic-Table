using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PeriodicAR.History;
using PeriodicAR.Tutor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Orchestrates the object-detection feature. Self-bootstrapping singleton.
    /// Adds a "Scan" button to the HUD canvas at runtime.
    /// </summary>
    public class ScanController : MonoBehaviour
    {
        // ---- Bootstrap ----------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ScanController>() != null) return;
            var go = new GameObject("[ScanController]");
            DontDestroyOnLoad(go);

            var ctrl   = go.AddComponent<ScanController>();
            ctrl._grabber = go.AddComponent<ARCameraFrameGrabber>();
            ctrl._client  = go.AddComponent<DetectionServerClient>();
            ctrl._client.config.serverUrl = "https://tpu.gonzalezerik.com/detect";
        }

        // ---- Settings -------------------------------------------------------
        public bool topDetectionOnly      = true;
        public bool autoHighlight         = true;
        public float popupDistanceMeters  = 0.6f;

        // ---- Internal refs --------------------------------------------------
        private ARCameraFrameGrabber  _grabber;
        private DetectionServerClient _client;
        private Button                _scanButton;
        private TMP_Text              _statusLabel;
        private GameObject            _activePopup;

        public static ScanController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (UI.AppShellController.Instance != null)
            {
                UI.AppShellController.Instance.RegisterWheelItem(new UI.WheelItem
                {
                    id       = "scan",
                    label    = "Scan",
                    iconName = "⊙",
                    page     = 1,
                    onTap    = TriggerScan,
                });
            }
            else
            {
                CreateHudButton();
            }
        }

        public void TriggerScan() => StartCoroutine(RunScan());

        private void CreateHudButton()
        {
            var hudController = FindAnyObjectByType<UI.ARHudController>();
            Canvas hudCanvas = hudController != null ? hudController.GetComponentInParent<Canvas>() : null;
            if (hudCanvas == null) hudCanvas = FindFirstObjectByType<Canvas>();
            if (hudCanvas == null) return;

            // Create Scan button at position adjacent to the Place button (top-right).
            var btnGo = new GameObject("ScanButton", typeof(RectTransform));
            btnGo.transform.SetParent(hudCanvas.transform, false);

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-240f, -40f); // left of Place button
            rt.sizeDelta        = new Vector2(180f, 180f);

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.08f, 0.16f, 0.08f, 0.85f);

            _scanButton = btnGo.AddComponent<Button>();
            _scanButton.onClick.AddListener(OnScanPressed);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 30); lrt.offsetMax = new Vector2(-4, -4);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Scan";
            tmp.fontSize  = 40f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            // Status label: small text below the button row.
            var statusGo = new GameObject("ScanStatus", typeof(RectTransform));
            statusGo.transform.SetParent(hudCanvas.transform, false);
            var srt = statusGo.GetComponent<RectTransform>();
            srt.anchorMin        = new Vector2(0f, 1f);
            srt.anchorMax        = new Vector2(1f, 1f);
            srt.pivot            = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0f, -240f);
            srt.sizeDelta        = new Vector2(-80f, 60f);
            _statusLabel = statusGo.AddComponent<TextMeshProUGUI>();
            _statusLabel.text      = string.Empty;
            _statusLabel.fontSize  = 32f;
            _statusLabel.alignment = TextAlignmentOptions.Center;
            _statusLabel.color     = new Color(0.9f, 0.9f, 0.9f, 1f);
        }

        private void OnScanPressed()
        {
            StartCoroutine(RunScan());
        }

        private IEnumerator RunScan()
        {
            if (_grabber == null || _client == null) yield break;

            SetButtonEnabled(false);
            SetStatus("Capturing…");

            // 1. Grab camera frame.
            byte[] jpeg = null;
            string captureError = null;
            yield return _grabber.CaptureJpeg(b => jpeg = b, e => captureError = e);

            if (captureError != null)
            {
                SetStatus($"Capture error: {captureError}");
                SetButtonEnabled(true);
                yield break;
            }

            SetStatus($"Uploading {jpeg.Length / 1024} KB…");

            // 2. Post to detection server.
            DetectionResponse resp = null;
            string serverError = null;
            yield return _client.PostFrame(jpeg, r => resp = r, e => serverError = e);

            if (serverError != null)
            {
                SetStatus($"Server error: {serverError}");
                SetButtonEnabled(true);
                yield break;
            }

            // 3. Pick the best detection.
            if (resp.detections == null || resp.detections.Length == 0)
            {
                SetStatus("No objects detected.");
                SetButtonEnabled(true);
                yield break;
            }

            Detection det = topDetectionOnly
                ? resp.detections[0]
                : resp.detections.OrderByDescending(d => d.score).First();

            SetStatus($"Found: {det.label} ({det.score:P0}) · {resp.inference_ms:F0} ms");

            // 4. Look up composition.
            ObjectElementsLoader.TryLookup(det.label, out ObjectComposition comp);

            // 5. Compute world-space position from bbox centre.
            Vector3 popupPos = ComputePopupPosition(det, popupDistanceMeters);

            // 6. Destroy any previous popup.
            if (_activePopup != null) Destroy(_activePopup);

            // 7. Build and show popup.
            _activePopup = ObjectDetectionPrefabFactory.CreatePopup();
            var popup    = _activePopup.GetComponent<DetectionPopup>();
            popup.Show(det, comp, popupPos, Camera.main,
                onHighlightRequested: symbols =>
                {
                    var h = ElementSetHighlighter.Instance;
                    h?.HighlightElements(symbols);
                },
                onClosed: () =>
                {
                    _activePopup = null;
                });

            // 8. Auto-highlight primary elements.
            if (autoHighlight && comp != null)
            {
                var h = ElementSetHighlighter.Instance;
                var allSymbols = (comp.primary ?? System.Array.Empty<string>())
                    .Concat(comp.trace ?? System.Array.Empty<string>());
                h?.HighlightElements(allSymbols);
            }

            // 9. Log to scan history.
            var loggedSymbols = comp != null
                ? (comp.primary ?? System.Array.Empty<string>()).Concat(comp.trace ?? System.Array.Empty<string>())
                : Enumerable.Empty<string>();

            ScanHistory.Add(det.label, loggedSymbols);

            // 10. Update AppStateProvider.
            if (AppStateProvider.Instance != null)
            {
                AppStateProvider.Instance.lastScannedObject   = det.label;
                AppStateProvider.Instance.lastScannedElements = loggedSymbols.ToList();
            }

            SetButtonEnabled(true);
        }

        private static Vector3 ComputePopupPosition(Detection det, float distance)
        {
            if (Camera.main == null || det.bbox == null || det.bbox.Length < 4)
                return Camera.main != null
                    ? Camera.main.transform.position + Camera.main.transform.forward * distance
                    : Vector3.zero;

            float cx = (det.bbox[0] + det.bbox[2]) * 0.5f;          // 0..1
            float cy = 1f - (det.bbox[1] + det.bbox[3]) * 0.5f;     // flip Y (bbox origin top-left)
            var screenPt = new Vector3(cx * Screen.width, cy * Screen.height, 0f);
            Ray ray = Camera.main.ScreenPointToRay(screenPt);
            return ray.origin + ray.direction * distance;
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null) _statusLabel.text = msg;
            UI.AppShellController.Instance?.ShowStatus(msg, 4f);
            Debug.Log($"[ScanController] {msg}");
        }

        private void SetButtonEnabled(bool enabled)
        {
            if (_scanButton != null) _scanButton.interactable = enabled;
        }

        // ---- Voice input hook (not implemented) ---------------------------------
        // public void OnVoiceInputAvailable(string transcribedText) { }
    }
}
