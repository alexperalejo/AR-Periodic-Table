// Assets/Scripts/ObjectDetection/DetectionPopup.cs
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Floating popup placed in front of the camera at the screen position of
    /// a detected object's bounding-box center. Uses a world-space canvas so
    /// the popup keeps a consistent on-screen size regardless of distance,
    /// while still respecting AR depth (it can be occluded by the table, etc).
    ///
    /// Designed to be created by ScanController.SpawnPopup — you do NOT need
    /// to wire this up in the Inspector; the popup prefab is auto-built from
    /// code in ObjectDetectionPrefabFactory if no prefab is supplied.
    /// </summary>
    public class DetectionPopup : MonoBehaviour
    {
        // Public so ObjectDetectionPrefabFactory / DetectionPopupBindings can wire
        // these at runtime without needing reflection or SerializedObject. If you
        // build a hand-authored prefab you can drag references in via the
        // Inspector — Unity will serialize public fields just fine.
        public TMP_Text titleLabel;
        public TMP_Text bodyLabel;
        public Button   closeButton;
        public Button   highlightButton;

        // How far in front of the camera to drop the popup, in meters. Tuned to
        // roughly arm's length so it sits in a comfortable reading position.
        [SerializeField] private float spawnDistance = 0.6f;

        // Lifetime in seconds before auto-dismiss. 0 = never auto-dismiss.
        [SerializeField] private float autoDismissAfter = 0f;

        private Camera _arCamera;
        private float  _spawnTime;
        private System.Action _onHighlight;

        public void Configure(Camera arCamera, Detection detection, ObjectComposition comp,
                              System.Action onHighlight, System.Action onClose)
        {
            _arCamera     = arCamera;
            _spawnTime    = Time.time;
            _onHighlight  = onHighlight;

            if (titleLabel != null)
                titleLabel.text = $"{Capitalize(detection.label)}  ({detection.score * 100f:F0}%)";

            if (bodyLabel != null)
                bodyLabel.text = BuildBody(comp);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => { onClose?.Invoke(); Destroy(gameObject); });
            }

            if (highlightButton != null)
            {
                bool hasElements = comp != null
                    && ((comp.primary != null && comp.primary.Length > 0)
                     || (comp.trace   != null && comp.trace.Length   > 0));
                highlightButton.gameObject.SetActive(hasElements);
                highlightButton.onClick.RemoveAllListeners();
                highlightButton.onClick.AddListener(() => _onHighlight?.Invoke());
            }

            PositionInFrontOfCamera(detection);
        }

        private void Update()
        {
            // Always face the camera so text stays readable as the user moves.
            if (_arCamera != null)
            {
                Vector3 toCam = transform.position - _arCamera.transform.position;
                if (toCam.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }

            if (autoDismissAfter > 0f && Time.time - _spawnTime > autoDismissAfter)
            {
                Destroy(gameObject);
            }
        }

        private void PositionInFrontOfCamera(Detection detection)
        {
            if (_arCamera == null) return;

            // Convert the normalized bbox center (origin top-left) into a Unity
            // viewport coordinate (origin bottom-left) and project that point
            // out to spawnDistance along the camera's forward axis.
            float vpX = Mathf.Clamp01(detection.CenterX);
            float vpY = Mathf.Clamp01(1f - detection.CenterY);

            Vector3 worldPos = _arCamera.ViewportToWorldPoint(
                new Vector3(vpX, vpY, spawnDistance));

            transform.position = worldPos;
            // Pre-orient so the first frame doesn't show the popup edge-on.
            transform.rotation = Quaternion.LookRotation(
                (worldPos - _arCamera.transform.position).normalized, Vector3.up);
        }

        private static string BuildBody(ObjectComposition comp)
        {
            if (comp == null) return "<i>No element data available.</i>";

            var sb = new StringBuilder(256);

            if (comp.primary != null && comp.primary.Length > 0)
            {
                sb.Append("<b>Primary:</b>  ");
                sb.Append(string.Join("  ", comp.primary));
                sb.Append('\n');
            }
            if (comp.trace != null && comp.trace.Length > 0)
            {
                sb.Append("<b>Trace:</b>  ");
                sb.Append(string.Join("  ", comp.trace));
                sb.Append('\n');
            }
            if (!string.IsNullOrEmpty(comp.note))
            {
                sb.Append('\n');
                sb.Append("<size=80%>");
                sb.Append(comp.note);
                sb.Append("</size>");
            }
            return sb.ToString();
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
