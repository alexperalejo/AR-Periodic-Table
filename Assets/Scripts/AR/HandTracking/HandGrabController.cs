using UnityEngine;
using UnityEngine.XR.ARFoundation;
using RTHand;
using PeriodicAR.AR;

namespace PeriodicAR.AR.HandTracking
{
    // Pinch-to-toggle hand grab.
    //
    // Old behaviour: the user had to keep pinching their fingers together to
    // hold the cube — releasing the pinch dropped it. With unstable hand
    // tracking that meant the cube fell off constantly.
    //
    // New behaviour:
    //   • First pinch (when fingertip is near a cube) → grab. The cube locks
    //     onto the open palm (middle-finger MCP joint) and follows it.
    //     The user can fully un-pinch and the cube stays put.
    //   • A subsequent pinch (after a short cooldown) → release. The cube
    //     falls and respawns via CubeHandGrabbable's existing logic.
    //
    // While held, the cube's position is driven from the palm center every
    // frame — that's both more stable than the index fingertip *and* keeps
    // the cube visually in the middle of the hand.
    public class HandGrabController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RealtimeHandManager handManager;
        [SerializeField] private HandCapabilityProbe capabilityProbe;
        [SerializeField] private TapToPlace tapToPlace;

        [Header("Thresholds")]
        [SerializeField] private float pinchDownDistance = 0.025f;
        [SerializeField] private float pinchReleaseDistance = 0.04f;
        [SerializeField] private float grabReachThreshold = 0.05f;
        [SerializeField] private float minConfidence = 0.5f;
        [Tooltip("Seconds after a grab during which a new pinch will NOT release the cube. Prevents accidental drop from a single jittery pinch reading.")]
        [SerializeField] private float releasePinchCooldown = 0.6f;

        private bool _systemEnabled;
        private bool _pinching;          // hysteresis state of the pinch gesture
        private bool _pinchEdgeArmed;    // becomes true on pinch release; next close = edge
        private CubeHandGrabbable _activeCube;
        private float _grabbedAtTime = -999f;

        private void Start()
        {
            if (handManager != null) handManager.enabled = false;
            if (capabilityProbe != null)
            {
                capabilityProbe.ProbeCompleted += OnProbeCompleted;
                if (capabilityProbe.Status != HandCapabilityProbe.Result.Unknown)
                    OnProbeCompleted(capabilityProbe.Status);
            }
        }

        private void OnDestroy()
        {
            if (capabilityProbe != null) capabilityProbe.ProbeCompleted -= OnProbeCompleted;
            if (handManager != null) handManager.HandUpdated -= OnHandUpdated;
        }

        private void OnProbeCompleted(HandCapabilityProbe.Result r)
        {
            _systemEnabled = r == HandCapabilityProbe.Result.Supported;
            if (!_systemEnabled)
            {
                Debug.Log("[HandGrabController] Device does not support LiDAR hand tracking; disabled.");
                return;
            }
            if (handManager != null)
            {
                handManager.enabled = true;
                handManager.HandUpdated += OnHandUpdated;
                Debug.Log("[HandGrabController] Hand tracking enabled.");
            }
        }

        private void OnHandUpdated(RealtimeHand hand)
        {
            if (!_systemEnabled) return;

            // Hand lost from tracking: keep the held cube in place (don't drop it),
            // skip pinch logic until the hand comes back.
            if (!hand.IsVisible) return;

            var indexTip  = hand.Joints[JointName.indexTip];
            var thumbTip  = hand.Joints[JointName.thumbTip];

            bool pinchInputValid =
                indexTip.isVisible && thumbTip.isVisible &&
                indexTip.confidence >= minConfidence && thumbTip.confidence >= minConfidence;

            // Update pinch state with hysteresis (same thresholds as before).
            if (pinchInputValid)
            {
                float pinchDist = Vector3.Distance(indexTip.worldPos, thumbTip.worldPos);
                if (!_pinching && pinchDist < pinchDownDistance)        _pinching = true;
                else if (_pinching && pinchDist > pinchReleaseDistance) _pinching = false;
            }

            // Edge detection: a "pinch event" happens on the rising edge of
            // _pinching (open → closed). _pinchEdgeArmed makes sure we only
            // fire once per close, even if hysteresis flickers.
            bool pinchEvent = false;
            if (_pinching)
            {
                if (_pinchEdgeArmed) { pinchEvent = true; _pinchEdgeArmed = false; }
            }
            else
            {
                _pinchEdgeArmed = true;
            }

            // Grab / release on pinch event.
            if (pinchEvent)
            {
                if (_activeCube == null && pinchInputValid)
                {
                    TryGrabNearestCube(indexTip.worldPos);
                    _grabbedAtTime = Time.unscaledTime;
                }
                else if (_activeCube != null &&
                         (Time.unscaledTime - _grabbedAtTime) >= releasePinchCooldown)
                {
                    _activeCube.ReleaseHandGrab();
                    _activeCube = null;
                }
            }

            // While we have a held cube, drive its world position from the
            // palm center so it sits in the middle of the open hand and
            // doesn't depend on continued pinching.
            if (_activeCube != null)
            {
                Vector3 palm = ResolvePalmCenter(hand);
                _activeCube.UpdateHandGrab(palm);
            }
        }

        private Vector3 ResolvePalmCenter(RealtimeHand hand)
        {
            // middleMCP is the knuckle at the base of the middle finger — a
            // good proxy for "center of the palm" when the hand is open.
            // Fall back to the average of all four MCP knuckles, then wrist,
            // then the active cube's current position.
            var mid = hand.Joints[JointName.middleMCP];
            if (mid.isVisible && mid.confidence >= minConfidence) return mid.worldPos;

            int n = 0; Vector3 sum = Vector3.zero;
            void Tally(RTHand.Joint j) { if (j.isVisible && j.confidence >= minConfidence) { sum += j.worldPos; n++; } }
            Tally(hand.Joints[JointName.indexMCP]);
            Tally(hand.Joints[JointName.middleMCP]);
            Tally(hand.Joints[JointName.ringMCP]);
            Tally(hand.Joints[JointName.littleMCP]);
            if (n > 0) return sum / n;

            var wrist = hand.Joints[JointName.wrist];
            if (wrist.isVisible && wrist.confidence >= minConfidence) return wrist.worldPos;

            return _activeCube != null ? _activeCube.transform.position : Vector3.zero;
        }

        private void TryGrabNearestCube(Vector3 fingertipWorld)
        {
            var allCubes = FindObjectsByType<CubeHandGrabbable>(FindObjectsSortMode.None);
            CubeHandGrabbable best = null;
            float bestDist = grabReachThreshold;
            foreach (var cube in allCubes)
            {
                if (!cube.IsAvailableForHandGrab) continue;
                float d = Vector3.Distance(cube.transform.position, fingertipWorld);
                if (d < bestDist) { bestDist = d; best = cube; }
            }
            if (best != null)
            {
                best.BeginHandGrab();
                _activeCube = best;
                Debug.Log($"[HandGrabController] Grabbed {best.name} at distance {bestDist:F3}m — locked to palm. Pinch again to drop.");
            }
            else
            {
                Debug.Log($"[HandGrabController] Pinch detected but no cube within {grabReachThreshold:F3}m of fingertip.");
            }
        }
    }
}
